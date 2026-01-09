/* 
 * File:   UART_Protocol.h
 * Author: E306_PC1
 *
 * Created on 19 décembre 2025, 08:23
 */

#ifndef UART_PROTOCOL_H
#define	UART_PROTOCOL_H
#define SET_ROBOT_STATE 0x0051
#define SET_ROBOT_MANUAL_CONTROL 0x0052
bool autoControlActivated = 1;
unsigned char UartCalculateChecksum(int msgFunction, int msgPayloadLength, unsigned char* msgPayload);
void UartEncodeAndSendMessage(int msgFunction, int msgPayloadLength, unsigned char* msgPayload);
void UartDecodeMessage(unsigned char c);
void UartProcessDecodedMessage(int function, int payloadLength, unsigned char* payload);
void SetRobotState(unsigned char msgPayload[])
#endif	/* UART_PROTOCOL_H */

