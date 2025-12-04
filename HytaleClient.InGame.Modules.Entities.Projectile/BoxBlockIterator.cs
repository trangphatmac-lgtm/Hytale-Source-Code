using System;
using System.Threading;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities.Projectile;

internal static class BoxBlockIterator
{
	public interface BoxIterationConsumer
	{
		bool Next();

		bool Accept(long x, long y, long z);
	}

	public class BoxIterationBuffer
	{
		public BoxIterationConsumer Consumer;

		public float Mx;

		public float My;

		public float Mz;

		public int SignX;

		public int SignY;

		public int SignZ;

		public long PosX;

		public long PosY;

		public long PosZ;
	}

	private static ThreadLocal<BoxIterationBuffer> ThreadLocalBuffer = new ThreadLocal<BoxIterationBuffer>(() => new BoxIterationBuffer());

	public static BoxIterationBuffer Buffer => ThreadLocalBuffer.Value;

	public static bool Iterate(BoundingBox box, Vector3 position, Vector3 d, float maxDistance, BoxIterationConsumer consumer)
	{
		return Iterate(box, position, d, maxDistance, consumer, Buffer);
	}

	public static bool Iterate(BoundingBox box, Vector3 pos, Vector3 d, float maxDistance, BoxIterationConsumer consumer, BoxIterationBuffer buffer)
	{
		return Iterate(box.Min, box.Max, pos, d, maxDistance, consumer, buffer);
	}

	public static bool Iterate(BoundingBox box, float px, float py, float pz, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer)
	{
		return Iterate(box, px, py, pz, dx, dy, dz, maxDistance, consumer, Buffer);
	}

	public static bool Iterate(BoundingBox box, float px, float py, float pz, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer, BoxIterationBuffer buffer)
	{
		return Iterate(box.Min, box.Max, px, py, pz, dx, dy, dz, maxDistance, consumer, buffer);
	}

	public static bool Iterate(Vector3 min, Vector3 max, float px, float py, float pz, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer)
	{
		return Iterate(min, max, px, py, pz, dx, dy, dz, maxDistance, consumer, Buffer);
	}

	public static bool Iterate(Vector3 min, Vector3 max, float px, float py, float pz, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer, BoxIterationBuffer buffer)
	{
		return Iterate(min.X, min.Y, min.Z, max.X, max.Y, max.Z, px, py, pz, dx, dy, dz, maxDistance, consumer, buffer);
	}

	public static bool Iterate(Vector3 min, Vector3 max, Vector3 pos, Vector3 d, float maxDistance, BoxIterationConsumer consumer)
	{
		return Iterate(min, max, pos, d, maxDistance, consumer, Buffer);
	}

	public static bool Iterate(Vector3 min, Vector3 max, Vector3 pos, Vector3 d, float maxDistance, BoxIterationConsumer consumer, BoxIterationBuffer buffer)
	{
		return Iterate(min.X, min.Y, min.Z, max.X, max.Y, max.Z, pos.X, pos.Y, pos.Z, d.X, d.Y, d.Z, maxDistance, consumer, buffer);
	}

	public static bool Iterate(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float px, float py, float pz, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer)
	{
		return Iterate(minX, minY, minZ, maxX, maxY, maxZ, px, py, pz, dx, dy, dz, maxDistance, consumer, Buffer);
	}

	public static bool Iterate(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float px, float py, float pz, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer, BoxIterationBuffer buffer)
	{
		if (minX > maxX)
		{
			throw new ArgumentException("minX is larger than maxX! Given: " + minX + " > " + maxX);
		}
		if (minY > maxY)
		{
			throw new ArgumentException("minY is larger than maxY! Given: " + minY + " > " + maxY);
		}
		if (minZ > maxZ)
		{
			throw new ArgumentException("minZ is larger than maxZ! Given: " + minZ + " > " + maxZ);
		}
		if (consumer == null)
		{
			throw new ArgumentException("consumer is null!");
		}
		if (buffer == null)
		{
			throw new ArgumentException("buffer is null!");
		}
		return Iterate0(minX, minY, minZ, maxX, maxY, maxZ, px, py, pz, dx, dy, dz, maxDistance, consumer, buffer);
	}

	private static bool Iterate0(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float posX, float posY, float posZ, float dx, float dy, float dz, float maxDistance, BoxIterationConsumer consumer, BoxIterationBuffer buffer)
	{
		buffer.Consumer = consumer;
		buffer.Mx = maxX - minX;
		buffer.My = maxY - minY;
		buffer.Mz = maxZ - minZ;
		buffer.SignX = ((!(dx > 0f)) ? 1 : (-1));
		buffer.SignY = ((!(dy > 0f)) ? 1 : (-1));
		buffer.SignZ = ((!(dz > 0f)) ? 1 : (-1));
		float num = posX + ((dx > 0f) ? maxX : minX);
		float num2 = posY + ((dy > 0f) ? maxY : minY);
		float num3 = posZ + ((dz > 0f) ? maxZ : minZ);
		buffer.PosX = (long)num;
		buffer.PosY = (long)num2;
		buffer.PosZ = (long)num3;
		return ServerBlockIterator.Iterate(num, num2, num3, dx, dy, dz, maxDistance, delegate(int x, int y, int z, float px, float py, float pz, float qx, float qy, float qz, BoxIterationBuffer buf)
		{
			int num4 = (int)System.Math.Ceiling(((buf.SignX < 0) ? (1f - px) : px) + buf.Mx);
			int num5 = (int)System.Math.Ceiling(((buf.SignY < 0) ? (1f - py) : py) + buf.My);
			int num6 = (int)System.Math.Ceiling(((buf.SignZ < 0) ? (1f - pz) : pz) + buf.Mz);
			if (x != buf.PosX)
			{
				for (int i = 0; i < num5; i++)
				{
					for (int j = 0; j < num6; j++)
					{
						if (!buf.Consumer.Accept(x, y + i * buf.SignY, z + j * buf.SignZ))
						{
							return false;
						}
					}
				}
				buf.PosX = x;
			}
			if (y != buf.PosY)
			{
				for (int k = 0; k < num6; k++)
				{
					for (int l = 0; l < num4; l++)
					{
						if (!buf.Consumer.Accept(x + l * buf.SignX, y, z + k * buf.SignZ))
						{
							return false;
						}
					}
				}
				buf.PosY = y;
			}
			if (z != buf.PosZ)
			{
				for (int m = 0; m < num4; m++)
				{
					for (int n = 0; n < num5; n++)
					{
						if (!buf.Consumer.Accept(x + m * buf.SignX, y + n * buf.SignY, z))
						{
							return false;
						}
					}
				}
				buf.PosZ = z;
			}
			return buf.Consumer.Next();
		}, buffer);
	}
}
