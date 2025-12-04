using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Core;
using HytaleClient.Data.EntityStats;
using HytaleClient.Data.UserSettings;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud.Abilities;

internal class SignatureAbility : Element
{
	private const float _signatureAnimationVelocity = 10f;

	private const float _backgroundAnimationVelocity = 20f;

	private InGameView InGameView;

	private Dictionary<string, Group> _inputBidingsMouse = new Dictionary<string, Group>();

	private Label _inputBidingLabel;

	private Group _inputBidingContainer;

	private float _targetSignaturePercentage;

	private float _renderSignaturePercentage;

	private CircularProgressBar _signatureProgressBar;

	private float _targetShadowSignaturePercentage;

	private float _renderShadowSignaturePercentage;

	private CircularProgressBar _signatureShadowProgressBar;

	private Group _signatureIcon;

	private Group _signatureReadyProgressBar;

	private Group _backgroundSignatureReady;

	private Group _backgroundSignatureNotReady;

	private Group _overlaySignatureError;

	private int _errorAnimationCount;

	private const int _errorAnimationTotal = 4;

	private const float _maxErrorAnimationPercentage = 1f;

	private const float _minErrorAnimationPercentage = 0.4f;

	private const float _errorAnimationSpeed = 10f;

	protected float _errorTargetPercentage;

	protected float _errorRenderPercentage;

	private bool _isSignatureReady;

	private float _targetBackgroundSignatureReadyOpacity;

	private float _renderBackgroundSignatureReadyOpacity;

	private float _backgroundSignatureSize;

	private float _backgroundSignatureScaleSize;

	private bool _animateSignatureBackgroundScaleUp;

	private bool _animateSignatureBackgroundScaleDown;

	public SignatureAbility(InGameView inGameView, Desktop desktop, Element parent)
		: base(desktop, parent)
	{
		InGameView = inGameView;
	}

	public void Build()
	{
		Desktop.Provider.TryGetDocument("InGame/Hud/Abilities/SignatureAbility.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		document.TryResolveNamedValue<float>(Desktop.Provider, "BackgroundSignatureSize", out _backgroundSignatureSize);
		document.TryResolveNamedValue<float>(Desktop.Provider, "BackgroundSignatureScaleSize", out _backgroundSignatureScaleSize);
		_signatureProgressBar = uIFragment.Get<CircularProgressBar>("SignatureProgressBar");
		_signatureShadowProgressBar = uIFragment.Get<CircularProgressBar>("ShadowSignatureProgressBar");
		_signatureIcon = uIFragment.Get<Group>("SignatureIcon");
		_signatureReadyProgressBar = uIFragment.Get<Group>("SignatureReadyProgressBar");
		_backgroundSignatureReady = uIFragment.Get<Group>("BackgroundSignatureReady");
		_backgroundSignatureNotReady = uIFragment.Get<Group>("BackgroundSignatureNotReady");
		_overlaySignatureError = uIFragment.Get<Group>("OverlaySignatureError");
		_overlaySignatureError.Visible = false;
		_overlaySignatureError.Parent.Layout();
		_inputBidingLabel = uIFragment.Get<Label>("InputBinding");
		_inputBidingsMouse.Add(InputBinding.GetMouseBoundInputLabel(Input.MouseButton.SDL_BUTTON_LEFT), uIFragment.Get<Group>("MouseLeft"));
		_inputBidingsMouse.Add(InputBinding.GetMouseBoundInputLabel(Input.MouseButton.SDL_BUTTON_MIDDLE), uIFragment.Get<Group>("MouseMiddle"));
		_inputBidingsMouse.Add(InputBinding.GetMouseBoundInputLabel(Input.MouseButton.SDL_BUTTON_RIGHT), uIFragment.Get<Group>("MouseRight"));
		_inputBidingContainer = uIFragment.Get<Group>("InputBindingContainer");
		AnimateSignatureReady();
	}

	private void SetIconOpacity(float opacity)
	{
		_signatureIcon.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(opacity * 255f));
		_signatureIcon.Layout();
	}

	protected override void OnMounted()
	{
		UpdateInputBiding();
		Desktop.RegisterAnimationCallback(Animate);
		ClientEntityStatValue clientEntityStatValue = InGameView?.InGame.Instance?.LocalPlayer?.GetEntityStat(DefaultEntityStats.SignatureEnergy);
		if (clientEntityStatValue != null)
		{
			if (clientEntityStatValue.AsPercentage() < 1f)
			{
				SignatureNotReady();
			}
			else
			{
				SignatureReady();
			}
		}
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	protected virtual void Animate(float deltaTime)
	{
		if (_targetSignaturePercentage != _renderSignaturePercentage)
		{
			_renderSignaturePercentage = MathHelper.Lerp(_renderSignaturePercentage, _targetSignaturePercentage, deltaTime * 10f);
			AnimateSignature();
		}
		if ((double)_renderSignaturePercentage > (double)_targetSignaturePercentage * 0.98)
		{
			_targetShadowSignaturePercentage = _renderSignaturePercentage;
		}
		if (_targetShadowSignaturePercentage != _renderShadowSignaturePercentage)
		{
			_renderShadowSignaturePercentage = MathHelper.Lerp(_renderShadowSignaturePercentage, _targetShadowSignaturePercentage, deltaTime * 10f);
			AnimateShadowSignature();
		}
		if (_renderShadowSignaturePercentage > 0.99f && !_isSignatureReady)
		{
			SignatureReady();
		}
		else if (_renderShadowSignaturePercentage < 0.99f && _isSignatureReady)
		{
			SignatureNotReady();
		}
		if (_targetBackgroundSignatureReadyOpacity != _renderBackgroundSignatureReadyOpacity)
		{
			_renderBackgroundSignatureReadyOpacity = MathHelper.Lerp(_renderBackgroundSignatureReadyOpacity, _targetBackgroundSignatureReadyOpacity, deltaTime * 10f);
			AnimateSignatureReady();
		}
		if (_errorTargetPercentage != _errorRenderPercentage)
		{
			_errorRenderPercentage = MathHelper.Lerp(_errorRenderPercentage, _errorTargetPercentage, deltaTime * 10f);
			AnimateError();
		}
		AnimateSignatureReadyScale(deltaTime);
	}

	protected void AnimateError()
	{
		_overlaySignatureError.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(_errorRenderPercentage * 255f));
		_overlaySignatureError.Parent.Layout();
		float num = 0.1f;
		if (_errorAnimationCount > 4)
		{
			HideErrorOverlay();
			return;
		}
		if (_errorRenderPercentage > 1f - num && _errorTargetPercentage == 1f)
		{
			_errorTargetPercentage = 0.4f;
			_errorAnimationCount++;
		}
		if (_errorRenderPercentage < 0.4f + num && _errorTargetPercentage == 0.4f)
		{
			_errorTargetPercentage = 1f;
			_errorAnimationCount++;
		}
	}

	private void AnimateSignatureReadyScale(float deltaTime)
	{
		if (_animateSignatureBackgroundScaleUp)
		{
			_animateSignatureBackgroundScaleDown = false;
			float value = _backgroundSignatureReady.Anchor.Height.GetValueOrDefault();
			_backgroundSignatureReady.Anchor.Width = (_backgroundSignatureReady.Anchor.Height = (int)MathHelper.Lerp(value, _backgroundSignatureScaleSize, deltaTime * 20f));
			if ((double?)_backgroundSignatureReady.Anchor.Width > 0.95 * (double)_backgroundSignatureScaleSize)
			{
				_animateSignatureBackgroundScaleUp = false;
				_animateSignatureBackgroundScaleDown = true;
			}
			_backgroundSignatureReady.Layout();
		}
		if (_animateSignatureBackgroundScaleDown)
		{
			_animateSignatureBackgroundScaleUp = false;
			float value2 = _backgroundSignatureReady.Anchor.Height.GetValueOrDefault();
			_backgroundSignatureReady.Anchor.Width = (_backgroundSignatureReady.Anchor.Height = (int)MathHelper.Lerp(value2, _backgroundSignatureSize, deltaTime * 20f));
			if ((double?)_backgroundSignatureReady.Anchor.Width < 1.01 * (double)_backgroundSignatureSize)
			{
				_backgroundSignatureReady.Anchor.Width = (_backgroundSignatureReady.Anchor.Height = (int)_backgroundSignatureSize);
				_animateSignatureBackgroundScaleDown = false;
			}
			_backgroundSignatureReady.Layout();
		}
	}

	private void AnimateSignatureReady()
	{
		_backgroundSignatureReady.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(_renderBackgroundSignatureReadyOpacity * 255f));
		_backgroundSignatureNotReady.Background.Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)((1f - _renderBackgroundSignatureReadyOpacity) * 255f));
		_backgroundSignatureNotReady.Layout();
		_backgroundSignatureReady.Layout();
	}

	private void SignatureReady()
	{
		_isSignatureReady = true;
		SetIconOpacity(1f);
		_signatureReadyProgressBar.Visible = true;
		_signatureReadyProgressBar.Parent.Layout();
		_targetBackgroundSignatureReadyOpacity = 1f;
		_animateSignatureBackgroundScaleUp = true;
		SetInputBidingsOpacity(1f);
	}

	private void SignatureNotReady()
	{
		_isSignatureReady = false;
		SetIconOpacity(0.4f);
		_signatureReadyProgressBar.Visible = false;
		_signatureReadyProgressBar.Parent.Layout();
		_targetBackgroundSignatureReadyOpacity = 0f;
		SetInputBidingsOpacity(0.4f);
	}

	private void SetInputBidingsOpacity(float opacity)
	{
		_inputBidingLabel.Style.TextColor.SetA((byte)(opacity * 255f));
		foreach (string key in _inputBidingsMouse.Keys)
		{
			_inputBidingsMouse[key].Background.Color.SetA((byte)(opacity * 255f));
		}
		_inputBidingContainer.Layout();
	}

	protected void AnimateSignature()
	{
		_signatureProgressBar.Value = _renderSignaturePercentage;
		_signatureProgressBar.Layout();
	}

	protected void AnimateShadowSignature()
	{
		_signatureShadowProgressBar.Value = _renderShadowSignaturePercentage;
		_signatureShadowProgressBar.Layout();
	}

	public void UpdateInputBiding()
	{
		if (_inputBidingLabel == null)
		{
			return;
		}
		foreach (string key in _inputBidingsMouse.Keys)
		{
			_inputBidingsMouse[key].Visible = false;
		}
		_inputBidingLabel.Visible = false;
		if (InGameView == null)
		{
			return;
		}
		InputBinding tertiaryItemAction = InGameView.Interface.App.Settings.InputBindings.TertiaryItemAction;
		if (tertiaryItemAction == null)
		{
			return;
		}
		if (_inputBidingsMouse.Keys.ToArray().Contains(tertiaryItemAction.BoundInputLabel))
		{
			Group group = _inputBidingsMouse[tertiaryItemAction.BoundInputLabel];
			if (group != null)
			{
				group.Visible = true;
				Layout();
			}
		}
		else
		{
			_inputBidingLabel.Text = tertiaryItemAction.BoundInputLabel;
			_inputBidingLabel.Style.FontSize = System.Math.Max(13 - tertiaryItemAction.BoundInputLabel.ToArray().Length, 8);
			_inputBidingLabel.Visible = true;
			Layout();
		}
	}

	public void OnSignatureEnergyStatChanged(ClientEntityStatValue entityStatValue)
	{
		if (entityStatValue == null)
		{
			_targetSignaturePercentage = 0f;
			_renderSignaturePercentage = 0f;
			AnimateSignature();
			_targetShadowSignaturePercentage = 0f;
			_renderShadowSignaturePercentage = 0f;
			AnimateShadowSignature();
		}
		else
		{
			_targetSignaturePercentage = ((entityStatValue.Max > 0f) ? entityStatValue.AsPercentage() : 0f);
		}
	}

	public void ShowErrorOverlay()
	{
		_overlaySignatureError.Visible = true;
		_overlaySignatureError.Parent.Layout();
		_errorTargetPercentage = 0.4f;
		_errorRenderPercentage = 1f;
		_errorAnimationCount = 0;
	}

	public void HideErrorOverlay()
	{
		_overlaySignatureError.Visible = false;
		_overlaySignatureError.Parent.Layout();
		_errorTargetPercentage = 1f;
		_errorRenderPercentage = 1f;
	}

	public void OnSignatureAction()
	{
		ClientEntityStatValue entityStat = InGameView.InGame.Instance.LocalPlayer.GetEntityStat(DefaultEntityStats.SignatureEnergy);
		if (entityStat.AsPercentage() < 1f)
		{
			ShowErrorOverlay();
		}
	}
}
