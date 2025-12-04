namespace HytaleClient.Graphics;

public struct GLBuffer
{
	public static readonly GLBuffer None = new GLBuffer(0u);

	public readonly uint InternalId;

	public GLBuffer(uint id)
	{
		InternalId = id;
	}

	public override bool Equals(object obj)
	{
		if (obj is GLBuffer)
		{
			return ((GLBuffer)obj).InternalId == InternalId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint internalId = InternalId;
		return internalId.GetHashCode();
	}

	public static bool operator ==(GLBuffer a, GLBuffer b)
	{
		return a.InternalId == b.InternalId;
	}

	public static bool operator !=(GLBuffer a, GLBuffer b)
	{
		return a.InternalId != b.InternalId;
	}
}
