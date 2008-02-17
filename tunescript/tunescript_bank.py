
# Base class for instrument banks

class Tunescript_bank():
	def playSequence(self, seq):
		pass
	
	def saveSequence(self, seq):
		pass
	
	
	def voiceBank(self, s): #return a list of available voices, rendered/unrendered
		return [('inst1',0),('test',1),('more',2)]
	
	def queryVoice(self, s):
		pass
	
	def user_queryVoice(self, s):
		results = self.queryVoice(s) #This method is probably overridden
		
		if results==None or len(results)==0:
			print 'No instruments called '+s
			return None
		elif len(results)==1:
			print 'Set instrument to '+str(results[0])
			return results[0]
		else:
			print 'Choose an instrument or press q to cancel:'
			for i in range(len(results)) in results: print str(i+1) + '> ' + str(results[i])
			print ''
			choice = raw_input()
			try: n = int(choice)-1
			except: return None
			if n<0 or n>=len(results): return None
			print 'Set instrument to '+str(results[n])
			return results[n]


#Helper functions
def midiToFrequency(n):
	return 8.1758 * (2** (n/12.0))

def pitchToName(nPitch):
	nOctave = int(nPitch / 12)-1
	nNote = nPitch % 12
	map = {0:'C',1:'C#',2:'D',3:'D#',4:'E',5:'F',6:'F#',7:'G',8:'G#',9:'A',10:'A#',11:'B'}
	return (map[nNote], nOctave)

def scaleTime(n):
	""" Turn MIDI time into seconds"""
	return float(n) / 128. #A half note is one second long
	
	
def add_at_length(baseArray, bankArray, nSamples):
	# trunc or add silence to get the right length
	if len(bankArray)==nSamples:
		baseArray.extend( bankArray )
	elif len(bankArray)> nSamples:
		baseArray.extend( bankArray[ 0 : nSamples])
	elif len(bankArray) < nSamples:
		baseArray.extend( bankArray )
		baseArray.extend([self.midval for i in xrange(len(bankArray) - nSamples)])

if __name__=='__main__':
	test = Tunescript_bank()
	test.user_queryVoice('a')
	
	a = [1,2,3]
	b = [4,5,6]
	a.extend(b)
	print a