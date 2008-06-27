//Ben Fisher, 2008
//halfhourhacks.blogspot.com
//GPL

//Generate 44100Hz, 1 channel sounds
/*
 * Usage:
 * Sine s = new Sine(440.0, 1.0)
 * WaveAudio w = s.CreateWaveFile(2.5) // make 2.5 seconds of audio
 * 
 * */

using System;
using CsWaveAudio.SynthesisBaseClasses;

namespace CsWaveAudio
{
    public class Sine : PeriodicSynthesisBase
    {
        public Sine(double freq, double amplitude) : base(freq, amplitude) { }
        protected override double WaveformFunction(int i)
        {
            return Math.Sin(i * timeScale);
        }
    }

    public class Square : PeriodicSynthesisBase
    {
        private readonly double cutpoint;
        public Square(double freq, double amplitude) : this(freq, amplitude, 0.5) { }
        public Square(double freq, double amplitude, double cutpointpos)
            : base(freq, amplitude)
        {
            this.cutpoint = this.period * cutpointpos;
        }
        protected override double WaveformFunction(int i)
        {
            return ((i % period) > cutpoint ? 1.0 : -1.0);
        }
    }

    public class Sawtooth : PeriodicSynthesisBase
    {
        private readonly double slope;
        public Sawtooth(double freq, double amplitude)
            : base(freq, amplitude)
        {
            this.slope = 2.0 / this.period; //because it goes from -1 to 1 in one period
        }
        protected override double WaveformFunction(int i)
        {
            return ((i % period) * slope - 1);
        }
    }

    public class Triangle : PeriodicSynthesisBase
    {
        private readonly double slope;
        public Triangle(double freq, double amplitude)
            : base(freq, amplitude)
        {
            this.slope = 4.0 / this.period; // //because it goes from -1 to 1 in half of a period
        }
        protected override double WaveformFunction(int i)
        {
            double wavePosition = (i % period);
            if (wavePosition < period / 2.0)
                return (wavePosition * slope - 1);
            else
                return ((wavePosition - period / 2) * -slope + 1);
        }
    }


    public class WhiteNoise : AperiodicSimpleSynthesisBase
    {
        private Random rand;
        public WhiteNoise(double amp) : base(amp) { rand = new Random(); }
        protected override double WaveformFunction(int i)
        {
            return (rand.NextDouble() * 2 - 1.0); //random number between -1 and 1
        }
    }

    // like a random walk. Can also be thought of as integral of white noise
    public class RedNoise : AperiodicSimpleSynthesisBase
    {
        private Random rand;
        private double location, factor;
        public RedNoise(double amp) : this(amp, 2.0) { }
        public RedNoise(double amp, double factor)
            : base(amp)
        {
            rand = new Random(); location = 0; this.factor = factor;
        }
        protected override double WaveformFunction(int i)
        {
            location += (rand.NextDouble() * 2 - 1.0) * factor;
            if (location > 1) location = 1;
            else if (location < -1) location = -1;
            return location;
        }
    }

    // On a whim I thought, what if instead of a sine wave I used semi-circles!
    // the result doesn't sound too interesting, but I use it for my "electric organ" effect.
    internal class CircleWave : PeriodicSynthesisBase
    {
        //circle is sqrt(1-x^2). I invented this. It doesn't sound that great.
        private readonly double qtrperiod;
        private readonly double halfperiod;
        public CircleWave(double freq, double amplitude)
            : base(freq, amplitude)
        {
            qtrperiod = period/4.0;
            halfperiod = period/2.0;
        }
        protected override double WaveformFunction(int i)
        {
            if (i % period < halfperiod)
                return amplitude * Math.Sqrt(1 - Math.Pow(((i % period) / qtrperiod - 1), 2.0));
            else
                return -amplitude * Math.Sqrt(1 - Math.Pow((((i % period) - halfperiod) / qtrperiod - 1), 2.0));
        }
    }
}

