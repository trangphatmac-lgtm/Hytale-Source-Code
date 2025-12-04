using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;

namespace HytaleClient.Application.Auth;

internal class AuthManager
{
	public class AuthSettings
	{
		public bool IsInsecure;

		public Guid Uuid;

		public string Username;

		public string UuidString => Uuid.ToString();
	}

	private static readonly Logger Logger;

	private static readonly SecureRandom SecureRandom;

	private static readonly DerObjectIdentifier OfficialHypixelIncOidIdentifier;

	private static readonly DerObjectIdentifier BsonSchema;

	private static readonly DerObjectIdentifier Uuid_;

	private AsymmetricKeyParameter _privateKey;

	private PkixCertPath _serverCert;

	private sbyte[] _nonceB;

	private sbyte[] _sharedSecretIv;

	private static readonly string _rootCert;

	public static readonly ISet TrustAnchors;

	public AuthSettings Settings { get; }

	public JObject Metadata { get; private set; }

	public byte[] CertPathBytes { get; private set; }

	public X509Certificate Cert { get; private set; }

	public RsaCipher Cipher { get; private set; }

	public AuthManager()
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		Settings = new AuthSettings();
		string certificatePath = OptionsHelper.CertificatePath;
		string privateKeyPath = OptionsHelper.PrivateKeyPath;
		if (OptionsHelper.InsecureUsername != null || certificatePath == null || !File.Exists(certificatePath) || privateKeyPath == null || !File.Exists(privateKeyPath))
		{
			Settings.IsInsecure = true;
			Settings.Username = OptionsHelper.InsecureUsername ?? "UnauthenticatedGuest";
			Logger.Info("Insecure mode with username {0}", Settings.Username);
			return;
		}
		string cert = File.ReadAllText(certificatePath, Encoding.UTF8);
		string privateKey = File.ReadAllText(privateKeyPath, Encoding.UTF8);
		Setup(cert, privateKey);
		DerBitString val = (DerBitString)Asn1Object.FromByteArray(((X509ExtensionBase)Cert).GetExtensionValue(Uuid_).GetOctets());
		byte[] octets = val.GetOctets();
		if (BitConverter.IsLittleEndian)
		{
			Logger.Info("Converting GUID because little endian!");
			_flipOctets(octets, 0, 4);
			_flipOctets(octets, 4, 2);
			_flipOctets(octets, 6, 2);
		}
		Guid guid = new Guid(octets);
		Logger.Info<Guid>("Got GUID loaded: {0}", guid);
		Settings.Uuid = guid;
		Settings.Username = ((object)Metadata.GetValue("name")).ToString();
		Logger.Info<Guid, string>("Authenticated as: {0} ({1})", Settings.Uuid, Settings.Username);
	}

	private void _flipOctets(byte[] source, int index, int length)
	{
		byte[] array = new byte[length];
		Array.Copy(source, index, array, 0, length);
		array = Arrays.Reverse(array);
		Array.Copy(array, 0, source, index, length);
	}

	public void WritePemDataSp(string pathKey, string pathCert)
	{
		WritePem(pathKey, _privateKey);
		File.WriteAllBytes(pathCert, CertPathBytes);
	}

	private void WritePem(string path, object o)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		using TextWriter textWriter = new StringWriter();
		new PemWriter(textWriter).WriteObject((object)new MiscPemGenerator(o));
		textWriter.Flush();
		byte[] bytes = Encoding.UTF8.GetBytes(textWriter.ToString());
		File.WriteAllBytes(path, bytes);
	}

	public void UpdateCertificate(byte[] data)
	{
		string certificatePath = OptionsHelper.CertificatePath;
		if (OptionsHelper.InsecureUsername == null && certificatePath != null && File.Exists(certificatePath))
		{
			SetupCertificate(data);
			File.WriteAllBytes(certificatePath, data);
		}
	}

	public void Setup(string cert, string privateKey)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		SetupCertificate(Encoding.UTF8.GetBytes(cert));
		AsymmetricKeyParameter privateKey2;
		using (TextReader textReader = new StringReader(privateKey))
		{
			privateKey2 = (AsymmetricKeyParameter)new PemReader(textReader).ReadObject();
		}
		Cipher = new RsaCipher(null, privateKey2);
		_privateKey = privateKey2;
	}

	private void SetupCertificate(byte[] cert)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		CertPathBytes = cert;
		PkixCertPath val = new PkixCertPath((Stream)new MemoryStream(CertPathBytes), "PEM");
		Cert = (X509Certificate)val.Certificates[0];
		if (((X509ExtensionBase)Cert).GetExtensionValue(BsonSchema) != null)
		{
			DerBitString val2 = (DerBitString)Asn1Object.FromByteArray(((X509ExtensionBase)Cert).GetExtensionValue(BsonSchema).GetOctets());
			using MemoryStream memoryStream = new MemoryStream(val2.GetBytes());
			BsonDataReader val3 = new BsonDataReader((Stream)memoryStream);
			try
			{
				Metadata = new JsonSerializer().Deserialize<JObject>((JsonReader)(object)val3);
				return;
			}
			finally
			{
				((IDisposable)val3)?.Dispose();
			}
		}
		Metadata = new JObject();
	}

	public Guid GetPlayerUuid()
	{
		return Settings.IsInsecure ? MakeNameUuidFromString("NO_AUTH|" + Settings.Username) : Settings.Uuid;
		static Guid MakeNameUuidFromString(string name)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(name);
			byte[] array = MD5.Create().ComputeHash(bytes);
			array[6] &= 15;
			array[6] |= 48;
			array[8] &= 63;
			array[8] |= 128;
			byte b = array[6];
			array[6] = array[7];
			array[7] = b;
			b = array[4];
			array[4] = array[5];
			array[5] = b;
			b = array[0];
			array[0] = array[3];
			array[3] = b;
			b = array[1];
			array[1] = array[2];
			array[2] = b;
			return new Guid(array);
		}
	}

	public void HandleAuth2(sbyte[] nonceAEncrypted, sbyte[] cert, out sbyte[] encryptedNonceA, out sbyte[] encryptedNonceB)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		Logger.Info("Starting handling of auth2");
		sbyte[] data = Cipher.DecryptSigned(nonceAEncrypted);
		sbyte[] array = Cipher.DecryptLong(cert);
		string @string = Encoding.UTF8.GetString((byte[])(object)array);
		Logger.Info("Got server cert string {0}", @string);
		_serverCert = new PkixCertPath((Stream)new MemoryStream((byte[])(object)array), "PEM");
		PkixCertPathValidator val = new PkixCertPathValidator();
		PkixCertPathValidatorResult val2 = val.Validate(_serverCert, GetValidationParameters());
		Logger.Info<PkixPolicyNode>("Validation result: {0}", val2.PolicyTree);
		_nonceB = new sbyte[36];
		((Random)(object)SecureRandom).NextBytes((byte[])(object)_nonceB);
		RsaCipher rsaCipher = new RsaCipher(((X509Certificate)_serverCert.Certificates[0]).GetPublicKey(), null);
		encryptedNonceA = rsaCipher.EncryptSigned(data);
		encryptedNonceB = rsaCipher.EncryptSigned(_nonceB);
	}

	public void HandleAuth4(sbyte[] secret, sbyte[] nonceBEncrypted)
	{
		_sharedSecretIv = Cipher.DecryptSigned(secret);
		sbyte[] array = Cipher.DecryptSigned(nonceBEncrypted);
		if (!Arrays.AreEqual((byte[])(object)_nonceB, (byte[])(object)array))
		{
			throw new Exception("NonceB mismatch!");
		}
	}

	public void HandleAuth6()
	{
	}

	static AuthManager()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		Logger = LogManager.GetCurrentClassLogger();
		SecureRandom = new SecureRandom();
		OfficialHypixelIncOidIdentifier = new DerObjectIdentifier("1.3.6.1.4.1.47901");
		BsonSchema = OfficialHypixelIncOidIdentifier.Branch("4");
		Uuid_ = OfficialHypixelIncOidIdentifier.Branch("2");
		_rootCert = "-----BEGIN CERTIFICATE-----\n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\n7qCV3X+cKVkD8yo59dZUfw==\n-----END CERTIFICATE-----";
		X509Certificate val;
		using (TextReader textReader = new StringReader(_rootCert))
		{
			val = (X509Certificate)new PemReader(textReader).ReadObject();
		}
		TrustAnchor val2 = new TrustAnchor(val, (byte[])null);
		TrustAnchors = (ISet)new HashSet();
		TrustAnchors.Add((object)val2);
	}

	private static PkixParameters GetValidationParameters()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		PkixParameters val = new PkixParameters(TrustAnchors);
		val.IsRevocationEnabled = false;
		return val;
	}
}
