""" eye_m_lib.py - Class and function lib for eye_m

Author:
	Dustin Fast (dustin.fast@outlook.com), 2018
"""

import MySQLdb

# Constants (TODO: Move to config)
MYSQL_HOST 	= '127.0.0.1'
MYSQL_DB 	= 'eye_m'
MYSQL_USER 	= 'root'
MYSQL_PASS 	= 'sqldev'

#############
#  CLASSES  #
#############

#############
# FUNCTIONS #
#############

def check_dependencies():
	""" Returns true iff all dependencies installed
	"""
	# TODO
	
	return True

def get_sqlconn():
	""" Based on defined MYSQL constants, returns:
			conn	- db connection object
			cur		- db cursor object
	"""
	conn = MySQLdb.connect(host=MYSQL_HOST,
						   user=MYSQL_USER,  
						   passwd=MYSQL_PASS,
						   db=MYSQL_DB )
	cur = conn.cursor()

	return conn, cur
		
def repl():
	""" Read-Eval-Print-Loop
	"""

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
		print('Evaluating "' + eval_string + '"...')  # debug
		try:
			eval(eval_string)
		except Exception as e:
			print('ERROR: Unsupported command')
			print('Tried: ' + eval_string)
			print(e)