/* 
 * File:   asservissement.h
 * Author: E306_PC1
 *
 * Created on 10 avril 2026, 10:39
 */

#ifndef ASSERVISSEMENT_H
#define	ASSERVISSEMENT_H

#ifdef	__cplusplus
extern "C" {
#endif

typedef struct _PidCorrector {
        float Kp;
        float Ki;
        float Kd;
        float erreurProportionelleMax;
        float erreurIntegraleMax;
        float erreurDeriveeMax;
        float erreurIntegrale;
        float epsilon_1;
        float erreur;
        //For Debug only
        float corrP;
        float corrI;
        float corrD;

        
    } PidCorrector;
   
//volatile PidCorrector PID_X;
//volatile PidCorrector PID_Theta;
            
//void InitPID_X();
//void InitPID_Theta();
void SetupPidValues(unsigned char msgPayload[]);
void SetupPidAsservissement(volatile PidCorrector* PidCorr, float Kp, float Ki, float Kd, float proportionelleMax, float integralMax, float deriveeMax);
void SendPidValues();
double Correcteur(volatile PidCorrector* PidCorr, double erreur);
void UpdateAsservissement();
#ifdef	__cplusplus
}
#endif

#endif	/* ASSERVISSEMENT_H */

