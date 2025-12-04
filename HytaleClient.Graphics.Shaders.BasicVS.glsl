#version 330 core

#ifndef NEED_POS_VS
#define NEED_POS_VS 0
#endif

#ifndef NEED_POS_WS
#define NEED_POS_WS 0
#endif

#ifndef NEED_FRAG_DEPTH_VS
#define NEED_FRAG_DEPTH_VS 0
#endif

#ifndef USE_VERT_NORMALS
#define USE_VERT_NORMALS 0
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
#if NEED_FRAG_DEPTH_VS || NEED_POS_VS
uniform mat4 uModelMatrix;
uniform mat4 uViewMatrix;
uniform mat4 uViewProjectionMatrix;
#else
uniform mat4 uMVPMatrix;
#endif

#if USE_VERT_NORMALS
uniform mat4 uNormalMatrix;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;
in vec2 vertTexCoords;
#if USE_VERT_NORMALS
in vec3 vertNormal;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec2 fragTexCoords;
#if USE_VERT_NORMALS
out vec3 fragNormal;
#endif
#if NEED_POS_VS
out vec3 fragPositionVS;
#endif
#if NEED_POS_WS
out vec3 fragPositionWS;
#endif
#ifdef NEED_FRAG_DEPTH_VS
out float fragDepthVS;
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
#if NEED_FRAG_DEPTH_VS || NEED_POS_VS
    vec4 positionWS = uModelMatrix * vec4(vertPosition, 1.0);
    gl_Position = uViewProjectionMatrix * positionWS;

    vec4 positionVS = uViewMatrix * positionWS;

    #if NEED_FRAG_DEPTH_VS
    fragDepthVS = positionVS.z;
    #endif
    #if NEED_POS_VS
    fragPositionVS = positionVS.xyz;
    #endif
    #if NEED_POS_WS
    fragPositionWS = positionWS.xyz;
    #endif

#else
    gl_Position = uMVPMatrix * vec4(vertPosition, 1.0);
#endif

#if USE_VERT_NORMALS
    fragNormal = (uNormalMatrix * vec4(vertNormal, 0)).xyz;
#endif

    fragTexCoords = vertTexCoords;
}
