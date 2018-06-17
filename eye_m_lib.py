""" eye_m_lib.py - Class and function lib for eye_m
"""

__author__ = "Dustin Fast (dustin.fast@outlook.com)"

import MySQLdb

# Constants (TODO: Move to config)
MYSQL_HOST     = '127.0.0.1'
MYSQL_DB     = 'eye_m'
MYSQL_USER     = 'root'
MYSQL_PASS     = 'sqldev'


#############
#  CLASSES  #
#############

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
    """ A Queue data structure.
    """

    def __init__(self, maxsize=None):
        self.data = []              # Queue container
        self.maxsize = maxsize      # Max number of elements in queue

        # Validate params
        if maxsize and maxsize < 0:
                raise Exception('Invalid maxsize parameter.')

    def clear(self):
        """ Clears contents of queue.
        """
        self.data = []

    def push(self, element):
        """ Adds an item to the right side of self.data.
        """
        if not self.is_full():
            self.data.append(element)
        else:
            raise Exception('Attempted to push element to a full queue.')

    def shove(self, element):
        """ Adds an item to the right side of self.data. If list was already
        full, make room for it by removing the oldest element.
        """
        if self.is_full():
            self.pop()
        self.data.append(element)

    def pop(self):
        """ Removes item from left side of self.data and returns it.
        """
        if self.is_empty():
            raise Exception('Attempted to pop element from an empty queue.')

        d = self.data[0]
        self.data = self.data[1:]
        return d

    def peek(self, n=0):
        """ Returns the nth queue element and leaves the queue unchanged.
        """
        if self.count() < n + 1:
            raise Exception('Attempted to peek at an out of bounds position.')
        return self.data[n]

    def is_empty(self):
        """ Returns true iff self.data is empty.
        """
        return self.count() == 0

    def is_full(self):
        """ Returns true iff queue is at max capacity.
        """
        return self.maxsize and self.count() >= self.maxsize

    def count(self):
        return len(self.data)

    def get_list(self):
        """ Returns the queue contents as a new list.
        """
        lst = []
        for d in self.data:
            lst.append(d)
        return lst

#############
# FUNCTIONS #
#############

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
                           db=MYSQL_DB )
    cur = conn.cursor()

    return conn, cur