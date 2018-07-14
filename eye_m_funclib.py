""" eye_m_lib.py - Class and function lib.
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import MySQLdb

# Constants
MYSQL_HOST = '127.0.0.1'
MYSQL_DB = 'eye_m'
MYSQL_USER = 'root'
MYSQL_PASS = 'sqldev'


#############
#  CLASSES  #
#############


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
    # TODO: optimal image opt
    # alpha = 1.0        # Frame contrast applied
    # beta = 0           # Frame brightness applied
    # gamma = 1          # Frame gamma correction applied
    # gamma_tbl = None   # Current gamma correction table

    # kern = np.ones((5, 5), np.uint8)   # frame transform kernal
    # frame = cv2.erode(frame, kern, iterations=1)
    # frame = cv2.dilate(frame, kern, iterations=1)

    # CLAHE optimize # Pretty good?
    # clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
    # frame = clahe.apply(frame)

    # cv2 optimize
    # frame = cv2.equalizeHist(frame)

    # Histogram analysis
    # hist, _ = np.histogram(frame.flatten(), 256, [0, 256])
    # cdf = hist.cumsum()
    # cdf_normalized = cdf * hist.max() / cdf.max()
    # cdf_m = np.ma.masked_equal(cdf, 0)
    # cdf_m = (cdf_m - cdf_m.min()) * 255 / (cdf_m.max() - cdf_m.min())
    # cdf = np.ma.filled(cdf_m, 0).astype('uint8')
    pass


##################
# MISC FUNCTIONS #
##################

def check_dependencies():
    """ Returns true iff all dependencies installed.
    """
    # TODO: check_dependencies()
    
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
