# eye_m 
eye_m uses a machine learning alogoritm to associate eye movement with mouse coordinates and clicks.
After sufficient learning, the mouse can be moved and "clicked" with the eyes.
Currently requires two eyes to function.

Eye tracking is accomplished with openCV (see eye_m_find.py)

Learning is accomplished via an ANN that anazlyzes ..
    (Implementation in progress)

# Usage
    python eye_m.py
        -h    : Displays command line arguments

# Dependencies
sudo apt-get install opencv-python
pip install numpy
pip install mysqlclient

http://scikit-learn.org/stable/auto_examples/classification/plot_classifier_comparison.html#sphx-glr-auto-examples-classification-plot-classifier-comparison-py
