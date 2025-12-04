using System;
using NLog;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.Encoders;

namespace HytaleClient.Application.Auth;

public class AesCipher
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly BufferedBlockCipher _cipher = (BufferedBlockCipher)new PaddedBufferedBlockCipher((IBlockCipher)new CbcBlockCipher((IBlockCipher)new AesEngine()), (IBlockCipherPadding)new Pkcs7Padding());

	private readonly ICipherParameters _cipherParameters;

	public AesCipher(byte[] keyBytes, byte[] ivBytes)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		//IL_0015: Expected O, but got Unknown
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		if (Logger.IsInfoEnabled)
		{
			Logger.Info<string, string>("{0} {1}", Hex.ToHexString(keyBytes), Hex.ToHexString(ivBytes));
		}
		_cipherParameters = (ICipherParameters)new ParametersWithIV((ICipherParameters)new KeyParameter(keyBytes), ivBytes);
	}

	public byte[] Encrypt(byte[] input)
	{
		((BufferedCipherBase)_cipher).Reset();
		((BufferedCipherBase)_cipher).Init(true, _cipherParameters);
		byte[] array = new byte[((BufferedCipherBase)_cipher).GetOutputSize(input.Length)];
		int num = ((BufferedCipherBase)_cipher).ProcessBytes(input, 0, input.Length, array, 0);
		num += ((BufferedCipherBase)_cipher).DoFinal(array, num);
		byte[] array2 = new byte[num];
		Array.Copy(array, array2, num);
		return array2;
	}

	public byte[] Decrypt(byte[] input)
	{
		if (Logger.IsInfoEnabled)
		{
			Logger.Info(Hex.ToHexString(input));
		}
		((BufferedCipherBase)_cipher).Reset();
		((BufferedCipherBase)_cipher).Init(false, _cipherParameters);
		byte[] array = new byte[((BufferedCipherBase)_cipher).GetOutputSize(input.Length)];
		int num = ((BufferedCipherBase)_cipher).ProcessBytes(input, 0, input.Length, array, 0);
		num += ((BufferedCipherBase)_cipher).DoFinal(array, num);
		byte[] array2 = new byte[num];
		Array.Copy(array, array2, num);
		return array2;
	}
}
