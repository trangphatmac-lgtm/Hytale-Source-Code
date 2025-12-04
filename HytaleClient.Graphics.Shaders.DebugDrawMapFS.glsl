#version 150 core

#include "Deferred_inc.glsl"
#include "DebugHeatGradient_inc.glsl"

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform sampler2D uTexture2D;
uniform sampler2DArray uTexture2DArray;
uniform samplerCube uTextureCubemap;

uniform vec4 uViewport;
uniform vec2 uTextureSize;
uniform int uMipLevel;
uniform float uOpacity;
uniform int uLayer;
uniform float uMultiplier;
uniform float uDebugMaxOverdraw;
uniform int uDebugZ;
uniform int uLinearZ;
uniform int uDebugTexture2DArray;
uniform int uCubemapFace;
uniform int uNormalQuantization;
uniform int uChromaSubsampling;
uniform int uColorChannels;

#define CHROMA_SUBSAMPLING_MODE_NONE 0
#define CHROMA_SUBSAMPLING_MODE_COLOR 1
#define CHROMA_SUBSAMPLING_MODE_LIGHT 2

//-------------------------------------------------------------------------------------------------------------------------
//                                  In
//-------------------------------------------------------------------------------------------------------------------------
in vec2 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 outColor;

//-------------------------------------------------------------------------------------------------------------------------

void main(void)
{
    // To avoid any reading/writing/decompression cost, completely discard (all) fragments if the shader opacity is clost to 0
    if (uOpacity < 0.01f) discard;

    if(uCubemapFace != 0)
    {
        vec2 mapcoord = 2.0 * fragTexCoords - 1.0;
        switch(uCubemapFace)
        {
            case 1:
                    // GL_TEXTURE_CUBE_MAP_POSITIVE_X
                    outColor.rgb = texture(uTextureCubemap, vec3(1.0, mapcoord.yx)).rgb;
                    outColor.a = uOpacity;
                    break;

            case 2:
                    // GL_TEXTURE_CUBE_MAP_NEGATIVE_X
                    outColor.rgb = texture(uTextureCubemap, vec3(-1.0, mapcoord.y, -mapcoord.x)).rgb;
                    outColor.a = uOpacity;
                    break;
            case 3:
                    // GL_TEXTURE_CUBE_MAP_POSITIVE_Y
                    outColor.rgb = texture(uTextureCubemap, vec3(mapcoord.x, 1.0, mapcoord.y)).rgb;
                    outColor.a = uOpacity;
                    break;
            case 4:
                    // GL_TEXTURE_CUBE_MAP_NEGATIVE_Y
                    outColor.rgb = texture(uTextureCubemap, vec3(mapcoord.x, -1.0, -mapcoord.y)).rgb;
                    outColor.a = uOpacity;
                    break;
            case 5:
                    // GL_TEXTURE_CUBE_MAP_POSITIVE_Z
                    outColor.rgb = texture(uTextureCubemap, vec3(mapcoord.xy, -1.0)).rgb;
                    outColor.a = uOpacity;
                    break;
            case 6:
                    // GL_TEXTURE_CUBE_MAP_NEGATIVE_Z
                    outColor.rgb = texture(uTextureCubemap, vec3(-mapcoord.x, mapcoord.y, 1.0)).rgb;
                    outColor.a = uOpacity;
                    break;
            default:
            break;
        }
    }
    else if (1 == uDebugTexture2DArray)
    {
        outColor.rgb = textureLod(uTexture2DArray, vec3(fragTexCoords, uLayer), float(uMipLevel)).rgb;
        outColor.a = uOpacity;
    }
    else if (1 == uDebugZ)
    {
        // Reads the values with textureFetch ( => no filtering ), since textureLod is not okay with Depth textures
        vec2 mipSize = uTextureSize / pow(2.0, float(uMipLevel));
        ivec2 texelPos = ivec2 (fragTexCoords * mipSize);
        float depth = texelFetch(uTexture2D, texelPos, uMipLevel).x;

        if (1 == uLinearZ)
        {
            // To help seeing linear Z values, we modify it
            outColor.rgb = vec3(depth, sqrt(depth) * 3, depth);
            outColor.a = uOpacity;
        }
        else
        {
            // Since Z values are kinda hard to "see" in an image file,
            // we use first linearize the Z assuming a far plane value of 16,
            // and then make the blue component dominant
            float n = 0.1f;
            float f = 16.0f;
            depth = (2 * n) / (f + n - depth * (f - n));

            outColor.r = depth * depth* depth;
            outColor.g = depth * depth* depth;
            outColor.b = depth;
            outColor.a = uOpacity;

            //outColor.rgb = vec3(1) - outColor.rgb;
        }    
    }
    else
    {
        // Reads using textureLod, which internally uses the filtering of the sampler
        vec4 color = textureLod(uTexture2D, fragTexCoords, float(uMipLevel));

        // Compute texel position, which may vary when decompressing data w/ renderscale
        ivec2 texelPos = ivec2((gl_FragCoord.xy - uViewport.xy) * uTextureSize / uViewport.zw);

        // RGBA channels to visualize defined by bitsets
        // Supported combinations :
        // 0001 ( = 1 ) : A
        // 0010 ( = 2 ) : B
        // 0100 ( = 4 ) : G
        // 1000 ( = 8 ) : R
        // 0110 ( = 3 ) : BA
        // 0110 ( = 6 ) : GB
        // 1010 ( = 10) : RB
        // 1100 ( = 12) : RG
        // 1110 ( = 14) : RGB
        switch (uColorChannels)
        {
            case 1 : outColor.rgb = vec3(color.a); break;
            case 2 : outColor.rgb = vec3(color.b); break;
            case 4 : outColor.rgb = vec3(color.g); break;
            case 8 : outColor.rgb = vec3(color.r); break;
            case 3 : 
                if (1 == uNormalQuantization) outColor.rgb = decodeNormal(color.ba);
                else if (CHROMA_SUBSAMPLING_MODE_COLOR == uChromaSubsampling) outColor.rgb = decodeCompressedAlbedo(color.ba, uTexture2D, fragTexCoords, texelPos);
                else if (CHROMA_SUBSAMPLING_MODE_LIGHT == uChromaSubsampling) outColor.rgb = decodeCompressedLight(color.ba, uTexture2D, fragTexCoords, texelPos);
                else outColor.rgb = vec3(color.ba, 0);
                break; 
            case 6 : outColor.rgb = vec3(color.gb, 0); break;
            case 10 : outColor.rgb = vec3(color.rb, 0); break;
            case 12 :            
                if (1 == uNormalQuantization) outColor.rgb = decodeNormal(color.rg);
                else if (CHROMA_SUBSAMPLING_MODE_COLOR == uChromaSubsampling) outColor.rgb = decodeCompressedAlbedo(color.rg, uTexture2D, fragTexCoords, texelPos);
                else if (CHROMA_SUBSAMPLING_MODE_LIGHT == uChromaSubsampling) outColor.rgb = decodeCompressedLight(color.rg, uTexture2D, fragTexCoords, texelPos);
                else outColor.rgb = vec3(color.rg, 0);
                break; 
            default : outColor.rgb = color.rgb; break;
        }

        if (uDebugMaxOverdraw > 0)
        {
            // Display a heat map,
            // and show the gradient on the left.
            float value = (fragTexCoords.x < 0.05) ? fragTexCoords.y * 1.05 : outColor.r / uDebugMaxOverdraw;

            if (value == 0) discard;
	
            outColor.rgb = getHeatGradient(value);
        }
        else
        {
            outColor.rgb *= uMultiplier;
        }

        outColor.a = uOpacity;
    }
}
