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

void SetupGhostValue(volatile GhostState* Ghost, float theta_ghost, float v_theta, float acc_theta, float v_theta_max) {
    Ghost->theta_ghost = theta_ghost;
    Ghost->v_theta = v_theta;
    Ghost->acc_theta = acc_theta;
    Ghost->v_theta_max = v_theta_max;

}

void UpdateGhostOrientation(volatile GhostState* ghost, float theta_waypoint) {
    while (ghost -> theta_ghost < theta_waypoint * (PI / 180)) {
        float theta_restant = ModuloByAngle(ghost -> theta_ghost, theta_waypoint) - (ghost -> theta_ghost);
        float theta_arret = ((ghost -> v_theta)*(ghost -> v_theta)) / (2.0 * (ghost -> acc_theta));
        float increment_theta = (ghost -> v_theta)*(1 / FREQ_ECH_QEI);
        if (ghost -> v_theta < 0) {
            theta_arret = -theta_arret;
        }
        if (((theta_arret >= 0) && (theta_restant >= 0)) || ((theta_arret <= 0) && (theta_restant <= 0)) && (((Abs(theta_restant) >= Abs(theta_arret))))) {
            ghost -> v_theta += (ghost -> acc_theta * (1 / FREQ_ECH_QEI));
            if (theta_restant > 0) {
                ghost -> v_theta = Min((ghost -> v_theta) + ((ghost -> acc_theta) / FREQ_ECH_QEI), ghost -> v_theta_max);
            }
            if (theta_restant < 0) {
                ghost -> v_theta = Max((ghost -> v_theta) - ((ghost -> acc_theta) / FREQ_ECH_QEI), ghost -> v_theta_max * -1);
            }
        } else {
            if ((ghost -> v_theta) > 0) {
                ghost -> v_theta = Min(ghost -> v_theta - ghost -> acc_theta * (1 / FREQ_ECH_QEI), 0);
            } else if ((ghost -> v_theta) < 0) {
                ghost -> v_theta = Max(ghost -> v_theta + ghost -> acc_theta * (1 / FREQ_ECH_QEI), 0);
            }
            if (Abs(theta_restant) < Abs(increment_theta)) {
                increment_theta = theta_restant;
            }
        }
        ghost -> theta_ghost += increment_theta;
        if ((ghost -> v_theta) == 0 && (Abs(theta_restant) < 0.01)) {
            ghost -> theta_ghost = theta_waypoint;
        }
        SendghostValues();
    }
}

void SendghostValues() {
    unsigned char positionPayload[16];
    getBytesFromFloat(positionPayload, 0, robotState.ghost.v_theta);
    getBytesFromFloat(positionPayload, 4, robotState.ghost.v_theta_max);
    getBytesFromFloat(positionPayload, 8, robotState.ghost.acc_theta);
    getBytesFromFloat(positionPayload, 12, robotState.ghost.theta_ghost);
    UartEncodeAndSendMessage(0x0070, 16, positionPayload);
}

//faire un c# des text box pour demander les valeurs du ghost 