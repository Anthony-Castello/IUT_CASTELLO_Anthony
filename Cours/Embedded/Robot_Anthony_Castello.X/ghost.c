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
#include <math.h>
#include "CB_TX1.h"
#include "CB_RX1.h" 
#include "UART_Protocol.h"
#include "QEI.h"
#include "asservissement.h"
#include "Utilitises.h"
#include "Toolbox.h"

void SetupGhostValue(volatile GhostState* Ghost, float theta_ghost, float v_theta, float acc_theta, float v_theta_max, float x, float y, float v_lineaire, float v_lin_max, float acc_lin) {
    Ghost->theta_ghost = theta_ghost;
    Ghost->v_theta = v_theta;
    Ghost->acc_theta = acc_theta;
    Ghost->v_theta_max = v_theta_max;
    Ghost->v_lineaire = v_lineaire;
    Ghost->acc_lineaire = acc_lin;
    Ghost->v_lineaire_max = v_lin_max;
    Ghost -> waypoint_x = x; //faire que l'ancienne val de x et y, on fasse nouvelle - ancienne
    Ghost -> waypoint_y = y;
    Ghost -> theta_waypoint = atan2f(y, x);
    Ghost -> v_lineaire = v_lineaire;
}



void UpdateGhostOrientation() {
    robotState.ghost.theta_restant = ModuloByAngle(robotState.ghost.theta_ghost, robotState.ghost.theta_waypoint) - robotState.ghost.theta_ghost;
    robotState.ghost.theta_arret = (robotState.ghost.v_theta * robotState.ghost.v_theta) / (2.0 * robotState.ghost.acc_theta);
    robotState.ghost.increment_theta = robotState.ghost.v_theta / FREQ_ECH_QEI;
    if (robotState.ghost.v_theta < 0) {
        robotState.ghost.theta_arret = -robotState.ghost.theta_arret;
    }
    if ((((robotState.ghost.theta_arret >= 0) && (robotState.ghost.theta_restant >= 0)) || ((robotState.ghost.theta_arret <= 0) && (robotState.ghost.theta_restant <= 0))) && (((Abs(robotState.ghost.theta_restant) >= Abs(robotState.ghost.theta_arret))))) {
        robotState.ghost.v_theta += (robotState.ghost.acc_theta * (1 / FREQ_ECH_QEI));
        if (robotState.ghost.theta_restant > 0) {
            robotState.ghost.v_theta = Min((robotState.ghost.v_theta) + ((robotState.ghost.acc_theta) / FREQ_ECH_QEI), robotState.ghost.v_theta_max);
        } else if (robotState.ghost.theta_restant < 0) {
            robotState.ghost.v_theta = Min(robotState.ghost.v_theta - (robotState.ghost.acc_theta / FREQ_ECH_QEI), -robotState.ghost.v_theta_max);
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

float calculerDistancePointSegment(float x, float y, float x_pre, float y_pre) {

    float dx = x - x_pre;
    float dy = y - y_pre;

    if (dx == 0 && dy == 0) {
        float pdx = x_pre - x;
        float pdy = y_pre - y;
        return sqrt(pdx * pdx + pdy * pdy);
    }

    float t = ((x_pre - x) * dx + (y_pre - y) * dy) / (dx * dx + dy * dy);

    if (t < 0) t = 0.0;
    if (t > 1) t = 1.0;

    float closestX = x + t * dx;
    float closestY = y + t * dy;

    float distX = x_pre - closestX;
    float distY = y_pre - closestY;

    return sqrt(distX * distX + distY * distY);
}

void UpdateGhostPosition() {

    robotState.ghost.distance_restante = calculerDistancePointSegment(robotState.ghost.waypoint_x, robotState.ghost.waypoint_y, robotState.ghost.x, robotState.ghost.y);

    // Si le WP est devant positif / si le WP est derrière : négatif !
    float angleWP = atan2(robotState.ghost.waypoint_y - robotState.ghost.y, robotState.ghost.waypoint_x - robotState.ghost.x);
    float ecartAngle = ModuloByAngle(robotState.ghost.theta_ghost, angleWP) - robotState.ghost.theta_ghost;

    if (Abs(ecartAngle) > PI / 2)
        robotState.ghost.distance_restante = -robotState.ghost.distance_restante;

    if (Abs(ecartAngle) < DegreeToRadian(1) || Abs(ecartAngle) > DegreeToRadian(179)) //On est aligné, donc on avance.
    {
        robotState.ghost.distance_arret = (robotState.ghost.v_lineaire * robotState.ghost.v_lineaire) / (2.0 * robotState.ghost.acc_lineaire);

        robotState.ghost.increment_lineaire = robotState.ghost.v_lineaire / FREQ_ECH_QEI;
        if (robotState.ghost.v_lineaire < 0) {
            robotState.ghost.lineaire_arret = -robotState.ghost.lineaire_arret;
        }
        if ((((robotState.ghost.lineaire_arret >= 0) && (robotState.ghost.distance_restante >= 0)) || ((robotState.ghost.lineaire_arret <= 0) && (robotState.ghost.distance_restante <= 0))) && (((Abs(robotState.ghost.distance_restante) >= Abs(robotState.ghost.lineaire_arret))))) {
            robotState.ghost.v_lineaire += (robotState.ghost.acc_lineaire * (1 / FREQ_ECH_QEI));
            if (robotState.ghost.distance_restante > 0) {
                robotState.ghost.v_lineaire = Min((robotState.ghost.v_lineaire) + ((robotState.ghost.acc_lineaire) / FREQ_ECH_QEI), robotState.ghost.v_lineaire_max);
            } else if (robotState.ghost.distance_restante < 0) {
                robotState.ghost.v_lineaire = Min(robotState.ghost.v_lineaire - (robotState.ghost.acc_lineaire / FREQ_ECH_QEI), -robotState.ghost.v_lineaire_max);
            }
        } else {
            if ((robotState.ghost.v_lineaire) > 0) {
                robotState.ghost.v_lineaire = Min(robotState.ghost.v_lineaire - robotState.ghost.acc_lineaire * (1.0 / FREQ_ECH_QEI), 0);
            } else if ((robotState.ghost.v_lineaire) < 0) {
                robotState.ghost.v_lineaire = Max(robotState.ghost.v_lineaire + robotState.ghost.acc_lineaire * (1.0 / FREQ_ECH_QEI), 0);
            }
        }
        robotState.ghost.x += robotState.ghost.increment_lineaire * cos(robotState.ghost.theta_ghost);
        robotState.ghost.y += robotState.ghost.increment_lineaire * sin(robotState.ghost.theta_ghost);

        if ((robotState.ghost.v_lineaire) == 0 && (Abs(robotState.ghost.distance_restante) < 0.01)) {
            robotState.ghost.lineaire_ghost = robotState.ghost.lineaire_waypoint;
        }
    }


}

void SendghostValues() {
    unsigned char positionPayload[32];
    getBytesFromFloat(positionPayload, 0, robotState.ghost.v_theta);
    getBytesFromFloat(positionPayload, 4, robotState.ghost.v_theta_max);
    getBytesFromFloat(positionPayload, 8, robotState.ghost.acc_theta);
    getBytesFromFloat(positionPayload, 12, robotState.ghost.theta_ghost);
    getBytesFromFloat(positionPayload, 16, robotState.ghost.theta_waypoint);
    getBytesFromFloat(positionPayload, 20, robotState.ghost.x);
    getBytesFromFloat(positionPayload, 24, robotState.ghost.y);
    getBytesFromFloat(positionPayload, 28, robotState.ghost.distance_restante);
    UartEncodeAndSendMessage(0x0070, 32, positionPayload);
}

//faire un c# des text box pour demander les valeurs du ghost 