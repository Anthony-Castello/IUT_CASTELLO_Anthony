#include "robot.h"
#include "asservissement.h"
#include "UART.h"
#include "UART_Protocol.h"
#include "Utilitises.h"
#include "Toolbox.h"
#include "QEI.h"
#include "PWM.h"

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
    double erreurProportionnelle = LimitToInterval(erreur, -PidCorr->erreurProportionelleMax/PidCorr->Kp, PidCorr->erreurProportionelleMax/PidCorr->Kp );
    PidCorr->corrP = PidCorr->Kp * erreurProportionnelle;
    PidCorr->erreurIntegrale += erreur/FREQ_ECH_QEI;
    PidCorr->erreurIntegrale = LimitToInterval(PidCorr->erreurIntegrale, -PidCorr->erreurIntegraleMax/PidCorr->Ki, PidCorr->erreurIntegraleMax/PidCorr->Ki);
    PidCorr->corrI = PidCorr ->Ki * PidCorr -> erreurIntegrale;
    double erreurDerivee = (erreur - PidCorr->epsilon_1) * FREQ_ECH_QEI;
    double deriveeBornee = LimitToInterval(erreurDerivee, -PidCorr->erreurDeriveeMax / PidCorr->Kd, PidCorr->erreurDeriveeMax / PidCorr->Kd);
    PidCorr->epsilon_1 = erreur;
    PidCorr->corrD = deriveeBornee * PidCorr->Kd;
    return PidCorr->corrP + PidCorr->corrI + PidCorr->corrD;
}

void UpdateAsservissement() {
    robotState.PidX.erreur = robotState.ConsigneLineaire - robotState.vitesseLineaireFromOdometry;
    //robotState.PidX.erreur = 0;
    robotState.PidTheta.erreur = robotState.ConsigneAngulaire - robotState.vitesseAngulaireFromOdometry;
    robotState.CorrectionVitesseLineaire = Correcteur(&robotState.PidX, robotState.PidX.erreur);
    robotState.CorrectionVitesseAngulaire = Correcteur(&robotState.PidTheta, robotState.PidTheta.erreur);
    PWMSetSpeedConsignePolaire(robotState.CorrectionVitesseLineaire, robotState.CorrectionVitesseAngulaire);
}
