using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hypixel.ProtoPlus;
using HytaleClient.Application.Auth;
using HytaleClient.Auth.Proto.Protocol;
using HytaleClient.Core;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.OpenSsl;

namespace HytaleClient.Application.Services;

internal class HytaleServices : Disposable
{
	private class AwaitingCallback
	{
		public readonly Action<ClientFailureNotification> FailureCallback;

		public readonly Action<ProtoPacket> SuccessCallback;

		public readonly DateTime TimeoutDateTime;

		public AwaitingCallback(Action<ClientFailureNotification> failureCallback, Action<ProtoPacket> successCallback)
		{
			FailureCallback = failureCallback;
			SuccessCallback = successCallback;
			TimeoutDateTime = DateTime.Now.AddSeconds(10.0);
		}
	}

	public enum ServiceState
	{
		Disconnected,
		Connected,
		Connecting,
		Authenticating
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly ConcurrentDictionary<Guid, ClientUserWrapper> Users = new ConcurrentDictionary<Guid, ClientUserWrapper>();

	public readonly ConcurrentDictionary<Guid, ClientUserState> UserStates = new ConcurrentDictionary<Guid, ClientUserState>();

	public readonly List<Guid> BlockedPlayers = new List<Guid>();

	public readonly Dictionary<Guid, long> IncomingFriendRequests = new Dictionary<Guid, long>();

	public readonly Dictionary<Guid, long> OutgoingFriendRequests = new Dictionary<Guid, long>();

	public readonly List<Guid> Friends = new List<Guid>();

	public readonly List<ClientGuildInvitationWrapper> GuildInvitations = new List<ClientGuildInvitationWrapper>();

	public ClientPartyWrapper PartyWrapper;

	public readonly List<ClientPartyInvitationWrapper> PartyInvitations = new List<ClientPartyInvitationWrapper>();

	public ClientGuildWrapper GuildWrapper;

	public List<ClientGameWrapper> Games;

	public List<ClientSharedSinglePlayerJoinableWorldWrapper> SharedSinglePlayerJoinableWorlds = new List<ClientSharedSinglePlayerJoinableWorldWrapper>();

	public List<ClientSharedSinglePlayerJoinableWorldWrapper> SharedSinglePlayerInvitedWorlds = new List<ClientSharedSinglePlayerJoinableWorldWrapper>();

	public readonly QueueTicketStateMachine QueueTicket;

	private readonly ServicesClient _client;

	private readonly ConcurrentDictionary<int, AwaitingCallback> _callbacks = new ConcurrentDictionary<int, AwaitingCallback>();

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private int _currentId;

	private readonly AuthManager _authManager;

	public HytaleServices(App app)
	{
		HytaleServices hytaleServices = this;
		_authManager = app.AuthManager;
		QueueTicket = new QueueTicketStateMachine(app);
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		try
		{
			ServicesEndpoint endpoint = OptionsHelper.Endpoint;
			Logger.Info<ServicesEndpoint>("Starting with endpoint {0}", endpoint);
			_client = new ServicesClient(app, endpoint.Host, endpoint.Port, endpoint.Secure, "ws", new ServicesPacketHandler(app, this), delegate
			{
				hytaleServices.QueueTicket.HandleConnectionClose();
				app.Engine.RunOnMainThread(hytaleServices, delegate
				{
					app.Interface.OnServicesStateChanged(ServiceState.Disconnected);
				}, allowCallFromMainThread: true);
			});
		}
		catch (Exception ex)
		{
			Logger.Error(ex, "Got exception when starting services client:");
		}
		ScheduleCallbackCleanup();
	}

	private void ScheduleCallbackCleanup()
	{
		Task.Delay(20000, _cancellationTokenSource.Token).ContinueWith(delegate(Task task)
		{
			if (!task.IsCanceled)
			{
				DateTime now = DateTime.Now;
				foreach (KeyValuePair<int, AwaitingCallback> item in new Dictionary<int, AwaitingCallback>(_callbacks))
				{
					if (now > item.Value.TimeoutDateTime)
					{
						_callbacks.TryRemove(item.Key, out var _);
					}
				}
				ScheduleCallbackCleanup();
			}
		});
	}

	private int GetNextId()
	{
		return Interlocked.Add(ref _currentId, 1);
	}

	public void ProcessTypedCallback(int id, ProtoPacket packet)
	{
		if (_callbacks.TryRemove(id, out var value))
		{
			value.SuccessCallback?.Invoke(packet);
		}
	}

	public void ProcessFailureCallback(int id, ClientFailureNotification notification)
	{
		if (_callbacks.TryRemove(id, out var value))
		{
			value.FailureCallback?.Invoke(notification);
		}
	}

	public bool IsConnected()
	{
		return _client != null && _client.IsConnected();
	}

	public void Ingest(ClientUser[] users)
	{
		foreach (ClientUser val in users)
		{
			Users[val.Uuid_] = new ClientUserWrapper(val);
		}
	}

	public void Ingest(ClientUser user)
	{
		Users[user.Uuid_] = new ClientUserWrapper(user);
	}

	public void Ingest(ClientUser[] users, List<Guid> into)
	{
		foreach (ClientUser val in users)
		{
			if (val != null)
			{
				Users[val.Uuid_] = new ClientUserWrapper(val);
				into.Add(val.Uuid_);
			}
		}
	}

	public void Ingest(ClientGuildMember[] guildMembers)
	{
		foreach (ClientGuildMember val in guildMembers)
		{
			Users[val.User.Uuid_] = new ClientUserWrapper(val.User);
		}
	}

	private void RegisterCallback<T>(int id, Action<ClientFailureNotification> onFailure, Action<T> onSuccess) where T : ProtoPacket
	{
		_callbacks[id] = new AwaitingCallback(onFailure, (onSuccess != null) ? ((Action<ProtoPacket>)delegate(ProtoPacket o)
		{
			onSuccess((T)(object)o);
		}) : null);
	}

	public void SetSkinRekey(sbyte[] pubKey, sbyte[] skinBlob, Action<ClientFailureNotification> onFailure = null, Action<ClientCertificateRefresh> onSuccess = null)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientCertificateRefresh>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientSetSkinRekey(pubKey, skinBlob, nextId));
	}

	public void SendFriendRequestByUsername(string username, Action<ClientFailureNotification> onFailure = null, Action<ClientSuccessNotification> onSuccess = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSuccessNotification>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientNamedFriendRequestServerbound(username, nextId));
	}

	public void AnswerFriendRequest(Guid userId, bool accept, Action<ClientFailureNotification> onFailure = null, Action<ClientFriendRequestAccepted> onSuccess = null)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientFriendRequestAccepted>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientFriendRequestReply(userId, accept, nextId));
	}

	public void RemoveFriend(Guid userId, Action<ClientFailureNotification> onFailure = null, Action<ClientSuccessNotification> onSuccess = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSuccessNotification>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientRemoveFriend(userId, nextId));
	}

	public void SendMessage(Guid recipient, string message, Action<ClientFailureNotification> onFailure = null, Action<ClientSuccessNotification> onSuccess = null)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSuccessNotification>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientPrivateMessageOutbound(recipient, message, nextId));
	}

	public void SendPartyMessage(string message, Action<ClientFailureNotification> onFailure = null, Action<ClientChannelMessageInbound> onSuccess = null)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientChannelMessageInbound>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientChannelMessageOutbound((ClientChatChannel)0, message, nextId));
	}

	public void DisbandParty(Action<ClientFailureNotification> onFailure = null, Action<ClientSetParty> onSuccess = null)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSetParty>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientPartyDisband(PartyWrapper.PartyId, nextId));
	}

	public void LeaveParty(Action<ClientFailureNotification> onFailure = null, Action<ClientSetParty> onSuccess = null)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSetParty>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientPartyLeave(PartyWrapper.PartyId, nextId));
	}

	public void RemoveMemberFromParty(Guid userId, Action<ClientFailureNotification> onFailure = null, Action<ClientPartyMembersChange> onSuccess = null)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientPartyMembersChange>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientRemovePartyMember(PartyWrapper.PartyId, userId, nextId));
	}

	public void CreateParty(Action<ClientFailureNotification> onFailure = null, Action<ClientSetParty> onSuccess = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSetParty>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientCreateParty(nextId));
	}

	public void InviteUserToParty(Guid userId, Action<ClientFailureNotification> onFailure = null, Action<ClientSuccessNotification> onSuccess = null)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		if (PartyWrapper == null)
		{
			CreateParty(onFailure, delegate
			{
				InviteUserToParty(userId, onFailure, onSuccess);
				onSuccess?.Invoke(null);
			});
		}
		else
		{
			int nextId = GetNextId();
			RegisterCallback<ClientSuccessNotification>(nextId, onFailure, onSuccess);
			_client.Write((ProtoPacket)new ClientPartyInvite(PartyWrapper.PartyId, userId, nextId));
		}
	}

	public void MakeUserPartyLeader(Guid userId, Action<ClientFailureNotification> onFailure = null, Action<ClientPartyNewLeader> onSuccess = null)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientPartyNewLeader>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientPartyTransfer(PartyWrapper.PartyId, userId, nextId));
	}

	public void FollowToServer(Guid userId, Action<ClientFailureNotification> onFailure = null, Action<ClientPartyNewLeader> onSuccess = null)
	{
	}

	public void JoinSharedSinglePlayerWorld(Guid worldId, Action<ClientFailureNotification> onFailure = null, Action<ClientSuccessNotification> onSuccess = null)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		QueueTicketStateMachine queueTicket = QueueTicket;
		Guid guid = worldId;
		if (queueTicket.TryQueue("ssp:" + guid, null, out var ticket))
		{
			int nextId = GetNextId();
			RegisterCallback<ClientSuccessNotification>(nextId, (Action<ClientFailureNotification>)delegate(ClientFailureNotification res)
			{
				onFailure?.Invoke(res);
			}, (Action<ClientSuccessNotification>)delegate(ClientSuccessNotification res)
			{
				onSuccess?.Invoke(res);
			});
			_client.Write((ProtoPacket)new ClientJoinSSPWorld(worldId, ticket, nextId));
		}
		else
		{
			onFailure?.Invoke(new ClientFailureNotification(0, "TryQueue returned null!", new Dictionary<string, string>()));
		}
	}

	public void CreateSharedSinglePlayerWorld(string name, Action<ClientFailureNotification> onFailure = null, Action<ClientSSPWorldCreated> onSuccess = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSSPWorldCreated>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientCreateSSPWorld(name, nextId));
	}

	public void AnswerPartyInvite(string partyId, bool accept, Action<ClientFailureNotification> onFailure = null, Action<ClientSetParty> onSuccess = null)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSetParty>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientPartyInviteResponse(partyId, accept, nextId));
	}

	public void ToggleUserBlocked(Guid userId, bool blocked, Action<ClientFailureNotification> onFailure = null, Action<ClientBlockToggleNotification> onSuccess = null)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientBlockToggleNotification>(nextId, onFailure, onSuccess);
		_client.Write((ProtoPacket)new ClientPlayerToggleBlock(userId, blocked, nextId));
	}

	public void JoinGameQueue(string queue, sbyte[] extra = null, bool active = true, Action<ClientFailureNotification> onFailure = null, Action<ClientServerQueueReply> onSuccess = null)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		if (QueueTicket.TryQueue(queue, extra, out var ticket))
		{
			int nextId = GetNextId();
			RegisterCallback<ClientServerQueueReply>(nextId, (Action<ClientFailureNotification>)delegate(ClientFailureNotification res)
			{
				onFailure?.Invoke(res);
			}, (Action<ClientServerQueueReply>)delegate(ClientServerQueueReply res)
			{
				onSuccess?.Invoke(res);
			});
			_client.Write((ProtoPacket)new ClientServerQueue(queue, ticket, extra, active, nextId));
		}
		else
		{
			onFailure?.Invoke(new ClientFailureNotification(0, "TryQueue returned null!", new Dictionary<string, string>()));
		}
	}

	public void JoinGameQueueDirect(string queue, string ticket, sbyte[] extra, bool active)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		int nextId = GetNextId();
		_client.Write((ProtoPacket)new ClientServerQueue(queue, ticket, extra, active, nextId));
	}

	public void LeaveGameQueue(Action<ClientFailureNotification> onFailure = null, Action<ClientSuccessNotification> onSuccess = null)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		int nextId = GetNextId();
		RegisterCallback<ClientSuccessNotification>(nextId, onFailure, (Action<ClientSuccessNotification>)delegate(ClientSuccessNotification res)
		{
			QueueTicket.OnLeaveQueueConfirm();
			onSuccess?.Invoke(res);
		});
		_client.Write((ProtoPacket)new ClientLeaveQueue(nextId));
	}

	public void SetPlayerOptions(JObject metadata, Action<Exception> callback = null)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		Logger.Info("SetPlayerOptions() executed");
		if (!IsConnected())
		{
			callback?.Invoke(new Exception("Tried to set player options while not connected to services!"));
			return;
		}
		sbyte[] skinBlob;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			BsonDataWriter val = new BsonDataWriter((Stream)memoryStream);
			try
			{
				new JsonSerializer().Serialize((JsonWriter)(object)val, (object)metadata);
				skinBlob = (sbyte[])(object)memoryStream.ToArray();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		sbyte[] pubKey;
		using (TextWriter textWriter = new StringWriter())
		{
			new PemWriter(textWriter).WriteObject((object)new MiscPemGenerator((object)_authManager.Cert.GetPublicKey()));
			textWriter.Flush();
			pubKey = (sbyte[])(object)Encoding.UTF8.GetBytes(textWriter.ToString());
		}
		SetSkinRekey(pubKey, skinBlob, delegate(ClientFailureNotification exception)
		{
			callback?.Invoke(new Exception("Failed to refresh with error " + exception.CauseLocalizable));
		}, delegate(ClientCertificateRefresh refresh)
		{
			Logger.Info<ClientCertificateRefresh>("Got certificate refresh {0}", refresh);
			_authManager.UpdateCertificate((byte[])(object)refresh.NewCertificate);
		});
	}

	protected override void DoDispose()
	{
		_cancellationTokenSource.Cancel();
		_client.Close();
	}
}
