using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Connect;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct VerifyIdTokenCallbackInfoInternal : ICallbackInfoInternal, IGettable<VerifyIdTokenCallbackInfo>, ISettable<VerifyIdTokenCallbackInfo>, IDisposable
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_ProductUserId;

	private int m_IsAccountInfoPresent;

	private ExternalAccountType m_AccountIdType;

	private IntPtr m_AccountId;

	private IntPtr m_Platform;

	private IntPtr m_DeviceType;

	private IntPtr m_ClientId;

	private IntPtr m_ProductId;

	private IntPtr m_SandboxId;

	private IntPtr m_DeploymentId;

	public Result ResultCode
	{
		get
		{
			return m_ResultCode;
		}
		set
		{
			m_ResultCode = value;
		}
	}

	public object ClientData
	{
		get
		{
			Helper.Get(m_ClientData, out object to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ClientData);
		}
	}

	public IntPtr ClientDataAddress => m_ClientData;

	public ProductUserId ProductUserId
	{
		get
		{
			Helper.Get(m_ProductUserId, out ProductUserId to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ProductUserId);
		}
	}

	public bool IsAccountInfoPresent
	{
		get
		{
			Helper.Get(m_IsAccountInfoPresent, out var to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_IsAccountInfoPresent);
		}
	}

	public ExternalAccountType AccountIdType
	{
		get
		{
			return m_AccountIdType;
		}
		set
		{
			m_AccountIdType = value;
		}
	}

	public Utf8String AccountId
	{
		get
		{
			Helper.Get(m_AccountId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_AccountId);
		}
	}

	public Utf8String Platform
	{
		get
		{
			Helper.Get(m_Platform, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_Platform);
		}
	}

	public Utf8String DeviceType
	{
		get
		{
			Helper.Get(m_DeviceType, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_DeviceType);
		}
	}

	public Utf8String ClientId
	{
		get
		{
			Helper.Get(m_ClientId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ClientId);
		}
	}

	public Utf8String ProductId
	{
		get
		{
			Helper.Get(m_ProductId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_ProductId);
		}
	}

	public Utf8String SandboxId
	{
		get
		{
			Helper.Get(m_SandboxId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_SandboxId);
		}
	}

	public Utf8String DeploymentId
	{
		get
		{
			Helper.Get(m_DeploymentId, out Utf8String to);
			return to;
		}
		set
		{
			Helper.Set(value, ref m_DeploymentId);
		}
	}

	public void Set(ref VerifyIdTokenCallbackInfo other)
	{
		ResultCode = other.ResultCode;
		ClientData = other.ClientData;
		ProductUserId = other.ProductUserId;
		IsAccountInfoPresent = other.IsAccountInfoPresent;
		AccountIdType = other.AccountIdType;
		AccountId = other.AccountId;
		Platform = other.Platform;
		DeviceType = other.DeviceType;
		ClientId = other.ClientId;
		ProductId = other.ProductId;
		SandboxId = other.SandboxId;
		DeploymentId = other.DeploymentId;
	}

	public void Set(ref VerifyIdTokenCallbackInfo? other)
	{
		if (other.HasValue)
		{
			ResultCode = other.Value.ResultCode;
			ClientData = other.Value.ClientData;
			ProductUserId = other.Value.ProductUserId;
			IsAccountInfoPresent = other.Value.IsAccountInfoPresent;
			AccountIdType = other.Value.AccountIdType;
			AccountId = other.Value.AccountId;
			Platform = other.Value.Platform;
			DeviceType = other.Value.DeviceType;
			ClientId = other.Value.ClientId;
			ProductId = other.Value.ProductId;
			SandboxId = other.Value.SandboxId;
			DeploymentId = other.Value.DeploymentId;
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientData);
		Helper.Dispose(ref m_ProductUserId);
		Helper.Dispose(ref m_AccountId);
		Helper.Dispose(ref m_Platform);
		Helper.Dispose(ref m_DeviceType);
		Helper.Dispose(ref m_ClientId);
		Helper.Dispose(ref m_ProductId);
		Helper.Dispose(ref m_SandboxId);
		Helper.Dispose(ref m_DeploymentId);
	}

	public void Get(out VerifyIdTokenCallbackInfo output)
	{
		output = default(VerifyIdTokenCallbackInfo);
		output.Set(ref this);
	}
}
