#!/usr/bin/env python
""" eye_m.py. The main module file for eye_m. """

__author__ = "Dustin Fast (dustin.fast@outlook.com)"


from eye_m_finder import Finder
from eye_m_classlib import Mouse
from eye_m_learn import Learner


finder = Finder()
learner = Learner()

def onclick(event):
    face = finder.run_single(True)  # TODO: Use IR
    print(event.Position)
    print(face)

    if face:
        learner.give_data(face.as_dict)
    return True

def start():
    mouse = Mouse(onclick=onclick)
    mouse.click_watch.start()

    # Start mousing after sufficient learning

    uinput = raw_input('Press Enter to quit...')

    # Cleanup
    mouse.click_watch.terminate()
    mouse.click_watch.join()


if __name__ == '__main__':
    # TODO: Command line args
    start()
