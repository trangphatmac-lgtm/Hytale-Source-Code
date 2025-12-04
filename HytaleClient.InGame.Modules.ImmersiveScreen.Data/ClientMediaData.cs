using System;
using Coherent.UI.Binding;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.ImmersiveScreen.Data;

[CoherentType]
public class ClientMediaData
{
	[CoherentType]
	public class ClientThumbnails
	{
		[CoherentProperty("small")]
		public string Small;

		[CoherentProperty("normal")]
		public string Normal;

		public ClientThumbnails()
		{
		}

		public ClientThumbnails(Thumbnails packet)
		{
			Small = packet.Small;
			Normal = packet.Normal;
		}

		public Thumbnails ToPacket()
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected O, but got Unknown
			return new Thumbnails(Small, Normal);
		}
	}

	[CoherentProperty("service")]
	public MediaService Platform;

	[CoherentProperty("id")]
	public string Id;

	[CoherentProperty("title")]
	public string Title;

	[CoherentProperty("channelId")]
	public string ChannelId;

	[CoherentProperty("channelName")]
	public string ChannelName;

	[CoherentProperty("publicationDate")]
	public string PublicationDate;

	[CoherentProperty("addedByUsername")]
	public string AddedByUsername;

	[CoherentProperty("addedByUuid")]
	public Guid AddedByUuid;

	[CoherentProperty("position")]
	public int Position;

	[CoherentProperty("seekPositionCounter")]
	public int SeekPositionCounter;

	[CoherentProperty("duration")]
	public int Duration;

	[CoherentProperty("playing")]
	public bool Playing;

	[CoherentProperty("stream")]
	public bool Stream;

	[CoherentProperty("thumbnail")]
	public ClientThumbnails Thumbnail;

	[CoherentProperty("viewCount")]
	public int ViewCount;

	public string GameTitle;

	public ClientMediaData()
	{
	}

	public ClientMediaData(ImmersiveViewMediaData packet)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		Platform = packet.Service;
		Id = packet.Id;
		Title = packet.Title;
		ChannelId = packet.ChannelId;
		ChannelName = packet.ChannelName;
		PublicationDate = packet.PublicationDate;
		AddedByUsername = packet.AddedByUsername;
		AddedByUuid = packet.AddedByUuid;
		Position = (int)packet.Position;
		Duration = (int)packet.Duration;
		Playing = packet.Playing;
		Stream = packet.Stream;
		SeekPositionCounter = packet.SeekPositionCounter;
		if (packet.Thumbnail != null)
		{
			Thumbnail = new ClientThumbnails(packet.Thumbnail);
		}
	}

	public ImmersiveViewMediaData ToPacket()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		return new ImmersiveViewMediaData
		{
			Service = Platform,
			Id = Id,
			Title = Title,
			ChannelId = ChannelId,
			ChannelName = ChannelName,
			PublicationDate = PublicationDate,
			AddedByUsername = AddedByUsername,
			AddedByUuid = AddedByUuid,
			Thumbnail = Thumbnail?.ToPacket(),
			Playing = Playing,
			Position = Position,
			Duration = Duration,
			SeekPositionCounter = SeekPositionCounter,
			Stream = Stream
		};
	}
}
