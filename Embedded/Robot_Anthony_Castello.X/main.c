#include <stdio.h>
#include <stdlib.h>
#include <xc.h>
#include "ChipConfig.h"
#include "IO.h"
#include "timer.h"
#include "PWM.h"
#include "ADC.h"

unsigned int * result;

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
    PWMSetSpeedConsigne(0, MOTEUR_DROIT);
    PWMSetSpeedConsigne(0, MOTEUR_GAUCHE);
    int ADCValue0, ADCValue1, ADCValue2;



    /***********************************************************************************************/
    // Boucle Principale
    /***********************************************************************************************/
    while (1) {
        if (ADCIsConversionFinished() == 1) {
            result = ADCGetResult();
            ADCValue0 = result[0];
            ADCValue1 = result[1];
            ADCValue2 = result[2];
            ADCClearConversionFinishedFlag();
        }
    } // fin main
}