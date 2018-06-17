# eye_m 
eye_m uses a machine learning alogoritm to learn to associate 
eye movement with mouse movement and clicks. After sufficient
learning, the mouse can be moved and "clicked" with the eye. 

Eye tracking
	(Implementation in progress) - Uses Pygaze's libwebcam
	(https://github.com/esdalmaijer/PyGaze/)

Learning is accomplished via an ANN that anazlyzes ..
	(Implementation in progress)

# Usage
	python eye_m.py 
		-h	: Displays command line arguments

# Dependencies
sudo apt-get install opencv-python

On windows w/ubuntu, install webcam via:
	git clone git://linuxtv.org/media_build.git
	cd media_build
	./build
	sudo make install 

	then - 

pip install numpy
pip install mysqlclient


http://scikit-learn.org/stable/auto_examples/classification/plot_classifier_comparison.html#sphx-glr-auto-examples-classification-plot-classifier-comparison-py

https://downloadcenter.intel.com/download/25044/Intel-RealSense-Depth-Camera-Manager

http://videocapture.sourceforge.net/
		
http://www.pygaze.org/2015/06/webcam-eye-tracker/

https://automatetheboringstuff.com/chapter18/
https://stackoverflow.com/questions/25848951/python-get-mouse-x-y-position-on-click


https://askubuntu.com/questions/935815/microsoft-surface-pro-3-camera-not-working-with-ubuntu-14-04
https://help.ubuntu.com/community/Webcam#Identifying_Your_Webcam
http://www.ideasonboard.org/uvc/
https://stackoverflow.com/questions/6624672/how-to-use-the-win32gui-module-with-python
https://stackoverflow.com/questions/50454652/where-to-get-linux-headers-4-4-0-17134-microsoft-for-intel-icc-compiler