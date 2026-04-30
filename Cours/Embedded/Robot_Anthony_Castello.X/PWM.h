
#ifndef PMW_H
#define	PMW_H
#define MOTEUR_DROIT 0
#define MOTEUR_GAUCHE 1


//void PWMSetSpeed(int, float);
void InitPWM(void);
void PWMUpdateSpeed(void);
void PWMSetSpeedConsignePolaire(float vitesseLineaire, float vitesseAngulaire);

#endif	/* PMW_H */

