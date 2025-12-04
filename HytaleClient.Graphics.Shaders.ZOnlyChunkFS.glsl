#version 330 core

#ifndef MIP_LOD_BIAS
#define MIP_LOD_BIAS 0
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
#if ALPHA_TEST
uniform sampler2D uTexture;
#endif
 
#if USE_DRAW_INSTANCED
#define MAX_CASCADES 4
uniform vec2 uViewportInfos[MAX_CASCADES];
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
#if ALPHA_TEST
in vec2 fragTexCoords;
#endif

#if USE_DRAW_INSTANCED
flat in int fragCascadeId;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
// none : we write in gl_FragDepth (implicitely)

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
#if ALPHA_TEST
    float opacity = texture(uTexture, fragTexCoords, MIP_LOD_BIAS).a;

   // Same as MapChunkVS.glsl & BlockyModelFS.glsl
   if (gl_FragCoord.z < 0.55 || (opacity == 0.0)) discard;
#endif

#if USE_DRAW_INSTANCED
    vec2 scaleSize = uViewportInfos[0];
    float texelX = (gl_FragCoord.x / scaleSize.y) / scaleSize.x;
    if (texelX < fragCascadeId || texelX > (fragCascadeId + 1)) discard;
#endif
}
