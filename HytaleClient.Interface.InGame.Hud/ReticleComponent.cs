using System;
using System.Collections.Generic;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame.Hud;

internal class ReticleComponent : InterfaceComponent
{
	private const string DefaultReticleAssetPath = "UI/Reticles/Default.png";

	private const float ClientEventTimeout = 0.3f;

	private readonly InGameView _inGameView;

	private Element _reticleContainer;

	private Element _serverEventContainer;

	private Element _clientEventContainer;

	private Group _interactionHintContainer;

	private float _serverEventDuration;

	private float _serverEventTimer;

	private float _clientEventTimer;

	private bool _animateClientEvent;

	private float _localHitOpacity;

	private Group _chargeProgressContainer;

	private Group _chargeProgressBar;

	private List<Group> _chargeProgressNotches = new List<Group>();

	private int _activeHotbarSlot;

	public bool IsReticleVisible { get; private set; }

	public ReticleComponent(InGameView view)
		: base(view.Interface, view.HudContainer)
	{
		_inGameView = view;
		Interface.RegisterForEventFromEngine<bool>("crosshair.setVisible", OnSetVisible);
		Interface.RegisterForEventFromEngine<InteractionModule.InteractionHintData>("crosshair.setInteractionHint", OnSetInteractionHint);
		Interface.RegisterForEventFromEngine<float>("combat.setChargeProgress", OnSetChargeProgress);
		Interface.RegisterForEventFromEngine<bool, float[]>("combat.setShowChargeProgress", OnSetShowChargeProgress);
	}

	public void Build()
	{
		Clear();
		_chargeProgressNotches.Clear();
		Interface.TryGetDocument("InGame/Hud/Reticle.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_reticleContainer = uIFragment.Get<Element>("Reticle");
		_serverEventContainer = uIFragment.Get<Element>("ServerEvent");
		_clientEventContainer = uIFragment.Get<Element>("ClientEvent");
		_interactionHintContainer = uIFragment.Get<Group>("InteractionHint");
		_chargeProgressContainer = uIFragment.Get<Group>("ChargeProgressContainer");
		_chargeProgressBar = uIFragment.Get<Group>("ChargeProgressBar");
		_localHitOpacity = document.ResolveNamedValue<float>(Desktop.Provider, "LocalHitOpacity");
		UpdateReticleImage();
	}

	public void ResetState(bool updateReticleImage)
	{
		IsReticleVisible = false;
		_chargeProgressContainer.Visible = false;
		_interactionHintContainer.Visible = false;
		base.Visible = false;
		ResetClientEvent();
		ResetServerEvent();
		_reticleContainer.Visible = true;
		_reticleContainer.Parent.Layout();
		if (updateReticleImage)
		{
			UpdateReticleImage();
		}
	}

	public bool RemoveClientReticle(ItemReticleClientEvent eventKey)
	{
		Group group = _clientEventContainer.Find<Group>(((object)(ItemReticleClientEvent)(ref eventKey)).ToString());
		if (group == null)
		{
			return false;
		}
		_clientEventContainer.Remove(group);
		group.Clear();
		return true;
	}

	public void ResetClientEvent()
	{
		_clientEventContainer.Clear();
		_animateClientEvent = false;
		_clientEventTimer = 0f;
	}

	private void ResetServerEvent()
	{
		_serverEventContainer.Clear();
		_serverEventTimer = 0f;
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
		_reticleContainer.Visible = true;
		_reticleContainer.Parent.Layout();
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		ResetClientEvent();
		ResetServerEvent();
	}

	private void Animate(float deltaTime)
	{
		if (_serverEventContainer.Children.Count > 0)
		{
			_serverEventTimer += deltaTime;
			if (_serverEventTimer >= _serverEventDuration)
			{
				_serverEventContainer.Clear();
			}
		}
		if (_animateClientEvent)
		{
			_clientEventTimer += deltaTime;
			if (_clientEventTimer >= 0.3f)
			{
				_clientEventContainer.Clear();
			}
		}
		if (_serverEventContainer.Children.Count == 0 && _clientEventContainer.Children.Count == 0 && !_reticleContainer.IsMounted)
		{
			_reticleContainer.Visible = true;
			_reticleContainer.Parent.Layout();
		}
	}

	private void OnSetVisible(bool visible)
	{
		IsReticleVisible = visible;
		_inGameView.UpdateReticleVisibility(doLayout: true);
	}

	private void OnSetChargeProgress(float progress)
	{
		_chargeProgressBar.Anchor.Width = (int)(progress / 100f * (float?)_chargeProgressContainer.Anchor.Width).Value;
		_chargeProgressBar.Layout();
	}

	private void OnSetShowChargeProgress(bool show, float[] chargeSteps)
	{
		if (!show)
		{
			_chargeProgressContainer.Visible = false;
			return;
		}
		foreach (Group chargeProgressNotch in _chargeProgressNotches)
		{
			_chargeProgressContainer.Remove(chargeProgressNotch);
		}
		_chargeProgressNotches.Clear();
		foreach (float num in chargeSteps)
		{
			if (num != 1f && num != 0f)
			{
				_chargeProgressNotches.Add(new Group(Desktop, _chargeProgressContainer)
				{
					Anchor = new Anchor
					{
						Width = 1,
						Left = (int)(num * (float?)_chargeProgressContainer.Anchor.Width).Value
					},
					Background = new PatchStyle(UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 130))
				});
			}
		}
		_chargeProgressBar.Anchor.Width = 0;
		_chargeProgressContainer.Visible = true;
		_chargeProgressContainer.Layout(_chargeProgressContainer.Parent.RectangleAfterPadding);
	}

	private void OnSetInteractionHint(InteractionModule.InteractionHintData interactionHint)
	{
		if (interactionHint.Target == InteractionModule.InteractionTargetType.None)
		{
			_interactionHintContainer.Visible = false;
			return;
		}
		string text = interactionHint.Name;
		if (interactionHint.Target == InteractionModule.InteractionTargetType.Block)
		{
			text = Desktop.Provider.GetText("items." + text + ".name");
		}
		string[] array = Desktop.Provider.GetText(interactionHint.Hint, new Dictionary<string, string>
		{
			{ "key", "KEY" },
			{ "name", text }
		}).Split(new string[1] { "[KEY]" }, StringSplitOptions.None);
		if (array.Length == 2)
		{
			((Label)_interactionHintContainer.Children[0]).Text = array[0].Trim();
			((Label)_interactionHintContainer.Children[1]).Text = Interface.App.Settings.InputBindings.BlockInteractAction.BoundInputLabel;
			((Label)_interactionHintContainer.Children[2]).Text = array[1].Trim();
			_interactionHintContainer.Visible = true;
			_interactionHintContainer.Layout(_interactionHintContainer.Parent.RectangleAfterPadding);
		}
	}

	private ClientItemReticleConfig GetItemReticleConfig(string itemId = null)
	{
		if (itemId == null)
		{
			if (_inGameView == null)
			{
				return null;
			}
			ClientItemStack hotbarItem = _inGameView.GetHotbarItem(_activeHotbarSlot);
			if (hotbarItem == null)
			{
				return null;
			}
			itemId = hotbarItem.Id;
		}
		ClientItemBase value;
		return _inGameView.Items.TryGetValue(itemId, out value) ? _inGameView.InGame.Instance.ServerSettings.ItemReticleConfigs[value.ReticleIndex] : null;
	}

	public void UpdateReticleImage()
	{
		ClientItemReticleConfig itemReticleConfig = GetItemReticleConfig();
		_reticleContainer.Clear();
		List<string> list = new List<string>();
		if (itemReticleConfig == null || itemReticleConfig.Base == null)
		{
			list.Add("UI/Reticles/Default.png");
		}
		else
		{
			string[] @base = itemReticleConfig.Base;
			foreach (string item in @base)
			{
				list.Add(item);
			}
		}
		foreach (string item2 in list)
		{
			if (!_inGameView.TryMountAssetTexture(item2, out var textureArea))
			{
				textureArea = Desktop.Provider.MissingTexture;
			}
			new Group(Desktop, _reticleContainer)
			{
				Anchor = new Anchor
				{
					Width = (int)((float)textureArea.Texture.Width / (float)textureArea.Scale),
					Height = (int)((float)textureArea.Texture.Height / (float)textureArea.Scale)
				},
				Background = new PatchStyle(textureArea)
			};
		}
		if (_reticleContainer.IsMounted)
		{
			_reticleContainer.Layout();
		}
	}

	public void OnClientEvent(ItemReticleClientEvent eventKey, string itemId = null)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Invalid comparison between Unknown and I4
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Invalid comparison between Unknown and I4
		ClientItemReticleConfig itemReticleConfig = GetItemReticleConfig(itemId);
		if (itemReticleConfig == null || itemReticleConfig.ClientEvents.Count == 0 || !itemReticleConfig.ClientEvents.TryGetValue(eventKey, out var value))
		{
			return;
		}
		ResetClientEvent();
		_reticleContainer.Visible = !value.HideBase;
		_reticleContainer.Parent.Layout();
		string[] parts = value.Parts;
		foreach (string assetPath in parts)
		{
			if (!_inGameView.TryMountAssetTexture(assetPath, out var textureArea))
			{
				textureArea = Desktop.Provider.MissingTexture;
			}
			PatchStyle patchStyle = new PatchStyle(textureArea);
			if ((int)eventKey == 0)
			{
				patchStyle.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(_localHitOpacity * 255f));
			}
			new Group(Desktop, _clientEventContainer)
			{
				Anchor = GetClientReticleAnchor(eventKey, textureArea),
				Background = patchStyle,
				Name = ((object)(ItemReticleClientEvent)(ref eventKey)).ToString()
			};
		}
		ItemReticleClientEvent val = eventKey;
		ItemReticleClientEvent val2 = val;
		if ((int)val2 != 0)
		{
			if ((int)val2 == 1)
			{
				_animateClientEvent = false;
			}
		}
		else
		{
			_clientEventTimer = 0f;
			_animateClientEvent = true;
		}
		if (_clientEventContainer.IsMounted)
		{
			_clientEventContainer.Layout();
		}
	}

	private Anchor GetClientReticleAnchor(ItemReticleClientEvent eventKey, TextureArea textureArea)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Invalid comparison between Unknown and I4
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Invalid comparison between Unknown and I4
		Anchor anchor = default(Anchor);
		anchor.Width = (int)((float)textureArea.Texture.Width / (float)textureArea.Scale);
		anchor.Height = (int)((float)textureArea.Texture.Height / (float)textureArea.Scale);
		Anchor result = anchor;
		float num = 0f;
		if (_reticleContainer.Children.Count > 0)
		{
			num = (float)_reticleContainer.Children[0].AnchoredRectangle.Width / Desktop.Scale;
		}
		float num2 = (float)_clientEventContainer.AnchoredRectangle.Width / Desktop.Scale / 2f;
		if ((int)eventKey == 2)
		{
			result.Left = (int)(num2 - num);
		}
		if ((int)eventKey == 3)
		{
			result.Right = (int)(num2 - num);
		}
		return result;
	}

	public void OnServerEvent(int eventIndex)
	{
		ResetServerEvent();
		ClientItemReticleConfig itemReticleConfig = GetItemReticleConfig();
		if (itemReticleConfig == null || itemReticleConfig.ServerEvents.Count == 0 || !itemReticleConfig.ServerEvents.TryGetValue(eventIndex, out var value))
		{
			return;
		}
		_reticleContainer.Visible = !value.HideBase;
		_reticleContainer.Parent.Layout();
		if (_animateClientEvent)
		{
			ResetClientEvent();
		}
		string[] parts = value.Parts;
		foreach (string assetPath in parts)
		{
			if (!_inGameView.TryMountAssetTexture(assetPath, out var textureArea))
			{
				textureArea = Desktop.Provider.MissingTexture;
			}
			new Group(Desktop, _serverEventContainer)
			{
				Anchor = new Anchor
				{
					Width = (int)((float)textureArea.Texture.Width / (float)textureArea.Scale),
					Height = (int)((float)textureArea.Texture.Height / (float)textureArea.Scale)
				},
				Background = new PatchStyle(textureArea)
			};
		}
		_serverEventDuration = value.Duration;
		if (_serverEventContainer.IsMounted)
		{
			_serverEventContainer.Layout();
		}
	}

	public void OnSetStacks()
	{
		UpdateReticleImage();
	}

	public void OnSetActiveSlot(int slot)
	{
		_activeHotbarSlot = slot;
		ResetClientEvent();
		ResetServerEvent();
		_reticleContainer.Visible = true;
		_reticleContainer.Parent.Layout();
		UpdateReticleImage();
	}
}
