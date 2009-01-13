void example1()
{
	//create a sample sine wave.
	AudioParams* audio =  AudioParamsNew();
	errormsg msg = audioparams_allocate(audio, 44100*4, 1, 44100); // 4 seconds of audio, mono.
	if (msg!=OK) puts(msg);
	
	double freq = 300;
	int i;
	for (i = 0; i < audio->length; i++)
	{
		audio->data[i] = 0.9 * sin(i * freq * 2.0 * PI / (double)audio->sampleRate);
	}
	
	FILE * f = fopen("out.wav", "wb");
	msg = audioparams_savewave(audio, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	audioparams_dispose( audio);
}


void example_mix()
{
	AudioParams* w1 =  AudioParamsNew();
	AudioParams* w2 =  AudioParamsNew();
	
	synth_sin(&w1, 300, 4.0, 0.8); //sine wave, 300Hz
	synth_sin(&w2, 430, 4.0, 0.8); //sine wave, 430Hz
	
	AudioParams* mix;
	msg = effect_mix(&mix, w1, w2, 0.5, 0.5);
	if (msg!=OK) puts(msg);
	
	FILE * f = fopen("out_mix.wav", "wb");
	msg = audioparams_savewave(mix, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	
	audioparams_dispose(mix);
	audioparams_dispose(w1);
	audioparams_dispose(w2);
}

void loadtests()
{
	char tests[6][255]={
"C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d22k16bit1ch.wav",
"C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d22k8bit1ch.wav",
"C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d22k8bit2ch.wav",
"C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d44k16bit1ch.wav",
"C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d44k16bit2ch.wav",
"C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d44k8bit1ch.wav"
	};
	
	int i; for (i=0; i<6; i++)
	{
		AudioParams * audio;
		FILE*fin = fopen(tests[i], "rb");
		errormsg msg = audioparams_loadwave(&audio, fin);
		if (msg != OK) puts(msg);
		fclose(fin);
		
		printf("\n%s\nLength :%d", tests[i], audio->length);
		inplaceeffect_volume(audio,0.1);
		
		char buf[128];
		sprintf(buf, "out%d.wav", i);
		FILE * fout=fopen(buf, "wb");
		msg = audioparams_savewave(audio, fout, 16);
		if (msg != OK) puts(msg);
		fclose(fout);
		
		audioparams_dispose(audio);
	}
}

void mixwithsine() // or modulate, or append, an easy change
{
	AudioParams * w1; AudioParams* w2;AudioParams* out;
	FILE*fin = fopen("C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d22k8bit1ch.wav", "rb");
	errormsg msg = audioparams_loadwave(&w1, fin);
	if (msg != OK) puts(msg);
	fclose(fin);
	
	synth_sin(&w2, 300, getLengthInSeconds(w1), 0.8); //sine wave, 300Hz
	
	msg =  effect_mix(&out, w1, w2, 0.5, 0.1); //effect_append(&out, w2, w1);
	if (msg != OK) { puts(msg);  return 0;}
	FILE * f = fopen("out.wav", "wb");
	msg = audioparams_savewave(out, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	
	audioparams_dispose( w1);
	audioparams_dispose( w2);
	audioparams_dispose( out);
}
void appendandclone()
{
	AudioParams * wsine; AudioParams* wsinelouder;AudioParams* out;

	synth_sin(&wsine, 300, 1.0, 0.3); //sine wave, 300Hz
	
	audioparams_clone(& wsinelouder, wsine);
	inplaceeffect_volume(wsinelouder, 3);
	
	msg =  effect_append(&out, wsine, wsinelouder);
	if (msg != OK) { puts(msg);  return 0;}
	FILE * f = fopen("out.wav", "wb");
	msg = audioparams_savewave(out, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	
	audioparams_dispose( wsine);
	audioparams_dispose( wsinelouder);
	audioparams_dispose( out);
}

void testfadein()
{
	AudioParams* audio;
	
	synth_sin(&audio, 300, 4.0, 0.3); //sine wave, 300Hz
	
	inplaceeffect_fade(audio, 0, 2.5); //fade out
	//~ inplaceeffect_fade(audio, 1, 2.5); // fade in
	
	FILE * f = fopen("out.wav", "wb");
	msg = audioparams_savewave(audio, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	audioparams_dispose( audio);
}
void testReverse()
{
	AudioParams * w1; 
	FILE*fin = fopen("C:\\pydev\\yalp\\Subversion\\csaudio\\WaveAudio\\WaveAudioTests\\test_media\\d22k8bit1ch.wav", "rb");
	errormsg msg = audioparams_loadwave(&w1, fin);
	if (msg != OK) puts(msg);
	fclose(fin);
	
	inplaceeffect_reverse(w1);
	
	FILE * f = fopen("outrev.wav", "wb");
	msg = audioparams_savewave(w1, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	audioparams_dispose( w1);
}

void testTremelo()
{
	AudioParams* audio;
	synth_sin(&audio, 300, 4.0, 0.3); //sine wave, 300Hz
	
	inplaceeffect_tremelo(audio, 4, 0.2);
	FILE * f = fopen("out.wav", "wb");
	msg = audioparams_savewave(audio, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	audioparams_dispose( audio);
}

void testPitchScaleOrVibrato()
{
	AudioParams * w1;AudioParams* out;
	FILE*fin = fopen("longinput.wav", "rb");
	errormsg msg = audioparams_loadwave(&w1, fin);
	if (msg != OK) puts(msg);
	fclose(fin);

	//~ msg =  effect_scale_pitch_duration(&out, w1, 0.7);
	msg =  effect_vibrato(&out, w1, 0.2, 0.1);
	if (msg != OK) { puts(msg);  return 0;}
	FILE * f = fopen("out.wav", "wb");
	msg = audioparams_savewave(out, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	
	audioparams_dispose( w1);
	audioparams_dispose( out);
}

void testSynth()
{
	AudioParams* audio;
	synth_sin(&audio, 300, 10.0, 0.8);
	
	FILE * f = fopen("out.wav", "wb");
	errormsg msg = audioparams_savewave(audio, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	audioparams_dispose( audio);
}
void testRedGlitchNess()
{
	AudioParams* w1; AudioParams* w2; AudioParams* combo;
	synth_redglitch(&w1, 100, 20.0, 0.8, 0.09, 0.261); //- cool!
	synth_redglitch(&w2, 99, 20.0, 0.8, 0.09, 0.33);
	
	//~ synth_redglitch(&w1, 100.0, 20.0, 0.8, 0.02, 0.061); //- also cool!
	//~ synth_redglitch(&w2, 100.6, 20.0, 0.8, 0.02, 0.061);
	
	//~ synth_redglitch(&w1, 80.0, 20.0, 0.8, 0.09, 0.661); //- also cool!
	//~ synth_redglitch(&w2, 120.6, 20.0, 0.8, 0.09, 0.661);
	
	//Left channel is w1, right channel is w2
	combo = AudioParamsNew();
	combo->length = w1->length;
	combo->sampleRate = w1->sampleRate;
	combo->data = w1->data;
	combo->data_right = w2->data;
	
	FILE * f = fopen("outcombo.wav", "wb");
	errormsg msg = audioparams_savewave(combo, f, 16);
	if (msg != OK) puts(msg);
	fclose(f);
	
	audioparams_dispose( combo);
	audioparams_dispose( w1);
	audioparams_dispose( w2);
}
void testWriteToMemory()
{
	AudioParams* w1; AudioParams* w2; AudioParams* combo;
	synth_sin(&w1, 300, 2.0, 0.8);
	synth_sin(&w2, 303, 2.0, 0.8);
	
	//Left channel is w1, right channel is w2
	combo = AudioParamsNew();
	combo->length = w1->length;
	combo->sampleRate = w1->sampleRate;
	combo->data = w1->data;
	combo->data_right = w2->data;
	
	//~ errormsg msg = audioparams_savewave(combo, "outsines.wav", 16);
	char *inmemory; uint inmemorylength;
	errormsg msg = audioparams_savewavemem(&inmemory, &inmemorylength, combo, 16);
	FILE*f=fopen("outsines.wav","wb");
	fwrite(inmemory, 1, inmemorylength, f);
	fclose(f);
	
	if (msg != OK) puts(msg);
	audioparams_dispose( combo);
	audioparams_dispose( w1);
	audioparams_dispose( w2);
	
	free(inmemory);
}