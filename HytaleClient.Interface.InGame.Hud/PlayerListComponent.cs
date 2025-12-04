using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Networking;

namespace HytaleClient.Interface.InGame.Hud;

internal class PlayerListComponent : InterfaceComponent
{
	private readonly InGameView _inGameView;

	private Group _serverDetails;

	private Label _serverName;

	private Label _motd;

	private Label _playerCount;

	private Group _listContainer;

	public PlayerListComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void ResetState()
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/PlayerList.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_serverDetails = uIFragment.Get<Group>("ServerDetails");
		_serverName = uIFragment.Get<Label>("ServerName");
		_motd = uIFragment.Get<Label>("Motd");
		_playerCount = uIFragment.Get<Label>("PlayerCount");
		_listContainer = uIFragment.Get<Group>("ListContainer");
		if (base.IsMounted)
		{
			UpdateServerDetails();
			UpdateList();
		}
	}

	protected override void OnMounted()
	{
		UpdateServerDetails();
		UpdateList();
	}

	public void UpdateServerDetails()
	{
		_serverDetails.Visible = _inGameView.ServerName != null;
		_serverName.Text = _inGameView.ServerName;
		_motd.Text = _inGameView.Motd;
		if (_serverDetails.IsMounted)
		{
			_serverDetails.Layout();
		}
	}

	public void UpdateList()
	{
		_listContainer.Clear();
		_playerCount.Text = ((_inGameView.MaxPlayers > 0) ? $"({_inGameView.Players.Count}/{_inGameView.MaxPlayers})" : $"({_inGameView.Players.Count})");
		_playerCount.Layout();
		if (_inGameView.Players.Count <= 0)
		{
			return;
		}
		foreach (PacketHandler.PlayerListPlayer value in _inGameView.Players.Values)
		{
			Group parent = new Group(Desktop, _listContainer)
			{
				LayoutMode = LayoutMode.Left
			};
			Label label = new Label(Desktop, parent)
			{
				Anchor = 
				{
					Width = 250
				}
			};
			Label label2 = new Label(Desktop, parent)
			{
				Anchor = 
				{
					Width = 250
				}
			};
			label.Text = value.DisplayName;
			label2.Text = value.Ping + "ms";
		}
		_listContainer.Layout();
	}
}
