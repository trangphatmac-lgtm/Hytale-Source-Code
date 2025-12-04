using System.Collections.Generic;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class KillFeedComponent : InterfaceComponent
{
	private class KillFeedEntry
	{
		public string Decedent;

		public string Killer;

		public float ExpirationTime;

		public string Icon;

		public Group Element;
	}

	public const float EntryDuration = 5f;

	private float _currentTime;

	private InGameView _inGameView;

	private Group _killFeed;

	private List<KillFeedEntry> _killFeedEntries = new List<KillFeedEntry>();

	public KillFeedComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/KillFeed.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_killFeed = uIFragment.Get<Group>("KillFeed");
		RebuildFeed();
	}

	protected override void OnMounted()
	{
		_currentTime = 0f;
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	public void ResetState()
	{
		_currentTime = 0f;
		_killFeedEntries.Clear();
		_killFeed.Clear();
	}

	public void RebuildFeed()
	{
		_killFeed.Clear();
		Interface.TryGetDocument("InGame/Hud/KillFeedEntry.ui", out var document);
		foreach (KillFeedEntry killFeedEntry in _killFeedEntries)
		{
			AddKillFeedEntry(killFeedEntry, document);
		}
		_killFeed.Layout();
	}

	private void Animate(float deltaTime)
	{
		bool flag = false;
		_currentTime += deltaTime;
		while (_killFeedEntries.Count > 0)
		{
			KillFeedEntry killFeedEntry = _killFeedEntries[0];
			if (killFeedEntry.ExpirationTime < _currentTime)
			{
				_killFeedEntries.RemoveAt(0);
				_killFeed.Remove(killFeedEntry.Element);
				killFeedEntry.Element = null;
				flag = true;
				continue;
			}
			break;
		}
		if (flag)
		{
			_killFeed.Layout();
		}
	}

	private void AddKillFeedEntry(KillFeedEntry entry, Document doc)
	{
		UIFragment uIFragment = doc.Instantiate(Desktop, _killFeed);
		if (entry.Killer == null)
		{
			uIFragment.Get<Label>("Killer").Visible = false;
		}
		else
		{
			uIFragment.Get<Label>("Killer").Text = entry.Killer;
		}
		uIFragment.Get<Label>("Decedent").Text = entry.Decedent;
		if (entry.Icon != null && _inGameView.TryMountAssetTexture(entry.Icon, out var textureArea))
		{
			uIFragment.Get<Element>("Icon").Background = new PatchStyle(textureArea);
		}
		entry.Element = uIFragment.Get<Group>("KillFeedEntry");
	}

	public void OnReceiveNewEntry(string decedent, string killer, string icon)
	{
		Interface.TryGetDocument("InGame/Hud/KillFeedEntry.ui", out var document);
		KillFeedEntry killFeedEntry = new KillFeedEntry
		{
			Decedent = decedent,
			Killer = killer,
			ExpirationTime = _currentTime + 5f,
			Icon = icon
		};
		AddKillFeedEntry(killFeedEntry, document);
		_killFeedEntries.Add(killFeedEntry);
		_killFeed.Layout();
	}
}
