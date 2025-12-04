namespace HytaleClient.Graphics;

public struct GLFramebuffer
{
	public static readonly GLFramebuffer None = new GLFramebuffer(0u);

	public readonly uint InternalId;

	public GLFramebuffer(uint id)
	{
		InternalId = id;
	}

	public override bool Equals(object obj)
	{
		if (obj is GLFramebuffer)
		{
			return ((GLFramebuffer)obj).InternalId == InternalId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint internalId = InternalId;
		return internalId.GetHashCode();
	}

	public static bool operator ==(GLFramebuffer a, GLFramebuffer b)
	{
		return a.InternalId == b.InternalId;
	}

	public static bool operator !=(GLFramebuffer a, GLFramebuffer b)
	{
		return a.InternalId != b.InternalId;
	}
}
