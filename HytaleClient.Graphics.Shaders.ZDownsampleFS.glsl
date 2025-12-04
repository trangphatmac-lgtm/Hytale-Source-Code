#version 330 core
#extension GL_ARB_gpu_shader5 : enable

#define PIXELSIZE uPixelSize
#include "Fallback_inc.glsl"
#include "Reconstruction_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uZBuffer;

uniform mat4 uProjectionMatrix;
uniform vec2 uFarClipAndInverse;
uniform vec2 uPixelSize;
uniform int uMode;

#define MODE_Z_MAX 0
#define MODE_Z_MIN 1
#define MODE_Z_MIN_MAX 2
#define MODE_Z_ROTATED_GRID 3

#define FAR_CLIP uFarClipAndInverse.x
#define INV_FAR_CLIP uFarClipAndInverse.y

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
#if OUTPUT_COLOR
layout (location = 0) out float outDepth;
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
	vec4 texels;
	float selectedZ;

	switch(uMode)
	{
	case MODE_Z_MAX:
	    texels = TextureGatherR(uZBuffer, fragTexCoords);
	    // Using the max for the Z value is the safest way to minimize it.
		selectedZ = max( max( texels.x, texels.y ), max( texels.z, texels.w ) );
		break;

	case MODE_Z_MIN:
	    texels = TextureGatherR(uZBuffer, fragTexCoords);
		selectedZ = min( min( texels.x, texels.y ), min( texels.z, texels.w ) );
		break;

	case MODE_Z_MIN_MAX:
	    texels = TextureGatherR(uZBuffer, fragTexCoords);
	
		// Alternate Min and Max in a checkerboard pattern
		float maxZ = max( max( texels.x, texels.y ), max( texels.z, texels.w ) );
		float minZ = min( min( texels.x, texels.y ), min( texels.z, texels.w ) );	

		ivec2 crd = ivec2(gl_FragCoord.xy);
		bool isEven = (crd.x & 1) == (crd.y & 1);
		// bool isEven = 1 == mod( gl_FragCoord.x + gl_FragCoord.y, 2);
		selectedZ = isEven ? maxZ : minZ;
		break;

	case MODE_Z_ROTATED_GRID:
		ivec2 ssP = ivec2(gl_FragCoord.xy);
		// Rotated grid subsampling to avoid XY directional bias or Z precision bias while downsampling
		ivec2 rotatedTexelPos = ivec2(ssP * 2 + ivec2((ssP.y & 1) ^ 1, (ssP.x & 1) ^ 1));
		selectedZ = texelFetch(uZBuffer, rotatedTexelPos, 0).r;
		break;
	}

	// Average Z should (almost) never be used, it makes no sense (usually)
    // selectedZ = (texels.x + texels.y + texels.z + texels.w) / 4.0;

#if OUTPUT_COLOR
#if INPUT_IS_LINEAR
    outDepth = selectedZ;
#else
    // The linear depth we get as a result is in the range [0.0-far].
    // We must move it in the range [0.0-1.0].
    float linearDepth = GetLinearDepthFromDepthHW(selectedZ, uProjectionMatrix);
    outDepth = linearDepth * INV_FAR_CLIP;
#endif
#endif

#if OUTPUT_DEPTH
#if INPUT_IS_LINEAR
    // The linear depth read from the input is in the range [0.0-1.0].
    // We must move it in the range [0.0-far].
    float linearDepth = selectedZ * FAR_CLIP;
    gl_FragDepth = GetDepthHWFromLinearDepth(linearDepth, uProjectionMatrix);
#else
    gl_FragDepth = selectedZ;
#endif
#endif // OUTPUT_DEPTH
}
