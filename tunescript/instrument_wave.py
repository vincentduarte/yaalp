import tunescript_bank

from yalp.wave_file import *
from yalp.wave_synthesis import *

import yalpsequence

class Wave_bank(Tunescript_bank):
	def __init__(self):
		cachedAudio = {}
		cachedAudioPitches = {}
		cachedAudioBitrates = {}
	
	def playSequence(self,seq):
		# First, make sure we have all of the notes in the sequence:
		audiodata= self.buildWave(seq)
		audiodata.play_memory()
		del audiodata.samples
		del audiodata

	def saveSequence(self, seq, filename):
		print 'Attempting to save to',filename
		if not filename.endswith('.wav'): filename += '.wav'
		audiodata= self.buildWave(seq)
		audiodata.save_wave(filename)
		del audiodata.samples
		del audiodata
		print 'Saved to',filename


	def buildWave(self, seq):
		assert seq.instrument[0]=='wave'
		strInstrumentPath = seq.instrument[1]
		
		# strInstrumentPath is the relative path and filename of this.
		
		# First, find the bitrate of this.
		if strInstrumentPath in self.cachedAudioBitrates:
			nBits, nSampleRate = self.cachedAudioBitrates[strInstrumentPath]
		else:
			nBits, nSampleRate = self.determineBitrate(strInstrumentPath)
			self.cachedAudioBitrates[strInstrumentPath] = (nBits, nSampleRate)
		
		# Create an empty wave file and add stuff to it
		seqaudiodata = WaveFile(nBits=nBits, nSampleRate=nSampleRate)
		
		for note in seq.notes:
			if note.pitch == 1: #This represents a rest
				seqaudiodata.add_silence(tunescript_bank.scaleTime(note.duration))
			else:
				if (strInstrumentPath, note.pitch) not in cachedAudio: # If there's a note we don't have, we'll have to cache it.
					self.cacheNote(strInstrumentPath, note.pitch)
				noteSample = cachedAudio[(strInstrument, note.pitch)]
				
				tunescript_bank.add_at_length( seqaudiodata.samples, noteSample.samples, int(tunescript_bank.scaleTime(note.duration) * seqaudiodata.nSampleRate))
		return seqaudiodata
		
	def cacheNote(self, strInstrument, n):
		if (strInstrument, n) in cachedAudio:
			return
		print 'Caching',n, '...'
		
		nBase, strFilename, audioFundamental = waveTable[strInstrument][0:3]
		if audioFundamental==None:
			# Get the 'fundamental'
			print 'Getting fundamental...'
			waveTable[strInstrument][2] = WaveFile('.\\bank\\'+strFilename)
			audioFundamental = waveTable[strInstrument][2]
			cachedAudio[(strInstrument, nBase)] = audioFundamental
			if n==nBase: return
		
		# Scale the 'fundamental:
		currentFreq = tunescript_bank.midiToFrequency(nBase)
		desiredFreq = tunescript_bank.midiToFrequency(n)
		
		import copy
		import audio_effects
		newaudio = audioFundamental.empty_copy()
		newaudio.samples = (audioFundamental.samples).__deepcopy__()
		audio_effects.fx_change_pitch(newaudio, desiredFreq/currentFreq)
		
		cachedAudio[(strInstrument, n)] = newaudio

	
	def queryVoice(self, strInstname):
		if strInstname=='': return None
		strInstname = strInstname.lower()
		
		# Load everything in the 'bank' folder and in the 'mysounds' folder
		import os
		directories = ('.' +os.sep+ 'tunescript' + os.sep + 'bank', '.'+os.sep+'mysounds')
		
		thewaves = []
		for dir in directories:
			lwaves = os.listdir(dir)
			thewaves.extend( [ (lwave, os.path.join(dir, lwave)) for lwave in lwaves if lwave.endswith('.wav')])
			
		results=[]
		
		for filename_path in thewaves:
			if strin=='_': continue
			if strInstname in filename_path[0].lower():
				results.append(filename_path)
		return results


