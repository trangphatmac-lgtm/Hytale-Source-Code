using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HytaleClient.Application;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Interface.Common;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Interface.MainMenu.Pages;

internal class AdventurePage : InterfaceComponent
{
	public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly MainMenuView MainMenuView;

	private Group _worldsContainer;

	private int _worldsPerRow;

	private int _tileWidth;

	private int _tileHeight;

	private int _tileSpacing;

	private PatchStyle _emptyTileBackground;

	private readonly Dictionary<string, Texture> _worldPreviewTextures = new Dictionary<string, Texture>();

	private readonly Dictionary<string, Element> _worldPreviewElements = new Dictionary<string, Element>();

	private int _tileCount;

	private Group _worldListPane;

	private Group _worldCreationPane;

	private Group _worldSettingsPane;

	private Group _currentPane;

	private Button _adventureTile;

	private Button _creativeTile;

	private TextField _worldNameInput;

	private Label _creativeModeSettingsSummary;

	private Button.ButtonStyle _adventureTileButtonStyle;

	private Button.ButtonStyle _creativeTileButtonStyle;

	private Button.ButtonStyle _adventureTileButtonSelectedStyle;

	private Button.ButtonStyle _creativeTileButtonSelectedStyle;

	private string _adventureIconPath;

	private string _adventureIconSelectedPath;

	private TextField _worldNameSettingInput;

	private TextButton _saveWorldSettingsButton;

	private TextButton _createWorldSettingsButton;

	private CheckBox _flatWorldSettingCheckbox;

	private CheckBox _npcsSettingCheckbox;

	private Group _creativeOptions;

	private Group _worldDirectoryOptions;

	private Group _worldSettingsImage;

	private Label _worldSettingsModeName;

	private Label _worldSettingsModeDescription;

	private readonly ModalDialog.DialogSetup _errorDialogSetup;

	private BackButton _backButton;

	private GameMode _gameMode = (GameMode)0;

	private string _editingWorld;

	public AdventurePage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		MainMenuView = mainMenuView;
		_errorDialogSetup = new ModalDialog.DialogSetup
		{
			Cancellable = false
		};
	}

	public void Build()
	{
		Clear();
		_currentPane = null;
		Interface.TryGetDocument("MainMenu/Adventure/AdventurePage.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		Interface.TryGetDocument("MainMenu/Adventure/WorldTile.ui", out var document2);
		_worldsPerRow = document2.ResolveNamedValue<int>(Desktop.Provider, "WorldsPerRow");
		_tileWidth = document2.ResolveNamedValue<int>(Desktop.Provider, "TileWidth");
		_tileHeight = document2.ResolveNamedValue<int>(Desktop.Provider, "TileHeight");
		_tileSpacing = document2.ResolveNamedValue<int>(Desktop.Provider, "TileSpacing");
		_emptyTileBackground = document2.ResolveNamedValue<PatchStyle>(Desktop.Provider, "EmptyTileBackground");
		Interface.TryGetDocument("MainMenu/Adventure/WorldList.ui", out var document3);
		UIFragment uIFragment2 = document3.Instantiate(Desktop, null);
		_worldListPane = uIFragment2.Get<Group>("WorldList");
		_worldsContainer = uIFragment2.Get<Group>("WorldsContainer");
		uIFragment2.Get<TextButton>("NewWorldButton").Activating = ShowWorldCreationPane;
		Interface.TryGetDocument("MainMenu/Adventure/WorldCreation.ui", out var document4);
		_adventureTileButtonStyle = document4.ResolveNamedValue<Button.ButtonStyle>(Interface, "AdventureTileButtonStyle");
		_creativeTileButtonStyle = document4.ResolveNamedValue<Button.ButtonStyle>(Interface, "CreativeTileButtonStyle");
		_adventureTileButtonSelectedStyle = document4.ResolveNamedValue<Button.ButtonStyle>(Interface, "AdventureTileButtonSelectedStyle");
		_creativeTileButtonSelectedStyle = document4.ResolveNamedValue<Button.ButtonStyle>(Interface, "CreativeTileButtonSelectedStyle");
		_adventureIconPath = document4.ResolveNamedValue<UIPath>(Interface, "AdventureIconPath").Value;
		_adventureIconSelectedPath = document4.ResolveNamedValue<UIPath>(Interface, "AdventureIconSelectedPath").Value;
		UIFragment uIFragment3 = document4.Instantiate(Desktop, null);
		_worldCreationPane = uIFragment3.Get<Group>("WorldCreation");
		_adventureTile = uIFragment3.Get<Button>("AdventureTile");
		_adventureTile.DoubleClicking = delegate
		{
			SetGameMode((GameMode)0);
			_adventureTile.Layout();
			_creativeTile.Layout();
			SaveWorld();
		};
		_adventureTile.Activating = delegate
		{
			SetGameMode((GameMode)0);
			_adventureTile.Layout();
			_creativeTile.Layout();
		};
		_creativeTile = uIFragment3.Get<Button>("CreativeTile");
		_creativeTile.DoubleClicking = delegate
		{
			SetGameMode((GameMode)1);
			_adventureTile.Layout();
			_creativeTile.Layout();
			SaveWorld();
		};
		_creativeTile.Activating = delegate
		{
			SetGameMode((GameMode)1);
			_adventureTile.Layout();
			_creativeTile.Layout();
		};
		_creativeTile.RightClicking = delegate
		{
			_worldNameSettingInput.Value = _worldNameInput.Value;
			_worldNameSettingInput.PlaceholderText = _worldNameInput.PlaceholderText;
			_worldDirectoryOptions.Visible = false;
			_saveWorldSettingsButton.Visible = false;
			_createWorldSettingsButton.Visible = true;
			_creativeOptions.Visible = true;
			_flatWorldSettingCheckbox.Parent.Visible = true;
			SetGameMode((GameMode)1);
			SetPane(_worldSettingsPane);
		};
		_creativeModeSettingsSummary = uIFragment3.Get<Label>("CreativeModeSettingsSummary");
		_worldNameInput = uIFragment3.Get<TextField>("WorldNameInput");
		uIFragment3.Get<TextButton>("CreateWorldButton").Activating = OnSaveWorld;
		Interface.TryGetDocument("MainMenu/Adventure/WorldSettings.ui", out var document5);
		UIFragment uIFragment4 = document5.Instantiate(Desktop, null);
		_worldSettingsPane = uIFragment4.Get<Group>("WorldSettings");
		_saveWorldSettingsButton = uIFragment4.Get<TextButton>("SaveWorldSettingsButton");
		_saveWorldSettingsButton.Activating = OnSaveWorld;
		_createWorldSettingsButton = uIFragment4.Get<TextButton>("CreateWorldSettingsButton");
		_createWorldSettingsButton.Activating = OnSaveWorld;
		_worldNameSettingInput = uIFragment4.Get<TextField>("WorldNameSettingInput");
		_creativeOptions = uIFragment4.Get<Group>("CreativeModeOptions");
		_worldDirectoryOptions = uIFragment4.Get<Group>("WorldDirectoryOptions");
		_worldSettingsImage = uIFragment4.Get<Group>("WorldSettingsImage");
		_worldSettingsModeName = uIFragment4.Get<Label>("WorldSettingsModeName");
		_worldSettingsModeDescription = uIFragment4.Get<Label>("WorldSettingsModeDescription");
		_flatWorldSettingCheckbox = uIFragment4.Get<CheckBox>("FlatWorldSettingCheckBox");
		_npcsSettingCheckbox = uIFragment4.Get<CheckBox>("NpcsSettingCheckBox");
		uIFragment4.Get<Button>("BackButton").Activating = delegate
		{
			SetPane((_editingWorld != null) ? _worldListPane : _worldCreationPane);
		};
		uIFragment4.Get<TextButton>("OpenWorldDirectoryButton").Activating = delegate
		{
			Interface.App.MainMenu.OpenSingleplayerWorldFolder(_editingWorld);
		};
		uIFragment4.Get<TextButton>("DeleteWorldButton").Activating = OnShowDeleteWorldPopup;
		_backButton = new BackButton(Interface, Dismiss);
		uIFragment.Get<Group>("BackButtonContainer").Add(_backButton);
		SetPane(_worldListPane);
		if (base.IsMounted)
		{
			ShowWorldListPane();
		}
	}

	protected override void OnMounted()
	{
		MainMenuView.ShowTopBar(showTopBar: true);
		Interface.App.MainMenu.GatherWorldList();
		ShowWorldListPane();
	}

	protected override void OnUnmounted()
	{
		_worldPreviewElements.Clear();
		ClearLoadedWorldPreviewImages();
		SetPane(_worldListPane);
	}

	private void SetGameMode(GameMode gameMode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		_gameMode = gameMode;
		_creativeTile.Style = (((int)gameMode == 1) ? _creativeTileButtonSelectedStyle : _creativeTileButtonStyle);
		_creativeTile.Find<Group>("Effect").Visible = (int)gameMode == 1;
		_adventureTile.Style = (((int)gameMode == 0) ? _adventureTileButtonSelectedStyle : _adventureTileButtonStyle);
		_adventureTile.Find<Group>("Effect").Visible = (int)gameMode == 0;
		_adventureTile.Find<Group>("Icon").Background = new PatchStyle(((int)gameMode == 0) ? _adventureIconSelectedPath : _adventureIconPath);
		_worldSettingsModeName.Text = "/ " + Desktop.Provider.GetText("ui.general.gamemodes." + ((object)(GameMode)(ref gameMode)).ToString().ToLower()) + " World";
		_worldSettingsModeDescription.Text = Desktop.Provider.GetText("ui.mainMenu.adventure." + ((object)(GameMode)(ref gameMode)).ToString().ToLower() + "ModeDescription");
		_worldSettingsImage.Background = new PatchStyle($"MainMenu/Adventure/{gameMode}ModeImage.png");
	}

	private void SetPane(Group pane)
	{
		if (_currentPane != pane)
		{
			if (_currentPane != null)
			{
				Remove(_currentPane);
			}
			_currentPane = pane;
			if (_currentPane == _worldCreationPane)
			{
				_creativeModeSettingsSummary.Text = Desktop.Provider.GetText(_flatWorldSettingCheckbox.Value ? "ui.mainMenu.adventure.flatWorld" : "ui.mainMenu.adventure.randomWorld") + " / " + Desktop.Provider.GetText(_npcsSettingCheckbox.Value ? "ui.mainMenu.adventure.npcSpawning" : "ui.mainMenu.adventure.noNpcSpawning");
			}
			Add(pane);
			if (pane.IsMounted)
			{
				pane.Layout(base.RectangleAfterPadding);
				Desktop.RefreshHover();
			}
			_backButton.Visible = _currentPane != _worldListPane;
			if (_backButton.IsMounted)
			{
				_backButton.Layout(_backButton.Parent.RectangleAfterPadding);
			}
		}
	}

	private void OnSaveWorld()
	{
		SaveWorld();
	}

	private void SaveWorld()
	{
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		string text;
		if (_currentPane == _worldCreationPane)
		{
			text = _worldNameInput.Value.Trim();
			if (text == "")
			{
				text = _worldNameInput.PlaceholderText.Trim();
			}
		}
		else
		{
			text = _worldNameSettingInput.Value.Trim();
			if (text == "")
			{
				text = _worldNameSettingInput.PlaceholderText.Trim();
			}
		}
		if (text == "")
		{
			return;
		}
		if (_editingWorld == null)
		{
			AppMainMenu.WorldOptions options = new AppMainMenu.WorldOptions
			{
				Name = text,
				FlatWorld = ((int)_gameMode != 0 && _flatWorldSettingCheckbox.Value),
				NpcSpawning = ((int)_gameMode == 0 || _npcsSettingCheckbox.Value),
				GameMode = _gameMode
			};
			if (!Interface.App.MainMenu.TryCreateSingleplayerWorld(options, out var error))
			{
				_errorDialogSetup.Title = "ui.general.error";
				_errorDialogSetup.Text = error;
				Interface.ModalDialog.Setup(_errorDialogSetup);
				Desktop.SetLayer(4, Interface.ModalDialog);
			}
		}
		else
		{
			AppMainMenu.WorldOptions options2 = new AppMainMenu.WorldOptions
			{
				Name = text,
				NpcSpawning = _npcsSettingCheckbox.Value,
				GameMode = _gameMode
			};
			if (!Interface.App.MainMenu.TryUpdateSingleplayerWorldOptions(_editingWorld, options2, out var error2))
			{
				_errorDialogSetup.Title = "ui.general.error";
				_errorDialogSetup.Text = error2;
				Interface.ModalDialog.Setup(_errorDialogSetup);
				Desktop.SetLayer(4, Interface.ModalDialog);
			}
			else
			{
				Interface.App.MainMenu.GatherWorldList();
				ShowWorldListPane();
			}
		}
	}

	private void JoinWorld(string directoryName)
	{
		Interface.App.MainMenu.JoinSingleplayerWorld(directoryName);
	}

	private void ShowWorldListPane()
	{
		if (Interface.HasMarkupError)
		{
			return;
		}
		_worldPreviewElements.Clear();
		if (Interface.App.MainMenu.Worlds.Count == 0)
		{
			ShowWorldCreationPane();
			return;
		}
		SetPane(_worldListPane);
		ClearLoadedWorldPreviewImages();
		_worldsContainer.Clear();
		List<AppMainMenu.World> worlds = Interface.App.MainMenu.Worlds;
		_tileCount = System.Math.Max(worlds.Count, _worldsPerRow * 2);
		if (_tileCount > _worldsPerRow * 2)
		{
			int num = _tileCount % _worldsPerRow;
			if (num > 0)
			{
				_tileCount += _worldsPerRow - num;
			}
		}
		for (int i = 0; i < _tileCount; i++)
		{
			if (i >= worlds.Count)
			{
				Group group = MakeTileContainer(i);
				group.Background = _emptyTileBackground;
				continue;
			}
			AppMainMenu.World world = worlds[i];
			Group root = MakeTileContainer(i);
			Interface.TryGetDocument("MainMenu/Adventure/WorldTile.ui", out var document);
			UIFragment uIFragment = document.Instantiate(Desktop, root);
			Button button = uIFragment.Get<Button>("Button");
			button.Activating = delegate
			{
				JoinWorld(world.Path);
			};
			button.RightClicking = delegate
			{
				OnEditWorldButtonActivate(world.Path);
			};
			string text = Path.Combine(Paths.Saves, world.Path, "preview.png");
			Element element = uIFragment.Get<Element>("Image");
			_worldPreviewElements[world.Path] = element;
			if (File.Exists(text) && Image.TryGetPngDimensions(text, out var width, out var height))
			{
				TextureArea textureArea = ExternalTextureLoader.FromPath(text);
				_worldPreviewTextures.Add(text, textureArea.Texture);
				if (_tileHeight > _tileWidth)
				{
					int num2 = _tileHeight / _tileHeight * width;
					int y = (height - num2) / 2;
					element.Background = new PatchStyle(textureArea)
					{
						Area = new Rectangle(0, y, width, num2),
						Color = UInt32Color.White
					};
				}
				else
				{
					int num3 = _tileWidth / _tileHeight * height;
					int x = (width - num3) / 2;
					element.Background = new PatchStyle(textureArea)
					{
						Area = new Rectangle(x, 0, num3, height),
						Color = UInt32Color.White
					};
				}
			}
			uIFragment.Get<Label>("Name").Text = world.Options.Name;
			uIFragment.Get<Label>("LastWriteTime").Text = Interface.FormatRelativeTime(DateTime.Parse(world.LastWriteTime, null, DateTimeStyles.RoundtripKind));
			uIFragment.Get<Element>("GameModeIcon").Background = new PatchStyle("MainMenu/Adventure/GameModeIcon" + ((object)(GameMode)(ref world.Options.GameMode)).ToString() + ".png");
		}
		int num4 = (int)System.Math.Ceiling((float)_tileCount / (float)_worldsPerRow);
		_worldsContainer.ContentHeight = num4 * _tileHeight + (num4 - 1) * _tileSpacing;
		_worldsContainer.Layout();
		Group MakeTileContainer(int index)
		{
			int value = index % _worldsPerRow * (_tileWidth + _tileSpacing);
			int value2 = index / _worldsPerRow * (_tileHeight + _tileSpacing);
			return new Group(Desktop, _worldsContainer)
			{
				Anchor = new Anchor
				{
					Left = value,
					Top = value2,
					Width = _tileWidth,
					Height = _tileHeight
				}
			};
		}
	}

	private void OnShowDeleteWorldPopup()
	{
		string name = Interface.App.MainMenu.Worlds.Find((AppMainMenu.World w) => w.Path == _editingWorld).Options.Name;
		string text = Desktop.Provider.GetText("ui.mainMenu.adventure.deleteWorldConfirmation").Replace("{name}", name);
		Interface.ModalDialog.Setup(new ModalDialog.DialogSetup
		{
			Title = "ui.mainMenu.adventure.deleteWorld",
			Text = text,
			ConfirmationText = "ui.general.delete",
			OnConfirm = OnConfirmDeleteWorld
		});
		Desktop.SetLayer(4, Interface.ModalDialog);
	}

	private void OnConfirmDeleteWorld()
	{
		if (!Interface.App.MainMenu.TryDeleteSingleplayerWorld(_editingWorld, out var error))
		{
			_errorDialogSetup.Title = "ui.general.error";
			_errorDialogSetup.Text = error;
			Interface.ModalDialog.Setup(_errorDialogSetup);
			Desktop.SetLayer(4, Interface.ModalDialog);
		}
		else
		{
			ShowWorldListPane();
		}
	}

	private void ShowWorldCreationPane()
	{
		_editingWorld = null;
		_worldNameInput.Value = "";
		_npcsSettingCheckbox.Value = false;
		_flatWorldSettingCheckbox.Value = false;
		SetGameMode((GameMode)0);
		string text = Desktop.Provider.GetText("ui.mainMenu.adventure.defaultWorldName");
		if (Interface.App.MainMenu.Worlds.Count > 0)
		{
			int num = -1;
			foreach (AppMainMenu.World world in Interface.App.MainMenu.Worlds)
			{
				if (world.Options.Name.Contains(text))
				{
					int result;
					if (world.Options.Name == text)
					{
						num = System.Math.Max(num, 0);
					}
					else if (int.TryParse(world.Options.Name.Replace(text, "").Trim(), out result))
					{
						num = System.Math.Max(num, result);
					}
				}
			}
			if (num == -1)
			{
				_worldNameInput.PlaceholderText = text;
			}
			else
			{
				_worldNameInput.PlaceholderText = text + " " + (num + 1);
			}
		}
		else
		{
			_worldNameInput.PlaceholderText = text;
		}
		SetPane(_worldCreationPane);
		Desktop.FocusElement(_worldNameInput);
	}

	private void OnEditWorldButtonActivate(string directoryName)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Invalid comparison between Unknown and I4
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		AppMainMenu.World world = Interface.App.MainMenu.Worlds.Find((AppMainMenu.World w) => w.Path == directoryName);
		_editingWorld = directoryName;
		_saveWorldSettingsButton.Visible = true;
		_createWorldSettingsButton.Visible = false;
		_worldNameSettingInput.Value = world.Options.Name;
		_worldNameSettingInput.PlaceholderText = "";
		_worldDirectoryOptions.Visible = true;
		_creativeOptions.Visible = (int)world.Options.GameMode == 1;
		SetGameMode(world.Options.GameMode);
		_npcsSettingCheckbox.Value = world.Options.NpcSpawning;
		_flatWorldSettingCheckbox.Value = world.Options.FlatWorld;
		_flatWorldSettingCheckbox.Parent.Visible = false;
		SetPane(_worldSettingsPane);
	}

	private void ClearLoadedWorldPreviewImages()
	{
		foreach (Texture value in _worldPreviewTextures.Values)
		{
			value.Dispose();
		}
		_worldPreviewTextures.Clear();
	}

	public void OnWorldPreviewUpdated(string worldDirectoryName)
	{
		Logger.Info("Reloading world preview for '{0}'", worldDirectoryName);
		if (!_worldPreviewElements.TryGetValue(worldDirectoryName, out var value))
		{
			Logger.Warn("No world preview element found!");
			return;
		}
		string text = Path.Combine(Paths.Saves, worldDirectoryName, "preview.png");
		if (File.Exists(text) && Image.TryGetPngDimensions(text, out var width, out var height))
		{
			TextureArea textureArea = ExternalTextureLoader.FromPath(text);
			_worldPreviewTextures.Add(text, textureArea.Texture);
			if (_tileHeight > _tileWidth)
			{
				int num = _tileHeight / _tileHeight * width;
				int y = (height - num) / 2;
				value.Background = new PatchStyle(textureArea)
				{
					Area = new Rectangle(0, y, width, num),
					Color = UInt32Color.White
				};
			}
			else
			{
				int num2 = _tileWidth / _tileHeight * height;
				int x = (width - num2) / 2;
				value.Background = new PatchStyle(textureArea)
				{
					Area = new Rectangle(x, 0, num2, height),
					Color = UInt32Color.White
				};
			}
		}
		else
		{
			Logger.Warn("Failed to load thumbnail");
		}
	}

	public void OnFailedToJoinUnknownWorld()
	{
		_errorDialogSetup.Title = "ui.mainMenu.adventure.worldDoesntExist.title";
		_errorDialogSetup.Text = "ui.mainMenu.adventure.worldDoesntExist.message";
		Interface.ModalDialog.Setup(_errorDialogSetup);
		Desktop.SetLayer(4, Interface.ModalDialog);
	}

	protected internal override void Validate()
	{
		if (_currentPane == _worldSettingsPane || _currentPane == _worldCreationPane || (_currentPane == _worldListPane && Interface.App.MainMenu.Worlds.Count == 0))
		{
			SaveWorld();
		}
	}

	protected internal override void Dismiss()
	{
		if (_currentPane == _worldSettingsPane)
		{
			SetPane((_editingWorld == null) ? _worldCreationPane : _worldListPane);
		}
		else if (_currentPane == _worldCreationPane && Interface.App.MainMenu.Worlds.Count > 0)
		{
			SetPane(_worldListPane);
		}
		else
		{
			Interface.App.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
		}
	}
}
