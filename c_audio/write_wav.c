#include <stdio.h>
#include <math.h>
#include <stdlib.h> //used by rand()
#include <time.h> //used by rand(), for srand

typedef int bool;

typedef char* errormsg;
#define OK 0

#include <limits.h>
#define  SHORT_MAX  SHRT_MAX
#define  SHORT_MIN  SHRT_MIN

typedef unsigned int uint;
typedef unsigned short ushort;
typedef char byte;

#define PI 3.1415926535 

void ftl_exit(char * msg)
{
	fputs(msg, stderr);
	exit(1);
}

typedef struct
{
	double *data;
	double *data_right; //this is null if the audio is mono
	int sampleRate;
	int length; //length in samples.
} CAudioData;



#define NUMCHANNELS(this) ((this->data_right == NULL)?1:2)
CAudioData* CAudioDataNew()
{
	// be sure to assign to ALL members.
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
	free(audio);
	audio->data = audio->data_right = NULL; audio = NULL;
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

// Caller responsible for freeing! You must call caudiodata_dispose!
errormsg caudiodata_clone(CAudioData** out, CAudioData* this)
{
	CAudioData* audio;
	audio = *out = CAudioDataNew(); //we'll use audio as an alias for the output, *out.
	
	errormsg msg = caudiodata_allocate(audio, this->length, NUMCHANNELS(this), this->sampleRate);
	if (msg!=OK) return msg;
	
	memcpy(audio->data, this->data, this->length*sizeof(double));
	if (this->data_right!=NULL)
		memcpy(audio->data_right, this->data_right, this->length*sizeof(double));
	
	return OK;
}
double getLengthInSeconds(CAudioData* this)
{
	if (this->data==NULL) return 0;
	return this->length / (double) this->sampleRate;
}

///////////////////////////////////////////////////////////////////
#include "wav_persist.c"
/////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////
#include "inplace_effects.c"
/////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////
#include "effects.c"
#include "effects_audacity.c"
/////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////
#include "synth.c"
/////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////
#include "synth_construct.c"
/////////////////////////////////////////////////////////////////////


void example1()
{
	//create a sample sine wave.
	CAudioData* audio =  CAudioDataNew();
	errormsg msg = caudiodata_allocate(audio, 44100*4, 1, 44100); // 4 seconds of audio, mono.
	if (msg!=OK) puts(msg);
	
	double freq = 300;
	int i; for (i = 0; i < audio->length; i++)
	{
		audio->data[i] = 0.9 * sin(i * freq * 2.0 * PI / (double)audio->sampleRate);
	}
	
	msg = caudiodata_savewave(audio, "out.wav", 16);
	if (msg != OK) puts(msg);
	caudiodata_dispose( audio);
}


int main()
{
	printf("Hello");

	
	//~ char *tmp = malloc(128);
	//~ gets(stdout);
	//~ caudiodata_dispose( audio);
	//~ gets(stdout);
	return 0;
}

//possible memory leak in the synths?
//audio = *out = CAudioDataNew(); //use audio as an alias for the output, *out.
//if (lengthSeconds<0) return "Invalid length"; if (freq<=0) return "Invalid frequency";
//user might not know to dispose after error

 // Nasca Octavian Paul- same guy who made ZynAddSubEffects
//todo: normalize inputs to wah wah, phaser. Perhaps all parameters doubles from 0 to 1
