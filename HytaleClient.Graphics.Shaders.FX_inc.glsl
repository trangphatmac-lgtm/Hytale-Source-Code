#ifndef FX_INCLUDE
#define FX_INCLUDE

void ComputeIntersectionHighlight(float sceneDepthVS, float particleDepthVS, float intersectionHighlightThreshold, vec3 intersectionHighlightColor, inout vec4 sceneColor)
{
    float thresholdScale = 20.0;

    // fadeScale must not be 0, this is why we do 1.1 - intersectionHighlightThreshold and not 1.0 - intersectionHighlightThreshold
    float fadeScale =  (1.1 - intersectionHighlightThreshold) * thresholdScale;

    float zDist = particleDepthVS - sceneDepthVS;
    float zFade = clamp(1.0 - clamp(zDist * fadeScale, 0.0, 1.0), 0.05, 1.0);

    sceneColor.rgb = mix(sceneColor.rgb, intersectionHighlightColor, zFade);
    sceneColor.a = max(sceneColor.a, zFade);
}

#endif //FX_INCLUDE
