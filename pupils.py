""" pupils.py Uses OpenCV to find users 
    face via the webcam and note coordinates of pupils and nose.
    A live mode, Eyes.run() displays these coords overlaid on live output.
    An on-demand mode processes frames as requested and returns the coords.
"""
__author__ = "Dustin Fast (dustin.fast@outlook.com)"


import cv2
from time import sleep

# Cascade models for cv2 classifiers
EYE_HAAR = 'models/haarcascade_eye.xml'
NOSE_HAAR = 'models/haarcascade_nose.xml'

# Frame display txt
# FONT = cv2.FONT_HERSHEY_SIMPLEX
# LINE = cv2.LINE

# MISC
ACCURACY_DENOM = 100
CAMERA_ID = 0


class ShoveQueue:
    """ A Queue data structure with pop and shove operations.
        Exposes reset, push, pop, shove, cut, peek, top, is_empty, item_count, 
        and get_items.
    """

    def __init__(self, maxsize=None):
        self.items = []             # Container
        self.maxsize = maxsize      # Max size of self.items

        # Validate maxsize and populate with defaultdata
        if maxsize and maxsize < 0:
                raise Exception('Invalid maxsize parameter.')

    def shove(self, item):
        """ Adds an item to the back of queue. If queue already full, makes
            room for it by removing the item at front. If an item is removed
            in this way, it is returned.
        """
        removed = None
        if self.is_full():
            removed = self.pop()
        self.items.append(item)
        return removed

    def pop(self):
        """ Removes front item from queue and returns it.
        """
        if self.is_empty():
            raise Exception('Attempted to pop from an empty queue.')
        d = self.items[0]
        self.items = self.items[1:]
        return d

    def is_empty(self):
        """ Returns true iff queue empty.
        """
        return self.item_count() == 0

    def is_full(self):
        """ Returns true iff queue at max capacity.
        """
        return self.maxsize and self.item_count() >= self.maxsize

    def item_count(self):
        """ Returns the count (an int) of the number of items in the queue.
        """
        return len(self.items)


class Face(object):
    """ Representation of a face as defined by its pupil and nose coordinates.
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
        """ Returns a list of two-tuples containing the facial feature coords:
            [ (l_eye), (r_eye), (nose) ]
        """
        pts = []
        pts.append((self.l_eye_x, self.l_eye_y))
        pts.append((self.r_eye_x, self.r_eye_y))
        pts.append((self.nose_x, self.nose_y))
        return pts


class Finder(object):
    """ Eye/Pupil detection class. Uses webcam to detect via openCV.
        Supports live (run_live()) and single frame (run_single()) output.
        Detected coordinates are stored in Face object. 
    """
    def __init__(self, eye_model=EYE_HAAR, nose_model=NOSE_HAAR):
        # Classifier
        self.eye_model = cv2.CascadeClassifier(eye_model)
        self.nose_model = cv2.CascadeClassifier(nose_model)
        self._camera = None
        self._do_extras = False  # Allow no accuracy or mark_frame, for perf
        self.face = Face()       # Face object. Contains coords, etc.
        self.frame = None        # Curr camera frame

        # Last 50 results (as bools), for accuracy calculation
        self.results = ShoveQueue(maxsize=ACCURACY_DENOM)

        # Optimized frame
        self.frame_opt = None   # Optimized version of self.frame
        # self.opt_interval = 0   # TODO: Optimize for lightining conds every x frames

    def _update(self):
        """ Does an iteration of eye finding and updates self accordingly
        """
        _, self.frame = self._camera.read()
        self.frame_opt = self._optimize_frame()
        self._reset_coords()

        self.success = self._find_eyes()

        # If do_extras enabled, adjust accuracy metrics and mark frame
        if self._do_extras:
            if self.success:
                self.results.shove(True)
            else:
                self.results.shove(False)
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

        # "eyes" is a box around each eye. Approximate pupil coords by taking
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
        # accordingly.
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
        # txt = 'ID Rate: ' + str(self.accuracy()) + '%'
        # cv2.putText(self.frame, txt, (10, 20), FONT, 0.4, (0, 0, 255), 1, LINE)

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

    def run_live(self, fps=0.015):  # fps=.03 -> 30fps, fps=.015 -> ~60fps
            """ Runs the data capture continuously. Displays frame and frame_opt
                in a window. Spacebar quits.
            """
            self._camera = cv2.VideoCapture(CAMERA_ID)
            self._do_extras = True
            while True:
                self._update()
                cv2.imshow('frame', self.frame)
                cv2.imshow('optimzed frame', self.frame_opt)
                if cv2.waitKey(1) == 32:
                    break  # spacebar to quit
                sleep(fps)
            cv2.destroyAllWindows()
            self._do_extras = False
            self._camera.release()


if __name__ == '__main__':
    finder = Finder()
    finder.run_live()
