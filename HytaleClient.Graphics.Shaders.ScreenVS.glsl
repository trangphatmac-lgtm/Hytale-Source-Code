#version 330 core

// This is the preferred VertexShader for fullscreen rendering.
// Customize at compile time w/ the following options :
//  #define USE_MVP_MATRIX 1 : use the MVP matrix; else assumes the vertices are already in projected space [-1;1]
//  #define USE_FULLSCREEN_TRIANGLE 1 : assumes the fullscreen is done w/ a single Triangle; else assumes a Quad
//  #define USE_FAR_CORNERS 1 : use the FarCorners (input/output) for fast Position reconstruction in the following Fragment Shader
//  #define USE_VBO_ATTRIBUTES 1 : use the attributes provided by the VBO; else generate them on - works only for fullscreen Triangle !

#ifndef USE_MVP_MATRIX
#define USE_MVP_MATRIX 0
#endif // USE_MVP_MATRIX
#ifndef USE_FAR_CORNERS
#define USE_FAR_CORNERS 0
#endif // USE_FAR_CORNERS
#ifndef USE_FULLSCREEN_TRIANGLE
#define USE_FULLSCREEN_TRIANGLE 1
#endif // USE_FULLSCREEN_TRIANGLE
#ifndef USE_VBO_ATTRIBUTES
#define USE_VBO_ATTRIBUTES 0
#endif
//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
#if USE_MVP_MATRIX
uniform mat4 uMVPMatrix;
#endif

#if USE_FAR_CORNERS
uniform vec3 uFarCorners[4];
#define CORNER_TOP_LEFT 0
#define CORNER_TOP_RIGHT 1
#define CORNER_BOTTOM_RIGH 2
#define CORNER_BOTTOM_LEFT 3
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
#if USE_VBO_ATTRIBUTES
in vec3 vertPosition;
in vec2 vertTexCoords;
#endif

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec2 fragTexCoords;

#if USE_FAR_CORNERS
out vec3 fragFrustumRay;
#endif

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
#if USE_VBO_ATTRIBUTES

#if USE_MVP_MATRIX
    gl_Position = uMVPMatrix * vec4(vertPosition, 1.0);
#else
    gl_Position = vec4(vertPosition, 1.0);
#endif // USE_MVP_MATRIX

    fragTexCoords = vertTexCoords;
#else

    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    gl_Position = vec4(x, y, 0, 1);

    fragTexCoords.x = (x+1.0)*0.5;
    fragTexCoords.y = (y+1.0)*0.5;

#endif

#if USE_FAR_CORNERS
#if USE_FULLSCREEN_TRIANGLE
#if USE_VBO_ATTRIBUTES
    if (fragTexCoords == vec2(0,2))
    {
        fragFrustumRay = uFarCorners[CORNER_TOP_LEFT];
    }
    else
    if (fragTexCoords == vec2(0,0))
    {
        fragFrustumRay = uFarCorners[CORNER_BOTTOM_LEFT];
    }
    else
    if (fragTexCoords == vec2(2,0))
    {
        fragFrustumRay = uFarCorners[CORNER_BOTTOM_RIGH];
    }
#else
    if (gl_VertexID == 0)
    {
        fragFrustumRay = uFarCorners[CORNER_BOTTOM_LEFT];
    }
    else
    if (gl_VertexID == 1)
    {
        fragFrustumRay = uFarCorners[CORNER_BOTTOM_RIGH];
    }
    else
    {
        fragFrustumRay = uFarCorners[CORNER_TOP_LEFT];
    }

#endif // USE_VBO_ATTRIBUTES
#else
    if (fragTexCoords == vec2(0,1))
    {
        fragFrustumRay = uFarCorners[CORNER_TOP_LEFT];
    }
    else
    if (fragTexCoords == vec2(1,1))
    {
        fragFrustumRay = uFarCorners[CORNER_TOP_RIGHT];
    }
    else
    if (fragTexCoords == vec2(0,0))
    {
        fragFrustumRay = uFarCorners[CORNER_BOTTOM_LEFT];
    }
    else
    if (fragTexCoords == vec2(1,0))
    {
        fragFrustumRay = uFarCorners[CORNER_BOTTOM_RIGH];
    }
#endif // USE_FULLSCREEN_TRIANGLE
#endif // USE_FAR_CORNERS
}
