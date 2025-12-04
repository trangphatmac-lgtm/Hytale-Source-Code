#version 330 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uMVPMatrix;
uniform mat4 uSunMVPMatrix;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertPosition;
in vec2 vertTexCoords;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out vec4 fragTexCoords;

//-------------------------------------------------------------------------------------------------------------------------

void main()
{
    const vec4 sunVertPosition = vec4(0.5,0.5,0,1);

    gl_Position = uMVPMatrix * vec4(vertPosition, 1.0);

    // transform sun position to screen space [-1;1]
    vec4 sunPos = uSunMVPMatrix * sunVertPosition;
    sunPos = sunPos * 2.0f - 1.0f;

    vec2 ts = vec2(sunPos.x / sunPos.w , sunPos.y / sunPos.w);

    // texture coordinates for source render target
    fragTexCoords.xy = vertTexCoords;

    // use sun position in screen space to determine texture coordinates for glow mask
    fragTexCoords.zw = fragTexCoords.xy - 0.5f * ts;
}
