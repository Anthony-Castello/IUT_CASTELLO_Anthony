/* 
 * File:   UART.h
 * Author: E306_PC1
 *
 * Created on 4 décembre 2025, 08:18
 */

#ifndef UART_H
#define	UART_H

void InitUART(void);
void SendMessageDirect(unsigned char* message, int length);
void __attribute__((interrupt, no_auto_psv)) _U1RXInterrupt(void);
#endif /* UART_H */


