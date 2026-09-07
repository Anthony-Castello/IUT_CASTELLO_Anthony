

#ifndef GHOST_H
#define GHOST_H

typedef struct _GhostState{
    float theta_ghost; // Position angulaire actuelle
    float v_theta; // Vitesse actuelle
    float acc_theta; // Accéleration/décéleration
    float v_theta_max; //Vitesse max permise
    float theta_waypoint; //orientation cible en degrée
    float theta_restant;
    float theta_arret;
    float increment_theta;
    int Ghostflag;
} GhostState;

void SetupGhostValue(volatile GhostState* Ghost, float theta_ghost, float v_theta, float acc_theta, float v_theta_max, float waypoint);
void UpdateGhostOrientation();
void SendghostValues();

#endif	/* GHOST_H */

