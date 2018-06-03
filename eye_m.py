#!/usr/bin/env python2
""" eye_m.py
	Contains main() for eye_m: Starts either the learning or mousing thread 
	based on cmd line args and gives REPL prompt.

Author:
	Dustin Fast (dustin.fast@outlook.com), 2018
"""

import argparse						# cmd line arg parser

from eye_m_lib import repl
from eye_m_learn import learn		# eye_m learning module
from eye_m_mouse import mouse		# eye_m mouse move/click module

#############
# CONSTANTS #
#############
MYSQL_DB = 'localhost'
MYSQL_USER = 'test'
MYSQL_PASS = 'test'

#############
# FUNCTIONS #
#############

########			
# MAIN #
########s

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
	