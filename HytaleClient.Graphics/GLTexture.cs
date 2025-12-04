namespace HytaleClient.Graphics;

public struct GLTexture
{
	public static readonly GLTexture None = new GLTexture(0u);

	public readonly uint InternalId;

	public GLTexture(uint id)
	{
		InternalId = id;
	}

	public override bool Equals(object obj)
	{
		if (obj is GLTexture)
		{
			return ((GLTexture)obj).InternalId == InternalId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint internalId = InternalId;
		return internalId.GetHashCode();
	}

	public static bool operator ==(GLTexture a, GLTexture b)
	{
		return a.InternalId == b.InternalId;
	}

	public static bool operator !=(GLTexture a, GLTexture b)
	{
		return a.InternalId != b.InternalId;
	}
}
