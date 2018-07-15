""" pupils.py - Uses OpenCV to find the users face via the webcam and populates
    a Face object with the approximate pupil and nose coordinates. Note that 
    detection is highly dependent on lighting conditions.

    Webcam output is displayed and coords are overlaid on the image stream
    (spacebar quits).
    
    Dependencies: OpenCV 2.4 (Installable with pip from deps/)
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"


import cv2
from time import sleep

# HAAR Cascade models for cv2 classifiers
EYE_HAAR = 'models/haarcascade_eye.xml'
NOSE_HAAR = 'models/haarcascade_nose.xml'

# Misc
ACCURACY_DENOM = 100
CAMERA_ID = 0


class Face(object):
    """ An abstraction of a face in terms of its pupil and nose coordinates.
    """
    def __init__(self):
        self.nose_x = -1
        self.nose_y = -1
        self.l_eye_x = -1
        self.l_eye_y = -1
        self.r_eye_x = -1
        self.r_eye_y = -1

    def __str__(self):
        """ Returns a string representation of the facial coords.
        """
        ret_str = 'l_eye: ('
        ret_str += str(self.l_eye_x) + ', '
        ret_str += str(self.l_eye_y) + ')\n'
        ret_str += 'r_eye: ('
        ret_str += str(self.r_eye_x) + ', '
        ret_str += str(self.r_eye_y) + ')\n'
        ret_str += 'nose: ('
        ret_str += str(self.nose_y) + ', '
        ret_str += str(self.nose_y) + ')'
        return ret_str

    def get_points(self):
        """ Returns a list of two-tuples containing the eyes/nose coords:
            [ (l_eye), (r_eye), (nose) ]
        """
        pts = []
        pts.append((self.l_eye_x, self.l_eye_y))
        pts.append((self.r_eye_x, self.r_eye_y))
        pts.append((self.nose_x, self.nose_y))
        return pts


class Finder(object):
    """ Pupil/Nose detection class. Uses webcam to detect pupil/nose coords
        using OpenCV. Detected coordinates are stored in Face object. 
    """
    def __init__(self, eye_model=EYE_HAAR, nose_model=NOSE_HAAR):
        # Classifier
        self.eye_model = cv2.CascadeClassifier(eye_model)
        self.nose_model = cv2.CascadeClassifier(nose_model)
        self._camera = None
        self.face = Face()       # Face object. Contains pupil/nose coords.
        self.frame = None        # Current camera frame
        self.grayframe = None    # Grayscale version of self.frame

    def _update(self):
        """ Does an iteration of eye finding and updates self accordingly
        """
        # Get frame from camera
        _, self.frame = self._camera.read()

        # Convert to grayscale
        self.grayframe = cv2.cvtColor(self.frame, cv2.COLOR_RGB2GRAY)

        # reset face object and attempt to update it with current pupil/nose
        # coords based on the current frame.
        self._reset_coords()
        self.success = self._find_face()

        # Mark the current frame for display with dots at the pupil/nose coords
        self._mark_frame()

    def _find_face(self):
        """ Finds eyes and nose and updates self.face with their coords.
            Returns True iff facial feature coordinates were found.
        """
        assert(self.grayframe is not None)

        # Detect facial features using openCV
        eyes = self.eye_model.detectMultiScale(self.grayframe, 1.3, 5)
        nose = self.nose_model.detectMultiScale(self.grayframe, 1.3, 5)

        # Fail if not two eyes and one nose
        if not (len(eyes) == 2):  
            return

        # "eyes" is a box around each eye, so approximate pupil coords by taking
        # the intersection of the triangles formed by each.
        for i, (x, y, w, z) in enumerate(eyes):
            # Pupil coord is 
            eye_x = x + w / 2
            eye_y = y + z / 2
            if i % 2 == 1:
                eye_1_x = eye_x
                eye_1_y = eye_y
            else:
                eye_2_x = eye_x
                eye_2_y = eye_y

        # Determine which set of coords is for which eye and update self.face
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

        # Update self.face with nose coords
        for (x, y, w, z) in nose:
            self.face.nose_x = x + w / 2
            self.face.nose_y = y + z / 2

        return True  # Successful find

    def _mark_frame(self):
        """ Marks self.frame with the facial feature coords, accuracy, etc.
        """ 
        # Add each face point to frame
        for p in self.face.get_points():
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

    def run(self, fps=0.015):  # fps=.03 -> 30fps, fps=.015 -> ~60fps
            """ Runs the data capture continuously. Displays frame and frame_opt
                in a window. Spacebar quits.
            """
            # Init camera
            self._camera = cv2.VideoCapture(CAMERA_ID)

            # Capture frames from camera and output the results
            while True:
                self._update()
                cv2.imshow('frame', self.frame)
                if cv2.waitKey(1) == 32:
                    break  # spacebar quits
                sleep(fps)
            cv2.destroyAllWindows()
            self._camera.release()


if __name__ == '__main__':
    # Init finder object
    finder = Finder()

    # Run and display output
    finder.run()
