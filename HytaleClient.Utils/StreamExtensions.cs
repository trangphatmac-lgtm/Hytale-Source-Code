using System;
using System.IO;

namespace HytaleClient.Utils;

internal static class StreamExtensions
{
	public static byte[] ReadAllBytes(this Stream reader)
	{
		using MemoryStream memoryStream = new MemoryStream();
		byte[] array = new byte[4096];
		int count;
		while ((count = reader.Read(array, 0, array.Length)) != 0)
		{
			memoryStream.Write(array, 0, count);
		}
		return memoryStream.ToArray();
	}

	public static void WriteInt32Be(this BinaryWriter bw, int i)
	{
		byte[] bytes = BitConverter.GetBytes(i);
		Array.Reverse((Array)bytes);
		bw.Write(bytes);
	}

	public static int ReadInt32Be(this BinaryReader br)
	{
		byte[] array = br.ReadBytes(4);
		Array.Reverse((Array)array);
		return BitConverter.ToInt32(array, 0);
	}
}
