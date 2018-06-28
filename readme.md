# eye_m

eye_m uses a machine learning alogoritm to associate eye movement with mouse coordinates and clicks.
After sufficient learning, the mouse can be moved and "clicked" with the eyes.
Currently requires two eyes to function.

Eye tracking is accomplished with openCV (see eye_m_find.py)

Learning is accomplished via an ANN that anazlyzes ..
    (Implementation in progress)

## Usage

    python eye_m.py
        -h    : Displays command line arguments

## Dependencies
see setup.py


DB:
event: click or keypress
device orientation: x, y, z
orig img size: x, y
face rectangle: x, y
nose: x, y
ears: x, y
pupils: x, y
pupil shapes: r, l


http://scikit-learn.org/stable/auto_examples/classification/plot_classifier_comparison.html#sphx-glr-auto-examples-classification-plot-classifier-comparison-py
