#version 330 core
#extension GL_ARB_gpu_shader5 : enable

#define PIXELSIZE uPixelSize
#include "Fallback_inc.glsl"
#include "Reconstruction_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uDepthTexture;

uniform vec2 uInvDepthTextureSize;
uniform mat4 uProjectionMatrix;
uniform float uFarClip;

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

vec2 uPixelSize = uInvDepthTextureSize;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec2 texCoords = fragTexCoords;

	// float depth = texture(uDepthTexture, texCoords).r;
	vec4 otherZ = TextureGatherR(uDepthTexture, texCoords);

	// NB : the value in otherZ.w is the same as the value depth since it's the same texel fetched... try to do better.
	float depth = otherZ.x;

#if USE_LINEAR_Z
	float linearDepth = depth * uFarClip;
#else
	float linearDepth = GetLinearDepthFromDepthHW(depth, uProjectionMatrix);
#endif

#if USE_LINEAR_Z
	otherZ = otherZ * uFarClip;
#else
	otherZ.x = GetLinearDepthFromDepthHW(otherZ.x, uProjectionMatrix);
	otherZ.y = GetLinearDepthFromDepthHW(otherZ.y, uProjectionMatrix);
	otherZ.z = GetLinearDepthFromDepthHW(otherZ.z, uProjectionMatrix);
	otherZ.w = GetLinearDepthFromDepthHW(otherZ.w, uProjectionMatrix);

#endif

	vec4 absDeltaZ = abs( vec4(linearDepth) - otherZ );
	float absDeltaZMin = min( min(absDeltaZ.x, absDeltaZ.y), min(absDeltaZ.z, absDeltaZ.w));
	float absDeltaZMax = max( max(absDeltaZ.x, absDeltaZ.y), max(absDeltaZ.z, absDeltaZ.w));

	float threshold = 0.065 * linearDepth; 
		
	if (absDeltaZMax < threshold)
	{
		discard;
	}
	else
	{
		outColor = vec4(1,0,0,1);
	}
}
