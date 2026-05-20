

#ifndef GHOST_H
#define GHOST_H

typedef struct _GhostState{
    float theta_ghost; // Position angulaire actuelle
    float v_theta; // Vitesse actuelle
    float acc_theta; // Accéleration/décéleration
    float v_theta_max; //Vitesse max permise
} GhostState;

void UpdateGhostOrientation(volatile GhostState* ghost, float theta_waypoint);
void SendghostValues();

#endif	/* GHOST_H */

