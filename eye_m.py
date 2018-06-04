#!/usr/bin/env python2
""" eye_m.py
	Contains main() for eye_m: Starts either the learning or mousing thread 
	based on cmd line args and gives REPL prompt.

	Structure:
		self->eye_m_learn->eye_m_mouse
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

from eye_m_learn import learn

if __name__ == '__main__':
	
	# init learner
	eye = learn(.1, None)

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
		print('Evaluating "' + eval_string + '"...\n')  # debug
		try:
			eval(eval_string)
		except Exception as e:
			print('ERROR: Unsupported command')
			print('Tried: ' + eval_string)
			print(e)