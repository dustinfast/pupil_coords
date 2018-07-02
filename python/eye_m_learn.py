""" A learning module. """

__author__ = "Dustin Fast (dustin.fast@outlook.com)"


import multiprocessing as mp
# from eye_m_lib import Queue
# from eye_m_classlib import Face


class Learner(object):
    """ TODO
        Exposes give_data()
    """
    def __init__(self, log_data=None):
        self._ann = None            # TODO ANN, shape, pre-trained model, etc.
        self.log_data = log_data    # Function to send data dict to for logging
        # maxrows = int(data_hz * (600 * str(data_hz)[::-1].find('.')))        
        # self.data_rows = Queue(maxsize=maxrows)  # TODO: aggregate learning data
        
    def _learn(self, data):
        """ Learns from the given data item (or list of data items)
            Use via give_data() only.
        """
        print('Received data: ' + str(data))

    def give_data(self, data):
        """ Accepts data item (in dictionary form), or a list of data items,
            and starts the learn process on it.
        """
        self.learn = mp.Process(target=self._learn, args=(data,))
        if self.log_data:
            self.log_data(data)

if __name__ == 'main':
    pass


        
    
