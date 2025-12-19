#include <xc.h>
#include "UART_Protocol.h"
#include "CB_RX1.h"
#include "CB_TX1.h"


int rcvState = 0; //waiting = 0
int msgDecodedFunction = 0;
int msgDecodedPayloadLength = 0;
unsigned char* msgDecodedPayload;
int msgDecodedPayloadIndex = 0;

unsigned char UartCalculateChecksum(int msgFunction, int msgPayloadLength, unsigned char* msgPayload) {
    //Fonction prenant entree la trame et sa longueur pour calculer le checksum
    unsigned char checksum = 0x00;
    checksum ^= 0xFE;
    checksum ^= (unsigned char) (msgFunction >> 8);
    checksum ^= (unsigned char) (msgFunction >> 0);
    checksum ^= (unsigned char) (msgPayloadLength >> 8);
    checksum ^= (unsigned char) (msgPayloadLength >> 0);
    for (int i = 0; i < msgPayloadLength; i++)
        checksum ^= msgPayload[i];

    return checksum;
}

void UartEncodeAndSendMessage(int msgFunction, int msgPayloadLength, unsigned char* msgPayload) {
    unsigned char trame[6 + msgPayloadLength];
    int pos = 0;
    trame[pos++] = 0xFE;
    trame[pos++] = (unsigned char) (msgFunction >> 8);
    trame[pos++] = (unsigned char) (msgFunction >> 0);
    trame[pos++] = (unsigned char) (msgPayloadLength >> 8);
    trame[pos++] = (unsigned char) (msgPayloadLength >> 0);
    for (int i = 0; i < msgPayloadLength; i++)
        trame[pos++] = msgPayload[i];
    trame[pos++] = UartCalculateChecksum(msgFunction, msgPayloadLength, msgPayload);
    SendMessage(trame, sizeof (trame));
}

void UartDecodeMessage(unsigned char c) {
    //Fonction prenant en entree un octet et servant a reconstituer les trames
    switch (rcvState) {
        case 0: //waiting
            if (c == 0xFE) {
                rcvState = 1; //FunctionMSB
            }
            break;
        case 1:
            msgDecodedFunction = (int) (c << 8);
            rcvState = 2; //FunctionLSB
            break;
        case 2:
            msgDecodedPayloadLength += (int)c;
            rcvState = 3; //PayloadLengthMSB
            break;
        case 3:
            msgDecodedPayloadLength = (int) (c<<8);
            rcvState = 4; //PayloadLengthLSB
            break;
        case 4:
            msgDecodedPayloadLength += (int) c;
            msgDecodedPayload[msgDecodedPayloadIndex] = (unsigned char)msgDecodedPayloadLength;
            msgDecodedPayloadIndex = 0;
            rcvState = 5; //Payload
            break;
        case 5:
            msgDecodedPayload[msgDecodedPayloadIndex] = c;
            msgDecodedPayloadIndex++;
            if (msgDecodedPayloadIndex == msgDecodedPayloadLength) {
                rcvState = 6; //CheckSum
            }
            break;
        case 6:
            if (UartCalculateChecksum(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload) == c)
                UartProcessDecodedMessage(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
            rcvState = 0;
            break;
        default:
            rcvState = 0;
            break;
    }
}

void UartProcessDecodedMessage(int function, int payloadLength, unsigned char* payload) {
//Fonction appelee apres le decodage pour executer l?action
//correspondant au message recu
//...
}
//*************************************************************************/
//Fonctions correspondant aux messages
//*************************************************************************/
