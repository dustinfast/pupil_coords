# eye_m

eye_m is a machine vision and learning system that attempts to model the relation of the users gaze with the coordinates of mouse clicks. After sufficient learning, the user can perform mouse clicks at a desired screen position by simply looking at the location and entering some convenient keyboard shortcut.

## Information Collected

eye_m does not currently log any information, however it accesses the user's webcam (including infrared, if available) and device orientation sensors to perform learning based onthe following information:  
Coordinates and shape of the user's left and right pupil and iris (the shape is useful because the cameras perceives the iris as more of an oval when we look to the right of the screen).
Coordinates of the user's nose.
Distance from camera to face (if depth perception available).
Device orientation (x, y, z) in space.
Mouse click coordinates.

## Development Status

**Platform: Currently experimenting with Python and C#/UWP implementations. C# is winning.
  
**Machine vision components:
The application currently identifies mouseclicks and captures the user's gaze attributes. The UWP implementation uses Microsoft's Windows.Media.FaceAnalysis and significantly outperforms the OpenCV Cascase Classifier used in the Python implementation.

**Data Capture: 
  
**Learning:
The application logs key "gaze attributes" such as nose, ear, and pupil locations, pupil "shape", and device orientation each time the mouse is clicked.
  
After sufficient learning, the mouse can be moved and "clicked" with the eyes.
Currently requires two eyes to function.

Eye tracking is accomplished with openCV (see eye_m_find.py)

Learning is accomplished via an ANN that anazlyzes ..
    (Implementation in progress)


## Usage

    python eye_m.py
        -h    : Displays command line arguments

## Dependencies

See setup.py


## Data Fields

event: click or keypress
device orientation: x, y, z
orig img size: x, y
face rectangle: x, y
nose: x, y
ears: x, y
pupils: x, y
pupil shapes: r, l

## Wish list

"Define action" mode: action-tag data for +-x minutes.
Keylearning: In addition to modeling gaze on mouse click, also learn by analyzing gaze information as the user types.
AutoCorrect: Learn a user-defined facial gesture to asynchronously autocorrect the spelling of their last misspelled word.
