// strangely enough, these can be implemented as in-place effects

// phaser from Audacity by Nasca Octavian Paul

/*
 freq       - Phaser's LFO frequency
 startphase - Phaser's LFO startphase (radians), needed for stereo Phasers
 depth      - Phaser depth (0 - no depth, 255 - max depth)
 stages     - Phaser stages (recomanded from 2 to 16-24, and EVEN NUMBER)
 drywet     - Dry/wet mix, (0 - dry, 128 - dry=wet, 255 - wet)
 fb         - Phaser FeedBack (0 - no feedback, 100 = 100% Feedback, -100 = -100% FeedBack)
*/

// How many samples are processed before compute the lfo value again
#define fxphaseraudlfoskipsamples 20
#define fxphaseraudlfoshape 4.0
#define fxphaseraudMAX_STAGES 24
void effect_phaseraud_impl(int length, double* data, double freq_scaled, double startphase, double fb, int depth, int stages, int drywet)
{
	if (data==NULL) return;
	// state variables	
	unsigned long skipcount;
	double old[fxphaseraudMAX_STAGES];
	double gain;
	double fbout;
	double lfoskip;
	double phase;
	
	// initialize state variables
	int i, j;
	for (j = 0; j < stages; j++) old[j] = 0;   
	skipcount = 0;
	gain = 0;
	fbout = 0;
	lfoskip = freq_scaled; //not in Hz, already converted.
	phase = startphase;
	
	double m, tmp, in, out;
	for (i = 0; i < length; i++)
	{
		in = data[i];

		m = in + fbout * fb / 100;
		if (((skipcount++) % fxphaseraudlfoskipsamples) == 0) 
		{
			gain = (1 + cos(skipcount * lfoskip + phase)) / 2; //compute sine between 0 and 1
			gain = (exp(gain * fxphaseraudlfoshape) - 1) / (exp(fxphaseraudlfoshape)-1); // change lfo shape
			gain = 1 - gain / 255 * depth;      // attenuate the lfo
		}
		// phasing routine
		for (j = 0; j < stages; j++)
		{
			tmp = old[j];
			old[j] = gain * tmp + m;
			m = tmp - gain * old[j];
		}
		fbout = m;
		out = (m * drywet + in * (255 - drywet)) / 255;
		
		// Prevent clipping
		if (out < -1.0) out = -1.0;
		else if (out > 1.0) out = 1.0;
		data[i] = out;
	}
	
}

errormsg effect_phaseraud(CAudioData* this, double freq, double fb, int depth, int stages, int drywet)
{
	if (stages > fxphaseraudMAX_STAGES) return "Too many stages"; if (stages % 2 != 0) return "Stages should be even.";
	double startphaseleft = 0;
	double startphaseright = startphaseleft+PI; //note that left and right channels should start pi out of phase
	double freq_scaled = 2.0 * PI * freq / (double)this->sampleRate;
	
	effect_phaseraud_impl(this->length,this->data,freq_scaled, startphaseleft, fb, depth, stages, drywet);
	effect_phaseraud_impl(this->length,this->data_right,freq_scaled, startphaseright, fb, depth, stages, drywet);
	return OK;
}


 
