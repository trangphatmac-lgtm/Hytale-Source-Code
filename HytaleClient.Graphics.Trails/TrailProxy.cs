using HytaleClient.Math;

namespace HytaleClient.Graphics.Trails;

internal class TrailProxy
{
	public TrailSettings Settings;

	public Trail Trail;

	public Vector2 TextureAltasInverseSize;

	public bool Visible = true;

	public Vector3 Position;

	public Quaternion Rotation = Quaternion.Identity;

	public float Scale = 1f;

	public bool IsLocalPlayer = false;

	public bool IsExpired = false;

	public bool IsFirstPerson { get; private set; } = false;


	public void SetFirstPerson(bool isFirstPerson)
	{
		IsFirstPerson = isFirstPerson;
		if (Trail != null)
		{
			Trail.IsFirstPerson = isFirstPerson;
		}
	}
}
