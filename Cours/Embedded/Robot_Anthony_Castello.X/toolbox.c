#include "Toolbox.h"


float Abs(float value) {
    if (value >= 0)
        return value;
    else return -value;
}

float Max(float value, float value2) {
    if (value > value2)
        return value;
    else
        return value2;
}

float Min(float value, float value2) {
    if (value < value2)
        return value;
    else
        return value2;
}

float LimitToInterval(float value, float lowLimit, float highLimit) {
    if (value > highLimit)
        value = highLimit;
    else if (value < lowLimit)
        value = lowLimit;
    return value;
}

float RadianToDegree(float value) {
    return value / PI * 180.0;
}

float DegreeToRadian(float value) {
    return value * PI / 180.0;
}

float getFloatFromBytes(const unsigned char *p, int index)
{
    float f;
    unsigned char *f_ptr = (unsigned char*)&f;

    for (int i = 0; i < 4; i++)
        f_ptr[i] = p[index + i];

    return f;
}
//
//float getFloat(unsigned char *p, int index)
//{
//    float *result_ptr = (float*)(p + index);
//    float result = *result_ptr;
//    return result;
//}
//
//double getDouble(unsigned char *p, int index)
//{
//    double *result_ptr = (double*)(p + index);
//    return *result_ptr;
//}
//
//void getBytesFromDouble(unsigned char *p, int index, double d)
//{
//    int i;
//    unsigned char *f_ptr = (unsigned char*)&d;
//    for (i = 0; i < 8; i++)
//        p[index + i] = f_ptr[i];
//}