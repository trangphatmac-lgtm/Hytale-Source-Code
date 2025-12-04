using HytaleClient.Math;

namespace HytaleClient.Graphics.Particles;

internal class ParticleSystemProxy
{
	public ParticleSystemSettings Settings;

	public ParticleSystem ParticleSystem;

	public Vector2 TextureAltasInverseSize;

	public bool VisibilityPrediction = true;

	public bool Visible = true;

	public Vector3 Position;

	public Quaternion Rotation = Quaternion.Identity;

	public float Scale = 1f;

	public UInt32Color DefaultColor = ParticleSettings.DefaultColor;

	public bool IsOvergroundOnly = false;

	public bool IsLocalPlayer = false;

	public bool IsTracked = false;

	public bool IsFirstPerson { get; private set; } = false;


	public bool IsExpired { get; private set; } = false;


	public bool HasInstantExpire { get; private set; } = false;


	public void SetFirstPerson(bool isFirstPerson)
	{
		IsFirstPerson = isFirstPerson;
		ParticleSystem?.SetFirstPerson(isFirstPerson);
	}

	public void Expire(bool instant = false)
	{
		IsExpired = true;
		HasInstantExpire = instant;
	}
}
