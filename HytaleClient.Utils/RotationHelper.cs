using System;
using System.Runtime.CompilerServices;
using HytaleClient.Data.Map;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Utils;

internal static class RotationHelper
{
	private static readonly Rotation[] _rotationWallYawValues = (Rotation[])(object)new Rotation[4]
	{
		default(Rotation),
		(Rotation)1,
		default(Rotation),
		(Rotation)1
	};

	private static readonly Rotation[] _rotationNoneYawValues = (Rotation[])(object)new Rotation[4];

	private static readonly Rotation[] _rotationNESWYawValues;

	public static Rotation Add(Rotation a, Rotation b)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Expected I4, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		int num = a + b;
		num %= typeof(Rotation).GetEnumValues().Length;
		return (Rotation)num;
	}

	public static Rotation Subtract(Rotation a, Rotation b)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Expected I4, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		int num = a - b;
		if (num < 0)
		{
			num += typeof(Rotation).GetEnumValues().Length;
		}
		return (Rotation)num;
	}

	public static void Rotate(int x, int z, Rotation rotation, out int outX, out int outZ)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected I4, but got Unknown
		switch ((int)rotation)
		{
		case 0:
			outX = x;
			outZ = z;
			break;
		case 1:
			outX = z;
			outZ = -x;
			break;
		case 2:
			outX = -x;
			outZ = -z;
			break;
		case 3:
			outX = -z;
			outZ = x;
			break;
		default:
			throw new NotSupportedException();
		}
	}

	public static void GetHorizontalNormal(Vector3 rotation, out float x, out float z)
	{
		if (rotation.Y >= -(float)System.Math.PI / 4f && rotation.Y <= (float)System.Math.PI / 4f)
		{
			x = 0f;
			z = 1f;
		}
		else if (rotation.Y >= (float)System.Math.PI / 4f && rotation.Y <= (float)System.Math.PI * 3f / 4f)
		{
			x = 1f;
			z = 0f;
		}
		else if (rotation.Y >= (float)System.Math.PI * 3f / 4f || rotation.Y <= (float)System.Math.PI * -3f / 4f)
		{
			x = 0f;
			z = -1f;
		}
		else
		{
			x = -1f;
			z = 0f;
		}
	}

	public static void GetVariantRotationOptions(Vector3 targetNormal, VariantRotation rotation, out bool rotateX, out bool rotateY, out Rotation defaultPitch, out Rotation[] yawValues)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected I4, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		rotateX = false;
		rotateY = false;
		defaultPitch = (Rotation)0;
		switch ((int)rotation)
		{
		case 0:
			yawValues = _rotationNoneYawValues;
			break;
		case 3:
			if (targetNormal.Y == 0f)
			{
				rotateX = true;
				defaultPitch = (Rotation)1;
				yawValues = _rotationWallYawValues;
			}
			else
			{
				yawValues = _rotationNESWYawValues;
			}
			break;
		case 4:
			if (targetNormal.Y == 0f)
			{
				rotateX = true;
				defaultPitch = (Rotation)1;
				yawValues = _rotationNESWYawValues;
			}
			else
			{
				rotateY = true;
				yawValues = _rotationNESWYawValues;
			}
			break;
		case 1:
			rotateX = true;
			yawValues = _rotationWallYawValues;
			break;
		case 2:
			rotateY = true;
			yawValues = _rotationNoneYawValues;
			break;
		case 6:
			rotateX = true;
			rotateY = true;
			yawValues = _rotationNESWYawValues;
			break;
		case 5:
			rotateX = true;
			yawValues = _rotationNESWYawValues;
			break;
		case 7:
			rotateX = true;
			rotateY = true;
			yawValues = _rotationNESWYawValues;
			break;
		default:
			throw new NotImplementedException($"Unknown BlockType.VariantRotation type: {rotation}");
		}
	}

	public static BlockFace FromNormal(Vector3 normal)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if ((double)System.Math.Abs(normal.X) > 0.5)
		{
			return (BlockFace)((normal.X < 0f) ? 6 : 5);
		}
		if ((double)System.Math.Abs(normal.Z) > 0.5)
		{
			return (BlockFace)((normal.Z < 0f) ? 3 : 4);
		}
		return (BlockFace)((!(normal.Y < 0f)) ? 1 : 2);
	}

	public static Vector3 ToNormal(BlockFace face)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected I4, but got Unknown
		return (face - 1) switch
		{
			0 => new Vector3(0f, 1f, 0f), 
			1 => new Vector3(0f, -1f, 0f), 
			2 => new Vector3(0f, 0f, -1f), 
			3 => new Vector3(0f, 0f, 1f), 
			4 => new Vector3(1f, 0f, 0f), 
			5 => new Vector3(-1f, 0f, 0f), 
			_ => throw new Exception("Invalid block face"), 
		};
	}

	public static BlockFace RotateBlockFace(BlockFace face, ClientBlockType block)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ToNormal(face);
		Vector3 normal = Vector3.Transform(position, block.RotationMatrix);
		return FromNormal(normal);
	}

	static RotationHelper()
	{
		Rotation[] array = new Rotation[4];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		_rotationNESWYawValues = (Rotation[])(object)array;
	}
}
