#ifndef DEBUG_HEAT_GRADIENT_INCLUDE
#define DEBUG_HEAT_GRADIENT_INCLUDE

// heat5 function from: https://www.shadertoy.com/view/ltlSRj
vec3 fromRedToGreen(float interpolant)
{
   return (interpolant < 0.5) ? vec3(1.0, 2.0 * interpolant, 0.0) : vec3(2.0 - 2.0 * interpolant, 1.0, 0.0); 
}

vec3 fromGreenToBlue(float interpolant)
{
   return (interpolant < 0.5) ? vec3(0.0, 1.0, 2.0 * interpolant) : vec3(0.0, 2.0 - 2.0 * interpolant, 1.0); 
}

vec3 heat5(float interpolant)
{
	return (interpolant < 0.5) ? fromGreenToBlue(1.0 - 2.0 * interpolant) : fromRedToGreen(2.0 - 2.0 * interpolant);
}

vec3 getHeatGradient(float value)
{
    return (value <= 1) ? heat5(value) : vec3(0);
}

#endif //DEBUG_HEAT_GRADIENT_INCLUDE
