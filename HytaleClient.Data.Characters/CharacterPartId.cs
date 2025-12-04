namespace HytaleClient.Data.Characters;

public class CharacterPartId
{
	public readonly string PartId;

	public readonly string VariantId;

	public readonly string ColorId;

	public CharacterPartId(string partId, string colorId = null)
	{
		PartId = partId;
		ColorId = colorId;
	}

	public CharacterPartId(string partId, string variantId, string colorId)
	{
		PartId = partId;
		ColorId = colorId;
		VariantId = variantId;
	}

	public static CharacterPartId FromString(string id)
	{
		if (id == null)
		{
			return null;
		}
		string[] array = id.Split(new char[1] { '.' });
		return new CharacterPartId(array[0], (array.Length > 2) ? array[2] : null, (array.Length > 1) ? array[1] : null);
	}

	public static string BuildString(string partId, string variantId, string colorId)
	{
		if (variantId != null)
		{
			return partId + "." + colorId + "." + variantId;
		}
		return partId + "." + colorId;
	}

	public override string ToString()
	{
		return BuildString(PartId, VariantId, ColorId);
	}

	protected bool Equals(CharacterPartId other)
	{
		return PartId == other.PartId && VariantId == other.VariantId && ColorId == other.ColorId;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((CharacterPartId)obj);
	}

	public override int GetHashCode()
	{
		int num = ((PartId != null) ? PartId.GetHashCode() : 0);
		num = (num * 397) ^ ((VariantId != null) ? VariantId.GetHashCode() : 0);
		return (num * 397) ^ ((ColorId != null) ? ColorId.GetHashCode() : 0);
	}

	public static bool Equals(CharacterPartId id1, CharacterPartId id2)
	{
		if (id1 == id2)
		{
			return true;
		}
		if (id1 == null || id2 == null)
		{
			return false;
		}
		return id1.Equals(id2);
	}
}
