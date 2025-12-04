#ifndef FALLBACK_INCLUDE
#define FALLBACK_INCLUDE

// Make sure you #enable the right extenstion and have the appropriate #defines before including this file.

// #extension GL_ARB_gpu_shader5 : enable
// #define PIXELSIZE uPixelSize

#ifdef GL_ARB_gpu_shader5
    // use the efficient built-in version
    #define TextureGather(t, p, c) textureGather(t, p, c)
    #define TextureGatherOffset(t, p, o, c) textureGatherOffset(t, p, o, c)
    #define TextureGatherOffsets(t, p, o, c) textureGatherOffsets(t, p, o, c)

	#define TextureGatherR(t, p) textureGather(t, p, 0)
	#define TextureGatherOffsetR(t, p, o) textureGatherOffset(t, p, o, 0)
	#define TextureGatherOffsetsR(t, p, o) textureGatherOffsets(t, p, o, 0)

	#define TextureGatherG(t, p) textureGather(t, p, 1)
	#define TextureGatherOffsetG(t, p, o) textureGatherOffset(t, p, o, 1)
	#define TextureGatherOffsetsG(t, p, o) textureGatherOffsets(t, p, o, 1)
#else        
    // provide replacement for missing hardware features
    // NB: textureGather bypasses filtering, so we must use texelFetch to reproduce this behaviour
    // see https://www.khronos.org/opengl/wiki/Sampler_(GLSL)#Texture_gather_accesses
    #define fallbackTextureGather(t, p, c)            vec4( texelFetch(t, ivec2(0,1) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, ivec2(1,1) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, ivec2(1,0) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, ivec2(p/PIXELSIZE),0).c )
    #define fallbackTextureGatherOffset(t, p, o, c) vec4(   texelFetch(t, o + ivec2(0,1) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, o + ivec2(1,1) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, o + ivec2(1,0) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, o + ivec2(p/PIXELSIZE),0).c )
    #define fallbackTextureGatherOffsets(t, p, o, c) vec4(  texelFetch(t, o[0] + ivec2(0,1) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, o[1] + ivec2(1,1) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, o[2] + ivec2(1,0) + ivec2(p/PIXELSIZE),0).c,\
                                                            texelFetch(t, o[3] + ivec2(p/PIXELSIZE),0).c )

////    #define fallbackTextureGather(t, p, c)            vec4( textureOffset(t, p, ivec2(0,1)).c,\
////                                                            textureOffset(t, p, ivec2(1,1)).c,\
////                                                            textureOffset(t, p, ivec2(1,0)).c,\
////                                                            texture(t, p).c )
////    #define fallbackTextureGatherOffset(t, p, o, c) vec4(   textureOffset(t, p, ivec2(0,1) + o).c,\
////                                                            textureOffset(t, p, ivec2(1,1) + o).c,\
////                                                            textureOffset(t, p, ivec2(1,0) + o).c,\
////                                                            textureOffset(t, p, o).c )
////    #define fallbackTextureGatherOffsets(t, p, o, c) vec4(  textureOffset(t, p, o[0] + ivec2(0,1)).c,\
////                                                            textureOffset(t, p, o[1] + ivec2(1,1)).c,\
////                                                            textureOffset(t, p, o[2] + ivec2(1,0)).c,\
////                                                            textureOffset(t, p, o[3]).c )

    #define TextureGather(t, p, c) fallbackTextureGather(t, (p - PIXELSIZE * 0.5), c)
    #define TextureGatherOffset(t, p, o, c) fallbackTextureGatherOffset(t, (p - PIXELSIZE * 0.5), o, c)
    #define TextureGatherOffsets(t, p, o, c) fallbackTextureGatherOffsets(t, (p - PIXELSIZE * 0.5), o, c)

	#define TextureGatherR(t, p) fallbackTextureGather(t,(p - PIXELSIZE * 0.5),r)
	#define TextureGatherOffsetR(t, p, o) fallbackTextureGatherOffset(t, (p - PIXELSIZE * 0.5), o, r)
	#define TextureGatherOffsetsR(t, p, o) fallbackTextureGatherOffsets(t, (p - PIXELSIZE * 0.5), o, r)

	#define TextureGatherG(t, p) fallbackTextureGather(t,p,g)
	#define TextureGatherOffsetG(t, p, o) fallbackTextureGatherOffset(t, p, o, g)
	#define TextureGatherOffsetsG(t, p, o) fallbackTextureGatherOffsets(t, p, o, g)
#endif

#endif //FALLBACK_INCLUDE
