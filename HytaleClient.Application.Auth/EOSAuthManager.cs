using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using HytaleClient.Application.Services;
using NLog;

namespace HytaleClient.Application.Auth;

public class EOSAuthManager
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly EOSPlatformManager _platformManager;

	private AuthInterface _authInterface;

	private ConnectInterface _connectInterface;

	public bool IsLoggedIn { get; private set; }

	public EpicAccountId LocalUserId { get; private set; }

	public ProductUserId ProductUserId { get; private set; }

	public EOSAuthManager(EOSPlatformManager platformManager)
	{
		_platformManager = platformManager;
		if (_platformManager.IsInitialized)
		{
			_authInterface = _platformManager.Platform.GetAuthInterface();
			_connectInterface = _platformManager.Platform.GetConnectInterface();
		}
	}

	public void LoginWithDeviceId(Action<Result> callback)
	{
		if (_connectInterface == null)
		{
			Logger.Error("Connect interface not available");
			callback?.Invoke(Result.NotConfigured);
			return;
		}
		CreateDeviceIdOptions createDeviceIdOptions = default(CreateDeviceIdOptions);
		createDeviceIdOptions.DeviceModel = "PC";
		CreateDeviceIdOptions options = createDeviceIdOptions;
		_connectInterface.CreateDeviceId(ref options, null, delegate(ref CreateDeviceIdCallbackInfo createCallbackInfo)
		{
			Result resultCode = createCallbackInfo.ResultCode;
			if (resultCode != 0 && resultCode != Result.DuplicateNotAllowed)
			{
				Logger.Error($"Failed to create device ID: {resultCode}");
				callback?.Invoke(resultCode);
			}
			else
			{
				Epic.OnlineServices.Connect.LoginOptions loginOptions = default(Epic.OnlineServices.Connect.LoginOptions);
				loginOptions.Credentials = new Epic.OnlineServices.Connect.Credentials
				{
					Type = ExternalCredentialType.DeviceidAccessToken,
					Token = null
				};
				Epic.OnlineServices.Connect.LoginOptions options2 = loginOptions;
				_connectInterface.Login(ref options2, null, delegate(ref Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo)
				{
					Result resultCode2 = loginCallbackInfo.ResultCode;
					ProductUserId localUserId = loginCallbackInfo.LocalUserId;
					if (resultCode2 == Result.Success)
					{
						ProductUserId = localUserId;
						IsLoggedIn = true;
						Logger.Info($"Successfully logged in with Product User ID: {ProductUserId}");
					}
					else
					{
						Logger.Error($"Failed to login: {resultCode2}");
					}
					callback?.Invoke(resultCode2);
				});
			}
		});
	}

	public void LoginWithEpicAccount(LoginCredentialType credentialType, string id, string token, Action<Result> callback)
	{
		if (_authInterface == null)
		{
			Logger.Error("Auth interface not available");
			callback?.Invoke(Result.NotConfigured);
			return;
		}
		Epic.OnlineServices.Auth.LoginOptions loginOptions = default(Epic.OnlineServices.Auth.LoginOptions);
		loginOptions.Credentials = new Epic.OnlineServices.Auth.Credentials
		{
			Type = credentialType,
			Id = id,
			Token = token
		};
		loginOptions.ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence;
		Epic.OnlineServices.Auth.LoginOptions options = loginOptions;
		_authInterface.Login(ref options, null, delegate(ref Epic.OnlineServices.Auth.LoginCallbackInfo authCallbackInfo)
		{
			Result resultCode = authCallbackInfo.ResultCode;
			EpicAccountId localUserId = authCallbackInfo.LocalUserId;
			if (resultCode == Result.Success)
			{
				LocalUserId = localUserId;
				Logger.Info($"Successfully logged in with Epic Account ID: {LocalUserId}");
				LinkEpicAccountToConnect(delegate
				{
					callback?.Invoke(resultCode);
				});
			}
			else
			{
				Logger.Error($"Failed to login to Epic account: {resultCode}");
				callback?.Invoke(resultCode);
			}
		});
	}

	private void LinkEpicAccountToConnect(Action onComplete)
	{
		if (_authInterface == null || LocalUserId == null || _connectInterface == null)
		{
			onComplete?.Invoke();
			return;
		}
		Epic.OnlineServices.Auth.CopyIdTokenOptions copyIdTokenOptions = default(Epic.OnlineServices.Auth.CopyIdTokenOptions);
		copyIdTokenOptions.AccountId = LocalUserId;
		Epic.OnlineServices.Auth.CopyIdTokenOptions options = copyIdTokenOptions;
		if (_authInterface.CopyIdToken(ref options, out var outIdToken) == Result.Success)
		{
			Epic.OnlineServices.Connect.LoginOptions loginOptions = default(Epic.OnlineServices.Connect.LoginOptions);
			loginOptions.Credentials = new Epic.OnlineServices.Connect.Credentials
			{
				Type = ExternalCredentialType.EpicIdToken,
				Token = outIdToken.Value.JsonWebToken
			};
			Epic.OnlineServices.Connect.LoginOptions options2 = loginOptions;
			_connectInterface.Login(ref options2, null, delegate(ref Epic.OnlineServices.Connect.LoginCallbackInfo connectCallbackInfo)
			{
				Result resultCode = connectCallbackInfo.ResultCode;
				ProductUserId localUserId = connectCallbackInfo.LocalUserId;
				switch (resultCode)
				{
				case Result.Success:
					ProductUserId = localUserId;
					Logger.Info($"Successfully linked Epic account to Connect interface with Product User ID: {ProductUserId}");
					break;
				case Result.InvalidUser:
					CreateConnectUser(onComplete);
					return;
				default:
					Logger.Error($"Failed to link Epic account to Connect: {resultCode}");
					break;
				}
				onComplete?.Invoke();
			});
			outIdToken = null;
		}
		else
		{
			Logger.Error("Failed to copy ID token for Connect login");
			onComplete?.Invoke();
		}
	}

	private void CreateConnectUser(Action onComplete)
	{
		if (_authInterface == null || LocalUserId == null || _connectInterface == null)
		{
			onComplete?.Invoke();
			return;
		}
		CopyUserAuthTokenOptions options = default(CopyUserAuthTokenOptions);
		if (_authInterface.CopyUserAuthToken(ref options, LocalUserId, out var outUserAuthToken) == Result.Success)
		{
			CreateUserOptions createUserOptions = default(CreateUserOptions);
			createUserOptions.ContinuanceToken = null;
			CreateUserOptions options2 = createUserOptions;
			_connectInterface.CreateUser(ref options2, null, delegate(ref CreateUserCallbackInfo createUserCallbackInfo)
			{
				Result resultCode = createUserCallbackInfo.ResultCode;
				ProductUserId localUserId = createUserCallbackInfo.LocalUserId;
				if (resultCode == Result.Success)
				{
					ProductUserId = localUserId;
					Logger.Info($"Successfully created Connect user with Product User ID: {ProductUserId}");
				}
				else
				{
					Logger.Error($"Failed to create Connect user: {resultCode}");
				}
				onComplete?.Invoke();
			});
			outUserAuthToken = null;
		}
		else
		{
			Logger.Error("Failed to copy user auth token for Connect user creation");
			onComplete?.Invoke();
		}
	}

	public void Logout(Action<Result> callback)
	{
		if (_authInterface != null && LocalUserId != null)
		{
			Epic.OnlineServices.Auth.LogoutOptions logoutOptions = default(Epic.OnlineServices.Auth.LogoutOptions);
			logoutOptions.LocalUserId = LocalUserId;
			Epic.OnlineServices.Auth.LogoutOptions options = logoutOptions;
			_authInterface.Logout(ref options, null, delegate(ref Epic.OnlineServices.Auth.LogoutCallbackInfo authCallbackInfo)
			{
				Result resultCode = authCallbackInfo.ResultCode;
				if (resultCode == Result.Success)
				{
					LocalUserId = null;
					IsLoggedIn = false;
					Logger.Info("Successfully logged out");
				}
				callback?.Invoke(resultCode);
			});
		}
		else
		{
			callback?.Invoke(Result.NotFound);
		}
	}
}
