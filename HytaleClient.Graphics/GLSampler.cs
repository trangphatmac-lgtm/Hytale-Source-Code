namespace HytaleClient.Graphics;

public struct GLSampler
{
	public static readonly GLSampler None = new GLSampler(0u);

	public readonly uint InternalId;

	public GLSampler(uint id)
	{
		InternalId = id;
	}

	public override bool Equals(object obj)
	{
		if (obj is GLSampler)
		{
			return ((GLSampler)obj).InternalId == InternalId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint internalId = InternalId;
		return internalId.GetHashCode();
	}

	public static bool operator ==(GLSampler a, GLSampler b)
	{
		return a.InternalId == b.InternalId;
	}

	public static bool operator !=(GLSampler a, GLSampler b)
	{
		return a.InternalId != b.InternalId;
	}
}
