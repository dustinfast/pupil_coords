""" eye_m_lib.py - Class and function lib.
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

# import MySQLdb

# # Constants (TODO: Move to config)
# MYSQL_HOST = '127.0.0.1'
# MYSQL_DB   = 'eye_m'
# MYSQL_USER = 'root'
# MYSQL_PASS = 'sqldev'


#############
#  CLASSES  #
#############
# TODO: Move these to dfstruct
class Stack:
    """ A stack data structure.
    """

    def __init__(self):
        self.data = []

    def isEmpty(self):
        return len(self.data) == 0

    def push(self, p):
        self.data.append(p)

    def pop(self):
        return self.data.pop()


class Queue:
    """ A Queue data structure supporting reset, push pop, shove, and peek.
    """
    def __init__(self, maxsize=None):
        self.items = []              # Queue container
        self.maxsize = maxsize      # Max number of elements in queue

        # Validate maxsize and populate with defaultdata
        if maxsize and maxsize < 0:
                raise Exception('Invalid maxsize parameter.')        

    def reset(self):
        """ Clears contents of queue.
        """
        self.items = []

    def push(self, element):
        """ Adds an item to the right side of self.items.
        """
        if not self.is_full():
            self.items.append(element)
        else:
            raise Exception('Attempted to push element to a full queue.')

    def shove(self, element):
        """ Adds an item to the right side of self.items. If list was already
        full, make room for it by removing the oldest element.
        """
        if self.is_full():
            self.pop()
        self.items.append(element)

    def pop(self):
        """ Removes item from left side of self.items and returns it.
        """
        if self.is_empty():
            raise Exception('Attempted to pop element from an empty queue.')

        d = self.items[0]
        self.items = self.items[1:]
        return d

    def peek(self, n=0):
        """ Returns the nth queue element and leaves the queue unchanged.
        """
        if self.item_count() < n + 1:
            raise Exception('Attempted to peek at an out of bounds position.')
        return self.items[n]

    def is_empty(self):
        """ Returns true iff self.items is empty.
        """
        return self.item_count() == 0

    def is_full(self):
        """ Returns true iff queue is at max capacity.
        """
        return self.maxsize and self.item_count() >= self.maxsize

    def item_count(self):
        return len(self.items)

    def get_list(self):
        """ Returns the queue contents as a new list.
        """
        lst = []
        for d in self.items:
            lst.append(d)
        return lst

#############
#  IMG LIB  #
#############

def img_crop(frame, x, y, w, z):
    """ Given an image frame and x, y, w, z crop coords, returns cropped img.
    """
    return frame[y:y + z, x:x + w]  # [Y1:Y2, X1:X2]

def img_find_optimization(self, mask=None):
    """ Given an image and lighting conds, returns, finds optimal optimzaton.
    """
    # alpha = 1.0        # Frame contrast applied
    # beta = 0           # Frame brightness applied
    # gamma = 1          # Frame gamma correction applied
    # gamma_tbl = None   # Current gamma correction table

    # kern = np.ones((5, 5), np.uint8)   # frame transform kernal
    # frame = cv2.erode(frame, kern, iterations=1)
    # frame = cv2.dilate(frame, kern, iterations=1)

    # CLAHE optimize # Better with varying
    # clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    # frame = clahe.apply(frame)

    # cv2 optimize
    # frame = cv2.equalizeHist(frame)
    pass


##################
# MISC FUNCTIONS #
##################

def check_dependencies():
    """ Returns true iff all dependencies installed.
    """
    # TODO
    
    return True

def get_sqlconn():
    """ Based on defined MYSQL constants, returns:
            conn    - db connection object
            cur        - db cursor object
    """
    conn = MySQLdb.connect(host=MYSQL_HOST,
                           user=MYSQL_USER,  
                           passwd=MYSQL_PASS,
                           db=MYSQL_DB)
    cur = conn.cursor()

    return conn, cur
