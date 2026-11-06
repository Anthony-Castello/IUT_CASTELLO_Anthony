import cv2
import numpy as np
import pyjevois

if pyjevois.pro:
    import libjevoispro as jevois
else:
    import libjevois as jevois

class JauneRouge:
    def __init__(self):
        # Seuils HSV : H entre 0 et 179 ; S et V entre 0 et 255.
        self.jaune_min = np.array([20, 100, 80], dtype=np.uint8)
        self.jaune_max = np.array([35, 255, 255], dtype=np.uint8)

        # Le rouge occupe deux plages de teinte.
        self.rouge1_min = np.array([0, 170, 80], dtype=np.uint8)
        self.rouge1_max = np.array([10, 255, 255], dtype=np.uint8)
        self.rouge2_min = np.array([170, 170, 80], dtype=np.uint8)
        self.rouge2_max = np.array([179, 255, 255], dtype=np.uint8)

        # Ignore les contours dont la surface est trop petite.
        self.surface_min = 300
        self.noyau = np.ones((3, 3), dtype=np.uint8)

    def detecter(self, image):
        hsv = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)

        jaune = cv2.inRange(hsv, self.jaune_min, self.jaune_max)
        rouge = cv2.bitwise_or(
            cv2.inRange(hsv, self.rouge1_min, self.rouge1_max),
            cv2.inRange(hsv, self.rouge2_min, self.rouge2_max),
        )

        resultat = image.copy()
        nombres = {"JAUNE": 0, "ROUGE": 0}

        for nom, masque, couleur in (
            ("JAUNE", jaune, (0, 255, 255)),
            ("ROUGE", rouge, (0, 0, 255)),
        ):
            # Supprime les petits parasites et comble les petits trous.
            masque = cv2.morphologyEx(
                masque, cv2.MORPH_OPEN, self.noyau
            )
            masque = cv2.morphologyEx(
                masque, cv2.MORPH_CLOSE, self.noyau
            )

            contours = cv2.findContours(
                masque, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE
            )[-2]

            for contour in contours:
                if cv2.contourArea(contour) < self.surface_min:
                    continue

                nombres[nom] += 1
                x, y, w, h = cv2.boundingRect(contour)

                cv2.rectangle(
                    resultat, (x, y), (x + w - 1, y + h - 1),
                    couleur, 2
                )
                cv2.putText(
                    resultat, nom, (x, max(15, y - 5)),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, couleur, 2
                )

        cv2.putText(
            resultat,
            "MOI J'AIME LE FROMAGE DANS LA QUICHE !!!! Jaune: {}  Rouge: {}".format(
                nombres["JAUNE"], nombres["ROUGE"]
            ),
            (5, 20), cv2.FONT_HERSHEY_SIMPLEX,
            0.5, (255, 255, 255), 1
        )
        return resultat

    # Affichage dans l'interface graphique JeVois-Pro.
    def processGUI(self, inframe, helper):
        helper.startFrame()

        image = inframe.getCvBGRp()
        resultat = self.detecter(image)
        
        affichage = cv2.cvtColor(resultat, cv2.COLOR_BGR2RGBA)
        
        helper.drawImage(
            "detections",
            affichage,
            True,  # Image au format BGR
            False,  # noalias
            False   # isoverlay
        )

        helper.endFrame()

    # Sortie video USB si ce mode est disponible.
    def process(self, inframe, outframe):
        image = inframe.getCvBGR()
        resultat = self.detecter(image)
        outframe.sendCv(self.detecter(image))


