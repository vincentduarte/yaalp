from yalpsequence import *

def shell():
	env = Environment() # Contains state about last duration, etc.
	currentInstrument = ('midi', 1)
	memory_sequences = {}
	sequence = None
	while True:
		strIn = raw_input('>')
		if strIn == '': continue
		if strIn == 'exit' or strIn=='q': return
			
		strSplit = strIn.split()
		if strSplit[0] == '<?': 
			#User is querying about an instrument
			if strSplit[1]=='m': #midi
				import instrument_midi
				instrument_midi.queryMidiInstrument(' '.join(strSplit[2:]))
			elif strSplit[1]=='s':
				import instrument_synth
				instrument_synth.querySynth(' '.join(strSplit[2:]))
			elif strSplit[1]=='w':
				import instrument_wave
				instrument_wave.queryWave(' '.join(strSplit[2:]))
			continue
		if strSplit[0] == '<':
			#Change the current instrument.
			if strSplit[1]=='m':
				import instrument_midi
				nInst = instrument_midi.parseMidiInstrument(' '.join(strSplit[2:]))
				if nInst != -1:
					currentInstrument = ('midi', nInst)
			elif strSplit[1]=='s':
				import instrument_synth
				strRet = instrument_synth.parseSynth(' '.join(strSplit[2:]))
				if strRet != None:
					currentInstrument = ('synth', strRet)
			elif strSplit[1]=='w':
				import instrument_wave
				strRet = instrument_wave.parseWave(' '.join(strSplit[2:]))
				if strRet != None:
					currentInstrument = ('wave', strRet)
			continue
		
		if len(strSplit) > 1 and strSplit[1] == '=':
			if not _isvalidname(strSplit[0]):
				print 'Name of seq,',strSplit[0],' not valid.'
				continue
			if len(strSplit)==2:
				print 'No sequence'
				continue
			strExpression = ' '.join(strSplit[2:])
			sequence = parse(strExpression, env, currentInstrument)
			if sequence == -1:
				print 'Sequence did not parse'
				continue
			memory_sequences[strSplit[0]] = sequence
			sequence.play()
			continue
		
		# is it a saved sequence?
		if strSplit[0] in memory_sequences:
			nTimes = 1
			strSaveTo = None
			if len(strSplit)>1:
				if strSplit[1].startswith('x'):
					nTimes = int(strSplit[1][1:])
				elif strSplit[1] == '>':
					strSaveTo = strSplit[2]
				else:
					print 'Unknown argument:', strSplit[1]
			import copy
			newseq = copy.deepcopy(memory_sequences[strSplit[0]])
			thenotes = copy.deepcopy(memory_sequences[strSplit[0]].notes)
			for i in range(nTimes-1):
				newseq.notes.extend(thenotes)
			newseq.play()
			
			if strSaveTo != None:
				if newseq.instrument[0]=='midi':print 'Cannot save to midi yet'
				elif newseq.instrument[0]=='synth':
					import instrument_synth
					instrument_synth.saveSequence(newseq, strSaveTo)
			
			continue
		else:
			# Otherwise, treat as a sequence and play it
			strExpression = ' '.join(strSplit)
			sequence = parse(strExpression, env, currentInstrument)
			if sequence == -1:
				print 'Expression did not parse'
				continue
				
			sequence.play()
			
def parse(strIn, env, currentInstrument):
	"""Takes a string, returns a Sequence"""
	seq = YalpSequence()
	seq.notes = []
	seq.instrument = currentInstrument
	ret = seq.AddNotes(strIn)
	print 'notes::::' , strIn
	if len(seq.notes)==0: return -1
	else: return seq

def _isvalidname(strIn):
	if strIn.isalnum():
		return True
	return False

if __name__=='__main__':
	shell()
