//constructive synthesis. summing other sounds to make sounds.

errormsg synth_redglitch(AudioParams**out,double freq, double lengthSeconds, double amp, double chunkLength, double rednoisefactor)
{
	STARTRAND();
	
	AudioParams* audio;
	audio = *out = AudioParamsNew(); //use audio as an alias for the output, *out.
	if (lengthSeconds<0) return "Invalid length"; if (freq<=0) return "Invalid frequency";
	int length = (int)(lengthSeconds * SampleRate);
	errormsg msg = audioparams_allocate(audio, length, 1, SampleRate);
	if (msg!=OK) return msg;	
	
	AudioParams* tmp;
	int nChunks = lengthSeconds/chunkLength;
	int i; for (i=0; i<nChunks; i++)
	{
		synth_rednoiseState = 0;
		synth_rednoise_factor = rednoisefactor;
		synth_periodicsynth(&tmp, (OscillatorFn)synth_rednoise_impl, 1,freq, chunkLength, amp);
		
		int offset = (i * chunkLength * (audio->sampleRate));
		memcpy(audio->data + offset, tmp->data, tmp->length*sizeof(double));
		audioparams_dispose(tmp);
	}
	
	return OK;
}


errormsg synth_electricorgan(AudioParams**out,double basefreq, double lengthSeconds, double amp)
{
	AudioParams* audio;
	audio = *out = AudioParamsNew(); //use audio as an alias for the output, *out.
	if (lengthSeconds<0) return "Invalid length"; if (basefreq<=0) return "Invalid frequency";
	int length = (int)(lengthSeconds * SampleRate);
	errormsg msg = audioparams_allocate(audio, length, 1, SampleRate);
	if (msg!=OK) return msg;
		
	OscillatorFn fn = (OscillatorFn) sin;
	
	//make sure all set to 0
	int i;for (i=0; i<audio->length;i++) audio->data[i] = 0;
	
	int n; for(n=0; n<8; n++)
	{
		double freq1 = basefreq*(n+1);
		double freq2 = (basefreq*1.00606)*(n+1);
		double weight = (n==0) ? pow(0.9, 8 - 1) : 0.1 * pow(0.9, 8 - n); //or 8-1 ?
		
		double timescale1 = freq1 * 2.0 * PI / (double) SampleRate;
		double timescale2 = freq2 * 2.0 * PI / (double) SampleRate;
		for (i=0; i<audio->length;i++) 
			audio->data[i] += amp*0.5*weight*(sin(i*timescale1)+sin(i*timescale2));
	}
	return OK;
}

// Intentional beat frequencies "smoothen" the sound and make it more musical.
errormsg synth_sinesmooth(AudioParams**out,double freq, double lengthSeconds, double amp)
{
	AudioParams* audio;
	audio = *out = AudioParamsNew(); //use audio as an alias for the output, *out.
	if (lengthSeconds<0) return "Invalid length"; if (freq<=0) return "Invalid frequency";
	int length = (int)(lengthSeconds * SampleRate);
	errormsg msg = audioparams_allocate(audio, length, 1, SampleRate);
	if (msg!=OK) return msg;
	
	AudioParams* w1;AudioParams* w2;
	synth_sin(&w1, freq, lengthSeconds, amp);
	synth_sin(&w2, freq* 1.0006079, lengthSeconds, amp);
	int i; for(i=0; i<audio->length; i++)
		audio->data[i] = 0.6*w1->data[i] + 0.4*w2->data[i];
	
	audioparams_dispose(w1);
	audioparams_dispose(w2);
	return OK;
}

// Intentional beat frequencies "smoothen" the sound and make it more musical.
errormsg synth_sineorgan(AudioParams**out,double freq, double lengthSeconds, double amp)
{
	AudioParams* audio;
	audio = *out = AudioParamsNew(); //use audio as an alias for the output, *out.
	if (lengthSeconds<0) return "Invalid length"; if (freq<=0) return "Invalid frequency";
	int length = (int)(lengthSeconds * SampleRate);
	errormsg msg = audioparams_allocate(audio, length, 1, SampleRate);
	if (msg!=OK) return msg;
	
	AudioParams* w1;AudioParams* w2;
	synth_sin(&w1, freq, lengthSeconds, amp);
	synth_sin(&w2, freq* 1.0006079, lengthSeconds, amp);
	int i; for(i=0; i<audio->length; i++)
		audio->data[i] = 0.6*w1->data[i] + 0.4*w2->data[i];
	
	audioparams_dispose(w1);
	audioparams_dispose(w2);
	return OK;
}
