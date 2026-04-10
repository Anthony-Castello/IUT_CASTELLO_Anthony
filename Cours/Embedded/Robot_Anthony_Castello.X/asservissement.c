#include "robot.h"
#include "asservissement.h"
#include "UART.h"
#include "UART_Protocol.h"
#include "Utilitises.h"
#include "Toolbox.h"

//void InitPID() {
//    PID_X.Kp = 0;
//    PID_X.Ki = 0;
//    PID_X.Kd = 0;
//    PID_X.erreurIntegraleMax = 0;
//    PID_X.erreurDeriveeMax = 0;
//    PID_X.erreurProportionelleMax = 0;
//}
//
//void InitPID_Theta() {
//    PID_Theta.Kd = 0;
//    PID_Theta.Ki = 0;
//    PID_Theta.Kd = 0;
//    PID_Theta.erreurIntegraleMax = 0;
//    PID_Theta.erreurDeriveeMax = 0;
//    PID_Theta.erreurProportionelleMax = 0;
//}

void SetupPidValues(unsigned char msgPayload[]) {
    float kp = getFloatFromBytes(msgPayload, 1);
    if (msgPayload[0] == 0)
        SetupPidAsservissement(&robotState.PidX, kp, 0, 0, 0, 0, 0);
    if (msgPayload[0] == 1)
        SetupPidAsservissement(&robotState.PidTheta, getFloatFromBytes(msgPayload, 1), 0, 0, 0, 0, 0);

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
    unsigned char positionPayload[48];
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
    UartEncodeAndSendMessage(0x0061, 48, positionPayload);
}
