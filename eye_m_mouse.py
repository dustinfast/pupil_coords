""" eye_m_mouse.py - The mouse movement/click module for eye_m.
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import time
import threading

class mouse(object):
	""" TODO
	"""

	def __init__(self, mouse_hz, learn_hz, ann):
		""" TODO
		"""
		self.ann = None
		self.threads_on = None

		self.learn_thread = threading.Thread(target=self._mouse)
		self.mouse_hz = mouse_hz

	# Threaded
	def _mouse(self):
		""" TODO
		"""
		pass

	def start(self):
		""" Starts the mousing threads.
		"""
		print('eye_m is mousing...')
		

	def stop(self):
		""" Stop mousing threads.
		"""
		self.threads_on = None

	def status(self):
		""" Outputs status to console.
		"""
		# if self.data_thread.isAlive():
		# 	print('Data Thread: On')
		# else:
		# 	print('Data Thread: Off')

		# if self.learn_thread.isAlive():
		# 	print('Learning Thread: On')
		# else:
		# 	print('Learning Thread: Off')