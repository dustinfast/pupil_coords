""" eye_findeyes.py. The eye/pupil detection class - Uses OpenCV to find users 
    face via the webcam and note coordinates of pupils and nose.
    A live mode, Eyes.run() displays these coords overlaid on live output.
    An on-demand mode processes frames as requested and returns the coords.

    Author: Dustin Fast (dustin.fast@outlook.com)
"""

import sys
import cv2
import time
import numpy as np
from time import sleep
from eye_m_lib import Queue
from random import randint

# Cascade models for cv2 classifiers
EYE_HAAR = 'models/haarcascade_eye.xml'
NOSE_HAAR = 'models/haarcascade_nose.xml'

# Frame display txt
FONT = cv2.FONT_HERSHEY_SIMPLEX
LINE = cv2.LINE_AA

# MISC
ACCURACY_DENOM = 100


class Finder(object):
    """ Eye/Pupil detection class. Uses webcam to detect via openCV.
        Supports live (run_live()) and single frame (run_single()) output.
        Detected coordinates are stored in Face object. 
    """
    def __init__(self, eye_model=EYE_HAAR, nose_model=NOSE_HAAR):
        # Classifier
        self.eye_model = cv2.CascadeClassifier(eye_model)
        self.nose_model = cv2.CascadeClassifier(nose_model)
        self.camera = cv2.VideoCapture(0)
        self.live_mode = False  # If live mode, we do _mark_frame()
        self.face = Face()      # Face object. Contains coords, etc.
        self.frame = None       # Curr camera frame

        # Last 50 results (as bools). For accuracy calculation.
        self.results = Queue(maxsize=ACCURACY_DENOM)

        # Optimized frame
        self.frame_opt = None   # Optimized version of self.frame
        # self.opt_interval = 0   # TODO: Optimize for lightining conds every x frames

    def _update(self):
        """ Does an iteration of eye finding and updates self accordingly
        """
        _, self.frame = self.camera.read()
        self.frame_opt = self._optimize_frame()
        self._reset_coords()

        if self._find_eyes():
            self.results.shove(True)
        else:
            self.results.shove(False)

        # Mark frame with coords and diag info if in live mode
        if self.live_mode:
            self._mark_frame()  

    def _optimize_frame(self):
        """ Returns a version of self.frame with current optimizations applied.
        """
        frame = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)  # frame->grayscale
        return frame

    def _find_eyes(self):
        """ Finds eyes and nose and updates self with their coords.
            Returns True iff facial feature coordinates were found.
        """
        assert(self.frame_opt is not None)

        # Detect facial features
        eyes = self.eye_model.detectMultiScale(self.frame_opt, 1.3, 5)
        nose = self.nose_model.detectMultiScale(self.frame_opt, 1.3, 5)

        # Fail if not two eyes and one nose
        if not (len(eyes) == 2 and len(nose) == 1):  
            return

        # Extract eye coords
        for i, (x, y, w, z) in enumerate(eyes):
            eye_x = x + w / 2
            eye_y = y + z / 2
            if i % 2 == 1:
                eye_1_x = eye_x
                eye_1_y = eye_y
            else:
                eye_2_x = eye_x
                eye_2_y = eye_y

        # Fail if eyes unreasonably close together # TODO: Needed?
        # if ((eye_2_x - eye_1_x) < 20) or ((eye_1_x - eye_2_x) < 20):
        #     _fail()  
        #     return 

        # Assign coords to correct eyes
        if eye_1_x < eye_2_x:  
            self.face.l_eye_x = eye_1_x
            self.face.l_eye_y = eye_1_y
            self.face.r_eye_x = eye_2_x
            self.face.r_eye_y = eye_2_y
        else:
            self.face.l_eye_x = eye_2_x
            self.face.l_eye_y = eye_2_y
            self.face.r_eye_x = eye_1_x
            self.face.r_eye_y = eye_1_y

        for (x, y, w, z) in nose:
            self.face.nose_x = x + w / 2
            self.face.nose_y = y + z / 2

        return True  # Successful find

    def _mark_frame(self):
        """ Marks self.frame with the facial feature coords, accuracy, etc.
        """ 
        # Add curr ID accuracy to frame
        txt = 'ID Rate: ' + str(self.accuracy()) + '%'
        cv2.putText(self.frame, txt, (10, 20), FONT, 0.4, (0, 0, 255), 1, LINE)

        # Add each face point to frame
        for p in self.face.points():
            cv2.circle(self.frame, (p[0], p[1]), 1, (0, 255, 0), 1)

    def _reset_coords(self):
        """ Sets all feature coords to 0.
        """
        self.face.nose_x = 0
        self.face.nose_y = 0
        self.face.l_eye_x = 0
        self.face.l_eye_y = 0
        self.face.r_eye_x = 0
        self.face.r_eye_y = 0

    def accuracy(self):
        """ Returns current find rate (as a whole percent).
        """
        finds = 0
        denom = self.results.item_count()

        if not denom:
            return 0

        for b in [b for b in self.results.items if b]:
            finds += 1
        return finds / denom * 100
        

    def _balance(self, frame):
        """ Given a frame, returns a version with milder gradients.
        """
        hist, _ = np.histogram(frame.flatten(), 256, [0, 256])
        cdf = hist.cumsum()
        cdf_normalized = cdf * hist.max() / cdf.max()
        cdf_m = np.ma.masked_equal(cdf, 0)
        cdf_m = (cdf_m - cdf_m.min()) * 255 / (cdf_m.max() - cdf_m.min())
        cdf = np.ma.filled(cdf_m, 0).astype('uint8')
        return cdf[frame]

    def run_single(self, showframe=False):
        """ Gets and processes a single frame, then returns a Face object.
            Optionally displays frames (handy for debugging).
        """
        self._update()

        if self.success:
            if showframe:
                cv2.imshow('frame', self.frame)
                cv2.imshow('optimzed frame', self.frame_opt)
            return self.face

    def run_live(self, fps=0.015):  # fps=.03 -> 30fps, fps=.015 -> 60fps
            """ Runs the data capture continuously. Displays frame and frame_opt
                in a window. Spacebar quits.
            """
            self.live_mode = True
            while True:
                self._update()
                cv2.imshow('frame', self.frame)
                cv2.imshow('optimzed frame', self.frame_opt)
                if cv2.waitKey(1) == 32:
                    break  # spacebar to quit
                sleep(fps)  # ~ 60 FPS
            cv2.destroyAllWindows()
            self.live_mode = False


class Face(object):
    """ The user's pupil and nose coordinates and other related information.
    """
    def __init__(self):
        self.nose_x = -1
        self.nose_y = -1
        self.l_eye_x = -1
        self.l_eye_y = -1
        self.r_eye_x = -1
        self.r_eye_y = -1
        self.depth = -1  # TODO
        self.blink = -1  # TODO

    def __str__(self):
        ret_str = 'l_eye: ('
        ret_str += str(self.l_eye_x) + ', '
        ret_str += str(self.l_eye_y) + ')\n '
        ret_str += 'r_eye: ( '
        ret_str += str(self.r_eye_x) + ', '
        ret_str += str(self.r_eye_y) + ')\n'
        ret_str += 'nose: ('
        ret_str += str(self.nose_y) + ', '
        ret_str += str(self.nose_y) + ')'
        return ret_str

    def points(self):
        """ Returns a list of two-tuples containing the facial feature coords:
            [ (l_eye x/y), (r_eye x/y), (nose x/y) ]
        """
        pts = []
        pts.append((self.l_eye_x, self.l_eye_y))
        pts.append((self.r_eye_x, self.r_eye_y))
        pts.append((self.nose_x, self.nose_y))
        return pts


if __name__ == '__main__':
   finder = Finder()
   finder.run_live()
