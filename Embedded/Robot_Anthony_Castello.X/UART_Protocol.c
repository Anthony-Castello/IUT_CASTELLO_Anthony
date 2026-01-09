#include <xc.h>
#include "UART_Protocol.h"
#include "CB_RX1.h"
#include "CB_TX1.h"
#include "main.h"

#define Waiting 0
#define FunctionMSB 1
#define FunctionLSB 2
#define PayloadLengthMSB 3
#define PayloadLengthLSB 4
#define Payload 5
#define CheckSum 6




int rcvState = Waiting;
int msgDecodedFunction = 0;
int msgDecodedPayloadLength = 0;
static unsigned char msgDecodedPayload[1024];
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
        case Waiting:
            if (c == 0xFE) {
                rcvState = FunctionMSB;
            }
            break;
        case FunctionMSB:
            msgDecodedFunction = (int) (c << 8);
            rcvState = FunctionLSB;
            break;
        case FunctionLSB:
            msgDecodedFunction += (int) c;
            rcvState = PayloadLengthMSB;
            break;
        case PayloadLengthMSB:
            msgDecodedPayloadLength = (int) (c << 8);
            rcvState = PayloadLengthLSB;
            break;
        case PayloadLengthLSB:
            msgDecodedPayloadLength += (int) c;
            msgDecodedPayloadIndex = 0;
            if (msgDecodedPayloadLength == 0)
                rcvState = CheckSum;
            else
                rcvState = Payload;
            break;
        case Payload:
            msgDecodedPayload[msgDecodedPayloadIndex++] = c;
            if (msgDecodedPayloadIndex >= msgDecodedPayloadLength) {
                rcvState = CheckSum;
            }
            break;
        case CheckSum:
        {
            unsigned char checksum = UartCalculateChecksum(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
            if (checksum == c)
                UartProcessDecodedMessage(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
            rcvState = Waiting;
        }
            break;
        default:
            rcvState = Waiting;
            break;
    }
}

void UartProcessDecodedMessage(int msgFunction, int msgPayloadLength, unsigned char msgPayload[]) {
    //Fonction appelee apres le decodage pour executer l?action
    //correspondant au message recu
    switch (msgFunction) {
        case SET_ROBOT_STATE:
            SetRobotState(msgPayload[0]);
            break;
        case SET_ROBOT_MANUAL_CONTROL:
            SetRobotAutoControlState(msgPayload[0]);
            break;
        default:
            msgFunction = SET_ROBOT_STATE;
            break;
    }
}
void SetRobotState(unsigned char c){
    switch(c){
        case STATE_ATTENTE : //0
            stateRobot = STATE_ATTENTE;
        case STATE_AVANCE : //2
            stateRobot = STATE_AVANCE;
            break;
        case STATE_TOURNE_GAUCHE : //4
            stateRobot = STATE_TOURNE_GAUCHE;
            break;
        case STATE_TOURNE_DROITE : //6
            stateRobot = STATE_TOURNE_DROITE;
            break;
        case STATE_TOURNE_SUR_PLACE_GAUCHE : //8
            stateRobot = STATE_TOURNE_SUR_PLACE_GAUCHE;
            break;
        case STATE_TOURNE_SUR_PLACE_DROITE : //10
            stateRobot = STATE_TOURNE_SUR_PLACE_DROITE;
            break;
        case STATE_ARRET : //12
            stateRobot = STATE_ARRET;
        case STATE_RECULE : //14
            stateRobot = STATE_RECULE;
            break;
        default :
            stateRobot = STATE_ATTENTE;
            break;
    }         
         
    
}

void SetRobotAutoControlState(unsigned char c){
    if(!c || c){
        autoControlActivated = c;//mode manuel = 0; mode auto = 1
    }
}

