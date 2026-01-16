#ifndef UTILITIES_H
#define	UTILITIES_H

#define PI 3.141592653589793

double Modulo2PIAngleRadian(double angleRadian) ;
float getFloat(unsigned char *p, int index);
double getDouble(unsigned char *p, int index);
void getBytesFromInt32(unsigned char *p, int index, long in);
void getBytesFromDouble(unsigned char *p, int index, double d);

#endif /*UTILITIES_H*/