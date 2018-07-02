import cv2

cam = cv2.VideoCapture(-1)

while True:
    _, img = cam.read()
    cv2.imshow('cam', img)
    if cv2.waitKey(1) == 27:
        break  # esc to quit

cv2.destroyAllWindows()