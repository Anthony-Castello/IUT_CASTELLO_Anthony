#include <stdio.h>
#include <stdlib.h>
#include <xc.h>
#include "ChipConfig.h"
#include "IO.h"
#include "timer.h"
#include "PWM.h"
#include "ADC.h"
#include "robot.h"
#include "main.h"
#include "UART.h"
#include <libpic30.h>
#include "CB_TX1.h"
#include "CB_RX1.h" 
#include "UART_Protocol.h"

unsigned int * result;
uint8_t flag_Final;
uint8_t flag_Gauche;
uint8_t flag_Ex_Gauche;
uint8_t flag_Droit;
uint8_t flag_Ex_Droit;
uint8_t flag_Centre;
unsigned char IR[5];
unsigned char etat[5];
unsigned char stateRobot;
unsigned int autoControlActivated = 0;

int main(void) {
    /***********************************************************************************************/
    //Initialisation oscillateur
    /***********************************************************************************************/
    InitOscillator();
    /***********************************************************************************************/
    // Configuration des input et output (IO)
    /***********************************************************************************************/
    InitIO();
    InitPWM();
    InitADC1();
    InitTimer1();
    InitTimer23();
    InitTimer4();
    InitUART();
    LED_BLANCHE_1 = 1;
    LED_BLEUE_1 = 1;
    LED_ORANGE_1 = 1;
    LED_ROUGE_1 = 1;
    LED_VERTE_1 = 1;
    LED_BLANCHE_2 = 1;
    LED_BLEUE_2 = 1;
    LED_ORANGE_2 = 1;
    LED_ROUGE_2 = 1;
    LED_VERTE_2 = 1;
    //unsigned char payload[] = {'Q', 'u', 'i', 'c', 'h', 'e', '!'};



    /***********************************************************************************************/
    // Boucle Principale
    /***********************************************************************************************/
    while (1) {
        //UartEncodeAndSendMessage(0x0080, sizeof(payload), payload);
        int i;
        for (i = 0; i < CB_RX1_GetDataSize(); i++) {
            unsigned char c = CB_RX1_Get();
            UartDecodeMessage(c);
            //SendMessage(&c, 1);
        }
        if (ADCIsConversionFinished() == 1) {
            ADCClearConversionFinishedFlag();
            unsigned int * result = ADCGetResult();
            float volts = ((float) result [0])* 3.3 / 4096;
            robotState.distanceTelemetreExGauche = 34 / volts - 5;
            volts = ((float) result [1])* 3.3 / 4096;
            robotState.distanceTelemetreGauche = 34 / volts - 5;
            volts = ((float) result [2])* 3.3 / 4096;
            robotState.distanceTelemetreCentre = 34 / volts - 5;
            volts = ((float) result [3])* 3.3 / 4096;
            robotState.distanceTelemetreDroit = 34 / volts - 5;
            volts = ((float) result [4])* 3.3 / 4096;
            robotState.distanceTelemetreExDroit = 34 / volts - 5;
            if (robotState.distanceTelemetreExGauche < 9)
                robotState.distanceTelemetreExGauche = 0;
            if (robotState.distanceTelemetreGauche < 9)
                robotState.distanceTelemetreGauche = 0;
            if (robotState.distanceTelemetreExDroit < 9)
                robotState.distanceTelemetreExDroit = 0;
            if (robotState.distanceTelemetreDroit < 9)
                robotState.distanceTelemetreDroit = 0;
            if (robotState.distanceTelemetreCentre < 9)
                robotState.distanceTelemetreCentre = 0;
            IR[0] = (unsigned char) robotState.distanceTelemetreExGauche;
            IR[1] = (unsigned char) robotState.distanceTelemetreGauche;
            IR[2] = (unsigned char) robotState.distanceTelemetreCentre;
            IR[3] = (unsigned char) robotState.distanceTelemetreDroit;
            IR[4] = (unsigned char) robotState.distanceTelemetreExDroit;
            UartEncodeAndSendMessage(0x0030, sizeof (IR), IR);

        }
        if (robotState.distanceTelemetreExGauche < 32) {
            LED_BLANCHE_1 = 1;
        } else {
            LED_BLANCHE_1 = 0;
        }
        if (robotState.distanceTelemetreGauche < 37) {
            LED_BLEUE_1 = 1;
        } else {
            LED_BLEUE_1 = 0;
        }
        if (robotState.distanceTelemetreCentre < 45) {
            LED_ORANGE_1 = 1;
        } else {
            LED_ORANGE_1 = 0;
        }
        if (robotState.distanceTelemetreDroit < 37) {
            LED_ROUGE_1 = 1;
        } else {
            LED_ROUGE_1 = 0;
        }
        if (robotState.distanceTelemetreExDroit < 32) {
            LED_VERTE_1 = 1;
        } else {
            LED_VERTE_1 = 0;
        }
    } // fin main
}


void OperatingSystemLoop(void) {
    switch (stateRobot) {
        case STATE_ATTENTE:
            timestamp = 0;
            PWMSetSpeedConsigne(0, MOTEUR_DROIT);
            PWMSetSpeedConsigne(0, MOTEUR_GAUCHE);
            stateRobot = STATE_ATTENTE_EN_COURS;
            etat[0] = etat [1] = etat[2] = etat[3] = 0;
            etat[4] = timestamp;
            UartEncodeAndSendMessage(0x0050, 5, etat);
        case STATE_ATTENTE_EN_COURS:
            if (autoControlActivated){
                if (timestamp > 1000)
                    stateRobot = STATE_AVANCE;
            }
            break;
        case STATE_AVANCE:
            PWMSetSpeedConsigne(30, MOTEUR_DROIT);
            PWMSetSpeedConsigne(30, MOTEUR_GAUCHE);
            stateRobot = STATE_AVANCE_EN_COURS;
            etat [1] = etat[2] = etat[3] = 0;
            etat [0] = 1;
            etat[4] = timestamp;
            UartEncodeAndSendMessage(0x0050, 5, etat);
            break;
        case STATE_AVANCE_EN_COURS:
            if (autoControlActivated)
                SetNextRobotStateInAutomaticMode();
            break;
        case STATE_TOURNE_GAUCHE:
            PWMSetSpeedConsigne(15, MOTEUR_DROIT);
            PWMSetSpeedConsigne(0, MOTEUR_GAUCHE);
            stateRobot = STATE_TOURNE_GAUCHE_EN_COURS;
            etat [1] = etat[0] = etat[3] = 0;
            etat [2] = 1;
            etat[4] = timestamp;
            UartEncodeAndSendMessage(0x0050, 5, etat);
            break;
        case STATE_TOURNE_GAUCHE_EN_COURS:
            if (autoControlActivated)
                SetNextRobotStateInAutomaticMode();
            break;
        case STATE_TOURNE_DROITE:
            PWMSetSpeedConsigne(0, MOTEUR_DROIT);
            PWMSetSpeedConsigne(15, MOTEUR_GAUCHE);
            stateRobot = STATE_TOURNE_DROITE_EN_COURS;
            etat [0] = etat[2] = etat[1] = 0;
            etat [3] = 1;
            etat[4] = timestamp;
            UartEncodeAndSendMessage(0x0050, 5, etat);
            break;
        case STATE_TOURNE_DROITE_EN_COURS:
            if (autoControlActivated)
                SetNextRobotStateInAutomaticMode();
            break;
        case STATE_TOURNE_SUR_PLACE_GAUCHE:
            PWMSetSpeedConsigne(15, MOTEUR_DROIT);
            PWMSetSpeedConsigne(-15, MOTEUR_GAUCHE);
            stateRobot = STATE_TOURNE_SUR_PLACE_GAUCHE_EN_COURS;
            etat [0] = etat[2] = etat[3] = 0;
            etat [1] = 1;
            etat[4] = timestamp;
            UartEncodeAndSendMessage(0x0050, 5, etat);
            break;
        case STATE_TOURNE_SUR_PLACE_GAUCHE_EN_COURS:
            if (autoControlActivated)
                SetNextRobotStateInAutomaticMode();
            break;
        case STATE_TOURNE_SUR_PLACE_DROITE:
            PWMSetSpeedConsigne(-20, MOTEUR_DROIT);
            PWMSetSpeedConsigne(20, MOTEUR_GAUCHE);
            stateRobot = STATE_TOURNE_SUR_PLACE_DROITE_EN_COURS;
            break;
        case STATE_TOURNE_SUR_PLACE_DROITE_EN_COURS:
            if (autoControlActivated)
                SetNextRobotStateInAutomaticMode();
            break;
        default:
            stateRobot = STATE_ATTENTE;
            break;
    }

}

unsigned char nextStateRobot = 0;

void SetNextRobotStateInAutomaticMode() {
    unsigned char positionObstacle = PAS_D_OBSTACLE;
    positionObstacle = PAS_D_OBSTACLE;
    flag_Final = 0b00000000;
    flag_Gauche = 0b00000000;
    flag_Ex_Gauche = 0b00000000;
    flag_Droit = 0b00000000;
    flag_Ex_Droit = 0b00000000;
    flag_Centre = 0b00000000;

    //ÈDtermination de la position des obstacles en fonction des ÈÈËtlmtres
    if (robotState.distanceTelemetreCentre < 40)
        flag_Centre |= 0b00000100;
    if (robotState.distanceTelemetreDroit < 32)
        flag_Droit |= 0b00000010;
    if (robotState.distanceTelemetreExDroit < 25)
        flag_Ex_Droit |= 0b00000001;
    if (robotState.distanceTelemetreGauche < 32)
        flag_Gauche |= 0b00001000;
    if (robotState.distanceTelemetreExGauche < 25)
        flag_Ex_Gauche |= 0b00010000;
    flag_Final |= flag_Ex_Gauche | flag_Gauche | flag_Centre | flag_Droit | flag_Ex_Droit;

    if (stateRobot != STATE_TOURNE_SUR_PLACE_GAUCHE_EN_COURS || stateRobot != STATE_TOURNE_SUR_PLACE_DROITE_EN_COURS || stateRobot != STATE_TOURNE_DROITE_EN_COURS || stateRobot != STATE_TOURNE_GAUCHE_EN_COURS) {
        if (flag_Final == 0b00010100 || flag_Final == 0b00001100 || flag_Final == 0b00011100)
            positionObstacle = OBSTACLE_EN_FACE_GAUCHE;
        else if (flag_Final == 0b00000101 || flag_Final == 0b00000110 || flag_Final == 0b00000111)
            positionObstacle = OBSTACLE_EN_FACE_GAUCHE;
        else if (flag_Final == 0b00000100 || flag_Final == 0b00001110 || flag_Final == 0b00010111 || flag_Final == 0b00011111 || flag_Final == 0b00001111 || flag_Final == 0b00011110 || flag_Final == 0b00011101 || flag_Final == 0b00010101)
            positionObstacle = OBSTACLE_EN_FACE_GAUCHE;
        else if (flag_Final == 0b00010000 || flag_Final == 0b00001000 || flag_Final == 0b00011000)
            positionObstacle = OBSTACLE_A_GAUCHE;
        else if (flag_Final == 0b00000001 || flag_Final == 0b00000010 || flag_Final == 0b00000011)
            positionObstacle = OBSTACLE_A_DROITE;
        else
            positionObstacle = PAS_D_OBSTACLE;
    }



    //ÈDtermination de lÈ?tat ‡venir du robot
    if (positionObstacle == PAS_D_OBSTACLE)
        nextStateRobot = STATE_AVANCE;
    else if (positionObstacle == OBSTACLE_A_DROITE)
        nextStateRobot = STATE_TOURNE_GAUCHE;
    else if (positionObstacle == OBSTACLE_A_GAUCHE)
        nextStateRobot = STATE_TOURNE_DROITE;
    else if (positionObstacle == OBSTACLE_EN_FACE_GAUCHE)
        nextStateRobot = STATE_TOURNE_SUR_PLACE_GAUCHE;
    else if (positionObstacle == OBSTACLE_EN_FACE_DROITE)
        nextStateRobot = STATE_TOURNE_SUR_PLACE_DROITE;
    //Si l?on n?est pas dans la transition de lÈ?tape en cours
    if (nextStateRobot != stateRobot - 1)
        stateRobot = nextStateRobot;
}