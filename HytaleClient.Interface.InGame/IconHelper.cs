using HytaleClient.Data.Items;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame;

internal static class IconHelper
{
	public static ClientItemIconProperties GetIconProperties(ClientItemBase item)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		ClientItemIconProperties defaultIconProperties = GetDefaultIconProperties(item, item.Armor?.ArmorSlot);
		if (item.IconProperties != null)
		{
			defaultIconProperties.Scale = item.IconProperties.Scale;
			if (item.IconProperties.Translation.HasValue)
			{
				defaultIconProperties.Translation = item.IconProperties.Translation;
			}
			if (item.IconProperties.Rotation.HasValue)
			{
				defaultIconProperties.Translation = item.IconProperties.Translation;
			}
		}
		return defaultIconProperties;
	}

	private static ClientItemIconProperties GetDefaultIconProperties(ClientItemBase item, ItemArmorSlot? armorSlot)
	{
		return GetDefaultIconProperties(item.Weapon != null, item.Tool != null, item.Armor != null, armorSlot);
	}

	public static ClientItemIconProperties GetDefaultIconProperties(bool isWeapon, bool isTool, bool isArmor, ItemArmorSlot? armorSlot)
	{
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected I4, but got Unknown
		if (isWeapon)
		{
			return new ClientItemIconProperties
			{
				Scale = 0.37f,
				Translation = new Vector2(-24.6f, -24.6f),
				Rotation = new Vector3(45f, 90f, 0f)
			};
		}
		if (isTool)
		{
			return new ClientItemIconProperties
			{
				Scale = 0.5f,
				Translation = new Vector2(-17.4f, -12f),
				Rotation = new Vector3(45f, 270f, 0f)
			};
		}
		if (isArmor)
		{
			switch (armorSlot)
			{
			case 1L:
				return new ClientItemIconProperties
				{
					Scale = 0.5f,
					Translation = new Vector2(-0f, -5f),
					Rotation = new Vector3(22.5f, 45f, 22.5f)
				};
			case 0L:
				return new ClientItemIconProperties
				{
					Scale = 0.5f,
					Translation = new Vector2(-0f, -3f),
					Rotation = new Vector3(22.5f, 45f, 22.5f)
				};
			case 3L:
				return new ClientItemIconProperties
				{
					Scale = 0.5f,
					Translation = new Vector2(-0f, -25.8f),
					Rotation = new Vector3(22.5f, 45f, 22.5f)
				};
			case 2L:
				return new ClientItemIconProperties
				{
					Scale = 0.92f,
					Translation = new Vector2(-0f, -10.8f),
					Rotation = new Vector3(22.5f, 45f, 22.5f)
				};
			}
		}
		return new ClientItemIconProperties
		{
			Scale = 0.58823f,
			Translation = new Vector2(0f, -13.5f),
			Rotation = new Vector3(22.5f, 45f, 22.5f)
		};
	}
}
