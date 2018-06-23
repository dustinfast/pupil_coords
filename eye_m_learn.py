""" A learning module. 
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import time
import threading
import multiprocessing as mp
from eye_m_lib import Queue
from eye_m_lib import get_sqlconn
from eye_m_find import Face
from eye_m_find import Finder

class Learner(object):
    """ TODO
    """

    def __init__(self):
        """ TODO
        """
        self.ann = None  # TODO ANN, shape, pre-trained model, etc.
        # maxrows = int(data_hz * (600 * str(data_hz)[::-1].find('.')))        
        # self.data_rows = Queue(maxsize=maxrows)  # TODO: aggregate learning data
        
    def _learn(self, data):
        """ Learns from the given data item (or list of data items)
            Use via give_data() only.
        """
        print('Received data: ' + str(data))

    def give_data(data):
        """ Accepts a data item (or list of items) and starts the learn process.
        """
        self.learn = mp.Process(target=self._learn, args=(data,))


        
    
