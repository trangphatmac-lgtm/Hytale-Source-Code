#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.WorldMap;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud;

internal class CompassComponent : InterfaceComponent
{
	private class CompassMarker
	{
		public string Id;

		public string MarkerImage;

		public Vector3 Position;

		public TexturePatch Icon;
	}

	private class DrawableCompassMarker
	{
		public TexturePatch Icon;

		public Vector3 Position;

		public float Width;

		public float Height;
	}

	public readonly InGameView InGameView;

	private readonly string[] _points = new string[9] { "N", "E", "S", "W", "N", "E", "S", "W", "N" };

	private static readonly UInt32Color MarkerDefaultColor = UInt32Color.White;

	private static readonly UInt32Color MarkerSelectionColor = UInt32Color.FromRGBA(242, 206, 5, byte.MaxValue);

	private TextureArea _compassMaskTextureArea;

	private int _maskOffset;

	private Rectangle _maskRectangle;

	private Font _pointFont;

	private int _pointStagger;

	private int _effectiveWidth;

	private int _halfEffectiveWidth;

	private int _halfVisibleWidth;

	private const float Scale = 0.4f;

	private const int MarkerScalingMaxDistance = 3000;

	private readonly Dictionary<string, CompassMarker> _markers = new Dictionary<string, CompassMarker>();

	private WorldMapModule.MarkerSelection _selectedMarker;

	public CompassComponent(InGameView view)
		: base(view.Interface, view.HudContainer)
	{
		InGameView = view;
		Anchor = new Anchor
		{
			Top = 10,
			Height = 21,
			Width = 500
		};
		Background = new PatchStyle("InGame/Hud/CompassBackground.png");
	}

	protected override void ApplyStyles()
	{
		base.ApplyStyles();
		_pointFont = Desktop.Provider.GetFontFamily("Default").RegularFont;
		_compassMaskTextureArea = Desktop.Provider.MakeTextureArea("InGame/Hud/CompassMask.png");
	}

	protected override void LayoutSelf()
	{
		_maskOffset = Desktop.ScaleRound(4f);
		_maskRectangle = new Rectangle(_anchoredRectangle.X, _anchoredRectangle.Y - _maskOffset, _anchoredRectangle.Width, _anchoredRectangle.Height + _maskOffset);
		_pointStagger = (int)((float)_rectangleAfterPadding.Width * 0.4f);
		_effectiveWidth = _pointStagger * 4;
		_halfEffectiveWidth = _effectiveWidth / 2;
		_halfVisibleWidth = _rectangleAfterPadding.Width / 2;
	}

	protected override void PrepareForDrawSelf()
	{
		base.PrepareForDrawSelf();
		if (!InGameView.InGame.Instance.IsPlaying)
		{
			return;
		}
		Desktop.Batcher2D.PushMask(_compassMaskTextureArea, _maskRectangle, Desktop.ViewportRectangle);
		float num = 14f * Desktop.Scale;
		ICameraController controller = InGameView.InGame.Instance.CameraModule.Controller;
		Vector3 position = controller.Position;
		float yaw = controller.Rotation.Yaw;
		float num2 = (yaw + (float)System.Math.PI) / ((float)System.Math.PI * 2f) * (float)_effectiveWidth - (float)((int)num / 2);
		int num3 = -(_pointStagger * 2);
		string[] points = _points;
		foreach (string text in points)
		{
			float x = (float)(_rectangleAfterPadding.X + num3) + num2 - (float)_effectiveWidth + (float)_halfVisibleWidth;
			Desktop.Batcher2D.RequestDrawText(_pointFont, num, text, new Vector3(x, _maskRectangle.Y + 1, 0f), UInt32Color.White, isBold: true);
			num3 += _pointStagger;
		}
		int num4 = 0;
		SortedList<int, DrawableCompassMarker> sortedList = new SortedList<int, DrawableCompassMarker>();
		foreach (CompassMarker value2 in _markers.Values)
		{
			float num5 = Vector3.Distance(vector2: new Vector3(value2.Position.X, value2.Position.Y, value2.Position.Z), vector1: position);
			if (!(num5 > 3000f))
			{
				float num6 = value2.Position.X - position.X;
				float num7 = value2.Position.Z - position.Z;
				float num8 = (float)System.Math.Atan2(0f - num6, 0f - num7);
				float num9 = (yaw - num8) / (float)System.Math.PI * (float)_halfEffectiveWidth;
				if (num9 < (float)(-_halfEffectiveWidth))
				{
					num9 += (float)_effectiveWidth;
				}
				else if (num9 > (float)_halfEffectiveWidth)
				{
					num9 -= (float)_effectiveWidth;
				}
				num9 += (float)_halfVisibleWidth;
				float num10 = NormalizeToScale(0f, 3000f, (float)value2.Icon.TextureArea.Rectangle.Width * Desktop.Scale, 0f, num5);
				float num11 = NormalizeToScale(0f, 3000f, (float)value2.Icon.TextureArea.Rectangle.Height * Desktop.Scale, 0f, num5);
				float num12 = ((float)_anchoredRectangle.Height - num11) / 2f;
				num9 -= num10 / 2f;
				int key = -(int)(num5 * 1000000f) + num4;
				DrawableCompassMarker value = new DrawableCompassMarker
				{
					Icon = value2.Icon,
					Position = new Vector3((float)_rectangleAfterPadding.X + num9, (float)_maskRectangle.Y + num12, 0f),
					Width = num10,
					Height = num11
				};
				sortedList.Add(key, value);
				num4++;
			}
		}
		foreach (DrawableCompassMarker value3 in sortedList.Values)
		{
			Desktop.Batcher2D.RequestDrawPatch(value3.Icon, value3.Position, value3.Width, value3.Height, Desktop.Scale);
		}
		Desktop.Batcher2D.PopMask();
	}

	protected override void OnUnmounted()
	{
		_markers.Clear();
	}

	public void ResetState()
	{
		_markers.Clear();
	}

	private static float NormalizeToScale(float oldScaleMin, float oldScaleMax, float newScaleMin, float newScaleMax, float value)
	{
		return newScaleMin + (newScaleMax - newScaleMin) * ((value - oldScaleMin) / (oldScaleMax - oldScaleMin));
	}

	public void OnWorldMapMarkerAdded(WorldMapModule.MapMarker marker)
	{
		AddMarker(marker);
	}

	public void OnWorldMapMarkerUpdated(WorldMapModule.MapMarker marker)
	{
		UpdateMarker(marker);
	}

	public void OnWorldMapMarkerRemoved(WorldMapModule.MapMarker marker)
	{
		if (marker != null)
		{
			RemoveMarker(marker.Id);
		}
	}

	private void OnContextMarkerSelected(WorldMapModule.MarkerSelection marker)
	{
		SelectContextMarker(marker);
	}

	private void OnContextMarkerDeselected()
	{
		DeselectContextMarker();
	}

	private void AddMarker(WorldMapModule.MapMarker marker, bool isSelected = false)
	{
		if (_markers.ContainsKey(marker.Id))
		{
			Trace.WriteLine("Tried to load a marker with ID " + marker.Id + " but it already exists.", "CompassComponent");
			return;
		}
		CompassMarker compassMarker = new CompassMarker
		{
			Id = marker.Id,
			MarkerImage = marker.MarkerImage,
			Position = new Vector3(marker.X, marker.Y, marker.Z),
			Icon = CreateMarkerTexturePatch(marker.MarkerImage)
		};
		if (isSelected)
		{
			compassMarker.Icon.Color = MarkerSelectionColor;
		}
		_markers.Add(marker.Id, compassMarker);
	}

	private void UpdateMarker(WorldMapModule.MapMarker marker)
	{
		RemoveMarker(marker.Id);
		AddMarker(marker);
	}

	private void RemoveMarker(string markerId)
	{
		_markers.Remove(markerId);
	}

	private TexturePatch CreateMarkerTexturePatch(string markerImage)
	{
		InGameView.TryMountAssetTexture("UI/WorldMap/MapMarkers/" + markerImage, out var textureArea);
		if (textureArea == null)
		{
			textureArea = Desktop.Provider.MissingTexture;
		}
		return Desktop.MakeTexturePatch(new PatchStyle(textureArea));
	}

	public void SelectContextMarker(WorldMapModule.MarkerSelection marker)
	{
		DeselectContextMarker();
		if (marker.MapMarker != null && _markers.ContainsKey(marker.MapMarker.Id))
		{
			_markers[marker.MapMarker.Id].Icon.Color = MarkerSelectionColor;
		}
		else if (marker.Type == WorldMapModule.MarkerSelectionType.Coordinates)
		{
			AddMarker(new WorldMapModule.MapMarker
			{
				Id = "Coordinates",
				MarkerImage = "Coordinate.png",
				X = marker.Coordinates.X,
				Y = 0f,
				Z = marker.Coordinates.Y
			}, isSelected: true);
		}
		_selectedMarker = marker;
	}

	public void DeselectContextMarker()
	{
		if (_selectedMarker.MapMarker != null && _markers.ContainsKey(_selectedMarker.MapMarker.Id))
		{
			_markers[_selectedMarker.MapMarker.Id].Icon.Color = MarkerDefaultColor;
		}
		else if (_selectedMarker.Type == WorldMapModule.MarkerSelectionType.Coordinates && _markers.ContainsKey("Coordinates"))
		{
			RemoveMarker("Coordinates");
		}
	}

	public void OnAssetsUpdated()
	{
		foreach (CompassMarker value in _markers.Values)
		{
			value.Icon = CreateMarkerTexturePatch(value.MarkerImage);
		}
	}
}
