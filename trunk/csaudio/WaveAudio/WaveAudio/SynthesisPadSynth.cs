//Ben Fisher, 2008
//halfhourhacks.blogspot.com
//GPL

// PadSynth algorithm by by Nasca O. Paul.
// References: http://en.wikibooks.org/wiki/PADsynth_synthesis_algorithm
using System;

namespace CsWaveAudio
{
    namespace SynthesisBaseClasses
    {
        /// <summary>
        /// Instruments provide an array in frequency domain, which is then later turned into samples using IFFT.
        /// Uses random phases.
        /// See FourierSynthesisReferenceImplementation for a simple implementation
        /// </summary>
        public abstract class FourierSynthesisBase : SynthesisBase
        {
            protected const int N = 65536 * 4; // enough for about 5 secs
            protected readonly double amplitude;
            protected abstract double[] BuildArrayInFrequency();
            public FourierSynthesisBase(double amplitude)
            {
                this.amplitude = amplitude;
            }
            protected override double[] generate(int nSamples)
            {
                // this array contains amplitudes, indices correspond to frequencies
                double[] arrayInFrequency = this.BuildArrayInFrequency();

                // create real and img. parts, using random phases
                Random rand = new Random();
                double[] imgData = new Double[arrayInFrequency.Length];
                double[] realData = new Double[arrayInFrequency.Length];
                for (int i = 0; i < arrayInFrequency.Length; i++)
                {
                    double phase = rand.NextDouble() * 2.0 * Math.PI;
                    realData[i] = arrayInFrequency[i] * Math.Cos(phase);
                    imgData[i] = arrayInFrequency[i] * Math.Sin(phase);
                }

                // perform IFFT
                double[] outSamples;
                Fourier.RawFrequencyToSamples(out outSamples, realData, imgData);

                //repeat the samples as many times as necessary to fill output buffer
                int nSize = outSamples.Length;
                double[] output = new double[nSamples];
                double pieces = nSamples / (double)nSize; //how many periods to add to the output
                for (int nPiece = 0; nPiece < (int)pieces; nPiece++)
                {
                    Array.Copy(outSamples, 0, output, nPiece * nSize, nSize);
                }
                // add the remainder to the output (not a full period)
                Array.Copy(outSamples, 0, output, (int)pieces * nSize, nSamples - (int)pieces * nSize);
                return output;
            }
        }

        /// <summary>
        /// Instruments provide a list of harmonics and weights.
        /// This "smooths" out the frequencies using a Gaussian distribution.
        /// The width is greater for higher harmonics, which happens to sound good.
        /// An improvement would be to decrease the amount of upper harmonics when playing lower frequencies (downscaling the array)
        /// </summary>
        public abstract class PadSynthesisBase : FourierSynthesisBase
        {
            protected readonly double frequency;

            public PadSynthesisBase(double frequency, double amplitude)
                : base(amplitude)
            {
                this.frequency = frequency;
            }

            // Get the frequency of the nth harmonic. Can be overridden to create metallic effects.
            protected virtual double getHarmonicFrequency(int nHarmonic)
            {
                return nHarmonic * this.frequency;
            }
            // Get the bandwidth of the nth harmonic (the width of the distribution).
            protected virtual double getBandwidthInHertz(int nHarmonic)
            {
                const double baseBandwidth = 60.0;
                return (Math.Pow(2.0, baseBandwidth / 1200.0) - 1.0) * getHarmonicFrequency(nHarmonic);
            }

            // This is the profile of one harmonic. In this case is a Gaussian distribution, e^(-x^2) 
            private double profile(double fi, double bandwidth)
            {
                double x = fi / bandwidth; // The amplitude is divided by the bandwidth to ensure that the harmonic keeps the same amplitude regardless of the bandwidth
                x *= x;
                return (x > 14.7128) ? 0.0 : Math.Exp(-x) / bandwidth; //avoid computing e^(-x^2) where results are very close to zero
            }

            // Create array in frequency. We smooth out peaks with Gaussian dist.
            protected override double[] BuildArrayInFrequency()
            {
                double[] A = GetHarmonicWeights();

                //only the first half of this is filled, because the second half is negative frequencies that are only important for a complex signal
                double[] freq_amp = new double[N]; 

                //for each harmonic, add to the array.
                for (int nHarmonic = 1; nHarmonic < A.Length; nHarmonic++)
                {
                    //bandwidth of the current harmonic measured in Hz
                    //note that this is wider for each harmonic
                    double bw_Hz = getBandwidthInHertz(nHarmonic);

                    double bwi = bw_Hz / (2.0 * SampleRate);
                    double fi = getHarmonicFrequency(nHarmonic) / (double)SampleRate;
                    for (int i = 0; i < N/2; i++)
                    {
                        double hprofile = profile((i / (double)N) - fi, bwi);
                        freq_amp[i] += hprofile * A[nHarmonic];
                    }
                }

                // normalize the results?

                return freq_amp;
            }
            // This should return a 1-based array of weights, for each harmonic. (Index 1 is the 1st harmonic, 2 is 2nd, and so on)
            protected abstract double[] GetHarmonicWeights();
        }
    }

    /// <summary>
    /// Reference of PadSynthesis
    /// </summary>
    public class PadSynthesis : SynthesisBaseClasses.PadSynthesisBase
    {
        public PadSynthesis(double frequency, double amplitude)
            : base(frequency, amplitude)
        {
        }
        protected override double[] GetHarmonicWeights()
        {
            const int nHarmonics = 64;
            double[] A = new double[nHarmonics]; //note that A[0] is unused
            for (int i = 1; i < nHarmonics; i++)
            {
                A[i] = (1.0 / (double)i);
            }
            return A;
        }
    }

    /// <summary>
    /// Uses formants to approximate singing, although if the formants are scaled it can sound like strings.
    /// </summary>
    public class PadSynthesisChoir : SynthesisBaseClasses.PadSynthesisBase
    {
        protected double fScaleFormants;
        public PadSynthesisChoir(double frequency, double amplitude, double fScaleFormants)
            : base(frequency, amplitude)
        {
            this.fScaleFormants = fScaleFormants;
        }
        protected override double[] GetHarmonicWeights()
        {
            const int nHarmonics = 64;

            double[] A = new double[nHarmonics]; //note that A[0] is unused
            double f1 = this.frequency * this.fScaleFormants; // 1/16 ~ strings

            double[,] formantParameters = new double[,] { 
                // freq, width, height
                { 600, 1/150.0, 1.0},
                { 900, 1/250.0, 1.0}, 
                { 2200, 1/200.0, 1.0 }, 
                { 2600, 1/250.0, 1.0 },
                { 0, 1/3000.0, 0.1 }
            };
            for (int i = 1; i < nHarmonics; i++)
            {
                double weight = 0.0;

                for (int j = 0; j < formantParameters.GetLength(0); j++)
                    weight += Math.Exp(-Math.Pow((i * f1 - formantParameters[j, 0]) * formantParameters[j, 1], 2.0)) * formantParameters[j, 2];

                A[i] = (1.0 / (double)i) * weight;
            }
            return A;
        }
    }

    /// <summary>
    /// Overrides some methods in PadSynthesis
    /// </summary>
    public class PadSynthesisExtended : SynthesisBaseClasses.PadSynthesisBase
    {
        protected double fScaleFormants;
        public PadSynthesisExtended(double frequency, double amplitude, double fScaleFormants)
            : base(frequency, amplitude)
        {
            this.fScaleFormants = fScaleFormants;
        }
        protected override double getHarmonicFrequency(int nHarmonic)
        {
            return this.frequency * nHarmonic;// *(nHarmonic * 0.02 + 1.0); //Gives a metallic non-organic quality
        }
        protected override double getBandwidthInHertz(int nHarmonic)
        {
            const double bandwidth = 60.0;
            const double bandwidthScale = 20.0;
            return this.frequency * (Math.Pow(2.0, bandwidth / 1200.0) - 1.0) * Math.Pow(getHarmonicFrequency(nHarmonic)/frequency, bandwidthScale);
        }

        protected override double[] GetHarmonicWeights()
        {
            const int nHarmonics = 64;

            double[] A = new double[nHarmonics]; //note that A[0] is unused
            double f1 = this.frequency * this.fScaleFormants; // 1/16 ~ strings

            double[,] formantParameters = new double[,] { 
                // freq, width, height
                { 600, 1/150.0, 1.0},
                { 900, 1/250.0, 1.0}, 
                { 2200, 1/200.0, 1.0 }, 
                { 2600, 1/250.0, 1.0 },
                { 0, 1/3000.0, 0.1 }
            };
            for (int i = 1; i < nHarmonics; i++)
            {
                double weight = 0.0;

                for (int j = 0; j < formantParameters.GetLength(0); j++)
                    weight += Math.Exp(-Math.Pow((i * f1 - formantParameters[j, 0]) * formantParameters[j, 1], 2.0)) * formantParameters[j, 2];

                A[i] = (1.0 / (double)i) * weight;
            }
            return A;
        }
    }






    internal class FourierSynthesisReferenceImplementation : SynthesisBaseClasses.FourierSynthesisBase
    {
        private double[] freqs;
        public FourierSynthesisReferenceImplementation(double amplitude) : base(amplitude)
        {
            this.freqs = new double[] { 440.0, 500.0 };
            // we'll create a tone with these frequencies
        }
        protected override double[] BuildArrayInFrequency()
        {
            double[] amplitudes = new double[N]; // the second half of this is negative frequency which we aren't concerned with
            
            double dScaledown = (N / 2) / (SampleRate / 2.0); // map 0-22050.0 to 0-512
            for (int i = 0; i < freqs.Length; i++)
            {
                int index = (int)(freqs[i] * dScaledown);
                amplitudes[index] = 4000;
            }
            return amplitudes;
        }
    }
}