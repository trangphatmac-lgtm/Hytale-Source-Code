using System;
using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;

internal class BrushData
{
	public const string MaterialKey = "Material";

	public const string FavoriteMaterialsKey = "FavoriteMaterials";

	public const string WidthKey = "Width";

	public const string HeightKey = "Height";

	public const string ThicknessKey = "Thickness";

	public const string CappedKey = "Capped";

	public const string ShapeKey = "Shape";

	public const string OriginKey = "Origin";

	public const string OriginRotationKey = "OriginRotation";

	public const string RotationAxisKey = "RotationAxis";

	public const string RotationAngleKey = "RotationAngle";

	public const string MirrorAxisKey = "MirrorAxis";

	public const string MaskKey = "Mask";

	public const string MaskAboveKey = "MaskAbove";

	public const string MaskNotKey = "MaskNot";

	public const string MaskBelowKey = "MaskBelow";

	public const string MaskAdjacentKey = "MaskAdjacent";

	public const string MaskNeighborKey = "MaskNeighbor";

	public const string MaskCommandsKey = "MaskCommands";

	public const string UseMaskCommandsKey = "UseMaskCommands";

	public const int MaskMaxNumber = 7;

	public readonly ClientItemStack[] EmptyClientItemStackArray = new ClientItemStack[0];

	public readonly string Material;

	public readonly string[] FavoriteMaterials;

	public readonly int Width;

	public readonly int Height;

	public readonly int Thickness;

	public readonly bool Capped;

	public readonly BrushShape Shape;

	public readonly BrushOrigin Origin;

	public readonly bool OriginRotation;

	public readonly BrushAxis RotationAxis;

	public readonly Rotation RotationAngle;

	public readonly BrushAxis MirrorAxis;

	public readonly string Mask;

	public readonly string MaskAbove;

	public readonly string MaskNot;

	public readonly string MaskBelow;

	public readonly string MaskAdjacent;

	public readonly string MaskNeighbor;

	public readonly string[] MaskCommands;

	public readonly bool UseMaskCommands;

	private const int DefaultWidth = 5;

	private const int DefaultHeight = 5;

	private readonly Action<string, string> _onDataChange;

	public BrushData(ClientItemStack item = null, BuilderTool builderTool = null, Action<string, string> onDataChange = null)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_004b: Expected O, but got Unknown
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		if (item == null)
		{
			return;
		}
		if (item.Metadata == null)
		{
			JObject val = new JObject();
			val.Add("BrushData", (JToken)new JObject());
			item.Metadata = val;
		}
		JToken val2 = default(JToken);
		if (item.Metadata.TryGetValue("BrushData", ref val2))
		{
			Width = (int)(val2[(object)"Width"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.Width.Default_ ?? 5));
			Height = (int)(val2[(object)"Height"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.Height.Default_ ?? 5));
			Thickness = (int)(val2[(object)"Thickness"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.Thickness.Default_ ?? Thickness));
			Capped = (bool)(val2[(object)"Capped"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.Capped.Default_ ?? Capped));
			Shape = (Enum.TryParse<BrushShape>((string)val2[(object)"Shape"], ignoreCase: true, out BrushShape result) ? result : (builderTool?.ToolItem.BrushData.Shape.Default_ ?? Shape));
			Origin = (Enum.TryParse<BrushOrigin>((string)val2[(object)"Origin"], ignoreCase: true, out BrushOrigin result2) ? result2 : (builderTool?.ToolItem.BrushData.Origin.Default_ ?? Origin));
			OriginRotation = (bool)(val2[(object)"OriginRotation"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.OriginRotation.Default_ ?? OriginRotation));
			RotationAxis = (Enum.TryParse<BrushAxis>((string)val2[(object)"RotationAxis"], ignoreCase: true, out BrushAxis result3) ? result3 : (builderTool?.ToolItem.BrushData.RotationAxis.Default_ ?? RotationAxis));
			RotationAngle = (Enum.TryParse<Rotation>((string)val2[(object)"RotationAngle"], ignoreCase: true, out Rotation result4) ? result4 : (builderTool?.ToolItem.BrushData.RotationAngle.Default_ ?? RotationAngle));
			MirrorAxis = (Enum.TryParse<BrushAxis>((string)val2[(object)"MirrorAxis"], ignoreCase: true, out BrushAxis result5) ? result5 : (builderTool?.ToolItem.BrushData.MirrorAxis.Default_ ?? MirrorAxis));
			Material = (string)(val2[(object)"Material"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.Material.Default_ ?? Material));
			if (val2[(object)"FavoriteMaterials"] != null)
			{
				FavoriteMaterials = val2[(object)"FavoriteMaterials"].ToObject<string[]>();
			}
			else if (builderTool != null && builderTool.ToolItem.BrushData.FavoriteMaterials.Length != 0)
			{
				FavoriteMaterials = Array.ConvertAll(builderTool.ToolItem.BrushData.FavoriteMaterials, (BuilderToolBlockArg b) => ((object)b).ToString());
			}
			Mask = (string)(val2[(object)"Mask"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.Mask.Default_ ?? Mask));
			MaskAbove = (string)(val2[(object)"MaskAbove"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.MaskAbove.Default_ ?? MaskAbove));
			MaskNot = (string)(val2[(object)"MaskNot"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.MaskNot.Default_ ?? MaskNot));
			MaskBelow = (string)(val2[(object)"MaskBelow"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.MaskBelow.Default_ ?? MaskBelow));
			MaskAdjacent = (string)(val2[(object)"MaskAdjacent"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.MaskAdjacent.Default_ ?? MaskAdjacent));
			MaskNeighbor = (string)(val2[(object)"MaskNeighbor"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.MaskNeighbor.Default_ ?? MaskNeighbor));
			if (val2[(object)"MaskCommands"] != null)
			{
				MaskCommands = val2[(object)"MaskCommands"].ToObject<string[]>();
			}
			else if (builderTool != null && builderTool.ToolItem.BrushData.MaskCommands.Length != 0)
			{
				MaskCommands = Array.ConvertAll(builderTool.ToolItem.BrushData.MaskCommands, (BuilderToolStringArg b) => ((object)b).ToString());
			}
			UseMaskCommands = (bool)(val2[(object)"UseMaskCommands"] ?? JToken.op_Implicit(builderTool?.ToolItem.BrushData.UseMaskCommands.Default_ ?? UseMaskCommands));
		}
		_onDataChange = onDataChange;
	}

	public void SetFavoriteMaterials(string[] materials)
	{
		_onDataChange?.Invoke("FavoriteMaterials", string.Join(",", materials));
	}

	public ClientItemStack[] GetFavoriteMaterialStacks()
	{
		if (FavoriteMaterials == null)
		{
			return EmptyClientItemStackArray;
		}
		ClientItemStack[] array = new ClientItemStack[FavoriteMaterials.Length];
		for (int i = 0; i < FavoriteMaterials.Length; i++)
		{
			array[i] = new ClientItemStack(FavoriteMaterials[i]);
		}
		return array;
	}

	public void SetBrushWidth(int width)
	{
		if (Width != width)
		{
			_onDataChange?.Invoke("Width", width.ToString());
		}
	}

	public void SetBrushHeight(int height)
	{
		if (Height != height)
		{
			_onDataChange?.Invoke("Height", height.ToString());
		}
	}

	public void SetBrushThickness(int thickness)
	{
		if (Thickness != thickness)
		{
			_onDataChange?.Invoke("Thickness", thickness.ToString());
		}
	}

	public void SetCapped(bool capped)
	{
		if (Capped != capped)
		{
			_onDataChange?.Invoke("Capped", capped.ToString());
		}
	}

	public void SetBrushShape(BrushShape brushShape)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (Shape != brushShape)
		{
			_onDataChange?.Invoke("Shape", ((object)(BrushShape)(ref brushShape)).ToString());
		}
	}

	public void SetBrushOrigin(BrushOrigin brushOrigin)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (Origin != brushOrigin)
		{
			_onDataChange?.Invoke("Origin", ((object)(BrushOrigin)(ref brushOrigin)).ToString());
		}
	}

	public void SetOriginRotation(bool originRotation)
	{
		if (OriginRotation != originRotation)
		{
			_onDataChange?.Invoke("OriginRotation", originRotation.ToString());
		}
	}

	public void SetRotationAxis(BrushAxis brushAxis)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (RotationAxis != brushAxis)
		{
			_onDataChange?.Invoke("RotationAxis", ((object)(BrushAxis)(ref brushAxis)).ToString());
		}
	}

	public void SetRotationAngle(Rotation rotation)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (RotationAngle != rotation)
		{
			_onDataChange?.Invoke("RotationAngle", ((object)(Rotation)(ref rotation)).ToString());
		}
	}

	public void SetMirrorAxis(BrushAxis brushAxis)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (MirrorAxis != brushAxis)
		{
			_onDataChange?.Invoke("MirrorAxis", ((object)(BrushAxis)(ref brushAxis)).ToString());
		}
	}

	public void SetBrushMaterial(string material)
	{
		if (material != Material)
		{
			_onDataChange?.Invoke("Material", material.ToString());
		}
	}

	public bool OffsetBrushWidth(int offset)
	{
		if (offset != 0)
		{
			int num = Width + offset;
			if (num > 0)
			{
				SetBrushWidth(num);
				return true;
			}
		}
		return false;
	}

	public bool OffsetBrushHeight(int offset)
	{
		if (offset != 0)
		{
			int num = Height + offset;
			if (num > 0)
			{
				SetBrushHeight(num);
				return true;
			}
		}
		return false;
	}

	public void InvertBrushShape()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected I4, but got Unknown
		BrushShape shape = Shape;
		BrushShape val = shape;
		switch (val - 3)
		{
		case 0:
			SetBrushShape((BrushShape)4);
			break;
		case 1:
			SetBrushShape((BrushShape)3);
			break;
		case 2:
			SetBrushShape((BrushShape)6);
			break;
		case 3:
			SetBrushShape((BrushShape)5);
			break;
		}
	}

	public BrushOrigin NextBrushOrigin(bool moveForward = true)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		BrushOrigin val = (BrushOrigin)((moveForward && (int)Origin == 2) ? ((BrushOrigin)0) : ((moveForward || (int)Origin != 0) ? (Origin + (moveForward ? 1 : (-1))) : ((BrushOrigin)2)));
		SetBrushOrigin(val);
		return val;
	}

	public BrushShape NextBrushShape(bool moveForward = true)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		BrushShape val = (BrushShape)((moveForward && (int)Shape == 7) ? ((BrushShape)0) : ((moveForward || (int)Shape != 0) ? (Shape + (moveForward ? 1 : (-1))) : ((BrushShape)7)));
		SetBrushShape(val);
		return val;
	}

	public Dictionary<string, string> ToArgValues()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int width = Width;
		dictionary.Add("Width", width.ToString());
		width = Height;
		dictionary.Add("Height", width.ToString());
		width = Thickness;
		dictionary.Add("Thickness", width.ToString());
		bool capped = Capped;
		dictionary.Add("Capped", capped.ToString());
		BrushShape shape = Shape;
		dictionary.Add("Shape", ((object)(BrushShape)(ref shape)).ToString());
		BrushOrigin origin = Origin;
		dictionary.Add("Origin", ((object)(BrushOrigin)(ref origin)).ToString());
		capped = OriginRotation;
		dictionary.Add("OriginRotation", capped.ToString());
		BrushAxis val = RotationAxis;
		dictionary.Add("RotationAxis", ((object)(BrushAxis)(ref val)).ToString());
		Rotation rotationAngle = RotationAngle;
		dictionary.Add("RotationAngle", ((object)(Rotation)(ref rotationAngle)).ToString());
		val = MirrorAxis;
		dictionary.Add("MirrorAxis", ((object)(BrushAxis)(ref val)).ToString());
		dictionary.Add("Material", Material ?? "");
		dictionary.Add("FavoriteMaterials", (FavoriteMaterials == null) ? "" : string.Join(",", FavoriteMaterials));
		dictionary.Add("Mask", Mask ?? "");
		dictionary.Add("MaskAbove", MaskAbove ?? "");
		dictionary.Add("MaskNot", MaskNot ?? "");
		dictionary.Add("MaskBelow", MaskBelow ?? "");
		dictionary.Add("MaskAdjacent", MaskAdjacent ?? "");
		dictionary.Add("MaskNeighbor", MaskNeighbor ?? "");
		dictionary.Add("MaskCommands", (MaskCommands == null) ? "" : string.Join(Environment.NewLine, MaskCommands));
		capped = UseMaskCommands;
		dictionary.Add("UseMaskCommands", capped.ToString());
		return dictionary;
	}

	public bool Equals(BrushData other)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		return other != null && other.Width == Width && other.Height == Height && other.Thickness == Thickness && other.Capped == Capped && other.Shape == Shape && other.Origin == Origin && other.OriginRotation == OriginRotation && other.RotationAxis == RotationAxis && other.RotationAngle == RotationAngle && other.MirrorAxis == MirrorAxis && other.Material == Material && other.FavoriteMaterials == FavoriteMaterials && other.Mask == Mask && other.MaskAbove == MaskAbove && other.MaskNot == MaskNot && other.MaskBelow == MaskBelow && other.MaskAdjacent == MaskAdjacent && other.MaskNeighbor == MaskNeighbor && other.MaskCommands == MaskCommands && other.UseMaskCommands == UseMaskCommands;
	}

	public override string ToString()
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		return "Brush Args\n" + string.Format("  {0}:  {1}\n", "Width", Width) + string.Format("  {0}:  {1}\n", "Height", Height) + string.Format("  {0}:  {1}\n", "Thickness", Thickness) + string.Format("  {0}:  {1}\n", "Capped", Capped) + string.Format("  {0}:  {1}\n", "Shape", Shape) + string.Format("  {0}:  {1}\n", "Origin", Origin) + string.Format("  {0}:  {1}\n", "OriginRotation", OriginRotation) + string.Format("  {0}:  {1}\n", "RotationAxis", RotationAxis) + string.Format("  {0}:  {1}\n", "RotationAngle", RotationAngle) + string.Format("  {0}:  {1}\n", "MirrorAxis", MirrorAxis) + "  Material:  " + Material + "\n" + string.Format("  {0}:  {1}\n", "FavoriteMaterials", FavoriteMaterials) + "  Mask:  " + Mask + "\n  MaskAbove:  " + MaskAbove + "\n  MaskNot:  " + MaskNot + "\n  MaskBelow:  " + MaskBelow + "\n  MaskAdjacent:  " + MaskAdjacent + "\n  MaskNeighbor:  " + MaskNeighbor + "\n" + string.Format("  {0}:  {1}\n", "MaskCommands", MaskCommands) + string.Format("  {0}:  {1}\n", "UseMaskCommands", UseMaskCommands);
	}
}
