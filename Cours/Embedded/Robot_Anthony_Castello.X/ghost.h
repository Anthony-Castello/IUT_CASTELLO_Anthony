

#ifndef GHOST_H
#define GHOST_H

typedef struct _GhostState{
    float theta_ghost; // Position angulaire actuelle
    float v_theta; // Vitesse actuelle
    float acc_theta; // Accéleration/décéleration
    float v_theta_max; //Vitesse max permise
} GhostState;

void SetupGhostValue(volatile GhostState* Ghost, float theta_ghost, float v_theta, float acc_theta, float v_theta_max);
void UpdateGhostOrientation(volatile GhostState* ghost, float theta_waypoint);
void SendghostValues();

#endif	/* GHOST_H */

