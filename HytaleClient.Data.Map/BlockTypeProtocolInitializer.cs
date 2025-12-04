using HytaleClient.Graphics;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Map;

internal static class BlockTypeProtocolInitializer
{
	public static void ConvertShadingMode(ShadingMode protocolShadingMode, out ShadingMode shadingMode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected I4, but got Unknown
		switch ((int)protocolShadingMode)
		{
		case 1:
			shadingMode = ShadingMode.Flat;
			break;
		case 2:
			shadingMode = ShadingMode.Fullbright;
			break;
		case 3:
			shadingMode = ShadingMode.Reflective;
			break;
		default:
			shadingMode = ShadingMode.Standard;
			break;
		}
	}
}
