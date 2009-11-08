

#include "bcaudio.h"


CAudioData* caudiodata_new()
{
	// one must assign to ALL members.
	CAudioData* ret = (CAudioData*) malloc(sizeof(CAudioData));
	ret->data = ret->data_right = NULL;
	ret->sampleRate = 44100;
	ret->length = 0;
	return ret;
}

void caudiodata_dispose(CAudioData* audio)
{
	free(audio->data); //remember that freeing NULL is completely ok
	free(audio->data_right);
	audio->data = audio->data_right = NULL;
	free(audio);
	audio = NULL;
}

errormsg caudiodata_allocate(CAudioData* this, int nSamples, int nChannels, int nSampleRate)
{
	free(this->data); free(this->data_right); //remember that freeing NULL is completely ok
	this->data = (double*) malloc(nSamples * sizeof(double));
	if (this->data == NULL)
		return "Not enough memory.";
	if (nChannels == 2)
	{
		this->data_right = (double*) malloc(nSamples * sizeof(double));
			if (this->data_right == NULL)
		return "Not enough memory.";
	}
	this->length = nSamples;
	this->sampleRate = nSampleRate;
	
	return OK;
}

// Caller responsible for calling caudiodata_dispose on the result!
errormsg caudiodata_clone(CAudioData** out, CAudioData* this)
{
	CAudioData* audio;
	audio = *out = caudiodata_new(); //we'll use audio as an alias for the output, *out.
	
	errormsg msg = caudiodata_allocate(audio, this->length, NUMCHANNELS(this), this->sampleRate);
	if (msg!=OK) return msg;
	
	memcpy(audio->data, this->data, this->length*sizeof(double));
	if (this->data_right!=NULL)
		memcpy(audio->data_right, this->data_right, this->length*sizeof(double));
	
	return OK;
}

double caudiodata_getLengthInSecs(CAudioData* this)
{
	if (this->data==NULL) return 0;
	return this->length / (double) this->sampleRate;
}


//todo:
//next: rename functions
//next: add debug system to catch mem leaks. limit malloc calls to one place
//validate all parameters, before allocating.
//possible memory leak in the synths?
//audio = *out = caudiodata_new(); //use audio as an alias for the output, *out.
//if (lengthSeconds<0) return "Invalid length"; if (freq<=0) return "Invalid frequency";
//user might not know to dispose after error

// Nasca Octavian Paul- same guy who made ZynAddSubEffects
//todo: normalize inputs to wah wah, phaser. Perhaps all parameters doubles from 0 to 1
