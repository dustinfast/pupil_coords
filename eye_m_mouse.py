""" eye_m_mouse.py - The mouse movement/click module for eye_m.
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import time
import threading
import win32api

class mouse(object):
    """ TODO
    """

    def __init__(self, mouse_hz):
        """ TODO
        """
        self.threads_on = None
        self.mouse_thread = threading.Thread(target=self._mouse)
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
        self.mouse_thread.start()

    def status(self):
        """ Outputs status to console.
        """
        if self.mouse_thread.isAlive():
            print('Mouse Thread: On')
        else:
            print('Mouse Thread: Off')

    @staticmethod
    def mouse_pos():
		x, y = win32api.GetCursorPos()
		return x, y


if __name__ == '__main__':
    print(mouse.mouse_pos())
    