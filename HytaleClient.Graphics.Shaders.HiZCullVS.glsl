#version 150 core

//-------------------------------------------------------------------------------------------------------------------------
//                                  Shader Params
//-------------------------------------------------------------------------------------------------------------------------
uniform mat4 uViewProjectionMatrix;
uniform vec2 uViewportSize;
uniform sampler2D uHiZBuffer;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Vertex Attributes
//-------------------------------------------------------------------------------------------------------------------------
in vec3 vertBoxMin;
in vec3 vertBoxMax;

//-------------------------------------------------------------------------------------------------------------------------
//                                  Out
//-------------------------------------------------------------------------------------------------------------------------
out int outVisible;

//-------------------------------------------------------------------------------------------------------------------------

vec4 boundingBox[8];

float max3(vec3 vector)
{
    return max(max(vector.x, vector.y), vector.z);
}

void UpdateBoundingBox(vec3 center, vec3 halfSize)
{    
     //create the bounding box of the object
    boundingBox[0] = uViewProjectionMatrix * vec4(center + vec3(  halfSize.x, halfSize.y, halfSize.z), 1.0 );
    boundingBox[1] = uViewProjectionMatrix * vec4(center + vec3( -halfSize.x, halfSize.y, halfSize.z), 1.0 );
    boundingBox[2] = uViewProjectionMatrix * vec4(center + vec3(  halfSize.x,-halfSize.y, halfSize.z), 1.0 );
    boundingBox[3] = uViewProjectionMatrix * vec4(center + vec3( -halfSize.x,-halfSize.y, halfSize.z), 1.0 );
    boundingBox[4] = uViewProjectionMatrix * vec4(center + vec3(  halfSize.x, halfSize.y,-halfSize.z), 1.0 );
    boundingBox[5] = uViewProjectionMatrix * vec4(center + vec3( -halfSize.x, halfSize.y,-halfSize.z), 1.0 );
    boundingBox[6] = uViewProjectionMatrix * vec4(center + vec3(  halfSize.x,-halfSize.y,-halfSize.z), 1.0 );
    boundingBox[7] = uViewProjectionMatrix * vec4(center + vec3( -halfSize.x,-halfSize.y,-halfSize.z), 1.0 );
    
//    vec3 position = center - halfSize;
//    vec3 extent = halfSize * 2.0;
//    boundingBox[0] = uViewProjectionMatrix * vec4(position + vec3(extent.x, extent.y, extent.z), 1.0);
//    boundingBox[1] = uViewProjectionMatrix * vec4(position + vec3(       0, extent.y, extent.z), 1.0);
//    boundingBox[2] = uViewProjectionMatrix * vec4(position + vec3(extent.x,        0, extent.z), 1.0);
//    boundingBox[3] = uViewProjectionMatrix * vec4(position + vec3(       0,        0, extent.z), 1.0);
//    boundingBox[4] = uViewProjectionMatrix * vec4(position + vec3(extent.x, extent.y,        0), 1.0);
//    boundingBox[5] = uViewProjectionMatrix * vec4(position + vec3(       0, extent.y,        0), 1.0);
//    boundingBox[6] = uViewProjectionMatrix * vec4(position + vec3(extent.x,        0,        0), 1.0);
//    boundingBox[7] = uViewProjectionMatrix * vec4(position + vec3(       0,        0,        0), 1.0);
}

int HiZOcclusionCull(vec3 boxCenter, vec3 boxHalfSize)
{
    // Perform perspective division for the bounding box, thus going to NDC [-1;1]
    for (int i=0; i<8; i++)
    {
        boundingBox[i].xyz /= boundingBox[i].w;
    }

    // TODO add a frustum culling step?

    // Calculate screen space bounding rectangle
    vec2 boundingRect[2];
    boundingRect[0].x = min(min(min(boundingBox[0].x, boundingBox[1].x),
                                min(boundingBox[2].x, boundingBox[3].x)),
                            min(min(boundingBox[4].x, boundingBox[5].x),
                                min(boundingBox[6].x, boundingBox[7].x))) * 0.5 + 0.5;
    boundingRect[0].y = min(min(min(boundingBox[0].y, boundingBox[1].y),
                                min(boundingBox[2].y, boundingBox[3].y)),
                            min(min(boundingBox[4].y, boundingBox[5].y),
                                min(boundingBox[6].y, boundingBox[7].y))) * 0.5 + 0.5;
    boundingRect[1].x = max(max(max(boundingBox[0].x, boundingBox[1].x),
                                max(boundingBox[2].x, boundingBox[3].x)),
                            max(max(boundingBox[4].x, boundingBox[5].x),
                                max(boundingBox[6].x, boundingBox[7].x))) * 0.5 + 0.5;
    boundingRect[1].y = max(max(max(boundingBox[0].y, boundingBox[1].y),
                                max(boundingBox[2].y, boundingBox[3].y)),
                            max(max(boundingBox[4].y, boundingBox[5].y),
                                max(boundingBox[6].y, boundingBox[7].y))) * 0.5 + 0.5;
    

    // Then the linear depth value of the front-most point
    float instanceMinDepth = min(min(min(boundingBox[0].z, boundingBox[1].z),
                                     min(boundingBox[2].z, boundingBox[3].z)),
                                 min(min(boundingBox[4].z, boundingBox[5].z),
                                     min(boundingBox[6].z, boundingBox[7].z)));
    
    // Then go from NDC [-1;1] to actual Z buffer storage range [0;1]
    instanceMinDepth = instanceMinDepth * 0.5 + 0.5;

    // Clamp to the [0,1] screen space coordinates
    boundingRect[0].x = clamp(boundingRect[0].x, 0, 1);
    boundingRect[0].y = clamp(boundingRect[0].y, 0, 1);
    boundingRect[1].x = clamp(boundingRect[1].x, 0, 1);
    boundingRect[1].y = clamp(boundingRect[1].y, 0, 1);

    // Now we calculate the bounding rectangle size in viewport coordinates
    float viewSizeX = (boundingRect[1].x - boundingRect[0].x) * uViewportSize.x;
    float viewSizeY = (boundingRect[1].y - boundingRect[0].y) * uViewportSize.y;

    // We calculate the texture LOD used for lookup in the depth buffer texture
    // Hack: to get better results, we use a different precision for small and big boundingBoxes.
    float lodDivider = any(lessThan(boxHalfSize, vec3(12.0))) ? 4.0 : 8.0;
    float LOD = min(8, ceil( log2( max(viewSizeX, viewSizeY) / lodDivider)));

    // We calculate the texel min / max
    ivec2 mipSize = ivec2(uViewportSize / pow(2.0,LOD));
    
    int minX = clamp(int(boundingRect[0].x * mipSize.x), 0, mipSize.x - 1);
    int maxX = clamp(int(boundingRect[1].x * mipSize.x), 0, mipSize.x - 1);
    int minY = clamp(int(boundingRect[0].y * mipSize.y), 0, mipSize.y - 1);
    int maxY = clamp(int(boundingRect[1].y * mipSize.y), 0, mipSize.y - 1);
    
    // Finally fetch the depth texture using explicit LOD lookups
    int level = int(LOD);
    float maxDepth = 0;
    for (int x = minX; x <= maxX; x++)
    {
        for (int y = minY; y <= maxY; y++)
        {
            ivec2 texelPos = ivec2(x,y);
            float depth = texelFetch(uHiZBuffer, texelPos, level).x;
            maxDepth = max(depth, maxDepth);
        }
    }

    // Hack to compensate precision issue we have w/ close chunks	
    float distanceToCamera = distance(boxCenter, vec3(0));
    float maxSize = max3(boxHalfSize) * 2;
    float threshold = sqrt(maxSize * maxSize * 3);

    // If the instance depth is lower than the depth in the texture, or if the chunk is too close, it's visible!
    return (instanceMinDepth <= maxDepth || distanceToCamera < threshold) ? 1 : 0;
}

//-------------------------------------------------------------------------------------------------------------------------

void main(void) 
{
    vec3 center = (vertBoxMax + vertBoxMin) * 0.5f;
    vec3 halfSize = (vertBoxMax - vertBoxMin) * 0.5f;

    // Clamp the bbox size to something "not too small", to avoid bad surprises like precision issues in the distance.
    halfSize = max(halfSize, vec3(0.5));

    UpdateBoundingBox(center, halfSize);
    outVisible = HiZOcclusionCull(center, halfSize);
}
