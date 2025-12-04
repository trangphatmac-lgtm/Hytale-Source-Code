namespace HytaleClient.Graphics;

public struct GLVertexArray
{
	public static readonly GLVertexArray None = new GLVertexArray(0u);

	public readonly uint InternalId;

	public GLVertexArray(uint id)
	{
		InternalId = id;
	}

	public override bool Equals(object obj)
	{
		if (obj is GLVertexArray)
		{
			return ((GLVertexArray)obj).InternalId == InternalId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint internalId = InternalId;
		return internalId.GetHashCode();
	}

	public static bool operator ==(GLVertexArray a, GLVertexArray b)
	{
		return a.InternalId == b.InternalId;
	}

	public static bool operator !=(GLVertexArray a, GLVertexArray b)
	{
		return a.InternalId != b.InternalId;
	}
}
