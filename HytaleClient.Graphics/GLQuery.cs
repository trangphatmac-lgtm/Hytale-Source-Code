namespace HytaleClient.Graphics;

public struct GLQuery
{
	public static readonly GLQuery None = new GLQuery(0u);

	public readonly uint InternalId;

	public GLQuery(uint id)
	{
		InternalId = id;
	}

	public static implicit operator uint(GLQuery q)
	{
		return q.InternalId;
	}

	public static implicit operator GLQuery(uint q)
	{
		return new GLQuery(q);
	}

	public override bool Equals(object obj)
	{
		if (obj is GLQuery)
		{
			return ((GLQuery)obj).InternalId == InternalId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint internalId = InternalId;
		return internalId.GetHashCode();
	}

	public static bool operator ==(GLQuery a, GLQuery b)
	{
		return a.InternalId == b.InternalId;
	}

	public static bool operator !=(GLQuery a, GLQuery b)
	{
		return a.InternalId != b.InternalId;
	}
}
