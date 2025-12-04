#version 330 core

#ifndef GENERATE_QUAD_CORNERS
#define GENERATE_QUAD_CORNERS 1
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uViewProjectionMatrix;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec4 vertPosition;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
// none : we only write the gl_Position

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    vec3 fxPosition = vertPosition.xyz;

    // It's better to avoid matching exactly :
    // it would produce discontinuities more easily between chunks planes,
    // which would result in holes in the HiZ map,
    // producing worse results during the culling test.
    //fxPosition.y += vertPosition.w;

#if GENERATE_QUAD_CORNERS
    const vec3 extent = vec3(32,32,32);
    const vec2 QuadCornersFactor[4] = vec2[] (  vec2(1,1),
                                                vec2(1,0),
                                                vec2(0,0),
                                                vec2(0,1));
    int quadVertexId = gl_VertexID % 4;

    fxPosition.xz += extent.xz * QuadCornersFactor[quadVertexId];
#endif

    gl_Position = uViewProjectionMatrix * vec4(fxPosition, 1.0);
}
