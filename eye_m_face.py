""" eye_face.py. Face class with pupil and nose coords.
"""

import cv2
import numpy as np
import time

MIRROR_IMG = True
EYE_MODEL = 'models/haarcascade_eye.xml'
# EYE_MODEL = 'models/haarcascade_eye_tree_eyeglasses.xml'
NOSE_MODEL = 'models/haarcascade_nose.xml'
UB_MODEL = 'models/haarcascade_upperbody.xml'

# Load cascade models # TODO: Test l/r eye masks
try:
    g_eye_model = cv2.CascadeClassifier(EYE_MODEL)
    g_nose_model = cv2.CascadeClassifier(NOSE_MODEL)
    g_ub_model = cv2.CascadeClassifier(UB_MODEL)
except:
    raise Exception('Error loading cascade models.')

class Face(object):
    def __init__(self, frame):
        self.frame = frame
        self.frame_mod = frame
        self.nose_x = 0
        self.nose_y = 0
        self.l_eye_x = 0
        self.l_eye_y = 0
        self.r_eye_x = 0
        self.r_eye_y = 0
        self._points = []
        
        self._set_features()
    
    def _set_features(self):
        """ Sets facial feature coordinates based on self.frame.
        """
        if self.frame_mod is None:
            return

        # Optimize frame for classification # TODO: Anything else?
        frame_mod = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)  # frame->rgb
        frame_mod = balance_brightness(frame_mod)
        self.frame_mod = frame_mod 

        # Detect facial features
        eyes = g_eye_model.detectMultiScale(frame_mod, 1.3, 5)
        nose = g_nose_model.detectMultiScale(frame_mod, 1.3, 5)

        # Proceed if both eyes detected
        if not len(eyes) == 2:  
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
            self.l_eye_x = eye_1_x
            self.l_eye_y = eye_1_y
            self.r_eye_x = eye_2_x
            self.r_eye_y = eye_2_y
        else:
            self.l_eye_x = eye_2_x
            self.l_eye_y = eye_2_y
            self.r_eye_x = eye_1_x
            self.r_eye_y = eye_1_y

        # TODO: Ensure eyes are reasonably far apart

        self._points.append((self.l_eye_x, self.l_eye_y))
        self._points.append((self.r_eye_x, self.r_eye_y))

        for (x, y, w, z) in nose:
            self.nose_x = x + w / 2
            self.nose_y = y + z / 2
            self._points.append((self.nose_x, self.nose_y))

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

    def get_marked(self, frame=None):
        """ Returns the given frame marked with the facial features.
            If not frame, self.frame is used.
        """
        if frame is None:
            frame = self.frame
        for p in self._points:
            cv2.circle(frame, (p[0], p[1]), 1, (0, 0, 255), 1)

        # debug
        # cv2.circle(frame, (self.l_eye_x + 1, self.l_eye_y + 1), 1, (0, 0, 255), 1)
        # cv2.circle(frame, (self.r_eye_x + 1, self.r_eye_y + 1), 1, (0, 255, 0), 1)

        return frame

def get_cropped(frame, x, y, w, z):
    return frame[y:y + z, x:x + w]  # [Y1:Y2, X1:X2]

def balance_brightness(frame):
    """ Apply transform to even out brightness.
        From: https://docs.opencv.org/3.1.0/d5/daf/
        tutorial_py_histogram_equalization.html
    """
    hist, bins = np.histogram(frame.flatten(), 256, [0, 256])
    cdf = hist.cumsum()
    cdf_normalized = cdf * hist.max() / cdf.max()
    cdf_m = np.ma.masked_equal(cdf, 0)
    cdf_m = (cdf_m - cdf_m.min()) * 255 / (cdf_m.max() - cdf_m.min())
    cdf = np.ma.filled(cdf_m, 0).astype('uint8')
    return cdf[frame]


if __name__ == '__main__':
    print('Starting...')
    cam = cv2.VideoCapture(0)

    while True:
        ret, frame = cam.read()
        if MIRROR_IMG:
            frame = cv2.flip(frame, 1)
        
        face = Face(frame)
        frame = face.get_marked()
        # frame = np.hstack((frame, face.pframe))  # put imgs side by side

        cv2.imshow('Output', frame)
        # cv2.imshow('Output2', face.get_marked(face.frame_mod))
        time.sleep(.1)

        if cv2.waitKey(1) == 32:
            break  # spacebar to quit
    cv2.destroyAllWindows()
