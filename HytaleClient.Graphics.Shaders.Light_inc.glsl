#ifndef LIGHT_INCLUDE
#define LIGHT_INCLUDE

float getChannelLight(float distanceToLight, float maxChannelLight) 
{
    return pow(max(0.0, 1.0 - distanceToLight / maxChannelLight), 1.5f) * maxChannelLight * 0.8; 
}

vec3 getDynamicLightColor(vec3 fragmentPosition, ivec2 lightIndices, vec3 lightPositions[MAX_LIGHTS], vec3 lightColors[MAX_LIGHTS])
{

// lightIndices :
#define lightIndexStart lightIndices.x
#define lightIndexEnd lightIndices.y

    vec3 dynamicLightColor = vec3(0, 0, 0);

    for (int i = lightIndexStart; i < lightIndexEnd; i++)
    {
        float distanceToLight = distance(lightPositions[i], fragmentPosition);
        
        // skip light if it is too far away ( = its influence is 0)
        const float maxLightRange = 9.0f;
        if (distanceToLight > maxLightRange) continue;

        float distanceToLightFactor = distanceToLight * 0.1f;

        dynamicLightColor.r += getChannelLight(distanceToLightFactor, lightColors[i].r);
        dynamicLightColor.g += getChannelLight(distanceToLightFactor, lightColors[i].g);
        dynamicLightColor.b += getChannelLight(distanceToLightFactor, lightColors[i].b);
    }
    dynamicLightColor = min(dynamicLightColor, vec3(1.0, 1.0, 1.0));

    return dynamicLightColor;

#undef lightIndexStart
#undef lightIndexEnd
}

float getAttenuation(float distanceToLight, float lightSize)
{
    return clamp(pow(1 - distanceToLight / lightSize, 2), 0.0, 1.0);
}

#endif //LIGHT_INCLUDE
