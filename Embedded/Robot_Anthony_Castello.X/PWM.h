
#ifndef PMW_H
#define	PMW_H
#define MOTEUR_DROIT 0
#define MOTEUR_GAUCHE 1

float acceleration = 2;
//void PWMSetSpeed(int, float);
void InitPWM(void);
void PWMUpdateSpeed(void);
void PWMSetSpeedConsigne(float, int);

#endif	/* PMW_H */

