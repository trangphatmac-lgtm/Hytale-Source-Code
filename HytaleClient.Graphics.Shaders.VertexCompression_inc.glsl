#ifndef VERTEXPACKING_INCLUDE
#define VERTEXPACKING_INCLUDE

vec4 unpackVec4FromUInt(uint value) {return vec4((value) & uint(0xff), ((value) >> 8) & uint(0xff), ((value) >> 16) & uint(0xff), (value) >> 24);}
ivec4 unpackIVec4FromUInt(uint value) {return ivec4((value) & uint(0xff), ((value) >> 8) & uint(0xff), ((value) >> 16) & uint(0xff), (value) >> 24);}
vec4 unpackVec4FromInt(int value) {return vec4((value) & 0xff, ((value) >> 8) & 0xff, ((value) >> 16) & 0xff, (value) >> 24);}
ivec4 unpackIVec4FromInt(int value) {return ivec4((value) & 0xff, ((value) >> 8) & 0xff, ((value) >> 16) & 0xff, (value) >> 24);}

vec3 unpackNormal(uint value)
{
    vec3 unpacked = vec3((value) & uint(0xff), ((value) >> 8) & uint(0xff), ((value) >> 16) & uint(0xff)) / vec3(255.0);
    return unpacked * vec3(2.0) - vec3(1.0);
}

#endif //VERTEXPACKING_INCLUDE
