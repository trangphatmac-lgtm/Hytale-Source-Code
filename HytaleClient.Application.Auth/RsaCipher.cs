using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;

namespace HytaleClient.Application.Auth;

public class RsaCipher
{
	private readonly AsymmetricKeyParameter _publicKey;

	private readonly AsymmetricKeyParameter _privateKey;

	public RsaCipher(AsymmetricKeyParameter publicKey, AsymmetricKeyParameter privateKey)
	{
		_publicKey = publicKey;
		_privateKey = privateKey;
	}

	public byte[] Encrypt(byte[] data)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		IAsymmetricBlockCipher val = (IAsymmetricBlockCipher)new OaepEncoding((IAsymmetricBlockCipher)new RsaEngine());
		val.Init(true, (ICipherParameters)(object)_publicKey);
		byte[] array = new byte[data.Length];
		Buffer.BlockCopy(data, 0, array, 0, data.Length);
		return val.ProcessBlock(array, 0, data.Length);
	}

	public sbyte[] EncryptSigned(sbyte[] data)
	{
		return (sbyte[])(object)Encrypt((byte[])(object)data);
	}

	public byte[] Decrypt(byte[] data)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		IAsymmetricBlockCipher val = (IAsymmetricBlockCipher)new OaepEncoding((IAsymmetricBlockCipher)new RsaEngine());
		val.Init(false, (ICipherParameters)(object)_privateKey);
		byte[] array = new byte[data.Length];
		Buffer.BlockCopy(data, 0, array, 0, data.Length);
		return val.ProcessBlock(array, 0, data.Length);
	}

	public sbyte[] DecryptSigned(sbyte[] data)
	{
		return (sbyte[])(object)Decrypt((byte[])(object)data);
	}

	public sbyte[] DecryptLong(sbyte[] data)
	{
		using MemoryStream input = new MemoryStream((byte[])(object)data);
		using BinaryReader binaryReader = new BinaryReader(input);
		int num = binaryReader.ReadInt16();
		byte[] array = binaryReader.ReadBytes(num);
		byte[] sourceArray = (byte[])(object)DecryptSigned((sbyte[])(object)array);
		byte[] array2 = new byte[16];
		Array.Copy(sourceArray, 0, array2, 0, array2.Length);
		byte[] array3 = new byte[16];
		Array.Copy(sourceArray, 16, array3, 0, array3.Length);
		byte[] input2 = binaryReader.ReadBytes(data.Length - (2 + num));
		AesCipher aesCipher = new AesCipher(array2, array3);
		return (sbyte[])(object)aesCipher.Decrypt(input2);
	}
}
