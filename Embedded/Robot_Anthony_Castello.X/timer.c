#include "robot.h"
#include <xc.h>
#include "timer.h"
#include "IO.h"
#include "PWM.h"
#include "ADC.h"
#include "main.h"
#include "ChipConfig.h"
#include "UART.h"
#include "Cb_TX1.h"
#include "CB_RX1.h"
#include "Toolbox.h"
#include "UART_Protocol.h"
#include "QEI.h"

unsigned char toggle = 0;
unsigned long timestamp = 0;
int counter = 0;


//Initialisation d?un timer 16 bits

void InitTimer1(void) {
    T1CONbits.TON = 0; // Disable Timer
    SetFreqTimer1(250); //Fréquence timer1
    T1CONbits.TCS = 0; //clock source = internal clock
    IFS0bits.T1IF = 0; // Clear Timer Interrupt Flag
    IEC0bits.T1IE = 1; // Enable Timer interrupt
    T1CONbits.TON = 1; // Enable Timer
}

void SetFreqTimer1(float freq) {
    T1CONbits.TCKPS = 0b00; //00 = 1:1 prescaler value
    if (FCY / freq > 65535) {
        T1CONbits.TCKPS = 0b01; //01 = 1:8 prescaler value
        if (FCY / freq / 8 > 65535) {
            T1CONbits.TCKPS = 0b10; //10 = 1:64 prescaler value
            if (FCY / freq / 64 > 65535) {
                T1CONbits.TCKPS = 0b11; //11 = 1:256 prescaler value
                PR1 = (int) (FCY / freq / 256);
            } else
                PR1 = (int) (FCY / freq / 64);
        } else
            PR1 = (int) (FCY / freq / 8);
    } else
        PR1 = (int) (FCY / freq);
}

//Interruption du timer 1

void __attribute__((interrupt, no_auto_psv)) _T1Interrupt(void) {
    IFS0bits.T1IF = 0;
    PWMUpdateSpeed();
    //LED_BLANCHE_1 = !LED_BLANCHE_1;
    //SendMessageDirect((unsigned char*) "Bonjour", 7);
    ADC1StartConversionSequence();
    QEIUpdateData();
    
    if(counter++%150==0)
    {
        unsigned char payload[8];
        getBytesFromFloat(payload, 0, -robotState.vitesseDroiteCommandeCourante);
        getBytesFromFloat(payload, 4, robotState.vitesseGaucheCommandeCourante);
        UartEncodeAndSendMessage(0x00040, 8, payload);
        SendPositionData();
        
    }
}
//Initialisation d?un timer 32 bits

void InitTimer23(void) {
    T3CONbits.TON = 0; // Stop any 16-bit Timer3 operation
    T2CONbits.TON = 0; // Stop any 16/32-bit Timer3 operation
    T2CONbits.T32 = 1; // Enable 32-bit Timer mode
    T2CONbits.TCS = 0; // Select internal instruction cycle clock
    T2CONbits.TCKPS = 0b00; // Select 1:1 Prescaler
    TMR3 = 0x00; // Clear 32-bit Timer (msw)
    TMR2 = 0x00; // Clear 32-bit Timer (lsw)
    PR3 = 0x727; // Load 32-bit period value (msw)
    PR2 = 0x0E00; // Load 32-bit period value (lsw)
    IPC2bits.T3IP = 0x01; // Set Timer3 Interrupt Priority Level
    IFS0bits.T3IF = 0; // Clear Timer3 Interrupt Flag
    IEC0bits.T3IE = 1; // Enable Timer3 interrupt
    T2CONbits.TON = 1; // Start 32-bit Timer
}

//Interruption du timer 32 bits sur 2-3
void __attribute__((interrupt, no_auto_psv)) _T3Interrupt(void) {
    IFS0bits.T3IF = 0; // Clear Timer3 Interrupt Flag
}

void InitTimer4(void) {
    T4CONbits.TON = 0; // Disable Timer
    SetFreqTimer4(1000);
    T4CONbits.TCS = 0; //clock source = internal clock
    IFS1bits.T4IF = 0; // Clear Timer Interrupt Flag
    IEC1bits.T4IE = 1; // Enable Timer interrupt
    T4CONbits.TON = 1; // Enable Timer
}

void SetFreqTimer4(float freq) {
    T4CONbits.TCKPS = 0b00; //00 = 1:1 prescaler value
    if (FCY / freq > 65535) {
        T4CONbits.TCKPS = 0b01; //01 = 1:8 prescaler value
        if (FCY / freq / 8 > 65535) {
            T4CONbits.TCKPS = 0b10; //10 = 1:64 prescaler value
            if (FCY / freq / 64 > 65535) {
                T4CONbits.TCKPS = 0b11; //11 = 1:256 prescaler value
                PR4 = (int) (FCY / freq / 256);
            } else
                PR4 = (int) (FCY / freq / 64);
        } else
            PR4 = (int) (FCY / freq / 8);
    } else
        PR4 = (int) (FCY / freq);
}


//Interruption du timer 32 bits sur 2-3

void __attribute__((interrupt, no_auto_psv)) _T4Interrupt(void) {
    IFS1bits.T4IF = 0; // Clear Timer3 Interrupt Flag
    timestamp++;
    OperatingSystemLoop();

}

