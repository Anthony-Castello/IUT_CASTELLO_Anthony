#include "robot.h"
#include "asservissement.h"
#include "UART.h"
#include "UART_Protocol.h"
#include "Utilitises.h"
#include "Toolbox.h"
#include "QEI.h"
#include "PWM.h"
#include "math.h"

void SetupPidValues(unsigned char msgPayload[]) {
    if (msgPayload[0] == 0)
        SetupPidAsservissement(&robotState.PidX, getFloatFromBytes(msgPayload, 1), getFloatFromBytes(msgPayload, 5), getFloatFromBytes(msgPayload, 9), getFloatFromBytes(msgPayload, 13), getFloatFromBytes(msgPayload, 17), getFloatFromBytes(msgPayload, 21));
    if (msgPayload[0] == 1)
        SetupPidAsservissement(&robotState.PidTheta, getFloatFromBytes(msgPayload, 1), getFloatFromBytes(msgPayload, 5), getFloatFromBytes(msgPayload, 9), getFloatFromBytes(msgPayload, 13), getFloatFromBytes(msgPayload, 17), getFloatFromBytes(msgPayload, 21));

}

void SetupPidAsservissement(volatile PidCorrector* PidCorr, float Kp, float Ki, float Kd, float proportionelleMax, float integralMax, float deriveeMax) {
    PidCorr->Kp = Kp;
    PidCorr->erreurProportionelleMax = proportionelleMax; //On limite la correction due au Kp
    PidCorr->Ki = Ki;
    PidCorr->erreurIntegraleMax = integralMax; //On limite la correction due au Ki
    PidCorr->Kd = Kd;
    PidCorr->erreurDeriveeMax = deriveeMax;
}

void SendPidValues() {
    unsigned char positionPayload[80];
    getBytesFromFloat(positionPayload, 0, robotState.PidX.Kp);
    getBytesFromFloat(positionPayload, 4, robotState.PidX.Ki);
    getBytesFromFloat(positionPayload, 8, robotState.PidX.Kd);
    getBytesFromFloat(positionPayload, 12, robotState.PidX.erreurProportionelleMax);
    getBytesFromFloat(positionPayload, 16, robotState.PidX.erreurIntegraleMax);
    getBytesFromFloat(positionPayload, 20, robotState.PidX.erreurDeriveeMax);
    getBytesFromFloat(positionPayload, 24, robotState.PidTheta.Kp);
    getBytesFromFloat(positionPayload, 28, robotState.PidTheta.Ki);
    getBytesFromFloat(positionPayload, 32, robotState.PidTheta.Kd);
    getBytesFromFloat(positionPayload, 36, robotState.PidTheta.erreurProportionelleMax);
    getBytesFromFloat(positionPayload, 40, robotState.PidTheta.erreurIntegraleMax);
    getBytesFromFloat(positionPayload, 44, robotState.PidTheta.erreurDeriveeMax);
    getBytesFromFloat(positionPayload, 48, robotState.PidX.erreur);
    getBytesFromFloat(positionPayload, 52, robotState.PidX.corrP);
    getBytesFromFloat(positionPayload, 56, robotState.PidX.corrI);
    getBytesFromFloat(positionPayload, 60, robotState.PidX.corrD);
    getBytesFromFloat(positionPayload, 64, robotState.PidTheta.erreur);
    getBytesFromFloat(positionPayload, 68, robotState.PidTheta.corrP);
    getBytesFromFloat(positionPayload, 72, robotState.PidTheta.corrI);
    getBytesFromFloat(positionPayload, 76, robotState.PidTheta.corrD);
    UartEncodeAndSendMessage(0x0061, 80, positionPayload);
}

double Correcteur(volatile PidCorrector* PidCorr, double erreur) {
    PidCorr->erreur = erreur;
    //double erreurProportionnelle = LimitToInterval(erreur, -PidCorr->erreurProportionelleMax/PidCorr->Kp, PidCorr->erreurProportionelleMax/PidCorr->Kp );
    PidCorr->corrP = LimitToInterval(PidCorr->Kp * erreur,
            -PidCorr->erreurProportionelleMax,
            PidCorr->erreurProportionelleMax);

    if (PidCorr->Ki != 0) {
        PidCorr->erreurIntegrale += erreur / FREQ_ECH_QEI;
        double lim = PidCorr->erreurIntegraleMax / PidCorr->Ki;
        PidCorr->erreurIntegrale = LimitToInterval(PidCorr->erreurIntegrale, -lim, lim);
        PidCorr->corrI = PidCorr->Ki * PidCorr->erreurIntegrale;
    } else {
        PidCorr->erreurIntegrale = 0;
        PidCorr->corrI = 0;
    }

    // Dérivé
    double erreurDerivee = (erreur - PidCorr->epsilon_1) * FREQ_ECH_QEI;
    PidCorr->epsilon_1 = erreur;
    PidCorr->corrD = LimitToInterval(PidCorr->Kd * erreurDerivee,
            -PidCorr->erreurDeriveeMax,
            PidCorr->erreurDeriveeMax);

    return PidCorr->corrP + PidCorr->corrI + PidCorr->corrD;
}

void UpdateAsservissement() {
    // 1. Calcul de l'erreur linéaire (distance entre le ghost et l'odométrie, projetée dans le repère du robot)
    double dx = robotState.ghost.x - robotState.xPosFromOdometry;
    double dy = robotState.ghost.y - robotState.yPosFromOdometry;
    
//     Erreur longitudinale (dans la direction du robot)
    robotState.PidX.erreur = dx * cos(robotState.angleRadianFromOdometry)
                           + dy * sin(robotState.angleRadianFromOdometry);
    // 2. Calcul de l'erreur angulaire par rapport à l'angle ACTUEL du ghost (et non le waypoint final)
    robotState.PidTheta.erreur = ModuloByAngle(robotState.angleRadianFromOdometry, robotState.ghost.theta_ghost) - robotState.angleRadianFromOdometry;

    // 3. Calcul des corrections PID (feedback)
    robotState.CorrectionVitesseLineaire = Correcteur(&robotState.PidX, robotState.PidX.erreur);
    robotState.CorrectionVitesseAngulaire = Correcteur(&robotState.PidTheta, robotState.PidTheta.erreur);

    // 4. Application de la consigne avec Feedforward (vitesse du ghost + correction PID)
    double consigneLineaire  = robotState.CorrectionVitesseLineaire  + robotState.ghost.v_lineaire;
    double consigneAngulaire = robotState.CorrectionVitesseAngulaire + robotState.ghost.v_theta;

    PWMSetSpeedConsignePolaire(consigneLineaire, consigneAngulaire);
}
