#!/usr/bin/env python2
""" eye_m.py
	Contains main() for eye_m: Starts either the learning or mousing thread 
	based on cmd line args and gives REPL prompt.

Author:
	Dustin Fast (dustin.fast@outlook.com), 2018
"""

import argparse						# cmd line arg parser

from eye_m_learn import learn		# eye_m learning module
from eye_m_mouse import mouse		# eye_m mouse move/click module

#############
# CONSTANTS #
#############

#############
# FUNCTIONS #
#############

def lineout(string):
	""" Prints a line to the console, if verbose mode on.
	"""
	if verbose_mode:
		print(string)
		
def repl():
	# Show welcome txt
	print('Type q to exit, ? for help.')

	# REPL
	while True:
        # Get user input
		uinput = raw_input('eye_m >> ').strip()

        # On blank input, continue.
		if not uinput:
			continue
		# On exit, break.
		elif uinput == 'q':
			break
		# On help.
		elif uinput == '?':
			print('Help not implemented.')
			continue
		# On malformed input (i.e. no whitespace seperator), continue
		elif ' ' not in uinput:
			print('ERROR: Malformed command')
			continue

		# Explode user input on whitespace to build class, method, args
		uinput = uinput.split(' ')
		device = uinput[0]
		cmd = uinput[1]
		args = uinput[2:]

		# Ensure cmd is not for an internal function
		if cmd[0] == '_':
			print('ERROR: Malformed command')
			continue

		# Build eval string
		arg_string = str(args).replace('[', '').replace(']', '')
		eval_string = device.lower() + '.' + cmd + '(' + arg_string + ')'

		# Attempt to evaluate command
		lineout('Evaluating "' + eval_string + '"...')  # debug
		try:
			eval(eval_string)
		except Exception as e:
			print('ERROR: Unsupported command.')
			lineout(e)

########			
# MAIN #
########

if __name__ == '__main__':
	# Ini argparse
	MODE_MAP = {'l': learn,
				'm': mouse }
	parser = argparse.ArgumentParser(description='Start eye_m')
	parser.add_argument('mode', choices=MODE_MAP,
						help='run in mouse mode (m) or learning mode (l)')
	parser.add_argument('-v', action='store_true', help='verbose output')

	# get cmd line args, start specified mode, and give repl prompt
	args = parser.parse_args()
	global verbose_mode
	verbose_mode = args.v
	run_mode = MODE_MAP[args.mode]
	run_mode()
	repl()
	