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

extern int etatghost;

void SetupGhostValue(volatile GhostState* Ghost, float theta_target, float v_theta, float acc_theta, float v_theta_max, float x, float y, float v_lineaire, float v_lin_max, float acc_lin) {
    Ghost->v_theta = v_theta;
    Ghost->acc_theta = acc_theta;
    Ghost->v_theta_max = v_theta_max;
    Ghost->v_lineaire = v_lineaire;
    Ghost->acc_lineaire = acc_lin;
    Ghost->v_lineaire_max = v_lin_max;
    
    Ghost->waypoint_x = x; 
    Ghost->waypoint_y = y;
    
    float dx = x - Ghost->x;
    float dy = y - Ghost->y;
    
    // Détection d'une rotation pure (distance à parcourir quasi-nulle)
    if (fabs(dx) < 0.001 && fabs(dy) < 0.001) {
        Ghost->theta_waypoint = theta_target; // On pointe vers l'angle demandé
    } else {
        Ghost->theta_waypoint = atan2f(dy, dx); // On pointe vers le waypoint
    }

    Ghost->x_start = Ghost->x;
    Ghost->y_start = Ghost->y;
    Ghost->lineaire_ghost = 0.0;
}

void ResetGhostValue(volatile GhostState* Ghost){
    Ghost -> waypoint_x = 0; //faire que l'ancienne val de x et y, on fasse nouvelle - ancienne
    Ghost -> waypoint_y = 0;
    Ghost -> x = 0;
    Ghost -> y = 0;
    Ghost->theta_ghost = 0;
    robotState.ghost.theta_restant = 0;
    robotState.ghost.theta_arret = 0;
    robotState.ghost.distance_restante = 0 ;
    robotState.ghost.lineaire_arret = 0;
    robotState.ghost.x_start = 0;
    robotState.ghost.y_start = 0;
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
    
    if (robotState.ghost.v_theta == 0 && (Abs(robotState.ghost.theta_restant) < 0.01)) {
        robotState.ghost.theta_ghost = robotState.ghost.theta_waypoint;
        etatghost = AVANCE;
        
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

    // Si le WP est derrière : distance négative
    float angleWP = atan2(robotState.ghost.waypoint_y - robotState.ghost.y, robotState.ghost.waypoint_x - robotState.ghost.x);
    float ecartAngle = ModuloByAngle(robotState.ghost.theta_ghost, angleWP) - robotState.ghost.theta_ghost;

    if (Abs(ecartAngle) > PI / 2){
        robotState.ghost.distance_restante = -robotState.ghost.distance_restante;
    }
    
    // CORRECTION 1 : Utilisation exclusive de lineaire_arret
    robotState.ghost.lineaire_arret = (robotState.ghost.v_lineaire * robotState.ghost.v_lineaire) / (2.0 * robotState.ghost.acc_lineaire);
    robotState.ghost.increment_lineaire = robotState.ghost.v_lineaire / FREQ_ECH_QEI;

    if (robotState.ghost.v_lineaire < 0) {
        robotState.ghost.lineaire_arret = -robotState.ghost.lineaire_arret;
    }
    
    // Phase d'accélération / croisière
    if ((((robotState.ghost.lineaire_arret >= 0) && (robotState.ghost.distance_restante >= 0)) || 
         ((robotState.ghost.lineaire_arret <= 0) && (robotState.ghost.distance_restante <= 0))) && 
        (((Abs(robotState.ghost.distance_restante) >= Abs(robotState.ghost.lineaire_arret))))) {
        
        robotState.ghost.v_lineaire += (robotState.ghost.acc_lineaire * (1.0 / FREQ_ECH_QEI));
        
        if (robotState.ghost.distance_restante > 0) {
            robotState.ghost.v_lineaire = Min((robotState.ghost.v_lineaire) + ((robotState.ghost.acc_lineaire) / FREQ_ECH_QEI), robotState.ghost.v_lineaire_max);
        } else if (robotState.ghost.distance_restante < 0) {
            robotState.ghost.v_lineaire = Max(robotState.ghost.v_lineaire - (robotState.ghost.acc_lineaire / FREQ_ECH_QEI), -robotState.ghost.v_lineaire_max);
        }
    } 
    // Phase de décélération
    else {
        if ((robotState.ghost.v_lineaire) > 0) {
            // CORRECTION 2 : Utilisation de Max pour ne pas passer sous 0
            robotState.ghost.v_lineaire = Max(robotState.ghost.v_lineaire - robotState.ghost.acc_lineaire * (1.0 / FREQ_ECH_QEI), 0.0);
        } else if ((robotState.ghost.v_lineaire) < 0) {
            // CORRECTION 2 : Utilisation de Min pour ne pas repasser au-dessus de 0
            robotState.ghost.v_lineaire = Min(robotState.ghost.v_lineaire + robotState.ghost.acc_lineaire * (1.0 / FREQ_ECH_QEI), 0.0);
        }
        
        // CORRECTION 3 : Bridage de l'incrément sur le dernier pas (anti-dépassement)
        if (Abs(robotState.ghost.distance_restante) < Abs(robotState.ghost.increment_lineaire)) {
            robotState.ghost.increment_lineaire = robotState.ghost.distance_restante;
        }
    }
    
    robotState.ghost.lineaire_ghost += robotState.ghost.increment_lineaire;
    
    // Mise à jour spatiale du fantôme
    robotState.ghost.x = robotState.ghost.x_start + robotState.ghost.lineaire_ghost * cos(robotState.ghost.theta_ghost);
    robotState.ghost.y = robotState.ghost.y_start + robotState.ghost.lineaire_ghost * sin(robotState.ghost.theta_ghost);

    // Condition d'arrêt
    if (robotState.ghost.v_lineaire == 0.0 && Abs(robotState.ghost.distance_restante) < 0.01) {
        // On snap parfaitement sur la cible pour corriger les micro-erreurs de calcul (float)
        robotState.ghost.x = robotState.ghost.waypoint_x;
        robotState.ghost.y = robotState.ghost.waypoint_y;
        
        robotState.ghost.x_start = robotState.ghost.x;
        robotState.ghost.y_start = robotState.ghost.y;
        
        // CORRECTION 4 : On remet l'accumulateur linéaire à 0 pour le prochain segment !
        robotState.ghost.lineaire_ghost = 0; 
        
        etatghost = ATTENTE;
    }
}

void Move_ghost(){
    switch(etatghost){
        case ATTENTE :
            break;
        case ROTATION :
            UpdateGhostOrientation();
            break;
        case AVANCE :
            UpdateGhostPosition();
            break;
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