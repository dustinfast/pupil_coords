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
from random import randint

# Cascade models for cv2 classifiers
N_HAAR = 'models/haarcascade_nose.xml'
E_HAAR = 'models/haarcascade_eye.xml'

# Frame display txt
FONT = cv2.FONT_HERSHEY_SIMPLEX
LINE = cv2.LINE_AA

# TODO: maxfails, alpha, beta, etc

class FindEyes(object):
    """ Representation of a face in the form of its nose and pupil coords.
        Contains image optimzation and feature detection methods.
    """
    def __init__(self, eye_model=E_HAAR, nose_model=N_HAAR):
        self.eye_model = cv2.CascadeClassifier(eye_model)
        self.nose_model = cv2.CascadeClassifier(nose_model)
        self.camera = cv2.VideoCapture(0)
        self.frame = None               # Curr camera frame

        # Coords # TODO: Do I need indiv coords? If so, use Face class
        self.nose_x = 0
        self.nose_y = 0
        self.l_eye_x = 0
        self.l_eye_y = 0
        self.r_eye_x = 0
        self.r_eye_y = 0
        self._points = []       # [(le_x, le_y), (re_x, re_y), (n_x, n_y)]
        # TODO Depth, etc?

        # Accuracy
        self.fails = 0          # Find failures, reset on compute accuracy
        self.count = 0          # count - acc_denom = frames till compute acc
        self.acc_denom = 25     # The denominator used to calc accuracy
        self.acc_curr = 0       # Current accuracy (as a decimal number)

        # When to optimize frame
        self.succ_fails = 0     # Succesive find failures
        self.max_fails = 15     # Most acceptable succesive failures
        self.max_loss = 60      # Least acceptable accuracy percentage

        # Optimized frame
        self.frame_opt = None   # Optimized version of curr frame
        self.alpha = 1.0        # Frame contrast applied
        self.beta = 0           # Frame brightness applied
        self.gamma = 1          # Frame gamma correction applied
        self.gamma_tbl = None  # Current gamma correction table
        
    def __str__(self):
        """ Returns a string representation of self.
        """
        ret_str = 'Pupils - lx, ly, rx, ry:'
        ret_str = str(self.l_eye_x) + ', '
        ret_str = str(self.l_eye_y) + ', '
        ret_str = str(self.r_eye_x) + ', '
        ret_str = str(self.r_eye_y) + '\n'
        ret_str += 'Nose - x,y: '
        ret_str = str(self.nose_y) + ', '
        ret_str = str(self.nose_y)
        return ret_str

    def _update(self):
        """ Updates the eyes object with the given frame. If too many 'missed'
            eyes in a row, will attempt to optimize image.
        """
        _, self.frame = self.camera.read()
        self.frame_opt = self._optimize_frame()
        self._reset_coords()

        if self._find_features() == True:
            self.fails += 1
            self.succ_fails += 1
            if self.succ_fails >= self.max_fails:
                self.succ_fails = 0
                self._find_new_opt()
        else:
            self.succ_fails = 0
        
        self._mark_img()  # Mark frame with data # TODO: Turn off for ondemand

        self.count += 1
        if self.count >= self.acc_denom:
            self.acc_curr = self._calc_acc()
            if self.acc_curr < self.max_loss:
                self._find_new_opt()
            self.count = 0
            self.fails = 0

    def _optimize_frame(self):
        """ Returns a version of self.frame with current optimizations applied.
        """
        frame = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)  # frame->grayscale

        # CLAHE optimize # Better with varying 
        # clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        # frame = clahe.apply(frame)

        # cv2 optimize
        # frame = cv2.equalizeHist(frame) 

        # custom gamma/alpha/beta
        # if self.gamma_tbl is not None:
        #     frame = cv2.LUT(frame, self.gamma_tbl)
        return frame

    def _find_new_opt(self, mask=None):
        """ Returns a version of self.frame adjusted for current lighting conds.
        """
        print('optimzing...')
        return
        max_tries = 10
        max_gamma = 3.0
        mix_gamma = 0.0
        max_alpha = 10.0
        min_alpha = -10.0
        max_beta = 20.0
        min_beta = -20.0

        prev_alpah = self.alpha
        prev_beta = self.beta
        prev_gamme = self.gamma
        
        # ?
        # frame = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)  # frame->grayscale

        gamma_step = .25
        # Hill climb by modifying varying alpha and beta
        # for i in range(max_tries):
        self.gamma = 1.0 / (self.gamma * gamma_step)
        self.gamma_tbl = np.array([((i / 255.0) ** self.gamma) * 255
            for i in np.arange(0, 256)]).astype("uint8")
        # self.frame = self._optimize_frame()

        # TODO Img func lib

        # debug
        # kern = np.ones((5, 5), np.uint8)   # frame transform kernal
        #     frame = cv2.erode(frame, self.kern, iterations=1)
        #     frame = cv2.erode(frame, self.kern, iterations=2)
        #     frame = cv2.dilate(frame, self.kern, iterations=1)
        #     frame = cv2.morphologyEx(frame, cv2.MORPH_OPEN, self.kern)
        #     frame = cv2.morphologyEx(frame, cv2.MORPH_CLOSE, self.kern)
        #     frame = cv2.morphologyEx(frame, cv2.MORPH_GRADIENT, self.kern)


    def _find_features(self):
        """ Finds eyes and nose and updates self with their coords.
            Returns True iff facial feature coordinates were found.
        """
        assert(self.frame_opt is not None)

        def _fail():
            self.fails += 1
            self._reset_coords()
            # print(str(self.fails) + (': no eyes'))  # debug

        # Detect facial features
        eyes = self.eye_model.detectMultiScale(self.frame_opt, 1.3, 5)
        nose = self.nose_model.detectMultiScale(self.frame_opt, 1.3, 5)

        # Fail if two eyes not deted  # TODO: And nose?
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

        return True  # Successful find

    def _mark_img(self):
        """ Marks self.frame with the facial feature coords, accuracy, etc.
        """ 
        # Add curr accuracy to frame
        txt = 'Accur: ' + str(self.accuracy(True)) + '%'
        cv2.putText(self.frame, txt, (10, 20), FONT, 0.4, (0, 0, 255), 1, LINE)
        txt = 'Alpha: ' + str(self.alpha)
        cv2.putText(self.frame, txt, (10, 40), FONT, 0.4, (0, 0, 255), 1, LINE)
        txt = 'Beta: ' + str(self.beta)
        cv2.putText(self.frame, txt, (10, 50), FONT, 0.4, (0, 0, 255), 1, LINE)
        txt = 'Gamma: ' + str(self.gamma)
        cv2.putText(self.frame, txt, (10, 60), FONT, 0.4, (0, 0, 255), 1, LINE)

        # Add each point to frame
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

    def _calc_acc(self):
        """ Returns the success rate of finding users eyes. Should be called 
            when self.count >= self.acc_denom.
        """
        return (self.acc_denom - self.fails) / self.acc_denom  # TODO: Fix acc

    def accuracy(self, whole_num=False):
        """ Returns the current accuracy, self.acc_curr, as either a
            whole number (ex: '95') or a float (ex: 0.95)
        """
        if whole_num:
            return self.acc_curr * 100
        else:
            return self.acc_curr

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

    def run_ondemand(self):
        """ Gets and processes a single frame. If success, returns coords:
            [ (l_eye_x, l_eye_y), (r_eye_x, r_eye_y), (nose_x, nose_y)
        """
        # TODO: Require success to return pts?
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
            print(self.acc_curr)
            if cv2.waitKey(1) == 32:
                break  # spacebar to quit
            # time.sleep(.1)
        cv2.destroyAllWindows()


if __name__ == '__main__':
    eyes = FindEyes()
    eyes.run_live()
