//Ben Fisher, 2008
//halfhourhacks.blogspot.com
//GPL

// References:
// http://www.fit.vutbr.cz/~cernocky/sig/
// http://ccrma.stanford.edu/~pdelac/154/m154paper.htm

using System;

namespace CsWaveAudio
{
    public static class PitchDetection
    {
        /// <summary>
        /// Autocorrelation deals with noise better. Amdf slightly better with reverb, but seems weaker overall.
        /// </summary>
        public enum PitchDetectAlgorithm
        {
            Autocorrelation, Amdf
        }

        public static double DetectPitch(WaveAudio w)
        {
            return detectPitchCalculation(w, 50.0, 500.0, 1, 1, PitchDetectAlgorithm.Autocorrelation)[0];
        }
        public static double DetectPitch(WaveAudio w, double minHz, double maxHz)
        {
            return detectPitchCalculation(w, minHz, maxHz, 1, 1, PitchDetectAlgorithm.Autocorrelation)[0];
        }
        public static double DetectPitch(WaveAudio w, double minHz, double maxHz, PitchDetectAlgorithm algorithm)
        {
            return detectPitchCalculation(w, minHz, maxHz, 1, 1, algorithm)[0];
        }
        public static double[] DetectPitchCandidates(WaveAudio w,double minHz, double maxHz, int nCandidates)
        {
            return detectPitchCalculation(w, minHz, maxHz, nCandidates, 1, PitchDetectAlgorithm.Autocorrelation);
        }
        public static double[] DetectPitchCandidates(WaveAudio w, double minHz, double maxHz, int nCandidates, PitchDetectAlgorithm algorithm)
        {
            return detectPitchCalculation(w, minHz, maxHz, nCandidates, 1, algorithm);
        }
        public static double[] DetectPitchCandidates(WaveAudio w, double minHz, double maxHz, int nCandidates, PitchDetectAlgorithm algorithm, int nResolution)
        {
            return detectPitchCalculation(w, minHz, maxHz, nCandidates, nResolution, algorithm);
        }


        private static double[] detectPitchCalculation(WaveAudio w, double minHz, double maxHz, int nCandidates, int nResolution, PitchDetectAlgorithm algorithm)
        {
            // note that higher frequency means lower period
            int nLowPeriodInSamples = HzToPeriodInSamples(maxHz, w.getSampleRate());
            int nHiPeriodInSamples = HzToPeriodInSamples(minHz, w.getSampleRate());
            if (nHiPeriodInSamples <= nLowPeriodInSamples) throw new Exception("Bad range for pitch detection.");
            if (w.getNumChannels() != 1) throw new Exception("Only mono supported.");
            double[] samples = w.data[0];
            if (samples.Length < nHiPeriodInSamples) throw new Exception("Not enough samples.");

            // both algorithms work in a similar way
            // they yield an array of data, and then we find the index at which the value is highest.
            double[] results = new double[nHiPeriodInSamples - nLowPeriodInSamples];

            if (algorithm == PitchDetectAlgorithm.Amdf)
            {
                for (int period = nLowPeriodInSamples; period < nHiPeriodInSamples; period += nResolution)
                {
                    double sum = 0;
                    // for each sample, see how close it is to a sample n away. Then sum these.
                    for (int i = 0; i < samples.Length - period; i++)
                        sum += Math.Abs(samples[i] - samples[i + period]);

                    double mean = sum / (double)samples.Length;
                    mean *= -1; //somewhat of a hack. We are trying to find the minimum value, but our findBestCandidates finds the max. value.
                    results[period - nLowPeriodInSamples] = mean;
                }
            }
            else if (algorithm == PitchDetectAlgorithm.Autocorrelation)
            {
                for (int period = nLowPeriodInSamples; period < nHiPeriodInSamples; period += nResolution)
                {
                    double sum = 0;
                    // for each sample, find correlation. (If they are far apart, small)
                    for (int i = 0; i < samples.Length - period; i++)
                        sum += samples[i] * samples[i + period];

                    double mean = sum / (double)samples.Length;
                    results[period - nLowPeriodInSamples] = mean;
                }
            }

            // find the best indices
            int[] bestIndices = findBestCandidates(nCandidates, ref results); //note findBestCandidates modifies parameter
            // convert back to Hz
            double[] res = new double[nCandidates];
            for (int i=0; i<nCandidates;i++)
                res[i] = PeriodInSamplesToHz(bestIndices[i]+nLowPeriodInSamples, w.getSampleRate());
            return res;
        }


        /// <summary>
        /// Finds n "best" values from an array. Returns the indices of the best parts.
        /// (One way to do this would be to sort the array, but that could take too long.
        /// Warning: Changes the contents of the array!!! Do not use result array afterwards.
        /// </summary>
        private static int[] findBestCandidates(int n,ref double[] inputs)
        {
            if (inputs.Length < n) throw new Exception("Length of inputs is not long enough.");
            int[] res = new int[n]; // will hold indices with the highest amounts.

            for (int c = 0; c < n; c++)
            {
                // find the highest.
                double fBestValue = double.MinValue;
                int nBestIndex = -1;
                for (int i = 0; i < inputs.Length; i++)
                    if (inputs[i] > fBestValue) { nBestIndex = i; fBestValue = inputs[i]; }

                // record this highest value
                res[c] = nBestIndex;

                // now blank out that index.
                inputs[nBestIndex] = double.MinValue;
            }

            return res;
        }



        private static int HzToPeriodInSamples(double hz, int sampleRate)
        {
            return (int)(1 / (hz / (double)sampleRate));
        }
        private static double PeriodInSamplesToHz(int period, int sampleRate)
        {
            return 1 / (period / (double)sampleRate);
        }

        /*
         * The idea was that one could break a wave input into sine waves. This didn't work so well.
        public static WaveAudio reconstruct(WaveAudio input)
        {
            if (input.getNumChannels() != 1) throw new Exception("Only mono supported.");
            // turn it into sine waves ?!

            WaveAudio reconstructed = new WaveAudio(input.getSampleRate(), 1);
            reconstructed.LengthInSamples = input.LengthInSamples;

            WaveAudio slice = new WaveAudio(input.getSampleRate(), 1);
            const double SLICELEN = 0.094; //0.005;
            slice.LengthInSeconds = SLICELEN;
            int nSize = slice.LengthInSamples;
            for (int i = 0; i < input.LengthInSamples-nSize; i += nSize)
            {
                // fill the slice with input.
                Array.Copy(input.data[0], i, slice.data[0], 0, nSize);
                // now find the pitch of the slice
                //double[] pitches = amdf(slice, 30, 500, 1, 1);
                double[] pitches = autocorrelate(slice, 200, 500, 1, 1);
                // create a sine wave of that pitch, as long as the slice
                WaveAudio sine = new Sine(pitches[0], 0.7).CreateWaveAudio(SLICELEN + 0.001);
                // copy the sine to the output
                Array.Copy(sine.data[0], 0, reconstructed.data[0], i, nSize);
            }
            return reconstructed;
        }*/

    }
}
