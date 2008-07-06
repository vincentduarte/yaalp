//Ben Fisher, 2008
//halfhourhacks.blogspot.com
//GPL


// PadSynth algorithm by by Nasca O. Paul.

using System;

namespace CsWaveAudio
{
    namespace SynthesisBaseClasses
    {
        /// <summary>
        /// Instruments provide an array in frequency domain, which is then later turned into samples using IFFT.
        /// Uses random phases
        /// </summary>
        public abstract class FourierSynthesisBase : SynthesisBase
        {
            protected const int N = 65536 * 4; // enough for about 5 secs 
            protected readonly double amplitude;
            protected abstract double[] BuildFrequencyArray();
            public FourierSynthesisBase(double amplitude)
            {
                this.amplitude = amplitude;
            }
            protected override double[] generate(int nSamples)
            {
                double[] inData = this.BuildFrequencyArray();

                // create real and img. parts, using random phases
                Random rand = new Random();
                double[] imgData = new Double[inData.Length];
                double[] realData = new Double[inData.Length];
                for (int i = 0; i < inData.Length; i++)
                {
                    double phase = rand.NextDouble() * 2.0 * Math.PI;
                    realData[i] = inData[i] * Math.Cos(phase);
                    imgData[i] = inData[i] * Math.Sin(phase);
                }

                // perform IFFT
                double[] outSamples;
                Fourier.RawFrequencyToSamples(out outSamples, realData, imgData);

                //repeat the samples as many times as necessary to fill output buffer
                // for now, doesn't fill to the brim because I'm lazy
                int nSize = outSamples.Length;
                double[] output = new double[nSamples];
                double pieces = nSamples / (double)nSize; //how many full periods to add to the output
                for (int nPiece = 0; nPiece < (int)pieces; nPiece++)
                {
                    Array.Copy(outSamples, 0, output, nPiece * nSize, nSize);
                }
                // add the rest of the output
                Array.Copy(outSamples, 0, output, (int)pieces * nSize, nSamples - (int)pieces * nSize);
                return output;
            }
        }



        public abstract class PadSynthesisBase : FourierSynthesisBase
        {
            protected readonly double frequency;
            protected double fBandwidth = 60.0;

            public PadSynthesisBase(double frequency, double amplitude)
                : base(amplitude)
            {
                this.frequency = frequency;
            }

            /*protected virtual double getHarmonicRatio()
            {
                return 
            }*/

            // This is the profile of one harmonic. In this case is a Gaussian distribution, e^(-x^2) 
            private double profile(double fi, double bandwidth)
            {
                double x = fi / bandwidth; // The amplitude is divided by the bandwidth to ensure that the harmonic keeps the same amplitude regardless of the bandwidth
                x *= x;
                return (x > 14.7128) ? 0.0 : Math.Exp(-x) / bandwidth; //this avoids computing the e^(-x^2) where results are very close to zero
            }

            // Create array in frequency. We smooth out peaks with Gaussian dist.
            protected override double[] BuildFrequencyArray()
            {
                double[] A = GetHarmonicWeights();

                double baseBandwidth = this.fBandwidth;
                double baseFrequency = this.frequency;

                double[] freq_amp = new double[N]; //only the first half of this is filled.

                //for each harmonic, add to the 
                for (int nHarmonic = 1; nHarmonic < A.Length; nHarmonic++)
                {
                    //bandwidth of the current harmonic measured in Hz
                    //note that this is wider for each harmonic
                    double bw_Hz = (Math.Pow(2.0, baseBandwidth / 1200.0) - 1.0) * baseFrequency * nHarmonic;

                    double bwi = bw_Hz / (2.0 * SampleRate);
                    double fi = baseFrequency * nHarmonic / (double)SampleRate;
                    for (int i = 0; i < N / 2; i++)
                    {
                        double hprofile = profile((i / (double)N) - fi, bwi);
                        freq_amp[i] += hprofile * A[nHarmonic];
                    }
                }

                // normalize the results?

                return freq_amp;

            }
            protected abstract double[] GetHarmonicWeights(); // creates an array of 

        }
    }

    public class PadSynthesis : SynthesisBaseClasses.PadSynthesisBase
    {
        protected double fScaleFormants;
        public PadSynthesis(double frequency, double amplitude, double fScaleFormantsIn)
            : base(frequency, amplitude)
        {
            this.fScaleFormants = fScaleFormantsIn;
        }
        protected override double[] GetHarmonicWeights()
        {
            const int nHarmonics = 64;

            double[] A = new double[nHarmonics]; //note that A[0] is unused
            double f1 = this.frequency * this.fScaleFormants; // 1/16 ~ strings

            double[,] formantParameters = new double[,] { 
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
                    weight += Math.Exp(-Math.Pow((i*f1 - formantParameters[j, 0]) * formantParameters[j, 1], 2.0)) * formantParameters[j,2];
                
                A[i] = (1.0 / (double)i) * weight;

            }
            return A;
        }

    }






    public class FourierSynthesis : SynthesisBaseClasses.FourierSynthesisBase
    {
        private double[] freqs;
        public FourierSynthesis(double amplitude)
            : base(amplitude)
        {
            this.freqs = new double[] { 440.0, 500.0 };
        }
        protected override double[] BuildFrequencyArray()
        {
            double[] amps = new double[N]; // the second half of this is negative frequency which we aren't concerned with
            // elem 0 is 0, elem N/2 is SampleRate/2
            double dScaledown = (N / 2) / (SampleRate / 2.0); // map 0-22050.0 to 0-512
            for (int i = 0; i < freqs.Length; i++)
            {
                int index = (int)(freqs[i] * dScaledown);
                amps[index] = 4000;
            }
            return amps;
        }
    }
}