namespace HytaleClient.Data.Characters;

public class ClientPlayerSkin
{
	public CharacterBodyType BodyType;

	public string SkinTone;

	public string Face;

	public CharacterPartId Eyes;

	public CharacterPartId Haircut;

	public CharacterPartId FacialHair;

	public CharacterPartId Eyebrows;

	public CharacterPartId Pants;

	public CharacterPartId Overpants;

	public CharacterPartId Undertop;

	public CharacterPartId Overtop;

	public CharacterPartId Shoes;

	public CharacterPartId HeadAccessory;

	public CharacterPartId FaceAccessory;

	public CharacterPartId EarAccessory;

	public CharacterPartId SkinFeature;

	public CharacterPartId Gloves;

	public ClientPlayerSkin()
	{
	}

	public ClientPlayerSkin(ClientPlayerSkin other)
	{
		BodyType = other.BodyType;
		SkinTone = other.SkinTone;
		Eyes = other.Eyes;
		FacialHair = other.FacialHair;
		Haircut = other.Haircut;
		Eyebrows = other.Eyebrows;
		Face = other.Face;
		Pants = other.Pants;
		Overpants = other.Overpants;
		Undertop = other.Undertop;
		Overtop = other.Overtop;
		Shoes = other.Shoes;
		HeadAccessory = other.HeadAccessory;
		FaceAccessory = other.FaceAccessory;
		EarAccessory = other.EarAccessory;
		SkinFeature = other.SkinFeature;
		Gloves = other.Gloves;
	}

	protected bool Equals(ClientPlayerSkin other)
	{
		return BodyType == other.BodyType && string.Equals(SkinTone, other.SkinTone) && CharacterPartId.Equals(Eyes, other.Eyes) && CharacterPartId.Equals(FacialHair, other.FacialHair) && CharacterPartId.Equals(Haircut, other.Haircut) && CharacterPartId.Equals(Eyebrows, other.Eyebrows) && object.Equals(Face, other.Face) && CharacterPartId.Equals(Pants, other.Pants) && CharacterPartId.Equals(Overpants, other.Overpants) && CharacterPartId.Equals(Undertop, other.Undertop) && CharacterPartId.Equals(Overtop, other.Overtop) && CharacterPartId.Equals(Shoes, other.Shoes) && CharacterPartId.Equals(HeadAccessory, other.HeadAccessory) && CharacterPartId.Equals(FaceAccessory, other.FaceAccessory) && CharacterPartId.Equals(EarAccessory, other.EarAccessory) && CharacterPartId.Equals(SkinFeature, other.SkinFeature) && CharacterPartId.Equals(Gloves, other.Gloves);
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
		return Equals((ClientPlayerSkin)obj);
	}

	public override int GetHashCode()
	{
		int bodyType = (int)BodyType;
		bodyType = (bodyType * 397) ^ ((SkinTone != null) ? SkinTone.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Eyes != null) ? Eyes.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((FacialHair != null) ? FacialHair.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Haircut != null) ? Haircut.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Eyebrows != null) ? Eyebrows.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Face != null) ? Face.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Pants != null) ? Pants.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Overpants != null) ? Overpants.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Undertop != null) ? Undertop.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Overtop != null) ? Overtop.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((Shoes != null) ? Shoes.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((HeadAccessory != null) ? HeadAccessory.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((FaceAccessory != null) ? FaceAccessory.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((EarAccessory != null) ? EarAccessory.GetHashCode() : 0);
		bodyType = (bodyType * 397) ^ ((SkinFeature != null) ? SkinFeature.GetHashCode() : 0);
		return (bodyType * 397) ^ ((Gloves != null) ? Gloves.GetHashCode() : 0);
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, ", "BodyType", BodyType) + "SkinTone: " + SkinTone + ", Face: " + Face + ", " + string.Format("{0}: {1}, ", "Eyes", Eyes) + string.Format("{0}: {1}, ", "FacialHair", FacialHair) + string.Format("{0}: {1}, ", "Haircut", Haircut) + string.Format("{0}: {1}, ", "Eyebrows", Eyebrows) + "Face: " + Face + ", " + string.Format("{0}: {1}, ", "Pants", Pants) + string.Format("{0}: {1}, ", "Overpants", Overpants) + string.Format("{0}: {1}, ", "Undertop", Undertop) + string.Format("{0}: {1}, ", "Overtop", Overtop) + string.Format("{0}: {1}, ", "Shoes", Shoes) + string.Format("{0}: {1}, ", "HeadAccessory", HeadAccessory) + string.Format("{0}: {1}, ", "FaceAccessory", FaceAccessory) + string.Format("{0}: {1}, ", "EarAccessory", EarAccessory) + string.Format("{0}: {1}, ", "SkinFeature", SkinFeature) + string.Format("{0}: {1}", "Gloves", Gloves);
	}
}
