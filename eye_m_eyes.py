""" eye_face.py. The Eyes detection class - Uses OpenCV to find users face 
    via the webcam and notes coordinates of their pupil and nose.
    A live mode, Eyes.run() displays these coords overlaid on live output.
    An on-demand mode processes frames as requested and returns the coords.

    Author: Dustin Fast (dustin.fast@outlook.com)
"""

import sys
import cv2
import numpy as np
from random import randint

N_MODEL = 'models/haarcascade_nose.xml'
E_MODEL = 'models/haarcascade_eye.xml'

class Eyes(object):
    """ Representation of a face in the form of its nose and pupil coords.
        Contains image optimzation and feature detection methods.
    """
    def __init__(self, eye_model=E_MODEL, nose_model=N_MODEL):
        self.eye_model = cv2.CascadeClassifier(eye_model)
        self.nose_model = cv2.CascadeClassifier(nose_model)
        self.kern = np.ones((5, 5), np.uint8)   # frame transform kernal
        self.cam = cv2.VideoCapture(0)          # ini camera

        # TODO: Do I need indiv coords
        self.nose_x = 0  
        self.nose_y = 0
        self.l_eye_x = 0
        self.l_eye_y = 0
        self.r_eye_x = 0
        self.r_eye_y = 0
        self._points = []           # [(le_x, le_y), (re_x, re_y), (n_x, n_y)]
        self.fails = 0              # Classificatoin failures
        self.frame_count = 0        # Classification attempts
        self.max_frames = 5         # Attempts before determine accuracy
        self.acc = 0                # Accuracy, as a percentage
        self.prev_acc = 0           # Prev accuracy percentage
        self.mask = 0               # Optimation mask
        # TODO Depth, etc?
    
    def __str__(self):
        """ Returns a string representation of self.
        """
        ret_str = 'Eyes: '
        ret_str = str(self.l_eye_x) + ', '
        ret_str = str(self.l_eye_y) + ', '
        ret_str = str(self.r_eye_x) + ', '
        ret_str = str(self.r_eye_y) + ', '
        ret_str += 'Nose: '
        ret_str = str(self.nose_y) + ', '
        ret_str = str(self.nose_y)
        return ret_str

    def _update(self):
        """ Updates the eyes object with the given frame according to the best
            optimization combination it can find.
        """
        _, self.frame = self.cam.read()  # Get cam data
        self._points = []
        self._optimize_frame(self.mask)
        self._find_features()
        self._mark_pts()

        self.frame_count += 1
        if self.frame_count == self.max_frames:
            self.prev_acc = self.acc
            self.acc = int((self.max_frames - self.fails) * 100.0 / self.max_frames)
            self.frame_count = 0
            self.fails = 0

            # Do hill climb to find best optimzation if needed
            print('Curr: ' + str(self.acc) + ', Prev: ' + str(self.prev_acc))
            if self.acc == 0 or self.acc + 5 < self.prev_acc:
                print('Looking for new best mask')
                masks = {}
                masks[self.acc] = self.mask # Include current mask in results

                # For max_frames diff masks
                for i in range(self.max_frames):
                    temp_mask = self._optimize_frame()
                    fails = 0
                    print('Temp mask: ' + str("{0:b}".format(self.mask)))
                    
                    # Try curr mask on max_frames frames
                    for j in range(self.max_frames):
                        _, self.frame = self.cam.read()  # Get cam data
                        if not self._find_features():
                            fails += 1
                    acc = int((self.max_frames - fails) * 100.0 / self.max_frames)
                    masks[acc] = temp_mask
                best = sorted(masks.keys())[0]
                self.mask = masks[best]
                print('New best: ' + str(best) + str("{0:b}".format(self.mask)))


    def _find_features(self):
        """ Returns True iff facial feature coordinates were found.
            Assumes self.frame_opt is not None.
        """
        def _fail():
            self.fails += 1
            self._reset_coords()
            # print(str(self.fails) + (': no eyes'))  # debug

        # Detect facial features
        eyes = self.eye_model.detectMultiScale(self.frame_opt, 1.3, 5)
        nose = self.nose_model.detectMultiScale(self.frame_opt, 1.3, 5)

        # Fail if two eyes not deted
        if not len(eyes) == 2:  
            _fail()
            return

        # Extract each coord and assign to self
        for i, (x, y, w, z) in enumerate(eyes):
            eye_x = x + w / 2
            eye_y = y + z / 2
            if i % 2 == 1:
                eye_1_x = eye_x
                eye_1_y = eye_y
            else:
                eye_2_x = eye_x
                eye_2_y = eye_y

        if eye_1_x < eye_2_x:  # ensure correct eye gets assigned
            if (eye_2_x - eye_1_x) < 20:
                _fail()  # if eyes unreasonably close together
                return
            self.l_eye_x = eye_1_x
            self.l_eye_y = eye_1_y
            self.r_eye_x = eye_2_x
            self.r_eye_y = eye_2_y
        else:
            if (eye_1_x - eye_2_x) < 20:
                _fail()  # if eyes unreasonably close together
                return
            self.l_eye_x = eye_2_x
            self.l_eye_y = eye_2_y
            self.r_eye_x = eye_1_x
            self.r_eye_y = eye_1_y

        self._points.append((self.l_eye_x, self.l_eye_y))
        self._points.append((self.r_eye_x, self.r_eye_y))

        for (x, y, w, z) in nose:
            self.nose_x = x + w / 2
            self.nose_y = y + z / 2
            self._points.append((self.nose_x, self.nose_y))
            break  # Ensure only one nose

        return True

    def _mark_pts(self):
        """ Marks self.frame with the facial features.
        """
        for p in self._points:
            cv2.circle(self.frame, (p[0], p[1]), 1, (0, 255, 0), 1)

        # debug
        # cv2.circle(self.frame, (self.l_eye_x + 1, self.l_eye_y + 1), 1, (0, 0, 255), 1)
        # cv2.circle(self.frame, (self.r_eye_x + 1, self.r_eye_y + 1), 1, (0, 255, 0), 1)

    def _reset_coords(self):
        """ Sets all feature coords to 0.
        """
        self.nose_x = 0
        self.nose_y = 0
        self.l_eye_x = 0
        self.l_eye_y = 0
        self.r_eye_x = 0
        self.r_eye_y = 0
        self._points = []

    def _optimize_frame(self, mask=None):
        """ Returns a version of self.frame optimzed for classification.
            If not mask, generates its own mask randomly
        """
        frame = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)

        if False:  # debug
            frame = cv2.erode(frame, self.kern, iterations=1)  # Erode foreground
            return frame

        if not mask:
            mask = randint(0, 7)  # TODO: tilt prop towards 0

        # If mask<5, bal img before opts, if>5, do it after. If==5, skip bal.
        if mask < 5:
            frame = self._balance(frame)

        if mask & 0:
            frame = cv2.erode(frame, self.kern, iterations=1)
        if mask & 1:
            frame = cv2.erode(frame, self.kern, iterations=2)
        if mask & 2:
            frame = cv2.dilate(frame, self.kern, iterations=1)
        if mask & 3:
            frame = cv2.dilate(frame, self.kern, iterations=2)
        if mask & 4:
            frame = cv2.morphologyEx(frame, cv2.MORPH_OPEN, self.kern)
        if mask & 5:
            frame = cv2.morphologyEx(frame, cv2.MORPH_CLOSE, self.kern)
        if mask & 6: 
            frame = cv2.morphologyEx(frame, cv2.MORPH_GRADIENT, self.kern)
        if mask & 7:
            # do nothing
            pass

        if mask > 5:
            frame = self._balance(frame)

        self.frame_opt = frame

        return mask

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

    def run_single(self):
        """ Gets and processes a single frame, then returns the feature points:
            [ (l_eye_x, l_eye_y), (r_eye_x, r_eye_y), (nose_x, nose_y)
        """
        self._update()
        return self._points

    def run_live(self):
        """ Runs the data capture continuously. Displays frame and frame_opt
            in a window. Spacebar quits.
        """
        while True:
            self._update()
            cv2.imshow('frame', self.frame)
            cv2.imshow('optimzed frame', self.frame_opt)
            if cv2.waitKey(1) == 32:
                break  # spacebar to quit
        cv2.destroyAllWindows()


if __name__ == '__main__':
    eyes = Eyes()
    eyes.run_live()
