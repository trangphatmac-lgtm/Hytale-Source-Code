#ifndef CURVE_TYPE_INCLUDE
#define CURVE_TYPE_INCLUDE

float QuartIn(float x)
{
    return x * x * x * x;
}

float QuartOut(float x)
{
    return 1 - pow(1 - x, 4);
}

float QuartInOut(float x)
{
    return x < 0.5 ? 8 * x * x * x * x : 1 - pow(-2 * x + 2, 4) / 2;
}

#endif // CURVE_TYPE_INCLUDE
