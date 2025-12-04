#ifndef QUATERNION_INCLUDE
#define QUATERNION_INCLUDE

vec4 quaternionConjugate(vec4 q)
{
    return vec4(-q.x, -q.y, -q.z, q.w); 
}

vec4 quaternionMultiply(vec4 q1, vec4 q2)
{
    vec4 qr;
    qr.x = (q1.w * q2.x) + (q1.x * q2.w) + (q1.y * q2.z) - (q1.z * q2.y);
    qr.y = (q1.w * q2.y) - (q1.x * q2.z) + (q1.y * q2.w) + (q1.z * q2.x);
    qr.z = (q1.w * q2.z) + (q1.x * q2.y) - (q1.y * q2.x) + (q1.z * q2.w);
    qr.w = (q1.w * q2.w) - (q1.x * q2.x) - (q1.y * q2.y) - (q1.z * q2.z);
    return qr;
}

vec4 quaternionFromAxisAngle(vec3 axis, float angle)
{
    vec4 qr;
    float half_angle = (angle * 0.5);// * 3.14159 / 180.0;
    qr.xyz = axis.xyz * sin(half_angle);
    qr.w = cos(half_angle);
    return qr;
}

vec3 rotateVector(vec3 position, vec4 quaternion)
{
    vec4 q = quaternion;
    vec3 v = position.xyz;
    return v + 2.0 * cross(q.xyz, cross(q.xyz, v) + q.w * v);
}

vec3 rotateVector(vec3 position, vec3 axis, float angle)
{
    vec4 q = quaternionFromAxisAngle(axis, angle);
    return rotateVector(position, q);
}

vec3 computeDirectionFromVelocity(vec3 velocity)
{
    float velocityLength = length(velocity);
    return (velocityLength > 0) ? -velocity / velocityLength : vec3(0.0, 1.0, 0.0);
}

vec4 quaternionFromVelocity(vec3 velocity)
{
    vec4 qr;

    const vec3 right = vec3(1,0,0);
    const vec3 up = vec3(0,1,0);
    const vec3 back = vec3(0,0,1);
    const vec3 vToCamera = back;
    
    vec3 direction = computeDirectionFromVelocity(velocity);

    float dot = dot(vToCamera, direction);

    if (dot >= 1.0f)
    {
        qr = vec4(0.0f, 0.0f, 0.0f, 1.0f);
    }
    else
    if (dot <= -1.0f)
    {
        // NB: since this is const data only, so we can precompute the result quaternion
#if 0
        vec3 axis = cross(right, vToCamera);
        if (length(axis) == 0.0f)  axis = cross(up, vToCamera);
        axis = normalize(axis);

        const float pi = 3.14159;
        qr = quaternionFromAxisAngle(axis, pi);
#endif
        qr = vec4(0.0f, -1.0f, 0.0f, 0.0f);
    }
    else
    {    
        float sqrtVar = sqrt((1.0f + dot) * 2.0f);
        vec3 crossVar = cross(vToCamera, direction) / sqrtVar;

        qr = vec4(crossVar.xyz, 0.5f * sqrtVar);
        qr = normalize(qr);
    }

    return qr;
}

#endif //QUATERNION_INCLUDE
