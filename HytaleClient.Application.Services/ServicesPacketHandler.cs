#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Application.Auth;
using HytaleClient.Auth.Proto.Protocol;
using HytaleClient.AuthHandshake.Proto.Protocol;
using HytaleClient.Core;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Application.Services;

internal class ServicesPacketHandler : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly BlockingCollection<Tuple<ProtoPacket, ServicesClient>> _packets = new BlockingCollection<Tuple<ProtoPacket, ServicesClient>>();

	private readonly HashSet<string> _unhandledPacketTypes = new HashSet<string>();

	private readonly Thread _thread;

	private readonly CancellationTokenSource _threadCancellationTokenSource = new CancellationTokenSource();

	private readonly CancellationToken _threadCancellationToken;

	private readonly App _app;

	private readonly HytaleServices _services;

	private void ProcessAuthDisconnect(Disconnect packet, ServicesClient client)
	{
		Logger.Warn("We were disconnected while authed with message: {0}", packet.Message);
	}

	private void ProcessAuth0(Auth0 packet, ServicesClient client)
	{
		client.AuthState.ProcessAuth0(packet);
	}

	private void ProcessAuth2(Auth2 packet, ServicesClient client)
	{
		client.AuthState.ProcessAuth2(packet);
	}

	private void ProcessAuth4(Auth4 packet, ServicesClient client)
	{
		client.AuthState.ProcessAuth4(packet);
	}

	private void ProcessAuthFinished(ClientAuth6 packet, ServicesClient client)
	{
		client.AuthState.ProcessAuthFinished(packet);
	}

	public ServicesPacketHandler(App app, HytaleServices services)
	{
		_app = app;
		_services = services;
		_threadCancellationToken = _threadCancellationTokenSource.Token;
		_thread = new Thread(ProcessPacketsThreadStart)
		{
			Name = "ServicesPacketHandler",
			IsBackground = true
		};
		_thread.Start();
	}

	public void Receive(ProtoPacket packet, ServicesClient client)
	{
		_packets.Add(Tuple.Create<ProtoPacket, ServicesClient>(packet, client), _threadCancellationToken);
	}

	protected override void DoDispose()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_threadCancellationTokenSource.Cancel();
		_thread.Join();
		_threadCancellationTokenSource.Dispose();
	}

	private void ProcessAuthPacket(ProtoPacket packet, ServicesClient client)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		switch (packet.GetId())
		{
		case 0:
			ProcessAuth0((Auth0)packet, client);
			return;
		case 3:
			ProcessAuth2((Auth2)packet, client);
			return;
		case 5:
			ProcessAuth4((Auth4)packet, client);
			return;
		case 1:
			ProcessDisconnectPacket(packet, client);
			return;
		}
		if (_unhandledPacketTypes.Add(((object)packet).GetType().Name))
		{
			Logger.Warn("Received unhandled packet type: {0}", ((object)packet).GetType().Name);
		}
	}

	private void ProcessPacket(ProtoPacket packet, ServicesClient client)
	{
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Expected O, but got Unknown
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Expected O, but got Unknown
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Expected O, but got Unknown
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Expected O, but got Unknown
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Expected O, but got Unknown
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Expected O, but got Unknown
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Expected O, but got Unknown
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Expected O, but got Unknown
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Expected O, but got Unknown
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Expected O, but got Unknown
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Expected O, but got Unknown
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Expected O, but got Unknown
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Expected O, but got Unknown
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected O, but got Unknown
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Expected O, but got Unknown
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Expected O, but got Unknown
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Expected O, but got Unknown
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Expected O, but got Unknown
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Expected O, but got Unknown
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Expected O, but got Unknown
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Expected O, but got Unknown
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Expected O, but got Unknown
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Expected O, but got Unknown
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Expected O, but got Unknown
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Expected O, but got Unknown
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Expected O, but got Unknown
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		switch (packet.GetId())
		{
		case 49:
			ProcessRequeueFailure((ClientRequeueFailure)packet, client);
			return;
		case 1:
			ProcessDisconnectPacket(packet, client);
			return;
		case 2:
			ProcessAuthFinished((ClientAuth6)packet, client);
			return;
		case 37:
			ProcessPartyMembersChange((ClientPartyMembersChange)packet, client);
			return;
		case 61:
			ProcessSetParty((ClientSetParty)packet, client);
			return;
		case 38:
			ProcessPartyNewLeader((ClientPartyNewLeader)packet, client);
			return;
		case 34:
			ProcessPartyInviteNotification((ClientPartyInviteNotification)packet, client);
			return;
		case 11:
			ProcessFriendRemoved((ClientFriendRemoved)packet, client);
			return;
		case 45:
			ProcessReceiveFriendRequest((ClientReceiveFriendRequest)packet, client);
			return;
		case 12:
			ProcessFriendRequestAccepted((ClientFriendRequestAccepted)packet, client);
			return;
		case 42:
			ProcessPlayerMessage((ClientPrivateMessageInbound)packet, client);
			return;
		case 22:
			ProcessGuildMemberAdd((ClientGuildMemberAdd)packet, client);
			return;
		case 19:
			ProcessGuildInviteNotification((ClientGuildInviteNotification)packet, client);
			return;
		case 23:
			ProcessGuildMemberRemove((ClientGuildMemberRemove)packet, client);
			return;
		case 24:
			ProcessGuildRankChange((ClientGuildRankChange)packet, client);
			return;
		case 60:
			ProcessSetGuild((ClientSetGuild)packet, client);
			return;
		case 57:
			ProcessSelfState((ClientSelfState)packet, client);
			return;
		case 40:
			ProcessPlayerStateChange((ClientPlayerStateChange)packet, client);
			return;
		case 67:
			ProcessUserBoundChat((ClientUserBoundChat)packet, client);
			return;
		case 6:
			ProcessChannelMessage((ClientChannelMessageInbound)packet, client);
			return;
		case 29:
			ProcessLocalizedMessage((ClientLocalizedMessage)packet, client);
			return;
		case 3:
			ProcessBlockToggleNotification((ClientBlockToggleNotification)packet, client);
			return;
		case 31:
			ProcessNewTempLanguageMapping((ClientNewTempLanguageMapping)packet, client);
			return;
		case 59:
			ProcessServerQueueReply((ClientServerQueueReply)packet, client);
			return;
		case 5:
			ProcessCertificateRefresh((ClientCertificateRefresh)packet, client);
			return;
		case 65:
			ProcessSuccessNotification((ClientSuccessNotification)packet, client);
			return;
		case 10:
			ProcessFailureNotification((ClientFailureNotification)packet, client);
			return;
		case 46:
			ProcessRejoinExpired((ClientRejoinExpired)packet, client);
			return;
		case 44:
			ProcessQueueRequest((ClientQueueRequest)packet, client);
			return;
		case 54:
			ProcessSharedSinglePlayerWorldAccessRemoved((ClientSSPWorldAccessRemoved)packet, client);
			return;
		case 56:
			ProcessSharedSinglePlayerWorldInviteNotice((ClientSSPWorldInviteNotice)packet, client);
			return;
		case 55:
			ProcessSharedSinglePlayerWorldCreated((ClientSSPWorldCreated)packet, client);
			return;
		case 15:
			return;
		}
		if (_unhandledPacketTypes.Add(((object)packet).GetType().Name))
		{
			Logger.Warn("Received unhandled packet type: {0}", ((object)packet).GetType().Name);
		}
	}

	private void ProcessDisconnectPacket(ProtoPacket packet, ServicesClient client)
	{
		Disconnect val = (Disconnect)(object)((packet is Disconnect) ? packet : null);
		if (val == null)
		{
			ClientDisconnect val2 = (ClientDisconnect)(object)((packet is ClientDisconnect) ? packet : null);
			if (val2 != null)
			{
				ProcessDisconnect(val2, client);
			}
		}
		else
		{
			ProcessAuthDisconnect(val, client);
		}
	}

	private void ProcessPacketsThreadStart()
	{
		Debug.Assert(ThreadHelper.IsOnThread(_thread));
		Tuple<ProtoPacket, ServicesClient> tuple = null;
		while (true)
		{
			CancellationToken threadCancellationToken = _threadCancellationToken;
			if (!threadCancellationToken.IsCancellationRequested)
			{
				try
				{
					tuple = _packets.Take(_threadCancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				if (tuple.Item2.AuthState.Authed)
				{
					ProcessPacket(tuple.Item1, tuple.Item2);
				}
				else
				{
					ProcessAuthPacket(tuple.Item1, tuple.Item2);
				}
				continue;
			}
			break;
		}
	}

	private void ProcessFriendRemoved(ClientFriendRemoved packet, ServicesClient client)
	{
		_services.Friends.Remove(packet.Player);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.OnServicesFriendsRemoved(packet.Player.ToString());
		});
		Logger.Info("You are no longer friends with {0}", _services.Users[packet.Player].Name);
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessReceiveFriendRequest(ClientReceiveFriendRequest packet, ServicesClient client)
	{
		Logger.Info("Got friend request from {0}!", packet.FriendRequest.User.Name);
		_services.Ingest(packet.FriendRequest.User);
		_services.IncomingFriendRequests.Add(packet.FriendRequest.User.Uuid_, packet.FriendRequest.ExpiresAt);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.TriggerEvent("services.users.updated", new ClientUserWrapper(packet.FriendRequest.User));
			_app.Interface.TriggerEvent("services.friendRequests.added", packet.FriendRequest.User.Uuid_.ToString(), packet.FriendRequest.ExpiresAt.ToString());
		});
	}

	private void ProcessFriendRequestAccepted(ClientFriendRequestAccepted packet, ServicesClient client)
	{
		_services.Ingest(packet.NewFriend);
		_services.Friends.Add(packet.NewFriend.Uuid_);
		_services.IncomingFriendRequests.Remove(packet.NewFriend.Uuid_);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.TriggerEvent("services.users.updated", new ClientUserWrapper(packet.NewFriend));
			_app.Interface.OnServicesFriendsAdded(packet.NewFriend.Uuid_.ToString());
			_app.Interface.TriggerEvent("services.friendRequests.removed", packet.NewFriend.Uuid_.ToString());
		});
		Logger.Info("You are now friends with {0}!", packet.NewFriend.Name);
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessPlayerMessage(ClientPrivateMessageInbound packet, ServicesClient client)
	{
		Logger.Info<string, string>("Received message from {0}: {1}", _services.Users[packet.From].Name, packet.Message);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.TriggerEvent("services.messages.received", packet.From.ToString(), packet.Message, packet.Timestamp.ToString());
		});
	}

	private void ProcessDisconnect(ClientDisconnect packet, ServicesClient client)
	{
		Logger.Info("We were disconnected while authed with message: {0}", packet.Message);
	}

	private void ProcessPlayerStateChange(ClientPlayerStateChange packet, ServicesClient client)
	{
		Logger.Info<ClientPlayerStateChange>("Service state change: {0}", packet);
		_services.UserStates[packet.Uuid_] = new ClientUserState(packet.State);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.OnServicesUserStateChanged(packet.Uuid_.ToString(), new ClientUserState(packet.State));
		});
	}

	private void ProcessRequeueFailure(ClientRequeueFailure packet, ServicesClient client)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Logger.Info<RequeueFailureCause>("Got requeue failure cause {0}", packet.Cause);
	}

	private void ProcessRejoinExpired(ClientRejoinExpired packet, ServicesClient client)
	{
		Logger.Info<Guid>("Got rejoin expired for {0}", packet.WorldId);
	}

	private void ProcessQueueRequest(ClientQueueRequest packet, ServicesClient client)
	{
		Logger.Info("Services requests that we queue for {0}", packet.Key);
		if (_services.QueueTicket.TryQueue(packet.Key, packet.Extra, out var ticket))
		{
			_services.JoinGameQueueDirect(packet.Key, ticket, packet.Extra, active: true);
		}
		else
		{
			Logger.Info<string, string>("Ignoring server queue request because we're already queued for {0} with ticket {1}", _services.QueueTicket.Key, _services.QueueTicket.Ticket);
		}
	}

	private void ProcessSelfState(ClientSelfState packet, ServicesClient client)
	{
		Logger.Info<ClientSelfState, ServicesClient>("Got self state {0} for client {1}", packet, client);
		long num = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
		if (num + 10000 < packet.CurrentTimeMillis || num - 10000 > packet.CurrentTimeMillis)
		{
			Logger.Warn<long, long>("Is your clock right? The server says it's {0} but the local clock reads {1}", packet.CurrentTimeMillis, num);
		}
		else
		{
			Logger.Info<long, long>("Your clock seems to be reasonably correct. Server says it's {0} and local clock reads {1}", packet.CurrentTimeMillis, num);
		}
		if (_services.QueueTicket != null)
		{
			_services.QueueTicket.HandleConnectionOpen();
		}
		AuthManager authManager = _app.AuthManager;
		_services.Games = packet.GameList.Games.Select((ClientGame game) => new ClientGameWrapper(game)).ToList();
		_services.Users.Clear();
		_services.Users[authManager.Settings.Uuid] = new ClientUserWrapper(authManager.Settings.Uuid, authManager.Settings.Username, null);
		_services.BlockedPlayers.Clear();
		_services.Ingest(packet.BlockedPlayers, _services.BlockedPlayers);
		_services.GuildInvitations.Clear();
		ClientGuildInvitation[] guildInvitations = packet.GuildInvitations;
		foreach (ClientGuildInvitation val in guildInvitations)
		{
			_services.Ingest(val.By);
			_services.GuildInvitations.Add(new ClientGuildInvitationWrapper(val));
		}
		_services.IncomingFriendRequests.Clear();
		ClientFriendRequest[] friendRequests = packet.FriendRequests;
		foreach (ClientFriendRequest val2 in friendRequests)
		{
			_services.Ingest(val2.User);
			_services.IncomingFriendRequests[val2.User.Uuid_] = val2.ExpiresAt;
		}
		_services.OutgoingFriendRequests.Clear();
		ClientFriendRequest[] friendRequestsOutbound = packet.FriendRequestsOutbound;
		foreach (ClientFriendRequest val3 in friendRequestsOutbound)
		{
			_services.Ingest(val3.User);
			_services.OutgoingFriendRequests[val3.User.Uuid_] = val3.ExpiresAt;
		}
		_services.Friends.Clear();
		foreach (KeyValuePair<ClientUser, ClientPlayerState> friend in packet.Friends)
		{
			Logger.Info<ClientUser, ClientPlayerState>("Handling friend: {0}, {1}", friend.Key, friend.Value);
			_services.Ingest(friend.Key);
			_services.UserStates[friend.Key.Uuid_] = new ClientUserState(friend.Value);
			_services.Friends.Add(friend.Key.Uuid_);
		}
		if (packet.Party != null)
		{
			_services.PartyWrapper = new ClientPartyWrapper(packet.Party);
			_services.Ingest(packet.Party.Members);
		}
		else
		{
			_services.PartyWrapper = null;
		}
		if (packet.Guild != null)
		{
			_services.GuildWrapper = new ClientGuildWrapper(packet.Guild);
			_services.Ingest(packet.Guild.Officers);
			_services.Ingest(packet.Guild.Members);
		}
		else
		{
			_services.GuildWrapper = null;
		}
		_services.SharedSinglePlayerJoinableWorlds.Clear();
		ClientSSPJoinableWorld[] joinableSspWorlds = packet.JoinableSspWorlds;
		foreach (ClientSSPJoinableWorld world in joinableSspWorlds)
		{
			_services.SharedSinglePlayerJoinableWorlds.Add(new ClientSharedSinglePlayerJoinableWorldWrapper(world));
		}
		_services.SharedSinglePlayerInvitedWorlds.Clear();
		ClientSSPJoinableWorld[] invitedSspWorlds = packet.InvitedSspWorlds;
		foreach (ClientSSPJoinableWorld world2 in invitedSspWorlds)
		{
			_services.SharedSinglePlayerInvitedWorlds.Add(new ClientSharedSinglePlayerJoinableWorldWrapper(world2));
		}
		_services.PartyInvitations.Clear();
		ClientPartyInvitation[] partyInvitations = packet.PartyInvitations;
		foreach (ClientPartyInvitation val4 in partyInvitations)
		{
			_services.Ingest(val4.InvitedBy);
			_services.PartyInvitations.Add(new ClientPartyInvitationWrapper(val4));
		}
		ClientRejoinableServer[] rejoinableServers = packet.RejoinableServers;
		foreach (ClientRejoinableServer val5 in rejoinableServers)
		{
			if (Logger.IsInfoEnabled)
			{
				Logger.Info("Found rejoinable server {0} on {1}:{2} expiring at {3} running mode {4}", new object[5] { val5.WorldId, val5.Ip, val5.Port, val5.ExpiresAt, val5.GameInfoJsonDebug });
			}
		}
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.OnServicesInitialized();
		});
	}

	private void ProcessUserBoundChat(ClientUserBoundChat packet, ServicesClient client)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		_services.Ingest(packet.From);
		if (Logger.IsInfoEnabled)
		{
			Logger.Info("[{0}] {1} ({2}): {3}", new object[4]
			{
				packet.Channel,
				packet.From.Name,
				packet.From.Uuid_,
				packet.Message
			});
		}
		_app.Engine.RunOnMainThread(this, delegate
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Invalid comparison between Unknown and I4
			_app.Interface.TriggerEvent("services.users.updated", new ClientUserWrapper(packet.From));
			if ((int)packet.Channel == 0)
			{
				_app.Interface.TriggerEvent("services.party.messages.received", ((object)packet.From).ToString(), packet.Message, packet.Timestamp.ToString());
			}
		});
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessChannelMessage(ClientChannelMessageInbound packet, ServicesClient client)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Logger.Info<ClientChatChannel, Guid, string>("[{0}] {1}: {2}", packet.Channel, packet.From, packet.Message);
		_app.Engine.RunOnMainThread(this, delegate
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			if ((int)packet.Channel == 0)
			{
				_app.Interface.TriggerEvent("services.party.messages.received", packet.From.ToString(), packet.Message, packet.Timestamp.ToString());
			}
		});
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessLocalizedMessage(ClientLocalizedMessage packet, ServicesClient client)
	{
		Logger.Info<string, Dictionary<string, string>>("Got localized message {0} with params {1}", packet.Key, packet.Params);
	}

	private void ProcessBlockToggleNotification(ClientBlockToggleNotification packet, ServicesClient client)
	{
		if (packet.Blocked)
		{
			_services.BlockedPlayers.Add(packet.Target);
			_app.Engine.RunOnMainThread(this, delegate
			{
				_app.Interface.TriggerEvent("services.blockedUsers.added", packet.Target.ToString());
			});
			Logger.Info("You are now blocking {0}", _services.Users[packet.Target].Name);
		}
		else
		{
			_services.BlockedPlayers.Remove(packet.Target);
			_app.Engine.RunOnMainThread(this, delegate
			{
				_app.Interface.TriggerEvent("services.blockedUsers.removed", packet.Target.ToString());
			});
			Logger.Info("You have unblocked {0}", _services.Users[packet.Target].Name);
		}
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessNewTempLanguageMapping(ClientNewTempLanguageMapping packet, ServicesClient client)
	{
	}

	private void ProcessServerQueueReply(ClientServerQueueReply packet, ServicesClient client)
	{
		_services.QueueTicket.HandleResponse(packet);
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessCertificateRefresh(ClientCertificateRefresh packet, ServicesClient client)
	{
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessSuccessNotification(ClientSuccessNotification packet, ServicesClient client)
	{
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessFailureNotification(ClientFailureNotification packet, ServicesClient client)
	{
		_services.ProcessFailureCallback(packet.Token, packet);
	}

	private void ProcessGuildMemberAdd(ClientGuildMemberAdd packet, ServicesClient client)
	{
		if (_services.GuildWrapper != null && _services.GuildWrapper.GuildId.Equals(packet.GuildId))
		{
			Logger.Info<ClientGuildMemberAdd>("Got guild member add {0}", packet);
			_services.GuildWrapper.Members.Add(new ClientGuildMemberWrapper(packet.Member));
			_services.Ingest(packet.Member.User);
		}
		else
		{
			Logger.Warn<ClientGuildWrapper, string>("Got guild member add for a guild we're not in: {0} vs {1}", _services.GuildWrapper, packet.GuildId);
		}
	}

	private void ProcessGuildInviteNotification(ClientGuildInviteNotification packet, ServicesClient client)
	{
		Logger.Info<ClientGuildInvitation>("You were invited to join guild {0}", packet.Invitation);
		_services.GuildInvitations.Add(new ClientGuildInvitationWrapper(packet.Invitation));
	}

	private void ProcessGuildMemberRemove(ClientGuildMemberRemove packet, ServicesClient client)
	{
		Logger.Info<ClientUserWrapper, string>("{0} was removed from guild {1}", _services.Users[packet.Removed], packet.GuildId);
		Predicate<ClientGuildMemberWrapper> match = (ClientGuildMemberWrapper member) => member.Uuid.Equals(packet.Removed);
		_services.GuildWrapper.Officers.RemoveAll(match);
		_services.GuildWrapper.Members.RemoveAll(match);
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessGuildRankChange(ClientGuildRankChange packet, ServicesClient client)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Invalid comparison between Unknown and I4
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Invalid comparison between Unknown and I4
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Invalid comparison between Unknown and I4
		if (_services.GuildWrapper != null && _services.GuildWrapper.GuildId.Equals(packet.GuildId))
		{
			Logger.Info<ClientGuildRankChange>("Got guild rank change {0}", packet);
			Predicate<ClientGuildMemberWrapper> match = (ClientGuildMemberWrapper test) => test.Uuid.Equals(packet.UpdatedMember);
			_services.GuildWrapper.Officers.RemoveAll(match);
			_services.GuildWrapper.Members.RemoveAll(match);
			ClientUserWrapper clientUserWrapper = _services.Users[packet.By];
			ClientGuildMemberWrapper clientGuildMemberWrapper = new ClientGuildMemberWrapper(packet.UpdatedMember, packet.NewRank);
			if ((int)clientGuildMemberWrapper.Rank == 0)
			{
				ClientGuildMemberWrapper leader = _services.GuildWrapper.Leader;
				_services.GuildWrapper.Leader = clientGuildMemberWrapper;
				_services.GuildWrapper.Officers.Add(leader);
				Logger.Info<string, string, string>("{0} promoted {1} to guild {2}", clientUserWrapper.Name, _services.Users[clientGuildMemberWrapper.Uuid].Name, "Rank");
			}
			else if ((int)clientGuildMemberWrapper.Rank == 1)
			{
				_services.GuildWrapper.Officers.Add(clientGuildMemberWrapper);
				Logger.Info<string, string, string>("{0} promoted {1} to guild {2}", clientUserWrapper.Name, _services.Users[clientGuildMemberWrapper.Uuid].Name, "Rank");
			}
			else if ((int)clientGuildMemberWrapper.Rank == 2)
			{
				_services.GuildWrapper.Members.Add(clientGuildMemberWrapper);
				Logger.Info<string, string, string>("{0} demoted {1} to guild {2}", clientUserWrapper.Name, _services.Users[clientGuildMemberWrapper.Uuid].Name, "Rank");
			}
		}
		else
		{
			Logger.Warn<ClientGuildWrapper, string>("Got guild rank change for a guild we're not in: {0} vs {1}", _services.GuildWrapper, packet.GuildId);
		}
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessSetGuild(ClientSetGuild packet, ServicesClient client)
	{
		if ((_services.GuildWrapper == null && packet.OldId == null) || (_services.GuildWrapper != null && packet.OldId != null && packet.OldId.Equals(_services.GuildWrapper.GuildId)))
		{
			if (packet.Guild != null)
			{
				_services.GuildWrapper = new ClientGuildWrapper(packet.Guild);
			}
			else
			{
				_services.GuildWrapper = null;
			}
			Logger.Info<ClientGuildWrapper>("Set guild to {0}", _services.GuildWrapper);
			if (packet.Guild != null)
			{
				_services.Ingest(packet.Guild.Leader.User);
				_services.Ingest(packet.Guild.Officers);
				_services.Ingest(packet.Guild.Members);
			}
		}
		else
		{
			Logger.Warn<string, ClientGuildWrapper>("Got mismatching guild IDs for {0} vs {1}", packet.OldId, _services.GuildWrapper);
		}
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessPartyMembersChange(ClientPartyMembersChange packet, ServicesClient client)
	{
		if (_services.PartyWrapper != null)
		{
			if (packet.PartyId.Equals(_services.PartyWrapper.PartyId))
			{
				if (packet.UserAdd != null)
				{
					_services.Ingest(packet.UserAdd);
					_services.PartyWrapper.Members.Add(packet.UserAdd.Uuid_);
					Logger.Info("{0} joined the party", packet.UserAdd.Name);
					_app.Engine.RunOnMainThread(this, delegate
					{
						_app.Interface.TriggerEvent("services.users.updated", new ClientUserWrapper(packet.UserAdd));
						_app.Interface.TriggerEvent("services.party.memberAdded", packet.UserAdd.Uuid_.ToString());
					});
				}
				else
				{
					_services.PartyWrapper.Members.RemoveAll((Guid e) => e.Equals(packet.UserDel));
					ClientUserWrapper clientUserWrapper = _services.Users[packet.UserDel];
					Logger.Info<ClientUserWrapper>("{0} is no longer a member of the party", clientUserWrapper);
					_app.Engine.RunOnMainThread(this, delegate
					{
						_app.Interface.TriggerEvent("services.party.memberRemoved", packet.UserDel.ToString());
					});
				}
			}
			else
			{
				Logger.Warn<string, string>("Received mismatching party ID {0} vs {1}", packet.PartyId, _services.PartyWrapper.PartyId);
			}
		}
		else
		{
			Logger.Warn<ClientPartyMembersChange>("Received {0} with no party set!", packet);
		}
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessSetParty(ClientSetParty packet, ServicesClient client)
	{
		if ((_services.PartyWrapper == null && packet.OldId == null) || (_services.PartyWrapper != null && packet.OldId.Equals(_services.PartyWrapper.PartyId)))
		{
			if (packet.CurrentParty != null)
			{
				_services.PartyWrapper = new ClientPartyWrapper(packet.CurrentParty);
				_services.Ingest(packet.CurrentParty.Leader);
				_services.Ingest(packet.CurrentParty.Members);
				_app.Engine.RunOnMainThread(this, delegate
				{
					_app.Interface.TriggerEvent("services.users.updated", new ClientUserWrapper(packet.CurrentParty.Leader));
					_app.Interface.TriggerEvent("services.users.updatedMultiple", packet.CurrentParty.Members.Select((ClientUser m) => new ClientUserWrapper(m)).ToArray());
				});
			}
			else
			{
				_services.PartyWrapper = null;
			}
			_app.Engine.RunOnMainThread(this, delegate
			{
				_app.Interface.TriggerEvent("services.party.set", _services.PartyWrapper);
			});
			Logger.Info<ClientPartyWrapper>("Set party to {}", _services.PartyWrapper);
		}
		else
		{
			Logger.Info<string, ClientPartyWrapper>("Got mismatching party IDs for {0} vs {1}", packet.OldId, _services.PartyWrapper);
		}
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessPartyNewLeader(ClientPartyNewLeader packet, ServicesClient client)
	{
		if (_services.PartyWrapper != null && _services.PartyWrapper.PartyId.Equals(packet.PartyIdHex))
		{
			ClientUserWrapper clientUserWrapper = _services.Users[_services.PartyWrapper.Leader];
			ClientUserWrapper newLeader = _services.Users[packet.NewLeader];
			_services.PartyWrapper.Leader = newLeader.Uuid;
			Logger.Info<string, string, string>("{0} promoted {1} to the party leader of {2}", clientUserWrapper.Name, newLeader.Name, packet.PartyIdHex);
			_app.Engine.RunOnMainThread(this, delegate
			{
				_app.Interface.TriggerEvent("services.party.leaderChanged", newLeader.Uuid.ToString());
			});
		}
		else
		{
			Logger.Info<string, ClientPartyWrapper>("Got mismaching party IDs for {0} vs {1}", packet.PartyIdHex, _services.PartyWrapper);
		}
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}

	private void ProcessPartyInviteNotification(ClientPartyInviteNotification packet, ServicesClient client)
	{
		Logger.Info<string, string>("{0} invited you to party {1}", packet.PartyInvitation.InvitedBy.Name, packet.PartyInvitation.PartyIdHex);
		_services.Ingest(packet.PartyInvitation.InvitedBy);
		_services.PartyInvitations.Add(new ClientPartyInvitationWrapper(packet.PartyInvitation));
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.TriggerEvent("services.users.updated", new ClientUserWrapper(packet.PartyInvitation.InvitedBy));
			_app.Interface.TriggerEvent("services.party.invitationReceived", new ClientPartyInvitationWrapper(packet.PartyInvitation));
		});
	}

	private void ProcessSharedSinglePlayerWorldAccessRemoved(ClientSSPWorldAccessRemoved packet, ServicesClient client)
	{
		Predicate<ClientSharedSinglePlayerJoinableWorldWrapper> match = delegate(ClientSharedSinglePlayerJoinableWorldWrapper world)
		{
			Guid worldId = world.WorldId;
			return worldId.Equals(packet.WorldId);
		};
		_services.SharedSinglePlayerJoinableWorlds.RemoveAll(match);
		Logger.Info<Guid>("World access removed for {0}", packet.WorldId);
	}

	private void ProcessSharedSinglePlayerWorldInviteNotice(ClientSSPWorldInviteNotice packet, ServicesClient client)
	{
		Logger.Info<ClientSSPJoinableWorld>("Got invitation for access to SharedSinglePlayer world {0}!", packet.World);
		_services.SharedSinglePlayerInvitedWorlds.Add(new ClientSharedSinglePlayerJoinableWorldWrapper(packet.World));
	}

	private void ProcessSharedSinglePlayerWorldCreated(ClientSSPWorldCreated packet, ServicesClient client)
	{
		ClientSharedSinglePlayerJoinableWorldWrapper item = new ClientSharedSinglePlayerJoinableWorldWrapper(packet.CreatedWorld);
		_services.SharedSinglePlayerJoinableWorlds.Add(item);
		_app.Engine.RunOnMainThread(this, delegate
		{
			_app.Interface.MainMenuView.SharedSinglePlayerPage.OnWorldsUpdated();
		});
		Logger.Info<ClientSSPJoinableWorld>("World was created: {0}!", packet.CreatedWorld);
		_services.ProcessTypedCallback(packet.Token, (ProtoPacket)(object)packet);
	}
}
