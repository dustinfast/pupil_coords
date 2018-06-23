#!/usr/bin/env python
""" eye_m.py. The main module file for eye_m.

    Structure:
        self->eye_m_learn->eye_m_mouse
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

from eye_m_finder import Finder
from eye_m_classlib import Mouse
from eye_m_learn import Learner

def onclick(event):
    print(event.Position)
    return True

def start():
    mouse = Mouse(27, onclick=onclick)
    mouse.click_watch.start()

    # Start mousing after sufficient learning

    uinput = raw_input('Press Enter to quit...')
    mouse.click_watch.terminate()
    mouse.click_watch.join()

if __name__ == '__main__':
    start()
