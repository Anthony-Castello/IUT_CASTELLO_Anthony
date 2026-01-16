/* 
 * File:   QEI.h
 * Author: E306_PC1
 *
 * Created on 16 janvier 2026, 08:48
 */

#ifndef QEI_H
#define	QEI_H


#define FREQ_ECH_QEI  250.0

void InitQEI1();
void InitQEI2();
void QEIUpdateData();
void SendPositionData();

#endif	/* QEI_H */

