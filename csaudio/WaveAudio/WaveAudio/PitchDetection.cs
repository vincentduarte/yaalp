// Resources:
// http://www.fit.vutbr.cz/~cernocky/sig/
// http://ccrma.stanford.edu/~pdelac/154/m154paper.htm

using System;

namespace CsWaveAudio
{
    public static class PitchDetection
    {


        private static int HzToPeriodInSamples(double hz, int sampleRate /*typically 44100*/)
        {
            return (int) (1 / (hz / (double)sampleRate));
        }
        private static double PeriodInSamplesToHz(int period, int sampleRate /*typically 44100*/)
        {
            return 1 / (period / (double)sampleRate);
        }

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
        }

        public static double[] amdf(WaveAudio w, double minHz, double maxHz, int nCandidates, int nResolution)
        {
            // note that higher frequency means lower period
            int nLowPeriodInSamples = HzToPeriodInSamples(maxHz, w.getSampleRate());
            int nHiPeriodInSamples = HzToPeriodInSamples(minHz, w.getSampleRate());
            if (nHiPeriodInSamples <= nLowPeriodInSamples) throw new Exception("Bad range for pitch detection.");
            if (w.getNumChannels() != 1) throw new Exception("Only mono supported.");
            double[] samples = w.data[0];
            if (samples.Length < nHiPeriodInSamples) throw new Exception("Not enough samples.");

            double[] amdfResults = new double[nHiPeriodInSamples - nLowPeriodInSamples];
            for (int period = nLowPeriodInSamples; period < nHiPeriodInSamples; period+=nResolution)
            {
                double sum = 0;
                // for each sample, see how close it is to a sample n away. Then sum these.
                for (int i = 0; i < samples.Length - period; i++)
                    sum += Math.Abs(samples[i] - samples[i + period]);

                double mean = sum / (double) samples.Length;
                mean *= -1; //somewhat of a hack. We are trying to find the minimum value, but our findBestCandidates finds the max. value.
                amdfResults[period - nLowPeriodInSamples] = mean;
            }

            // find the best indices
            int[] bestIndicies = findBestCandidates(nCandidates, amdfResults);
            // convert back to Hz
            double[] res = new double[nCandidates];
            for (int i=0; i<nCandidates;i++)
                res[i] = PeriodInSamplesToHz(bestIndicies[i]+nLowPeriodInSamples, w.getSampleRate());
            return res;
        }

        public static double[] autocorrelate(WaveAudio w, double minHz, double maxHz, int nCandidates, int nResolution)
        {
            // note that higher frequency means lower period.
            int nLowPeriodInSamples = HzToPeriodInSamples(maxHz, w.getSampleRate());
            int nHiPeriodInSamples = HzToPeriodInSamples(minHz, w.getSampleRate());
            if (nHiPeriodInSamples <= nLowPeriodInSamples) throw new Exception("Bad range for pitch detection.");
            if (w.getNumChannels() != 1) throw new Exception("Only mono supported.");
            double[] samples = w.data[0];
            if (samples.Length < nHiPeriodInSamples) throw new Exception("Not enough samples.");

            double[] autocorrelateResults = new double[nHiPeriodInSamples - nLowPeriodInSamples];
            for (int period = nLowPeriodInSamples; period < nHiPeriodInSamples; period += nResolution)
            {
                double sum = 0;
                // for each sample, find correlation. (If they are far apart, small)
                for (int i = 0; i < samples.Length - period; i++)
                    sum += samples[i] * samples[i + period];

                double mean = sum / (double)samples.Length;
                autocorrelateResults[period - nLowPeriodInSamples] = mean;
            }

            // find the best indicies
            int[] bestIndicies = findBestCandidates(nCandidates, autocorrelateResults);
            // convert back to Hz
            double[] res = new double[nCandidates];
            for (int i = 0; i < nCandidates; i++)
                res[i] = PeriodInSamplesToHz(bestIndicies[i] + nLowPeriodInSamples, w.getSampleRate());
            return res;
        }

        /// <summary>
        /// Finds n "best" values from an array. Returns the indices of the best parts.
        /// Warning: Mutates the array!!!
        /// (One way to do this would be to sort the array, but that could take too long.
        /// </summary>
        private static int[] findBestCandidates(int n, double[] inputs)
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




        public static void test()
        {


            Console.WriteLine(HzToPeriodInSamples(440.0, 44100)); // 100
            Console.WriteLine(PeriodInSamplesToHz(100, 44100)); // 440


            WaveAudio wnote = new Square(300, 0.6).CreateWaveAudio(0.5);
            WaveAudio wnote2 = new Sine(400, 0.6).CreateWaveAudio(0.5);
            //WaveAudio w = WaveAudio.Mix(wnote, 0.999, new WhiteNoise(0.6).CreateWaveAudio(0.5), 0.000000001);
            WaveAudio w = WaveAudio.Mix(wnote, wnote2);
            
            double[] pitches = amdf(w, 20, 440, 10, 1);
            printar(pitches);
            Console.WriteLine();
            double[] pitches2 = autocorrelate(w, 20, 440, 10, 1);
            printar(pitches2);
            
        }
        private static void printar(double[] ar)
        {
            for (int i = 0; i < ar.Length; i++) Console.Write(ar[i] + " ");
            Console.WriteLine();
        }



    }
}
