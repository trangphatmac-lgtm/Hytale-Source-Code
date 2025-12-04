using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Application;
using HytaleClient.Core;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.Interface.DevTools;

internal class DevToolsOverlay : Element
{
	private enum GameInfoKey
	{
		Branch,
		Revision,
		FrameworkVersion,
		GPUVendor,
		GPURenderer,
		GPUVersion,
		WindowSize,
		ViewportSize,
		SceneSize,
		Biome,
		Zone,
		Environment,
		Weather,
		Entities,
		ViewDistance,
		Heightmap,
		Tint,
		Light,
		AudioEvents,
		ImmersiveViews,
		TargetBlock,
		HitBox,
		Orientation,
		FeetPosition,
		ChunkPosition,
		ChunksLoaded,
		ChunksDrawable,
		ChunksMax,
		MapAtlasSize,
		EntityAtlasSize,
		IconAtlasSize,
		UIAtlasSize,
		CustomUIAtlasSize,
		ActivelyReferencedAssets,
		BuiltInAssets,
		CachedAssets,
		UIScale,
		ParticleSystems,
		ParticleProxies,
		ParticleBlend,
		ParticleErosion,
		ParticleDistortion,
		NetworkSent,
		NetworkReceived
	}

	private enum Tabs
	{
		Console,
		GameInfo
	}

	public enum MessageType
	{
		Info,
		Warning,
		Error
	}

	private struct Message
	{
		public string Text;

		public MessageType MessageType;

		public int Count;

		public Button Element;
	}

	private struct InfoEntry
	{
		public string Value;

		public Label Label;
	}

	private readonly Interface _interface;

	private readonly PopupMenuLayer _popup;

	private Document _infoHeaderDoc;

	private Document _infoEntryDoc;

	private TabNavigation _tabNavigation;

	private Group _consoleLog;

	private Group _gameInfo;

	private readonly List<Message> _consoleMessages = new List<Message>();

	private readonly InfoEntry[] _infoEntries;

	private Tabs _activeTab = Tabs.Console;

	private float _deltaTime;

	public DevToolsOverlay(Interface @interface, Desktop desktop)
		: base(desktop, null)
	{
		_interface = @interface;
		_popup = new PopupMenuLayer(Desktop, null);
		_infoEntries = new InfoEntry[Enum.GetValues(typeof(GameInfoKey)).Length];
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("DevTools/DevToolsOverlay.ui", out var document);
		_popup.Style = document.ResolveNamedValue<PopupMenuLayerStyle>(Desktop.Provider, "PopupMenuLayerStyle");
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_consoleLog = uIFragment.Get<Group>("ConsoleLog");
		_consoleLog.Visible = true;
		_gameInfo = uIFragment.Get<Group>("GameInfo");
		_tabNavigation = uIFragment.Get<TabNavigation>("Tabs");
		_tabNavigation.SelectedTab = _activeTab.ToString();
		uIFragment.Get<TextButton>("MenuButton").Activating = delegate
		{
			_popup.SetItems(new PopupMenuItem[2]
			{
				new PopupMenuItem(Desktop.Provider.GetText("ui.devTools.popupMenu.copyGameInfo"), CopyAllValues),
				new PopupMenuItem(Desktop.Provider.GetText("ui.devTools.popupMenu.createDefect"), CreateDefect)
			});
			_popup.Open();
		};
		string[] names = Enum.GetNames(typeof(Tabs));
		TabNavigation.Tab[] array = new TabNavigation.Tab[names.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new TabNavigation.Tab
			{
				Id = names[i],
				Text = Desktop.Provider.GetText("ui.devTools.tabs." + names[i])
			};
		}
		_tabNavigation.Tabs = array;
		_tabNavigation.SelectedTabChanged = delegate
		{
			_activeTab = (Tabs)Enum.Parse(typeof(Tabs), _tabNavigation.SelectedTab);
			switch (_activeTab)
			{
			case Tabs.Console:
				_gameInfo.Visible = false;
				_consoleLog.Visible = true;
				break;
			case Tabs.GameInfo:
				_gameInfo.Visible = true;
				_consoleLog.Visible = false;
				break;
			}
			Layout();
			ScrollDownLog();
		};
		for (int j = 0; j < _consoleMessages.Count; j++)
		{
			Message message = _consoleMessages[j];
			message.Element = BuildConsoleMessage(message);
			_consoleMessages[j] = message;
		}
		BuildInfoEntries();
		if (base.IsMounted)
		{
			InitializeStaticValues();
			UpdateValues();
		}
	}

	protected override void OnMounted()
	{
		InitializeStaticValues();
		UpdateValues();
		ScrollDownLog();
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		if (_gameInfo.IsMounted)
		{
			_deltaTime += deltaTime;
			if (!(_deltaTime < 1f))
			{
				_deltaTime = 0f;
				UpdateValues();
				Layout();
			}
		}
	}

	private void BuildSectionHeader(string key)
	{
		UIFragment uIFragment = _infoHeaderDoc.Instantiate(Desktop, _gameInfo);
		uIFragment.Get<Label>("Label").Text = Desktop.Provider.GetText("ui.devTools.gameInfo.sections." + key);
	}

	private void InitializeStaticValues()
	{
		SetValue(GameInfoKey.Branch, BuildInfo.BranchName);
		SetValue(GameInfoKey.Revision, BuildInfo.RevisionId);
		SetValue(GameInfoKey.FrameworkVersion, Environment.Version.ToString());
		GraphicsDevice graphics = Desktop.Graphics;
		string value = graphics.GPUInfo.Vendor.ToString();
		string renderer = graphics.GPUInfo.Renderer;
		string version = graphics.GPUInfo.Version;
		SetValue(GameInfoKey.GPUVendor, value);
		SetValue(GameInfoKey.GPURenderer, renderer);
		SetValue(GameInfoKey.GPUVersion, version);
	}

	private void BuildEntry(GameInfoKey key, string assetEditorType = null)
	{
		UIFragment uIFragment = _infoEntryDoc.Instantiate(Desktop, _gameInfo);
		Label label = uIFragment.Get<Label>("Name");
		Label label2 = uIFragment.Get<Label>("Value");
		Button button = uIFragment.Get<Button>("Button");
		button.RightClicking = delegate
		{
			List<PopupMenuItem> list = new List<PopupMenuItem>
			{
				new PopupMenuItem(Desktop.Provider.GetText("ui.devTools.gameStats.popupMenu.copyValue"), delegate
				{
					SDL.SDL_SetClipboardText(_infoEntries[(int)key].Value);
				})
			};
			if (assetEditorType != null)
			{
				list.Add(new PopupMenuItem(Desktop.Provider.GetText("ui.devTools.gameStats.popupMenu.editInAssetEditor"), delegate
				{
					string value = _infoEntries[(int)key].Value;
					if (!(value == ""))
					{
						_interface.App.DevTools.Close();
						_interface.App.InGame.OpenAssetIdInAssetEditor(assetEditorType, value);
					}
				}));
			}
			_popup.SetItems(list);
			_popup.Open();
		};
		label.Text = Desktop.Provider.GetText("ui.devTools.gameInfo.keys." + key);
		_infoEntries[(int)key].Label = label2;
	}

	private void BuildInfoEntries()
	{
		_gameInfo.Clear();
		for (int i = _infoEntries.Length; i < _infoEntries.Length; i++)
		{
			_infoEntries[i] = default(InfoEntry);
		}
		Desktop.Provider.TryGetDocument("DevTools/InfoHeader.ui", out _infoHeaderDoc);
		Desktop.Provider.TryGetDocument("DevTools/InfoEntry.ui", out _infoEntryDoc);
		BuildSectionHeader("Build");
		BuildEntry(GameInfoKey.Branch);
		BuildEntry(GameInfoKey.Revision);
		BuildEntry(GameInfoKey.FrameworkVersion);
		BuildSectionHeader("Hardware");
		BuildEntry(GameInfoKey.GPUVendor);
		BuildEntry(GameInfoKey.GPURenderer);
		BuildEntry(GameInfoKey.GPUVersion);
		BuildEntry(GameInfoKey.WindowSize);
		BuildEntry(GameInfoKey.ViewportSize);
		BuildEntry(GameInfoKey.SceneSize);
		BuildSectionHeader("World");
		BuildEntry(GameInfoKey.Biome);
		BuildEntry(GameInfoKey.Zone);
		BuildEntry(GameInfoKey.Environment, "Environment");
		BuildEntry(GameInfoKey.Weather, "Weather");
		BuildEntry(GameInfoKey.Entities);
		BuildEntry(GameInfoKey.ViewDistance);
		BuildEntry(GameInfoKey.Heightmap);
		BuildEntry(GameInfoKey.Tint);
		BuildEntry(GameInfoKey.Light);
		BuildEntry(GameInfoKey.AudioEvents);
		BuildEntry(GameInfoKey.ImmersiveViews);
		BuildEntry(GameInfoKey.TargetBlock);
		BuildEntry(GameInfoKey.HitBox);
		BuildEntry(GameInfoKey.Orientation);
		BuildEntry(GameInfoKey.FeetPosition);
		BuildEntry(GameInfoKey.ChunkPosition);
		BuildSectionHeader("Chunks");
		BuildEntry(GameInfoKey.ChunksLoaded);
		BuildEntry(GameInfoKey.ChunksDrawable);
		BuildEntry(GameInfoKey.ChunksMax);
		BuildSectionHeader("AtlasSizes");
		BuildEntry(GameInfoKey.MapAtlasSize);
		BuildEntry(GameInfoKey.EntityAtlasSize);
		BuildEntry(GameInfoKey.IconAtlasSize);
		BuildEntry(GameInfoKey.UIAtlasSize);
		BuildEntry(GameInfoKey.CustomUIAtlasSize);
		BuildSectionHeader("Assets");
		BuildEntry(GameInfoKey.ActivelyReferencedAssets);
		BuildEntry(GameInfoKey.BuiltInAssets);
		BuildEntry(GameInfoKey.CachedAssets);
		BuildSectionHeader("UI");
		BuildEntry(GameInfoKey.UIScale);
		BuildSectionHeader("Particles");
		BuildEntry(GameInfoKey.ParticleSystems);
		BuildEntry(GameInfoKey.ParticleProxies);
		BuildEntry(GameInfoKey.ParticleBlend);
		BuildEntry(GameInfoKey.ParticleDistortion);
		BuildEntry(GameInfoKey.ParticleErosion);
		BuildSectionHeader("Network");
		BuildEntry(GameInfoKey.NetworkSent);
		BuildEntry(GameInfoKey.NetworkReceived);
	}

	private void UpdateValues()
	{
		Point size = _interface.Engine.Window.GetSize();
		SetValue(GameInfoKey.WindowSize, $"{size.X}x{size.Y}");
		SetValue(GameInfoKey.ViewportSize, $"{_interface.Engine.Window.Viewport.Width}x{_interface.Engine.Window.Viewport.Height}");
		SetValue(GameInfoKey.ActivelyReferencedAssets, Desktop.Provider.FormatNumber(AssetManager.ActivelyReferencedAssetsCount));
		SetValue(GameInfoKey.BuiltInAssets, Desktop.Provider.FormatNumber(AssetManager.BuiltInAssetsCount));
		SetValue(GameInfoKey.CachedAssets, Desktop.Provider.FormatNumber(AssetManager.CachedAssetsCount));
		SetValue(GameInfoKey.UIAtlasSize, $"{_interface.TextureAtlasSize.X}x{_interface.TextureAtlasSize.X}");
		SetValue(GameInfoKey.CustomUIAtlasSize, $"{_interface.InGameCustomUIProvider.TextureAtlasSize.X}x{_interface.InGameCustomUIProvider.TextureAtlasSize.X}");
		SetValue(GameInfoKey.UIScale, Desktop.Provider.FormatNumber(Desktop.Scale));
		if (_interface.App.Stage == App.AppStage.InGame)
		{
			UpdateInGameValues();
		}
	}

	private void UpdateInGameValues()
	{
		GameInstance instance = _interface.InGameView.InGame.Instance;
		ProfilingModule profilingModule = instance.ProfilingModule;
		SetValue(GameInfoKey.MapAtlasSize, $"{instance.MapModule.TextureAtlas.Width}x{instance.MapModule.TextureAtlas.Height}");
		SetValue(GameInfoKey.EntityAtlasSize, $"{instance.EntityStoreModule.TextureAtlas.Width}x{instance.EntityStoreModule.TextureAtlas.Height}");
		if (_interface.InGameView.ItemIconAtlasTexture != null)
		{
			SetValue(GameInfoKey.IconAtlasSize, $"{_interface.InGameView.ItemIconAtlasTexture.Width}x{_interface.InGameView.ItemIconAtlasTexture.Height}");
		}
		else
		{
			SetValue(GameInfoKey.IconAtlasSize, "");
		}
		SetValue(GameInfoKey.ImmersiveViews, Desktop.Provider.FormatNumber(instance.ImmersiveScreenModule.GetScreenCount()));
		Vector2 viewportSize = instance.SceneRenderer.Data.ViewportSize;
		SetValue(GameInfoKey.SceneSize, $"{viewportSize.X}x{viewportSize.Y}");
		SetValue(GameInfoKey.Entities, Desktop.Provider.FormatNumber(instance.EntityStoreModule.GetEntitiesCount()));
		SetValue(GameInfoKey.Environment, instance.WeatherModule.CurrentEnvironment.Id);
		SetValue(GameInfoKey.Weather, instance.WeatherModule.CurrentWeather.Id);
		SetValue(GameInfoKey.ViewDistance, $"{instance.App.Settings.ViewDistance} (Effective: {instance.MapModule.EffectiveViewDistance:##.0})");
		Vector3 position = instance.LocalPlayer.Position;
		if (instance.WorldMapModule.TryGetBiomeAtPosition(position, out var biomeData))
		{
			SetValue(GameInfoKey.Biome, biomeData.BiomeName);
			SetValue(GameInfoKey.Zone, biomeData.ZoneName);
		}
		else
		{
			SetValue(GameInfoKey.Biome, "");
			SetValue(GameInfoKey.Zone, "");
		}
		SetValue(GameInfoKey.ParticleSystems, Desktop.Provider.FormatNumber(instance.Engine.FXSystem.Particles.ParticleSystemCount));
		SetValue(GameInfoKey.ParticleProxies, Desktop.Provider.FormatNumber(instance.Engine.FXSystem.Particles.ParticleSystemProxyCount));
		SetValue(GameInfoKey.ParticleBlend, Desktop.Provider.FormatNumber(instance.Engine.FXSystem.Particles.PreviousFrameBlendDrawCount));
		SetValue(GameInfoKey.ParticleDistortion, Desktop.Provider.FormatNumber(instance.Engine.FXSystem.Particles.PreviousFrameDistortionDrawCount));
		SetValue(GameInfoKey.ParticleErosion, Desktop.Provider.FormatNumber(instance.Engine.FXSystem.Particles.PreviousFrameErosionDrawCount));
		SetValue(GameInfoKey.NetworkSent, $"{profilingModule.LastAccumulatedSentPacketLength / 1000f:0.000} KB/s ({(float)profilingModule.TotalSentPacketLength / 1000f:0.000} KB)");
		SetValue(GameInfoKey.NetworkReceived, $"{profilingModule.LastAccumulatedReceivedPacketLength / 1000f:0.000} KB/s ({(float)profilingModule.TotalReceivedPacketLength / 1000f:0.000} KB)");
		if (instance.InteractionModule.HasFoundTargetBlock)
		{
			HitDetection.RaycastHit targetBlockHit = instance.InteractionModule.TargetBlockHit;
			ClientBlockType clientBlockType = instance.MapModule.ClientBlockTypes[targetBlockHit.BlockId];
			SetValue(GameInfoKey.TargetBlock, $"{clientBlockType.Name} ({targetBlockHit.BlockPosition.X}, {targetBlockHit.BlockPosition.Y}, {targetBlockHit.BlockPosition.Z})");
			SetValue(GameInfoKey.HitBox, clientBlockType.HitboxType.ToString());
		}
		else
		{
			SetValue(GameInfoKey.TargetBlock, "");
			SetValue(GameInfoKey.HitBox, "");
		}
		double num = System.Math.Round(position.X, 3);
		double num2 = System.Math.Round(position.Y, 3);
		double num3 = System.Math.Round(position.Z, 3);
		SetValue(GameInfoKey.FeetPosition, $"{num:##.000}, {num2:##.000}, {num3:##.000}");
		double num4 = System.Math.Round((double)(instance.LocalPlayer.LookOrientation.X * 180f) / System.Math.PI, 4);
		double num5 = System.Math.Round((double)(instance.LocalPlayer.LookOrientation.Y * 180f) / System.Math.PI, 4);
		double num6 = System.Math.Round((double)(instance.LocalPlayer.LookOrientation.Z * 180f) / System.Math.PI, 4);
		SetValue(GameInfoKey.Orientation, $"{num4:##.0000}, {num5:##.0000}, {num6:##.0000}");
		int num7 = (int)System.Math.Floor(position.X);
		int num8 = (int)System.Math.Floor(position.Y);
		int num9 = (int)System.Math.Floor(position.Z);
		int num10 = num7 >> 5;
		int num11 = num8 >> 5;
		int num12 = num9 >> 5;
		int num13 = num7 - num10 * 32;
		int num14 = num8 - num11 * 32;
		int num15 = num9 - num12 * 32;
		SetValue(GameInfoKey.ChunkPosition, $"({num13}, {num14}, {num15}) in ({num10}, {num11}, {num12})");
		ChunkColumn chunkColumn = instance.MapModule.GetChunkColumn(num10, num12);
		if (chunkColumn != null)
		{
			int num16 = (num15 << 5) + num13;
			uint num17 = chunkColumn.Tints[num16];
			SetValue(GameInfoKey.Heightmap, chunkColumn.Heights[num16].ToString());
			SetValue(GameInfoKey.Tint, $"#{(byte)(num17 >> 16):X2}{(byte)(num17 >> 8):X2}{(byte)num17:X2}");
			Chunk chunk = chunkColumn.GetChunk(num11);
			if (chunk != null)
			{
				string text = "-";
				string text2 = "-";
				int num18 = ChunkHelper.IndexOfWorldBlockInChunk(num7, num8, num9);
				if (chunk.Data.SelfLightAmounts != null)
				{
					ushort num19 = chunk.Data.SelfLightAmounts[num18];
					int num20 = num19 & 0xF;
					int num21 = (num19 >> 4) & 0xF;
					int num22 = (num19 >> 8) & 0xF;
					int num23 = (num19 >> 12) & 0xF;
					text = $"R: {num20}, G: {num21}, B: {num22}, S: {num23}";
				}
				if (chunk.Data.BorderedLightAmounts != null)
				{
					int num24 = ChunkHelper.IndexOfBlockInBorderedChunk(num18, 0, 0, 0);
					ushort num25 = chunk.Data.BorderedLightAmounts[num24];
					int num26 = num25 & 0xF;
					int num27 = (num25 >> 4) & 0xF;
					int num28 = (num25 >> 8) & 0xF;
					int num29 = (num25 >> 12) & 0xF;
					text2 = $"R: {num26}, G: {num27}, B: {num28}, S: {num29}";
				}
				SetValue(GameInfoKey.Light, "Local: " + text + ", Global: " + text2);
			}
			else
			{
				SetValue(GameInfoKey.Light, "");
			}
		}
		else
		{
			SetValue(GameInfoKey.Light, "");
			SetValue(GameInfoKey.Heightmap, "");
			SetValue(GameInfoKey.Tint, "");
		}
		SetValue(GameInfoKey.ChunksLoaded, Desktop.Provider.FormatNumber(instance.MapModule.LoadedChunksCount));
		SetValue(GameInfoKey.ChunksDrawable, Desktop.Provider.FormatNumber(instance.MapModule.DrawableChunksCount));
		SetValue(GameInfoKey.ChunksMax, Desktop.Provider.FormatNumber(instance.MapModule.ChunkColumnCount()));
	}

	private void SetValue(GameInfoKey key, string value)
	{
		ref InfoEntry reference = ref _infoEntries[(int)key];
		reference.Value = value;
		reference.Label.Text = value;
	}

	private void CopyAllValues()
	{
		UpdateValues();
		Layout();
		string text = "";
		foreach (Element child in _gameInfo.Children)
		{
			if (child is Label label)
			{
				if (text != "")
				{
					text += "\n";
				}
				text = text + label.TextSpans[0].Text + "\n";
			}
			if (child is Button button)
			{
				text = text + " - " + button.Find<Label>("Name").TextSpans.FirstOrDefault()?.Text + ": " + button.Find<Label>("Value").TextSpans.FirstOrDefault()?.Text + "\n";
			}
		}
		SDL.SDL_SetClipboardText(text);
	}

	private void CreateDefect()
	{
		OpenUtils.OpenTrustedUrlInDefaultBrowser("https://h-qa.atlassian.net/secure/CreateIssue.jspa?issuetype=10005&pid=10000");
	}

	private void ScrollDownLog()
	{
		Group consoleLog = _consoleLog;
		int? y = _consoleLog.ScaledScrollSize.Y;
		consoleLog.SetScroll(null, y);
	}

	public void AddConsoleMessage(MessageType type, string text)
	{
		Message value = _consoleMessages.LastOrDefault();
		if (value.Text != null && value.Text == text && value.MessageType == type)
		{
			value.Count++;
			if (!_interface.HasMarkupError && _interface.HasLoaded)
			{
				Label label = value.Element.Find<Label>("DuplicateCount");
				label.Visible = true;
				label.Text = Desktop.Provider.FormatNumber(value.Count);
			}
			_consoleMessages[_consoleMessages.Count - 1] = value;
			return;
		}
		Message message = default(Message);
		message.MessageType = type;
		message.Text = text;
		message.Count = 1;
		Message message2 = message;
		if (!_interface.HasMarkupError && _interface.HasLoaded)
		{
			message2.Element = BuildConsoleMessage(message2);
		}
		_consoleMessages.Add(message2);
	}

	public void LayoutLog()
	{
		if (_consoleLog.IsMounted)
		{
			_consoleLog.Layout();
		}
	}

	private Button BuildConsoleMessage(Message message)
	{
		Desktop.Provider.TryGetDocument("DevTools/ConsoleEntry.ui", out var document);
		PatchStyle background = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "ErrorIcon");
		PatchStyle background2 = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "WarningIcon");
		UInt32Color textColor = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "ErrorLabelColor");
		UInt32Color textColor2 = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "WarningLabelColor");
		Button.ButtonStyle style = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "ErrorStyle");
		Button.ButtonStyle style2 = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "WarningStyle");
		UIFragment uIFragment = document.Instantiate(Desktop, _consoleLog);
		Button button = uIFragment.Get<Button>("Button");
		Group group = uIFragment.Get<Group>("Icon");
		Label label = uIFragment.Get<Label>("Message");
		Label label2 = uIFragment.Get<Label>("DuplicateCount");
		button.RightClicking = delegate
		{
			_popup.SetItems(new PopupMenuItem[1]
			{
				new PopupMenuItem(Desktop.Provider.GetText("ui.devTools.logEntry.popupMenu.copyMessage"), delegate
				{
					SDL.SDL_SetClipboardText(message.Text);
				})
			});
			_popup.Open();
		};
		label.Text = message.Text;
		if (message.Count > 1)
		{
			label2.Visible = true;
			label2.Text = Desktop.Provider.FormatNumber(message.Count);
		}
		switch (message.MessageType)
		{
		case MessageType.Error:
			group.Visible = true;
			group.Background = background;
			label.Style.TextColor = textColor;
			button.Style = style;
			break;
		case MessageType.Warning:
			group.Visible = true;
			group.Background = background2;
			label.Style.TextColor = textColor2;
			button.Style = style2;
			break;
		}
		message.Element = button;
		return button;
	}

	public void ResetGameInfoState()
	{
		if (!_interface.HasMarkupError)
		{
			BuildInfoEntries();
			if (base.IsMounted)
			{
				Layout();
			}
		}
	}

	protected internal override void Dismiss()
	{
		_interface.App.DevTools.Close();
	}
}
