import cv2
print(cv2.__version__)

import numpy as np
from urllib.request import urlopen
req = urlopen("http://www.vgies.com/downloads/robocup.png")
arr = np.asarray(bytearray(req.read()), dtype=np.uint8)
img = cv2.imdecode(arr, -1)
##cv2.imshow("RoboCup␣image", img)
##cv2.waitKey(0)

B, G, R = cv2.split(img)
cv2.imshow("original", img)
cv2.waitKey(0)
##cv2.imshow("blue", B)
##cv2.waitKey(0)
##cv2.imshow("Green", G)
##cv2.waitKey(0)
##cv2.imshow("Red", R)
##cv2.waitKey(0)

imagehsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
H, S, V = cv2.split(imagehsv)
cv2.imshow("Hue", H)
cv2.waitKey(0)
cv2.imshow("Saturation", S)
cv2.waitKey(0)
cv2.imshow("Value", V)
cv2.waitKey(0)
