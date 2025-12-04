using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal delegate void UpdateParticleCollisionFunc(ParticleSpawner particleSpawner, ref ParticleBuffers.ParticleSimulationData particleData0, ref ParticleBuffers.ParticleRenderData particleData1, ref Vector2 particleScale, ref ParticleBuffers.ParticleLifeData particleLife, Vector3 previousPosition, Quaternion inverseRotation);
