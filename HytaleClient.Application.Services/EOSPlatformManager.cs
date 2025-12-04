using System;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Application.Services;

public class EOSPlatformManager : IDisposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private PlatformInterface _platformInterface;

	private bool _isInitialized;

	private bool _isShuttingDown;

	private const string PRODUCT_ID = "2478e3fa38a64759ae0a92c9e82f97db";

	private const string SANDBOX_ID = "357cc3ff28e64b81b2e54bce94f83658";

	private const string DEPLOYMENT_ID = "93e892227d32478e8b2d5cba0afca1cb";

	private const string CLIENT_ID = "xyza789191612kVFquMeGDt4TY3ANrrv";

	private const string CLIENT_SECRET = "ULcoQJfUztdw+Vsfs8Q0oIdiu4VH6DdMAouzCpboguI";

	public PlatformInterface Platform => _platformInterface;

	public bool IsInitialized => _isInitialized;

	public EOSPlatformManager()
	{
		LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Verbose);
		LoggingInterface.SetCallback(delegate(ref LogMessage message)
		{
			switch (message.Level)
			{
			case LogLevel.Error:
				Logger.Error($"[EOS] {message.Message}");
				break;
			case LogLevel.Warning:
				Logger.Warn($"[EOS] {message.Message}");
				break;
			case LogLevel.Info:
				Logger.Info($"[EOS] {message.Message}");
				break;
			case LogLevel.Verbose:
			case LogLevel.VeryVerbose:
				Logger.Debug($"[EOS] {message.Message}");
				break;
			}
		});
	}

	public Result Initialize()
	{
		if (_isInitialized)
		{
			Logger.Warn("EOS Platform is already initialized");
			return Result.Success;
		}
		try
		{
			InitializeOptions initializeOptions = default(InitializeOptions);
			initializeOptions.ProductName = "testing";
			initializeOptions.ProductVersion = "1.0.0";
			initializeOptions.Reserved = IntPtr.Zero;
			initializeOptions.AllocateMemoryFunction = IntPtr.Zero;
			initializeOptions.ReallocateMemoryFunction = IntPtr.Zero;
			initializeOptions.ReleaseMemoryFunction = IntPtr.Zero;
			initializeOptions.SystemInitializeOptions = IntPtr.Zero;
			initializeOptions.OverrideThreadAffinity = null;
			InitializeOptions options = initializeOptions;
			Result result = PlatformInterface.Initialize(ref options);
			if (result != 0)
			{
				Logger.Error($"Failed to initialize EOS SDK: {result}");
				return result;
			}
			Options options2 = default(Options);
			options2.Reserved = IntPtr.Zero;
			options2.ProductId = "2478e3fa38a64759ae0a92c9e82f97db";
			options2.SandboxId = "357cc3ff28e64b81b2e54bce94f83658";
			options2.ClientCredentials = new ClientCredentials
			{
				ClientId = "xyza789191612kVFquMeGDt4TY3ANrrv",
				ClientSecret = "ULcoQJfUztdw+Vsfs8Q0oIdiu4VH6DdMAouzCpboguI"
			};
			options2.IsServer = false;
			options2.DeploymentId = "93e892227d32478e8b2d5cba0afca1cb";
			options2.Flags = PlatformFlags.None;
			options2.CacheDirectory = Path.Combine(Paths.UserData, "EOSCache");
			options2.TickBudgetInMilliseconds = 0u;
			options2.RTCOptions = null;
			options2.IntegratedPlatformOptionsContainerHandle = null;
			options2.SystemSpecificOptions = IntPtr.Zero;
			options2.TaskNetworkTimeoutSeconds = 30.0;
			Options options3 = options2;
			_platformInterface = PlatformInterface.Create(ref options3);
			if (_platformInterface == null)
			{
				Logger.Error("Failed to create EOS Platform Interface");
				return Result.UnexpectedError;
			}
			_isInitialized = true;
			Logger.Info("EOS Platform initialized successfully");
			return Result.Success;
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Exception during EOS initialization");
			return Result.UnexpectedError;
		}
	}

	public void Tick()
	{
		if (_isInitialized && !_isShuttingDown)
		{
			_platformInterface?.Tick();
		}
	}

	public void Shutdown()
	{
		if (!_isInitialized || _isShuttingDown)
		{
			return;
		}
		_isShuttingDown = true;
		Logger.Info("Shutting down EOS Platform");
		try
		{
			_platformInterface?.Release();
			_platformInterface = null;
			PlatformInterface.Shutdown();
			_isInitialized = false;
			Logger.Info("EOS Platform shut down successfully");
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Exception during EOS shutdown");
		}
	}

	public void Dispose()
	{
		Shutdown();
	}
}
