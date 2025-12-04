using System;

namespace HytaleClient.Math;

public class BlockHitbox : IEquatable<BlockHitbox>
{
	public BoundingBox BoundingBox;

	public BoundingBox[] Boxes;

	public BlockHitbox()
	{
		BoundingBox = new BoundingBox(Vector3.Zero, Vector3.One);
		Boxes = new BoundingBox[1] { BoundingBox };
	}

	public BlockHitbox(BoundingBox[] boxes)
	{
		Boxes = boxes;
		BoundingBox = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
		BoundingBox[] boxes2 = Boxes;
		for (int i = 0; i < boxes2.Length; i++)
		{
			BoundingBox boundingBox = boxes2[i];
			BoundingBox.Min = Vector3.Min(BoundingBox.Min, boundingBox.Min);
			BoundingBox.Max = Vector3.Max(BoundingBox.Max, boundingBox.Max);
		}
	}

	public BlockHitbox(float[][] boxes)
	{
		Boxes = new BoundingBox[boxes.Length];
		for (int i = 0; i < boxes.Length; i++)
		{
			Boxes[i].Min = new Vector3(boxes[i][0], boxes[i][1], boxes[i][2]);
			Boxes[i].Max = new Vector3(boxes[i][3], boxes[i][4], boxes[i][5]);
		}
		BoundingBox = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
		BoundingBox[] boxes2 = Boxes;
		for (int j = 0; j < boxes2.Length; j++)
		{
			BoundingBox boundingBox = boxes2[j];
			BoundingBox.Min = Vector3.Min(BoundingBox.Min, boundingBox.Min);
			BoundingBox.Max = Vector3.Max(BoundingBox.Max, boundingBox.Max);
		}
	}

	public void Rotate(int pitch, int yaw)
	{
		RotateBoxes(Boxes, pitch, yaw);
	}

	public void Translate(Vector3 offset)
	{
		for (int i = 0; i < Boxes.Length; i++)
		{
			Boxes[i].Translate(offset);
		}
	}

	public BoundingBox GetVoxelBounds()
	{
		return new BoundingBox(new Vector3((float)System.Math.Floor(BoundingBox.Min.X), (float)System.Math.Floor(BoundingBox.Min.Y), (float)System.Math.Floor(BoundingBox.Min.Z)), new Vector3((float)System.Math.Ceiling(BoundingBox.Max.X), (float)System.Math.Ceiling(BoundingBox.Max.Y), (float)System.Math.Ceiling(BoundingBox.Max.Z)));
	}

	public bool IsOversized()
	{
		return BoundingBox.Min.X < 0f || BoundingBox.Min.Y < 0f || BoundingBox.Min.Z < 0f || BoundingBox.Max.X > 1f || BoundingBox.Max.Y > 1f || BoundingBox.Max.Z > 1f;
	}

	public BlockHitbox Clone()
	{
		BoundingBox[] array = new BoundingBox[Boxes.Length];
		for (int i = 0; i < Boxes.Length; i++)
		{
			array[i] = Boxes[i];
		}
		return new BlockHitbox(array);
	}

	public static void RotateBoxes(BoundingBox[] boxes, int pitch, int yaw)
	{
		switch (pitch)
		{
		case 90:
		{
			for (int j = 0; j < boxes.Length; j++)
			{
				float y3 = boxes[j].Min.Y;
				float y4 = boxes[j].Max.Y;
				float z3 = boxes[j].Min.Z;
				float z4 = boxes[j].Max.Z;
				boxes[j].Min.Y = z3;
				boxes[j].Min.Z = y3;
				boxes[j].Max.Y = z4;
				boxes[j].Max.Z = y4;
			}
			break;
		}
		case 180:
		{
			for (int i = 0; i < boxes.Length; i++)
			{
				float y = boxes[i].Min.Y;
				float y2 = boxes[i].Max.Y;
				float z = boxes[i].Min.Z;
				float z2 = boxes[i].Max.Z;
				boxes[i].Min.Y = 1f - y2;
				boxes[i].Max.Y = 1f - y;
				boxes[i].Min.Z = 1f - z2;
				boxes[i].Max.Z = 1f - z;
			}
			break;
		}
		default:
			throw new Exception("Unsupported pitch for BlockHitbox.RotateBoxes");
		case 0:
			break;
		}
		switch (yaw)
		{
		case 0:
			break;
		case 90:
		{
			for (int l = 0; l < boxes.Length; l++)
			{
				float x3 = boxes[l].Min.X;
				float x4 = boxes[l].Max.X;
				float z7 = boxes[l].Min.Z;
				float z8 = boxes[l].Max.Z;
				boxes[l].Min.X = z7;
				boxes[l].Min.Z = 1f - x4;
				boxes[l].Max.X = z8;
				boxes[l].Max.Z = 1f - x3;
			}
			break;
		}
		case 180:
		{
			for (int m = 0; m < boxes.Length; m++)
			{
				float x5 = boxes[m].Min.X;
				float x6 = boxes[m].Max.X;
				float z9 = boxes[m].Min.Z;
				float z10 = boxes[m].Max.Z;
				boxes[m].Min.X = 1f - x6;
				boxes[m].Max.X = 1f - x5;
				boxes[m].Min.Z = 1f - z10;
				boxes[m].Max.Z = 1f - z9;
			}
			break;
		}
		case 270:
		{
			for (int k = 0; k < boxes.Length; k++)
			{
				float x = boxes[k].Min.X;
				float x2 = boxes[k].Max.X;
				float z5 = boxes[k].Min.Z;
				float z6 = boxes[k].Max.Z;
				boxes[k].Min.X = 1f - z6;
				boxes[k].Min.Z = x;
				boxes[k].Max.X = 1f - z5;
				boxes[k].Max.Z = x2;
			}
			break;
		}
		default:
			throw new Exception("Unsupported yaw for BlockHitbox.RotateBoxes");
		}
	}

	public bool Equals(BlockHitbox other)
	{
		if (!BoundingBox.Equals(other.BoundingBox) || Boxes.Length != other.Boxes.Length)
		{
			return false;
		}
		for (int i = 0; i < Boxes.Length; i++)
		{
			if (!Boxes[i].Equals(other.Boxes[i]))
			{
				return false;
			}
		}
		return true;
	}
}
