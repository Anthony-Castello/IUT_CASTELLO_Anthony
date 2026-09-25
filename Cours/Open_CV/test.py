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
##cv2.imshow("Hue", H)
##cv2.waitKey(0)
##cv2.imshow("Saturation", S)
##cv2.waitKey(0)
##cv2.imshow("Value", V)
##cv2.waitKey(0)

#Definition des limites basses et hautes de la couleur jaune en HSV
#A noter que le jaune se situe vers les 25 degres dans la roue de couleur HSV en H
lower_yellow = np.array([20, 100, 100])
upper_yellow = np.array([30,255,255])
#Masquage de l’image HSV pour ne garder que les zones jaunes
imagemaskyellow = cv2.inRange(imagehsv, lower_yellow, upper_yellow)
yellow_color = (0, 255, 255)
yellow_uniform = np.zeros_like(img)
yellow_uniform[imagemaskyellow > 0] = yellow_color
##cv2.imshow("Jaune Uniforme", yellow_uniform)
##cv2.waitKey(0)


lower_green = np.array([45, 50, 50])
upper_green = np.array([75,255,255])
#Masquage de l’image HSV pour ne garder que les zones vertes
imagemaskgreen = cv2.inRange(imagehsv, lower_green, upper_green)
# --- Remplacement du blanc par du vert uniforme ---
# Définition de la couleur en BGR (ici vert pur : Bleu=0, Vert=255, Rouge=0)
green_color = (0, 255, 0)
green_uniform = np.zeros_like(img)
green_uniform[imagemaskgreen > 0] = green_color
##cv2.imshow("Vert Uniforme", green_uniform)
##cv2.waitKey(0)

lower_sombre = np.array([0, 0, 0])
upper_sombre = np.array([255,100,100])
#Masquage de l’image HSV pour ne garder que les zones vertes
imagemasksombre = cv2.inRange(imagehsv, lower_sombre, upper_sombre)
##cv2.imshow("Sombre (en bleu) Uniforme", imagemasksombre)
##cv2.waitKey(0)

blue_color = (255, 0, 0)
blue_uniform = np.zeros_like(img)
blue_uniform[imagemasksombre > 0] = blue_color
##cv2.imshow("Sombre (en bleu) Uniforme", blue_uniform)
##cv2.waitKey(0)

yellow_layer = cv2.bitwise_and(yellow_uniform, yellow_uniform, mask=imagemaskyellow)
green_layer  = cv2.bitwise_and(green_uniform, green_uniform, mask=imagemaskgreen)
blue_layer   = cv2.bitwise_and(blue_uniform, blue_uniform, mask=imagemasksombre)

# --- Regroupement des 3 calques ---
# Pour combiner des zones colorées sur fond noir, l'opération logique est un OR
result = cv2.bitwise_or(yellow_layer, green_layer)
result = cv2.bitwise_or(result, blue_layer)

##cv2.imshow("Fusion des 3 couleurs", result)
##cv2.waitKey(0)


#TRANSFORMATIONS MANUELLES D’UNE IMAGE
height = img.shape[0]
width = img.shape[1]
channels = img.shape[2]
imgTransform = img
niveau = 1/width
for x in range(0, (int)(width)): ##Parcours sur la largeur de l'image
    for y in range (0, (int)(height)): ##Parcours sur la hauteur de l'image
        imgTransform[y,x][0] *= (1-niveau) ##Gère le niveau de Bleu
        imgTransform[y,x][1] *= (1-niveau) ##Gère le niveau de Vert
        imgTransform[y,x][2] *= (1-niveau) ##Gère le niveau de Rouge
    niveau += 1/width
cv2.imshow("Transformation␣manuelle␣de␣l’image", imgTransform)
cv2.waitKey(0)


