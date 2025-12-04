#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Interface.InGame.Pages;

internal class CustomPage : InterfaceComponent
{
	private readonly InGameView _inGameView;

	private readonly Desktop _pageDesktop;

	private readonly Element _pageLayer;

	private readonly Group _loadingOverlay;

	private readonly Label _loadingLabel;

	private bool _isLoading;

	private float _loadingTimer;

	public bool HasPageDesktopFocusedElement => _pageDesktop.FocusedElement != null;

	public CustomPage(InGameView inGameView)
		: base(inGameView.Interface, null)
	{
		_inGameView = inGameView;
		_pageDesktop = new Desktop(Interface.InGameCustomUIProvider, Desktop.Graphics, Interface.Engine.Graphics.Batcher2D);
		_pageLayer = new Element(_pageDesktop, null);
		_loadingOverlay = new Group(Desktop, null)
		{
			Background = new PatchStyle(0u),
			LayoutMode = LayoutMode.Center
		};
		_loadingLabel = new Label(Desktop, _loadingOverlay)
		{
			Anchor = new Anchor
			{
				Bottom = 140,
				Height = 70
			},
			Background = new PatchStyle(0u),
			Padding = new Padding
			{
				Full = 10
			},
			Style = new LabelStyle
			{
				FontSize = 36f
			}
		};
	}

	public void Build()
	{
		Clear();
		_loadingLabel.Text = Interface.GetText("ui.general.loading");
		if (_isLoading)
		{
			Add(_loadingOverlay);
		}
	}

	public void ResetState()
	{
		_pageLayer.Clear();
		Clear();
		_isLoading = false;
		_loadingTimer = 0f;
	}

	protected override void OnMounted()
	{
		_pageDesktop.SetLayer(0, _pageLayer);
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		_pageDesktop.ClearAllLayers();
	}

	private void Animate(float deltaTime)
	{
		_pageDesktop.Update(deltaTime);
		if (_isLoading)
		{
			_loadingTimer += deltaTime;
		}
	}

	public void Apply(CustomPage packet)
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected O, but got Unknown
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Invalid comparison between Unknown and I4
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Invalid comparison between Unknown and I4
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		if (_isLoading)
		{
			_isLoading = false;
			Remove(_loadingOverlay);
		}
		if (packet.Clear)
		{
			_pageLayer.Clear();
		}
		try
		{
			Interface.InGameCustomUIProvider.ApplyCommands(packet.Commands, _pageLayer);
		}
		catch (Exception ex)
		{
			_pageLayer.Clear();
			_inGameView.DisconnectWithError(ex.Message, ex);
			return;
		}
		try
		{
			CustomUIEventBinding[] eventBindings = packet.EventBindings;
			foreach (CustomUIEventBinding binding in eventBindings)
			{
				CustomUIProvider.ResolveSelector(binding.Selector, _pageLayer, out var selectedElement, out var selectedPropertyPath);
				if (selectedElement == null)
				{
					throw new Exception("Target element in CustomUI event binding was not found. Selector: " + binding.Selector);
				}
				if (selectedPropertyPath != null)
				{
					throw new Exception("CustomUI event cannot be bound on a property. Selector: " + binding.Selector);
				}
				JObject template = ((binding.Data != null) ? ((JObject)BsonHelper.FromBson(binding.Data)) : new JObject());
				FieldInfo field = selectedElement.GetType().GetField(((object)(CustomUIEventBindingType)(ref binding.Type)).ToString(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty);
				if ((int)binding.Type == 6)
				{
					if (selectedElement.GetType() != typeof(ItemGrid) || field == null || field.FieldType != typeof(Action<int, int>))
					{
						throw new Exception($"Target element in CustomUI event binding has no compatible {binding.Type} event. Selector: {binding.Selector}");
					}
					field.SetValue(selectedElement, (Action<int, int>)delegate(int index, int button)
					{
						//IL_0002: Unknown result type (might be due to invalid IL or missing references)
						//IL_0007: Unknown result type (might be due to invalid IL or missing references)
						//IL_0019: Unknown result type (might be due to invalid IL or missing references)
						//IL_0030: Expected O, but got Unknown
						JObject val3 = new JObject();
						val3.Add("SlotIndex", JToken.op_Implicit(index));
						val3.Add("PressedMouseButton", JToken.op_Implicit(button));
						SendData(val3);
					});
				}
				else if ((int)binding.Type == 3)
				{
					if (selectedElement.GetType() != typeof(ReorderableList) || field == null || field.FieldType != typeof(Action<int, int>))
					{
						throw new Exception($"Target element in CustomUI event binding has no compatible {binding.Type} event. Selector: {binding.Selector}");
					}
					field.SetValue(selectedElement, (Action<int, int>)delegate(int sourceIndex, int targetIndex)
					{
						//IL_0002: Unknown result type (might be due to invalid IL or missing references)
						//IL_0007: Unknown result type (might be due to invalid IL or missing references)
						//IL_0019: Unknown result type (might be due to invalid IL or missing references)
						//IL_0030: Expected O, but got Unknown
						JObject val2 = new JObject();
						val2.Add("SourceIndex", JToken.op_Implicit(sourceIndex));
						val2.Add("TargetIndex", JToken.op_Implicit(targetIndex));
						SendData(val2);
					});
				}
				else
				{
					if (field == null || field.FieldType != typeof(Action))
					{
						throw new Exception($"Target element in CustomUI event binding has no compatible {binding.Type} event. Selector: {binding.Selector}");
					}
					field.SetValue(selectedElement, (Action)delegate
					{
						SendData(null);
					});
				}
				void SendData(JObject extraData)
				{
					Debug.Assert(!_isLoading);
					JObject val;
					try
					{
						val = GatherDataFromTemplate(template);
					}
					catch (Exception exception2)
					{
						_inGameView.DisconnectWithError("Failed to gather CustomUI event binding data", exception2);
						return;
					}
					if (extraData != null)
					{
						((JContainer)val).Merge((object)extraData);
					}
					_inGameView.InGame.SendCustomPageData(val);
					if (binding.LocksInterface)
					{
						StartLoading();
					}
				}
			}
		}
		catch (Exception exception)
		{
			_inGameView.DisconnectWithError("Failed to apply CustomUI event bindings", exception);
			return;
		}
		Desktop.RefreshHover();
	}

	private JObject GatherDataFromTemplate(JObject template)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		JObject val = new JObject();
		Recurse(template, val);
		return val;
		void Recurse(JObject templacePiece, JObject resultPiece)
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Invalid comparison between Unknown and I4
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Expected O, but got Unknown
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Expected O, but got Unknown
			foreach (KeyValuePair<string, JToken> item in templacePiece)
			{
				if (item.Key.StartsWith("@"))
				{
					if (!CustomUIProvider.TryGetPropertyValueAsJsonFromSelector((string)item.Value, _pageLayer, out var value))
					{
						throw new Exception("Could not gather property value for CustomUI event binding. Key: " + item.Key);
					}
					resultPiece[item.Key] = value;
				}
				else if ((int)item.Value.Type == 1)
				{
					JObject val2 = new JObject();
					resultPiece[item.Key] = (JToken)(object)val2;
					Recurse((JObject)item.Value, val2);
				}
				else
				{
					resultPiece[item.Key] = item.Value;
				}
			}
		}
	}

	public void StartLoading()
	{
		if (!_isLoading)
		{
			_isLoading = true;
			_loadingTimer = 0f;
			Add(_loadingOverlay);
			_loadingOverlay.Layout(_pageDesktop.RootLayoutRectangle);
			_pageDesktop.ClearInput(clearFocus: false);
			_pageDesktop.RefreshHover();
		}
	}

	public override Element HitTest(Point position)
	{
		return _anchoredRectangle.Contains(position) ? this : null;
	}

	protected override void OnMouseMove()
	{
		_pageDesktop.OnMouseMove(Desktop.MousePosition);
	}

	protected override void OnMouseButtonDown(MouseButtonEvent evt)
	{
		if (!_isLoading)
		{
			_pageDesktop.OnMouseDown(evt.Button, evt.Clicks);
		}
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		if (!_isLoading)
		{
			_pageDesktop.OnMouseUp(evt.Button, evt.Clicks);
		}
	}

	protected internal override void OnKeyDown(SDL_Keycode keycode, int repeat)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if ((int)keycode == 27)
		{
			Dismiss();
		}
		else if (!_isLoading)
		{
			_pageDesktop.OnKeyDown(keycode, repeat);
		}
	}

	protected internal override void OnKeyUp(SDL_Keycode keycode)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!_isLoading)
		{
			_pageDesktop.OnKeyUp(keycode);
		}
	}

	protected internal override void OnTextInput(string text)
	{
		if (!_isLoading)
		{
			_pageDesktop.OnTextInput(text);
		}
	}

	protected internal override bool OnMouseWheel(Point offset)
	{
		if (!_isLoading)
		{
			_pageDesktop.OnMouseWheel(offset);
		}
		return true;
	}

	protected override void LayoutSelf()
	{
		_pageDesktop.SetViewport(Desktop.ViewportRectangle, Desktop.Scale);
	}

	protected override void PrepareForDrawSelf()
	{
		if (_isLoading)
		{
			float num = ((_loadingTimer > 0.2f) ? MathHelper.Min(1f, (_loadingTimer - 0.2f) / 0.5f) : 0f);
			double num2 = System.Math.Pow(num, 3.0);
			_loadingOverlay.Background.Color = UInt32Color.FromRGBA(0, 0, 0, (byte)(127.0 * num2));
			_loadingLabel.Background.Color = UInt32Color.FromRGBA(0, 0, 0, (byte)(255.0 * num2));
			_loadingLabel.Style.TextColor = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(255.0 * num2));
			_loadingOverlay.Layout();
		}
		_pageDesktop.PrepareForDraw();
	}

	public void OnChangeDrawOutlines()
	{
		_pageDesktop.DrawOutlines = Desktop.DrawOutlines;
	}
}
