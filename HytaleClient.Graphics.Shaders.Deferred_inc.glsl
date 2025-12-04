#ifndef DEFERRED_INCLUDE
#define DEFERRED_INCLUDE

// ------------------------------------------------------------------------------------------------------------------------------------------------
// Encodes a float (usually depth) into a common RGBA 32 bits
vec4 encodeFloatToRGBA(float v) 
{
    vec4 enc = vec4(1.0, 255.0, 65025.0, 160581375.0) * v;
    enc = fract(enc);
    enc -= enc.yzww * vec4(1.0/255.0,1.0/255.0,1.0/255.0,0.0);

    return enc;
}

// Decodes a float (usually depth) written with encodeFloatToRGBA
float decodeFloatFromRGBA(vec4 rgba) 
{
    return dot(rgba, vec4(1.0, 1/255.0, 1/65025.0, 1/160581375.0));
}

// another set of versions, just in case...
vec4 encodeFloatToRGBA_v2(float v) 
{
    const vec4 bitSh = vec4(256.0 * 256.0 * 256.0, 256.0 * 256.0, 256.0, 1.0);
    const vec4 mask = vec4(0.0, 1.0 / 256.0, 1.0 / 256.0, 1.0 / 256.0);
    vec4 res = fract(v * bitSh);
    return res.xxyz * mask;
}

float decodeFloatFromRGBA_v2(vec4 value)
{
    const vec4 bitSh = vec4(1.0 / (256.0 * 256.0 * 256.0), 1.0 / (256.0 * 256.0), 1.0 / 256.0, 1.0);
    return(dot(value, bitSh));
}
// ------------------------------------------------------------------------------------------------------------------------------------------------
// Normal compression to 2 channels
// SphereMap encoding is inspired from CryTek's presentation 'A bit more deferred'.
// SphericalCoordinates encoding is inspired from MJP's blog post 'STORING NORMALS USING SPHERICAL COORDINATES'.
// For more ref, see :
// https://aras-p.info/texts/CompactNormalStorage.html#method03spherical
// https://aras-p.info/texts/CompactNormalStorage.html#method04spheremap

#define kPI 3.1415926536

vec2 encodeNormalSphericalCoordinates(vec3 n)
{
    return vec2(atan(n.x, n.y)/kPI, n.z) * vec2(0.5) + vec2(0.5);
}


vec3 decodeNormalSphericalCoordinates(vec2 enc)
{
    vec2 ang = enc * vec2(2.0) - vec2(1.0);
    vec2 scth;
    scth.y = sin(ang.x * kPI);
    scth.x = cos(ang.x * kPI);

    vec2 scphi = vec2(sqrt(1.0 - ang.y * ang.y), ang.y);
    return vec3(scth.y * scphi.x, scth.x * scphi.x, scphi.y);
}

vec2 encodeNormalSphereMap(vec3 normal)
{
//    if (normal == vec3(0,0,-1)) return vec2(1); else
    return (normalize(normal.xy) * sqrt(-normal.z * 0.5 + 0.5)) * 0.5 + 0.5;
}

vec3 decodeNprmalSphereMap(vec2 compressedNormal)
{
    vec3 normal;
    
    // Edge case that must be dealt properly
//    if (compressedNormal == vec2(1)) return vec3(0, 0, -1);
    if (compressedNormal == vec2(0)) return vec3(0, 0, 1);

    vec4 nn = vec4(compressedNormal, 0.0, 0.0) * vec4(2.0, 2.0, 0.0, 0.0) + vec4(-1.0, -1.0, 1.0, -1.0);
    float l = dot(nn.xyz, -nn.xyw);
    nn.z = l;
    nn.xy *= sqrt(l);
    return nn.xyz * vec3(2.0) + vec3(0.0, 0.0, -1.0);
}

vec2 encodeNormal(vec3 normal)
{
    // Prefer the spherical coordinates encoding, as it works nicely w/ both world space normals & view space normals.
    return encodeNormalSphericalCoordinates(normal);
//    return encodeNormalSphereMap(normal);
}

vec3 decodeNormal(vec2 compressedNormal)
{
    // Prefer the spherical coordinates encoding, as it works nicely w/ both world space normals & view space normals.
    return decodeNormalSphericalCoordinates(compressedNormal);
//    return decodeNprmalSphereMap(compressedNormal);
}

vec3 fastNormalFromPosition(vec3 position)
{
    vec3 p = position;
    vec3 p1 = dFdx(p);
    vec3 p2 = dFdy(p);
    vec3 normal = cross(p1, p2);

    return normalize(normal);
}

//-----------------------------------------------------------
// Depth compression

// Used to pack 16bit depth into 2 8bit channels
vec2 packDepth(float key) 
{
    // For Debug
    //return vec2(key, 0);

    // Round to the nearest 1/256.0
    float temp = floor(key * 256.0);

    vec2 p;

    // Integer part
    p.x = temp * (1.0 / 256.0);

    // Fractional part
    p.y = key * 256.0 - temp;

    return p;
}

// Used to unpack 16bit depth from 2 8bit channels
float unpackDepth(vec2 p) 
{
    // For Debug
    //return p.x;
    return p.x * (256.0 / 257.0) + p.y * (1.0 / 257.0);
}

// ------------------------------------------------------------------------------------------------------------------------------------------------
// Color compression 
// For more information, see http://www.pmavridis.com/research/fbcompression/

vec3 RGB2Same(vec3 c){ return c.rgb;}

vec3 Same2RGB(vec3 c){ return c.rgb;}

vec3 RGB2YCbCr(vec3 c)
{
    return vec3( 0.299 * c.r + 0.587 * c.g + 0.114 * c.b, -0.168 * c.r - 0.331 * c.g + 0.5 * c.b + 0.5, 0.5 * c.r + -0.418 * c.g - 0.081 * c.b + 0.5);
}

vec3 YCbCr2RGB(vec3 c)
{
    return vec3(c.r + 1.402 * (c.b - 0.5), c.r - 0.344 * (c.g - 0.5) - 0.714 * (c.b - 0.5) , c.r + 1.772 * (c.g - 0.5));
}

vec3 RGB2YCoCg(vec3 c)
{
    return vec3( 0.25 * c.r + 0.5 * c.g + 0.25 * c.b, 0.5 * c.r - 0.5 * c.b + 0.5, -0.25 * c.r + 0.5 * c.g - 0.25 * c.b + 0.5);
}

vec3 YCoCg2RGB(vec3 c)
{
    c.y -= 0.5;
    c.z -= 0.5;
    return vec3(c.r + c.g - c.b, c.r + c.b, c.r - c.g - c.b);
}

float colorEdgeFilter(vec2 center, vec2 a0, vec2 a1, vec2 a2, vec2 a3)
{
    const float THRESH = 10.0 / 255.0;

    vec4 lum = vec4(a0.x, a1.x, a2.x, a3.x);
    vec4 w = 1.0 - step(THRESH, abs(lum - center.x));
    float W = w.x + w.y + w.z + w.w;

    // Handle the special case where all the weights are zero.
    // In HDR scenes it's better to set the chrominance to zero. 
    // Here we just use the chrominance of the first neighbor.
    w.x = (W == 0.0) ? 1.0 : w.x;
    W = (W == 0.0) ? 1.0 : W;

    return (w.x * a0.y + w.y * a1.y + w.z * a2.y + w.w * a3.y) / W;
}

vec2 encodeAlbedoCompressed(vec3 c, ivec2 fragCoord)
{
    // Convert the output color to the YCoCg space - and compress it
    //vec3 YCoCg = RGB2Same(c.rgb); 
    vec3 YCoCg = RGB2YCoCg(c.rgb); 
    //vec3 YCoCg = RGB2YCbCr(c.rgb); 
    
    // DEBUG : Chroma subsampling disabled
    // return RGB2Same(c.rgb); 
    //return YCoCg.rgb;
    
    // ivec2 crd = ivec2(fragCoord);

    // Store the YCo and YCg in a checkerboard pattern 
    bool isEven = (fragCoord.x & 1) == (fragCoord.y & 1);
    // bool isEven = 1 == mod( fragCoord.x + fragCoord.y, 2);
    //return (0 == mod( fragCoord.x + fragCoord.y, 2)) ? YCoCg.rg : YCoCg.rg;
    return vec2(isEven ? YCoCg.rg : YCoCg.rb);
}

vec3 decodeCompressedAlbedo(vec2 compressedYCoCg, sampler2D compressedTexture, vec2 texCoord, ivec2 fragCoord)
{
    // Only channels .rg were written (r = luma, g = chroma)
    // vec4 YCoCg = texture(compressedTexture, texCoord);
    vec3 YCoCg = vec3(compressedYCoCg, 0);

    // DEBUG : Chroma subsampling disabled
    // return Same2RGB(YCoCg.rgb);
    // return YCoCg2RGB(YCoCg.rgb);
    // return YCbCr2RGB(YCoCg.rgb);

    // Version 1 : use color edge filtering to remove artifacts from subsampling
    // vec2 a0 = textureOffset(compressedTexture, texCoord, ivec2(1,0)).rg ;
    // vec2 a1 = textureOffset(compressedTexture, texCoord, ivec2(-1,0)).rg;
    // vec2 a2 = textureOffset(compressedTexture, texCoord, ivec2(0,1)).rg;
    // vec2 a3 = textureOffset(compressedTexture, texCoord, ivec2(0,-1)).rg;    
    // vec2 a0 = texelFetch(compressedTexture, fragCoord + ivec2(1,0), 0).rg ;
    // vec2 a1 = texelFetch(compressedTexture, fragCoord + ivec2(-1,0), 0).rg;
    // vec2 a2 = texelFetch(compressedTexture, fragCoord + ivec2(0,1), 0).rg;
    // vec2 a3 = texelFetch(compressedTexture, fragCoord + ivec2(0,-1), 0).rg;    
    vec2 a0 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(1,0)).rg ;
    vec2 a1 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(-1,0)).rg;
    vec2 a2 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0,1)).rg;
    vec2 a3 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0,-1)).rg;    
    float chroma = colorEdgeFilter(YCoCg.rg, a0, a1, a2, a3);

    // Version 2 : use bilinear filtering to remove artifacts from subsampling
    // FIXME use HW bilinear filtering instead !
    // float chroma = 0.25 *    (textureOffset(compressedTexture, texCoord, ivec2(1,0)).g +
    //                          textureOffset(compressedTexture, texCoord, ivec2(-1,0)).g +
    //                          textureOffset(compressedTexture, texCoord, ivec2(0,1)).g +
    //                          textureOffset(compressedTexture, texCoord, ivec2(0,-1)).g);
    // float chroma = 0.25 *    (texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(1,0)).g +
    //                          texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(-1,0)).g +
    //                          texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0, 1)).g +
    //                          texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0,-1)).g);

    // Version 3 : use point filtering - but this won't remove artifacts from subsampling
    //float chroma = textureOffset(compressedTexture, texCoord, ivec2(1,0)).g;

    YCoCg.b = chroma;

    // Checker board pattern 
    bool isEven = (fragCoord.x & 1) == (fragCoord.y & 1);
    // bool isEven = 1 == mod( fragCoord.x + fragCoord.y, 2);
    //YCoCg.rgb = isEven ? YCoCg.rgb : YCoCg.rgb;
    YCoCg.rgb = isEven ? YCoCg.rgb : YCoCg.rbg;

    //return Same2RGB(YCoCg.rgb);
    return YCoCg2RGB(YCoCg.rgb);
    //return YCbCr2RGB(YCoCg.rgb);
}

vec2 encodeLightCompressed(vec3 c, ivec2 fragCoord)
{
    // Convert the output color to the YCoCg space - and compress it
    vec3 YCoCg = RGB2Same(c.rgb); 
    //vec3 YCoCg = RGB2YCoCg(c.rgb);
    //vec3 YCoCg = RGB2YCbCr(c.rgb); 

    // DEBUG : Chroma subsampling disabled
    // return RGB2Same(c.rgb); 
    //return YCoCg.rgb;

    // ivec2 crd = ivec2(fragCoord);

    // Store the YCo and YCg in a checkerboard pattern 
    bool isEven = (fragCoord.x & 1) == (fragCoord.y & 1);
    // bool isEven = 1 == mod( fragCoord.x + fragCoord.y, 2);
    //return (0 == mod( fragCoord.x + fragCoord.y, 2)) ? YCoCg.rg : YCoCg.rg;
    return vec2(isEven ? YCoCg.rg : YCoCg.rb);
}

vec3 decodeCompressedLight(vec2 compressedYCoCg, sampler2D compressedTexture, vec2 texCoord, ivec2 fragCoord)
{
    // Only channels .rg were written (r = luma, g = chroma)
    // vec4 YCoCg = texture(compressedTexture, texCoord);
    vec3 YCoCg = vec3(compressedYCoCg, 0);

    // DEBUG : Chroma subsampling disabled
    // return Same2RGB(YCoCg.rgb);
    // return YCoCg2RGB(YCoCg.rgb);
    // return YCbCr2RGB(YCoCg.rgb);

    // Version 1 : use color edge filtering to remove artifacts from subsampling
    // vec2 a0 = textureOffset(compressedTexture, texCoord, ivec2(1,0)).rg ;
    // vec2 a1 = textureOffset(compressedTexture, texCoord, ivec2(-1,0)).rg;
    // vec2 a2 = textureOffset(compressedTexture, texCoord, ivec2(0,1)).rg;
    // vec2 a3 = textureOffset(compressedTexture, texCoord, ivec2(0,-1)).rg;
    // vec2 a0 = texelFetch(compressedTexture, fragCoord + ivec2(1,0), 0).rg ;
    // vec2 a1 = texelFetch(compressedTexture, fragCoord + ivec2(-1,0), 0).rg;
    // vec2 a2 = texelFetch(compressedTexture, fragCoord + ivec2(0,1), 0).rg;
    // vec2 a3 = texelFetch(compressedTexture, fragCoord + ivec2(0,-1), 0).rg;
    vec2 a0 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(1, 0)).rg;
    vec2 a1 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(-1, 0)).rg;
    vec2 a2 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0, 1)).rg;
    vec2 a3 = texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0, -1)).rg;
    float chroma = colorEdgeFilter(YCoCg.rg, a0, a1, a2, a3);

    // Version 2 : use bilinear filtering to remove artifacts from subsampling
    // FIXME use HW bilinear filtering instead !
    // float chroma = 0.25 *    (textureOffset(compressedTexture, texCoord, ivec2(1,0)).g +
    //                          textureOffset(compressedTexture, texCoord, ivec2(-1,0)).g +
    //                          textureOffset(compressedTexture, texCoord, ivec2(0,1)).g +
    //                          textureOffset(compressedTexture, texCoord, ivec2(0,-1)).g);
    // float chroma = 0.25 *    (texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(1,0)).g +
    //                          texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(-1,0)).g +
    //                          texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0, 1)).g +
    //                          texelFetchOffset(compressedTexture, fragCoord, 0, ivec2(0,-1)).g);

    // Version 3 : use point filtering - but this won't remove artifacts from subsampling
    //float chroma = textureOffset(compressedTexture, texCoord, ivec2(1,0)).g;

    YCoCg.b = chroma;

    // Checker board pattern 
    bool isEven = (fragCoord.x & 1) == (fragCoord.y & 1);
    // bool isEven = 1 == mod( fragCoord.x + fragCoord.y, 2);
    //YCoCg.rgb = isEven ? YCoCg.rgb : YCoCg.rgb;
    YCoCg.rgb = isEven ? YCoCg.rgb : YCoCg.rbg;

    return Same2RGB(YCoCg.rgb);
    //return YCoCg2RGB(YCoCg.rgb);
    //return YCbCr2RGB(YCoCg.rgb);
}

//---------------------------------------------------------------------------------------------------------
float rand21(vec2 co)
{
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

//---------------------------------------------------------------------------------------------------------
float packFragBits(bool b0, bool b1, bool b7)
{
    float value = b0 ? 1.0f : 0.0f;
    value += b1 ? 2.0f : 0.0f;
    //value += b2 ? 4.0f : 0.0f;
    //value += b3 ? 8.0f : 0.0f;
    //value += b4 ? 16.0f : 0.0f;
    //value += b5 ? 32.0f : 0.0f;
    //value += b6 ? 64.0f : 0.0f;
    value += b7 ? 128.0f : 0.0f;

    return value/255.0f;
}

void unpackFragBits(float value, out bool b0, out bool b1, out bool b7)
{
    int bitField = int(value * 255.0f);
    b0 = (1 & bitField) != 0;
    b1 = (2 & bitField) != 0;
    //b2 = (4 & bitField) != 0;
    //b3 = (8 & bitField) != 0;
    //b4 = (16 & bitField) != 0;
    //b5 = (32 & bitField) != 0;
    //b6 = (64 & bitField) != 0;
    b7 = (128 & bitField) != 0;
}

void unpackFragBit1(float value, out bool b1)
{
    int bitField = int(value * 255.0f);
    b1 = (2 & bitField) != 0;
}

void unpackFragBit7(float value, out bool b7)
{
    int bitField = int(value * 255.0f);
    b7 = (128 & bitField) != 0;
}

#endif //DEFERRED_INCLUDE
