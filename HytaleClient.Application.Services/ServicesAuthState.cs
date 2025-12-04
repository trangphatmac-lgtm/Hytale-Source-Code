using System;
using System.IO;
using System.Text;
using Hypixel.ProtoPlus;
using HytaleClient.Application.Auth;
using HytaleClient.Auth.Proto.Protocol;
using HytaleClient.AuthHandshake.Proto.Protocol;
using NLog;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace HytaleClient.Application.Services;

internal class ServicesAuthState
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static readonly SecureRandom SecureRandom = new SecureRandom();

	private readonly ServicesClient _parent;

	private readonly byte[] _certPathBytes;

	private readonly RsaCipher _cipher;

	private PkixCertPath _serverCert;

	private sbyte[] _nonceB;

	private sbyte[] _sharedSecretIv;

	public bool Authed = false;

	public ServicesAuthState(AuthManager authManager, ServicesClient parent)
	{
		_parent = parent;
		_certPathBytes = authManager.CertPathBytes;
		_cipher = authManager.Cipher;
	}

	public void ProcessAuth0(Auth0 packet)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		if (packet.ProtocolHash != "6faac88713fa024f591f1576afa06d7387364727cf1bd8841bbe8cfd78587")
		{
			Logger.Error("Got incompatible protocol version from server {0} timestamp {1} vs our {2} timestamp {3}", new object[4] { packet.ProtocolHash, packet.ProtocolCompileTimestamp, "6faac88713fa024f591f1576afa06d7387364727cf1bd8841bbe8cfd78587", 1551618658016L });
		}
		else
		{
			_parent.Write((ProtoPacket)new Auth1((sbyte[])(object)_certPathBytes, "6faac88713fa024f591f1576afa06d7387364727cf1bd8841bbe8cfd78587", 1551618658016L));
		}
	}

	public void ProcessAuth2(Auth2 packet)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		Logger.Info("Starting handling of auth2");
		sbyte[] data = _cipher.DecryptSigned(packet.NonceA);
		sbyte[] array = _cipher.DecryptLong(packet.Cert);
		string @string = Encoding.UTF8.GetString((byte[])(object)array);
		Logger.Info("Got server cert string {0}", @string);
		_serverCert = new PkixCertPath((Stream)new MemoryStream((byte[])(object)array), "PEM");
		PkixCertPathValidator val = new PkixCertPathValidator();
		PkixCertPathValidatorResult val2 = val.Validate(_serverCert, getValidationParameters());
		Logger.Info<PkixPolicyNode>("Validation result: {0}", val2.PolicyTree);
		_nonceB = new sbyte[36];
		((Random)(object)SecureRandom).NextBytes((byte[])(object)_nonceB);
		RsaCipher rsaCipher = new RsaCipher(((X509Certificate)_serverCert.Certificates[0]).GetPublicKey(), null);
		sbyte[] array2 = rsaCipher.EncryptSigned(data);
		sbyte[] array3 = rsaCipher.EncryptSigned(_nonceB);
		_parent.Write((ProtoPacket)new Auth3(array2, array3));
	}

	public void ProcessAuth4(Auth4 packet)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		_sharedSecretIv = _cipher.DecryptSigned(packet.Secret);
		sbyte[] array = _cipher.DecryptSigned(packet.NonceB);
		if (!Arrays.AreEqual((byte[])(object)_nonceB, (byte[])(object)array))
		{
			throw new Exception("NonceB mismatch!");
		}
		_parent.Write((ProtoPacket)new Auth5());
		Authed = true;
	}

	public void ProcessAuthFinished(ClientAuth6 packet)
	{
		Logger.Info<ServicesClient>("Received authentication finished for {0}", _parent);
	}

	private PkixParameters getValidationParameters()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		PkixParameters val = new PkixParameters(AuthManager.TrustAnchors);
		val.IsRevocationEnabled = false;
		return val;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}, {6}: {7}, {8}: {9}", "_certPathBytes", _certPathBytes, "_cipher", _cipher, "_serverCert", _serverCert, "_nonceB", _nonceB, "_sharedSecretIv", _sharedSecretIv);
	}
}
