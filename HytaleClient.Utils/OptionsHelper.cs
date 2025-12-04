using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HytaleClient.Application.Services;
using HytaleClient.Data.Boot;
using NDesk.Options;

namespace HytaleClient.Utils;

internal static class OptionsHelper
{
	private static readonly string ExeName = AppDomain.CurrentDomain.FriendlyName;

	public static ServicesEndpoint Endpoint { get; private set; } = ServicesEndpoint.Default;


	public static string ServerAddress { get; private set; }

	public static string WorldName { get; private set; }

	public static bool AutoProfiling { get; private set; }

	public static bool DisableAffinity { get; private set; }

	public static string UserDataDirectory { get; private set; }

	public static string DataDirectory { get; private set; }

	public static string JavaExecutable { get; private set; }

	public static string AssetsDirectory { get; private set; }

	public static string ServerJar { get; private set; }

	public static IReadOnlyList<string> CustomServerArgumentList { get; private set; } = new List<string>();


	public static string CertificatePath { get; private set; }

	public static string PrivateKeyPath { get; private set; }

	public static bool IsUiDevEnabled { get; private set; }

	public static string LogFileOverride { get; private set; }

	public static bool LaunchEditor { get; private set; }

	public static string OpenAssetPath { get; private set; }

	public static string OpenAssetId { get; private set; }

	public static string OpenAssetType { get; private set; }

	public static bool OpenCosmetics { get; private set; }

	public static float AutoReconnectDelay { get; private set; }

	public static string InsecureUsername { get; private set; }

	public static bool DisableCharacterAtlasCompression { get; private set; }

	public static bool GenerateUIDocs { get; private set; }

	public static bool DisableServices { get; private set; }

	private static string WorkspacesDirectory { get; set; }

	public static bool Setup(string[] args)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0493: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e1: Expected O, but got Unknown
		bool showHelp = false;
		OptionSet val = new OptionSet();
		val.Add("endpoint=", "Sets the endpoint to use for auth/services", (Action<string>)delegate(string v)
		{
			Endpoint = ServicesEndpoint.Parse(v);
		});
		val.Add("s|server=", "Connect to the specified server", (Action<string>)delegate(string v)
		{
			ServerAddress = v;
		});
		val.Add("w|world=", "Load the specified world", (Action<string>)delegate(string v)
		{
			WorldName = v;
		});
		val.Add("p|profiling", "Show the debug screen by default and run the command '.profiling on all' when joining a game", (Action<string>)delegate(string v)
		{
			AutoProfiling = v != null;
		});
		val.Add("disableaffinity", "Disables setting the process affinity", (Action<string>)delegate(string v)
		{
			DisableAffinity = v != null;
		});
		val.Add("data-dir=", "Path to the game's data directory", (Action<string>)delegate(string v)
		{
			DataDirectory = v;
		});
		val.Add("user-dir=", "Path to the user's data directory", (Action<string>)delegate(string v)
		{
			UserDataDirectory = v;
		});
		val.Add("java-exec=", "Path to the Java executable used for starting the server in single-player", (Action<string>)delegate(string v)
		{
			JavaExecutable = v;
		});
		val.Add("assets-dir=", "Path to the assets directory", (Action<string>)delegate(string v)
		{
			AssetsDirectory = v;
		});
		val.Add("server-jar=", "Path to the server JAR executable", (Action<string>)delegate(string v)
		{
			ServerJar = v;
		});
		val.Add("cert=", "Path to the client's certificate", (Action<string>)delegate(string v)
		{
			CertificatePath = v;
		});
		val.Add("key=", "Path to the client's private key", (Action<string>)delegate(string v)
		{
			PrivateKeyPath = v;
		});
		val.Add("log-file=", "Override the path used for the log file", (Action<string>)delegate(string v)
		{
			LogFileOverride = v;
		});
		val.Add("editor", "Launches the editor", (Action<string>)delegate(string v)
		{
			LaunchEditor = v != null;
		});
		val.Add("open-asset-path=", "Opens the asset with the specified path in the asset editor", (Action<string>)delegate(string v)
		{
			OpenAssetPath = v;
		});
		val.Add("open-asset-id=", "Opens the asset with the specified id in the asset editor", (Action<string>)delegate(string v)
		{
			OpenAssetId = v;
		});
		val.Add("open-asset-type=", "Opens the asset with the specified type in the asset editor", (Action<string>)delegate(string v)
		{
			OpenAssetType = v;
		});
		val.Add("open-cosmetics", "Launches the cosmetics editor without opening an asset", (Action<string>)delegate(string v)
		{
			OpenCosmetics = v != null;
		});
		val.Add("ui-dev", "If specified, UI development mode will be enabled", (Action<string>)delegate(string v)
		{
			IsUiDevEnabled = v != null;
		});
		val.Add("dev-workspace-dir=", "Root directory containing the cloned client/server/assets repositories", (Action<string>)delegate(string v)
		{
			WorkspacesDirectory = v;
		});
		val.Add("auto-reconnect-delay=", "Enables auto-reconnect and sets the delay in milliseconds", (Action<string>)delegate(string v)
		{
			AutoReconnectDelay = float.Parse(v, CultureInfo.InvariantCulture);
		});
		val.Add("insecure-username=", "Sets the username for insecure auth", (Action<string>)delegate(string v)
		{
			InsecureUsername = v;
		});
		val.Add("disable-characteratlas-compression=", "Disables compression for the character atlas", (Action<string>)delegate(string v)
		{
			DisableCharacterAtlasCompression = v != null;
		});
		val.Add("generate-ui-docs", "Generates UI documentation files and quits immediately", (Action<string>)delegate(string v)
		{
			GenerateUIDocs = v != null;
		});
		val.Add("disable-services=", "Disables Hytale services", (Action<string>)delegate(string v)
		{
			DisableServices = v != null;
		});
		val.Add("h|help", "Show this message and exit", (Action<string>)delegate(string v)
		{
			showHelp = v != null;
		});
		OptionSet val2 = val;
		try
		{
			List<string> list = val2.Parse((IEnumerable<string>)args);
			if (list.Count > 0)
			{
				LoadLegacyPayload(string.Join(" ", list));
			}
		}
		catch (Exception e)
		{
			return ShowParseError(e);
		}
		if (showHelp)
		{
			return ShowHelp(val2);
		}
		SetDevelopmentDefaults();
		return ValidateArguments();
	}

	private static bool ValidateArguments()
	{
		try
		{
			ValidateDirectory(DataDirectory, "Data directory");
			ValidateDirectory(AssetsDirectory, "Assets directory");
			CreateDirectory(UserDataDirectory, "User data directory");
			OpenFile(JavaExecutable, "Java executable", required: false);
			OpenFile(ServerJar, "Server JAR file", required: false);
			if (LogFileOverride != null)
			{
				ValidateDirectory(Path.GetDirectoryName(LogFileOverride), "Log file directory");
			}
		}
		catch (Exception e)
		{
			return ShowParseError(e);
		}
		OpenFile(CertificatePath, "Client certificate", required: false);
		OpenFile(PrivateKeyPath, "Client private key", required: false);
		return true;
	}

	[Obsolete]
	private static void LoadLegacyPayload(string json)
	{
		LegacyBootPayload legacyBootPayload = LegacyBootPayload.Parse(json);
		AssetsDirectory = legacyBootPayload.AssetsDirectory;
		ServerJar = legacyBootPayload.ServerJar;
		CustomServerArgumentList = legacyBootPayload.CustomServerArguments.Split(new char[1] { ' ' });
		JavaExecutable = legacyBootPayload.JavaExecutable;
		if (DataDirectory == null)
		{
			DataDirectory = Path.Combine(Paths.App, "Data");
		}
		if (UserDataDirectory == null)
		{
			UserDataDirectory = Path.Combine(Paths.App, LaunchEditor ? "EditorUserData" : "UserData");
		}
		LoadLegacyCredentials();
	}

	private static void OpenFile(string file, string name, bool required = true)
	{
		if (required && ValidateOption(file, name, required) && !File.Exists(file))
		{
			throw new FileNotFoundException(name + " is set to a file that doesn't exist: " + file);
		}
	}

	private static bool ShowHelp(OptionSet p)
	{
		Console.WriteLine("Usage: " + ExeName + " [OPTIONS]+");
		Console.WriteLine();
		Console.WriteLine("Options:");
		p.WriteOptionDescriptions(Console.Out);
		return false;
	}

	private static bool ShowParseError(Exception e)
	{
		Console.Write(ExeName + ": ");
		Console.WriteLine(e.Message);
		Console.WriteLine("Try '" + ExeName + " --help' for more information.");
		return false;
	}

	private static string GetDefaultJavaExecutable()
	{
		return EnvironmentHelper.ResolvePathExecutable((BuildInfo.Platform == Platform.Windows) ? "java.exe" : "java");
	}

	private static void LoadLegacyCredentials()
	{
		try
		{
			string homeDirectory = LegacyLauncher.GetHomeDirectory();
			string text = Path.Combine(homeDirectory, "cert");
			string text2 = Path.Combine(homeDirectory, "privKey");
			if (!File.Exists(text))
			{
				throw new FileNotFoundException("Could not find certificate file: " + text);
			}
			if (!File.Exists(text2))
			{
				throw new FileNotFoundException("Could not find private key file: " + text2);
			}
			CertificatePath = text;
			PrivateKeyPath = text2;
		}
		catch (Exception value)
		{
			Console.WriteLine("Couldn't load credentials from launcher v1!");
			Console.WriteLine(value);
		}
	}

	private static void SetDevelopmentDefaults()
	{
		if (WorkspacesDirectory == null)
		{
			WorkspacesDirectory = Path.GetFullPath(Path.Combine(Paths.App, "..", "..", "..", "..", ".."));
		}
		if (JavaExecutable == null)
		{
			JavaExecutable = GetDefaultJavaExecutable();
		}
		if (DataDirectory == null)
		{
			string fullPath = Path.GetFullPath(Path.Combine(Paths.App, "..", "..", "..", ".."));
			DataDirectory = Path.Combine(fullPath, "Data");
		}
		if (UserDataDirectory == null)
		{
			UserDataDirectory = Path.Combine(Paths.App, LaunchEditor ? "EditorUserData" : "UserData");
		}
		if (AssetsDirectory == null)
		{
			AssetsDirectory = Path.Combine(WorkspacesDirectory, "HytaleAssets");
		}
		if (ServerJar == null)
		{
			ServerJar = Path.Combine(WorkspacesDirectory, "HytaleServer", "dist", "HytaleServer", "HytaleServer.jar");
		}
		if (PrivateKeyPath == null || CertificatePath == null)
		{
			Console.WriteLine("Incomplete certificate/key pair provided for authentication, will try to find credentials from launcher v1");
			LoadLegacyCredentials();
		}
		IsUiDevEnabled = true;
	}

	private static void CreateDirectory(string dir, string name, bool required = false)
	{
		if (ValidateOption(dir, name, required))
		{
			Directory.CreateDirectory(dir);
		}
	}

	private static void ValidateDirectory(string dir, string name, bool required = true)
	{
		if (required && ValidateOption(dir, name, required) && !Directory.Exists(dir))
		{
			throw new DirectoryNotFoundException(name + " is set to non-existent directory: " + dir);
		}
	}

	private static bool ValidateOption(string value, string name, bool required)
	{
		if (value == null)
		{
			if (required)
			{
				throw new NullReferenceException(name + " not specified");
			}
			return false;
		}
		return true;
	}
}
