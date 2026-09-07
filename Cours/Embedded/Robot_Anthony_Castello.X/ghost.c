#include "ghost.h"
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
#include "QEI.h"
#include "asservissement.h"
#include "Utilitises.h"
#include "Toolbox.h"

void SetupGhostValue(volatile GhostState* Ghost, float theta_ghost, float v_theta, float acc_theta, float v_theta_max, float waypoint) {
    Ghost->theta_ghost = theta_ghost;
    Ghost->v_theta = v_theta;
    Ghost->acc_theta = acc_theta;
    Ghost->v_theta_max = v_theta_max;
    waypoint = (waypoint*PI)/180;
    Ghost -> theta_waypoint = waypoint;
    Ghost -> Ghostflag = 0;
}

void UpdateGhostOrientation() {
        robotState.ghost.theta_restant = ModuloByAngle(robotState.ghost.theta_ghost, robotState.ghost.theta_waypoint) - (robotState.ghost.theta_ghost);
        robotState.ghost.theta_arret = ((robotState.ghost.v_theta)*(robotState.ghost.v_theta)) / (2.0 * (robotState.ghost.acc_theta));
        robotState.ghost.increment_theta = (robotState.ghost.v_theta)*(1 / FREQ_ECH_QEI);
        if (robotState.ghost.v_theta < 0) {
            robotState.ghost.theta_arret = -robotState.ghost.theta_arret;
        }
        if (((robotState.ghost.theta_arret >= 0) && (robotState.ghost.theta_restant >= 0)) || ((robotState.ghost.theta_arret <= 0) && (robotState.ghost.theta_restant <= 0)) && (((Abs(robotState.ghost.theta_restant) >= Abs(robotState.ghost.theta_arret))))) {
            robotState.ghost.v_theta += (robotState.ghost.acc_theta * (1 / FREQ_ECH_QEI));
            if (robotState.ghost.theta_restant > 0) {
                robotState.ghost.v_theta = Min((robotState.ghost.v_theta) + ((robotState.ghost.acc_theta) / FREQ_ECH_QEI), robotState.ghost.v_theta_max);
            }
            if (robotState.ghost.theta_restant < 0) {
                robotState.ghost.v_theta = Max((robotState.ghost.v_theta) - ((robotState.ghost.acc_theta) / FREQ_ECH_QEI), robotState.ghost.v_theta_max * -1);
            }
        } else {
            if ((robotState.ghost.v_theta) > 0) {
                robotState.ghost.v_theta = Min(robotState.ghost.v_theta - robotState.ghost.acc_theta * (1 / FREQ_ECH_QEI), 0);
            } else if ((robotState.ghost.v_theta) < 0) {
                robotState.ghost.v_theta = Max(robotState.ghost.v_theta + robotState.ghost.acc_theta * (1 / FREQ_ECH_QEI), 0);
            }
            if (Abs(robotState.ghost.theta_restant) < Abs(robotState.ghost.increment_theta)) {
                robotState.ghost.increment_theta = robotState.ghost.theta_restant;
            }
        }
        robotState.ghost.theta_ghost += robotState.ghost.increment_theta;
        if ((robotState.ghost.v_theta) == 0 && (Abs(robotState.ghost.theta_restant) < 0.01)) {
            robotState.ghost.theta_ghost = robotState.ghost.theta_waypoint;
        }
}

void SendghostValues() {
    unsigned char positionPayload[20];
    getBytesFromFloat(positionPayload, 0, robotState.ghost.v_theta);
    getBytesFromFloat(positionPayload, 4, robotState.ghost.v_theta_max);
    getBytesFromFloat(positionPayload, 8, robotState.ghost.acc_theta);
    getBytesFromFloat(positionPayload, 12, robotState.ghost.theta_ghost);
    getBytesFromFloat(positionPayload, 16, robotState.ghost.theta_waypoint);
    UartEncodeAndSendMessage(0x0070, 20, positionPayload);
}

//faire un c# des text box pour demander les valeurs du ghost 