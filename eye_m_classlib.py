""" Class library for eye_m.py """

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import multiprocessing as mp

class Queue:
    """ A Queue data structure.
        Exposes reset, push, pop, shove, cut, peek, top, is_empty, item_count, 
        and get_items.
    """
    def __init__(self, maxsize=None):
        self.items = []             # Container
        self.maxsize = maxsize      # Max size of self.items

        # Validate maxsize and populate with defaultdata
        if maxsize and maxsize < 0:
                raise Exception('Invalid maxsize parameter.')

    def reset(self):
        """ Clear/reset queue items.
        """
        self.items = []

    def push(self, item):
        """ Adds an item to the back of queue.
        """
        if self.is_full():
            raise Exception('Attempted to push item to a full queue.')
        self.items.append(item)

    def shove(self, item):
        """ Adds an item to the back of queue. If queue already full, makes
            room for it by removing the item at front. If an item is removed
            in this way, is returned.
        """
        removed = None
        if self.is_full():
            removed = self.pop()
        self.items.append(item)
        return removed

    def cut(self, n, item):
        """ Inserts an item at the nth position from queue front. Existing
            items are moved back to accomodate it.
        """
        if self.item_count() < n + 1:
            raise Exception('Attempted to cut at an out of bounds position.')
        if self.item_count1 >= self.maxsize:
            raise Exception('Attempted to cut into a full queue.')
        self.items = self.items[:n] + item + self.items[n:]  # TODO: Test cut

    def pop(self):
        """ Removes front item from queue and returns it.
        """
        if self.is_empty():
            raise Exception('Attempted to pop from an empty queue.')
        d = self.items[0]
        self.items = self.items[1:]
        return d

    def peek(self, n=0):  # TODO: Test safety of peek
        """ Returns the nth item from queue front. Leaves queue unchanged.
        """
        if self.item_count() < n + 1:
            raise Exception('Attempted to peek at an out of bounds position.')
        if self.us_empty():
            raise Exception('Attempted to peek at an empty.')
        return self.items[n]

    def is_empty(self):
        """ Returns true iff queue empty.
        """
        return self.item_count() == 0

    def is_full(self):
        """ Returns true iff queue at max capacity.
        """
        return self.maxsize and self.item_count() >= self.maxsize

    def item_count(self):
        return len(self.items)

    def get_items(self):
        """ Returns queue contents as a new list.
        """
        return [item for item in self.items]  # TODO: Test get_items


class Face(object):
    """ The user's pupil and nose coordinates and other related information.
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

    def as_points(self):
        """ Returns a list of two-tuples containing the facial feature coords:
            [ (l_eye), (r_eye), (nose) ]
        """
        pts = []
        pts.append((self.l_eye_x, self.l_eye_y))
        pts.append((self.r_eye_x, self.r_eye_y))
        pts.append((self.nose_x, self.nose_y))
        return pts

    def as_dict(self):
        """ Returns a dict of the facial feature coords.
        """
        dic = {}
        dic['l_eye_x'] = self.l_eye_x
        dic['l_eye_y'] = self.l_eye_y
        dic['nose_x'] = self.nose_x
        dic['nosey'] = self.nosy_y
        return dic
