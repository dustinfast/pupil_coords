""" eye_face.py. Face class with pupil and nose coords.
"""

import sys
import cv2
import numpy as np

NOSE_MODEL = 'models/haarcascade_nose.xml'
EYE_MODEL = 'models/haarcascade_eye.xml'

# Load cascade models # TODO: Test l/r eye masks
try:
    g_eye_model = cv2.CascadeClassifier(EYE_MODEL)
    g_nose_model = cv2.CascadeClassifier(NOSE_MODEL)
    # g_ub_model = cv2.CascadeClassifier(UB_MODEL)
except:
    raise Exception('Error loading cascade models.')

class Face(object):
    """ TODO
    """
    def __init__(self, frame=None):
        self.cam = cv2.VideoCapture(0)  # ini camera
        self.nose_x = 0  # TODO: Do we need indiv coords like this?
        self.nose_y = 0
        self.l_eye_x = 0
        self.l_eye_y = 0
        self.r_eye_x = 0
        self.r_eye_y = 0
        self._points = []           # [ (l_eye_xy), (r_eye_xy), (nose_xy) ]
        self.fail_count = 0
        self.frame_count = 0
        self.max_count = sys.maxint
        # TODO Depth, etc?
        
        if frame is not None:
            self.frame = frame
            self.frame_opt = self._img_opt()
            self._set_features()
    
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

    def _update(self, frame):
        """ Updates the face object with the given frame.
        """
        self.frame = frame
        self.frame_opt = self._img_opt()
        self._points = []

        self._set_features()

        self.frame_count += 1
        if self.frame_count == self.max_count:
            print('Resetting accuracy metrics. Last=%' + str(self.accuracy()))
            self.frame_count = 0
            self.fail_count = 0

    def _set_features(self):
        """ Sets facial feature coordinates based on self.frame_opt.
            Assumes self.frame_opt is not None.
        """
        def _fail():
            self.fail_count += 1
            self._reset_coords()
            # print(str(self.fail_count) + (': no eyes'))  # debug

        # Detect facial features
        eyes = g_eye_model.detectMultiScale(self.frame_opt, 1.3, 5)
        nose = g_nose_model.detectMultiScale(self.frame_opt, 1.3, 5)
        # ub = g_ub_model.detectMultiScale(self.frame_opt, 1.3, 5)

        # Proceed if both eyes detected
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
                _fail()  # eyes too close together
                return
            self.l_eye_x = eye_1_x
            self.l_eye_y = eye_1_y
            self.r_eye_x = eye_2_x
            self.r_eye_y = eye_2_y
        else:
            if (eye_1_x - eye_2_x) < 20:
                _fail()  # eyes too close together
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

        # Mark features on self.frame
        self._mark_pts()

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

    def _img_opt(self):
        """ Returns a version of self.frame optimzed for classification.
            Transform is from: https://docs.opencv.org/3.1.0/d5/daf/
            tutorial_py_histogram_equalization.html
        """
        # TODO Optimize heuristics
        # TODO: check heuristics and adjust based on lighting
        # TODO: Erosion? https://docs.opencv.org/3.0-beta/doc/py_tutorials/
        # py_imgproc/py_morphological_ops/py_morphological_ops.html
        frame = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)
        hist, _ = np.histogram(frame.flatten(), 256, [0, 256])
        cdf = hist.cumsum()
        cdf_normalized = cdf * hist.max() / cdf.max()
        cdf_m = np.ma.masked_equal(cdf, 0)
        cdf_m = (cdf_m - cdf_m.min()) * 255 / (cdf_m.max() - cdf_m.min())
        cdf = np.ma.filled(cdf_m, 0).astype('uint8')

        return cdf[frame]

    def accuracy(self):
        """ Returns an int representing the percent of successfull eye finds.
        """
        return (self.frame_count - self.fail_count) * 100.0 / self.frame_count

    def run_single(self):
        """ Gets and processes a single frame, then returns the feature points.
        """
        _, frame = self.cam.read()
        self._update(frame)

        return self._points

    def run_live(self):
        """ Runs the data capture continuously. Displays frame and frame_opt
            in a window. Spacebar quits.
        """
        while True:
            _, frame = self.cam.read()
            self._update(frame)
            cv2.imshow('frame', frame)
            cv2.imshow('optimzed frame', self.frame_opt)
            if cv2.waitKey(1) == 32:
                break  # spacebar to quit
        cv2.destroyAllWindows()


if __name__ == '__main__':
    face = Face()
    face.run_live()
