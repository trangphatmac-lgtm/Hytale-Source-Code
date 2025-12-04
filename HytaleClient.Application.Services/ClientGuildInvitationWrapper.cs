using System;
using HytaleClient.Auth.Proto.Protocol;

namespace HytaleClient.Application.Services;

public class ClientGuildInvitationWrapper
{
	public string Id;

	public string Name;

	public Guid By;

	public long ExpiresAt;

	public ClientGuildInvitationWrapper(ClientGuildInvitation invitation)
	{
		Id = invitation.HexId;
		Name = invitation.Name;
		By = invitation.By.Uuid_;
		ExpiresAt = invitation.ExpiresAt;
	}
}
