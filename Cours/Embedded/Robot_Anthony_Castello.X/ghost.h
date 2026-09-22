

#ifndef GHOST_H
#define GHOST_H

#define ATTENTE 0
#define ROTATION 1
#define AVANCE 2

typedef struct _GhostState{
    float theta_ghost; // Position angulaire actuelle
    float v_theta; // Vitesse actuelle
    float acc_theta; // Accéleration/décéleration
    float v_theta_max; //Vitesse max permise
    float theta_waypoint; //orientation cible en degrée
    float theta_restant;
    float theta_arret;
    float increment_theta;
    float waypoint_x;
    float waypoint_y;
    float distance_restante;
    float v_lineaire;
    float acc_lineaire;
    float increment_lineaire;
    float lineaire_arret;
    float lineaire_waypoint;
    float lineaire_ghost;
    float v_lineaire_max;
    float x;
    float y;
    float distance_arret;
    int start;
} GhostState;

typedef struct {
    double x;
    double y;
} Point;


void SetupGhostValue(volatile GhostState* Ghost, float theta_ghost, float v_theta, float acc_theta, float v_theta_max, float x, float y, float v_lineaire, float v_lin_max, float acc_ang);
void UpdateGhostOrientation();
void UpdateGhostPosition();
void SendghostValues();
void DeplacementGhost();
float calculerDistancePointSegment(float x, float y, float x_pre, float y_pre);



#endif	/* GHOST_H */

