""" eye_m_learn.py - The learning module for eye_m.
	Obtains an image from webcam-eyetracker, the depth of the eye from th
	screen and 

	camtracker: 
	https://github.com/esdalmaijer/webcam-eyetracker
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import time
import threading
from eye_m_lib import Queue
from eye_m_track import get_xyz
from eye_m_lib import get_sqlconn	# mysql wrapper

class learn(object):
	""" TODO
	"""

	def __init__(self, data_hz, ann_path=None):
		""" TODO
		"""
		maxrows = int(data_hz * (600 * str(data_hz)[::-1].find('.')))		
		self.data_rows = Queue(maxsize=maxrows)
		self.learn_thread = threading.Thread(target=self._learn,)
		self.data_thread = threading.Thread(target=self._data)
		self.data_hz = data_hz
		self.curr_error = 100
		self.threads_on = None
		self.ann = None # TODO

	# Threaded
	def _data(self):
		""" Aggregates pupil x, y, z data into self.data_rows at a rate of
			self.data_hz. Maximum temporal depth is 1 second (i.e. maxrows)
		"""
		while True:
			# Get eye xyz
			x, y, z = 1, 2, 3
			# get_xyz()
			print([x,y,z])
			self.data_rows.shove([x, y, z])

			if not self.threads_on:
				break

			time.sleep(self.data_hz)
		print('b')

	# Threaded
	def _learn(self):
		""" Waits for a mouse click and, on-click, runs self.data_rows through
			ANN.
		"""
		# For testing, wait for space instead of mouse-click
		# for row in self.data_rows.get_list():
		# 	print(row)
		
		# if not self.threads_on:
		# 		break
		pass

	def start(self):
		""" Starts the learning threads.
		"""
		print('eye_m is learning...')

		# Start data collection and learning threads
		self.threads_on = True
		self.data_thread.start()
		self.learn_thread.start()
		

	def stop(self):
		""" Stop learning threads
		"""
		self.threads_on = None

	def status(self):
		""" Outputs status to console.
		"""
		if self.data_thread.isAlive():
			print('Data Thread: On')
		else:
			print('Data Thread: Off')

		if self.learn_thread.isAlive():
			print('Learning Thread: On')
		else:
			print('Learning Thread: Off')
		