#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HytaleClient.Application;
using HytaleClient.Data.Characters;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;

namespace HytaleClient.Interface.MainMenu.Pages.MyAvatar;

internal class MyAvatarPage : InterfaceComponent
{
	private enum MyAvatarPageTab
	{
		General,
		Face,
		Emotes,
		FacialHair,
		Eyebrows,
		Eyes,
		Haircut,
		Pants,
		Overpants,
		Undertop,
		Overtop,
		Shoes,
		Gloves,
		HeadAccessory,
		FaceAccessory,
		EarAccessory
	}

	private int _previewWidth;

	private int _previewHeight;

	private int _previewMargin;

	private int _previewsPerRow;

	private int _visiblePreviewRows;

	private UInt32Color _previewBackgroundColor;

	private UInt32Color _previewBackgroundColorHovered;

	private PatchStyle _previewFrameBackground;

	private PatchStyle _previewFrameBackgroundSelected;

	private PatchStyle _previewBackgroundSelected;

	private Anchor _previewSelectedFrameSize;

	private Button.ButtonStyle _assetButtonStyle;

	private Group _colors;

	private Group _parts;

	private Group _variantsContainer;

	private DropdownBox _variants;

	private Label _categoryName;

	private TextField _searchField;

	private DropdownBox _tags;

	public readonly HashSet<string> SelectedTags = new HashSet<string>();

	public readonly List<AppMainMenu.RenderCharacterPartPreviewCommand> RenderCharacterPartPreviewCommandQueue = new List<AppMainMenu.RenderCharacterPartPreviewCommand>();

	private readonly Dictionary<string, PartPreviewComponent> _previewComponents = new Dictionary<string, PartPreviewComponent>();

	private readonly Dictionary<string, Texture> _previewCache = new Dictionary<string, Texture>();

	private Group _skinTones;

	private Button _bodyTypeMasculine;

	private Button _bodyTypeFeminine;

	private Button.ButtonStyle _bodyTypeButtonStyle;

	private Button.ButtonStyle _bodyTypeButtonSelectedStyle;

	public readonly MainMenuView MainMenuView;

	private MyAvatarPageTab _activeTab = MyAvatarPageTab.General;

	private readonly Dictionary<MyAvatarPageTab, Button> _tabButtons = new Dictionary<MyAvatarPageTab, Button>();

	private readonly SkinJsonPopup _skinJsonPopup;

	private Group _partListContainer;

	private Group _emoteListContainer;

	private Group _basicAttributesContainer;

	private Group _colorsContainer;

	private PatchStyle _undoIcon;

	private PatchStyle _undoIconDisabled;

	private PatchStyle _redoIcon;

	private PatchStyle _redoIconDisabled;

	private Button _undoButton;

	private Button _redoButton;

	private TextButton _getJsonButton;

	private TextButton _reloadButton;

	private readonly ModalDialog.DialogSetup _failedToSyncDialogSetup;

	private Button.ButtonStyle _randomizationLockedButtonStyle;

	private Button.ButtonStyle _randomizationUnlockedButtonStyle;

	private Button.ButtonStyle _matchHairColorsOnButtonStyle;

	private Button.ButtonStyle _matchHairColorsOffButtonStyle;

	private Button _bodyTypeRandomizationLock;

	private Button _skinToneRandomizationLock;

	private Button _matchHairColorsButton;

	private Button _partRandomizationLock;

	private HashSet<PlayerSkinProperty> _lockedCharacterOptionsForRandomization = new HashSet<PlayerSkinProperty>();

	private bool _matchHairColors = true;

	private Group _emotes;

	private ButtonSounds _emoteButtonSounds;

	private void Animate(float deltaTime)
	{
		if (RenderCharacterPartPreviewCommandQueue.Count > 0)
		{
			MainMenuView.MainMenu.RenderAssetPreviews(RenderCharacterPartPreviewCommandQueue.ToArray());
			RenderCharacterPartPreviewCommandQueue.Clear();
		}
	}

	public void OnPreviewRendered(CharacterPartId id, Texture texture)
	{
		if (_previewCache.TryGetValue(id.PartId, out var value))
		{
			value.Dispose();
		}
		_previewCache[id.PartId] = texture;
		_previewComponents[id.PartId].Texture = texture;
	}

	public void UpdateTags()
	{
		_tags.Entries = new List<DropdownBox.DropdownEntryInfo>();
		if (_activeTab == MyAvatarPageTab.General || _activeTab == MyAvatarPageTab.Emotes)
		{
			return;
		}
		PlayerSkinProperty property = GetProperty(_activeTab);
		List<CharacterPart> parts = Interface.App.CharacterPartStore.GetParts(property);
		List<string> tags = Interface.App.CharacterPartStore.GetTags(parts);
		List<DropdownBox.DropdownEntryInfo> list = new List<DropdownBox.DropdownEntryInfo>();
		foreach (string item in tags)
		{
			list.Add(new DropdownBox.DropdownEntryInfo(item, item));
		}
		_tags.Entries = list;
	}

	private void UpdatePartList()
	{
		_parts.Clear();
		_colorsContainer.Visible = _activeTab != MyAvatarPageTab.Face;
		if (_activeTab == MyAvatarPageTab.General || _activeTab == MyAvatarPageTab.Emotes)
		{
			return;
		}
		PlayerSkinProperty property = GetProperty(_activeTab);
		bool flag = _lockedCharacterOptionsForRandomization.Contains(property);
		_partRandomizationLock.Style = (flag ? _randomizationLockedButtonStyle : _randomizationUnlockedButtonStyle);
		_partRandomizationLock.TooltipText = Desktop.Provider.GetText(flag ? "ui.myAvatar.unlockProperty" : "ui.myAvatar.lockProperty");
		_matchHairColorsButton.Visible = property == PlayerSkinProperty.Haircut || property == PlayerSkinProperty.FacialHair || property == PlayerSkinProperty.Eyebrows;
		List<CharacterPart> parts = Interface.App.CharacterPartStore.GetParts(property);
		List<CharacterPart> list = new List<CharacterPart>();
		string text = _searchField.Value.Trim().ToLower();
		CharacterPartId selectedPartId = GetSelectedPartId(property);
		CharacterPart selectedPart = ((selectedPartId == null) ? null : parts.Find((CharacterPart p) => p.Id == selectedPartId.PartId));
		foreach (CharacterPart item in parts)
		{
			if (text != "" && !item.Name.ToLower().Contains(text))
			{
				continue;
			}
			if (SelectedTags.Count > 0)
			{
				foreach (string selectedTag in SelectedTags)
				{
					if (item.Tags != null && item.Tags.Contains(selectedTag))
					{
						list.Add(item);
						break;
					}
				}
			}
			else
			{
				list.Add(item);
			}
		}
		int num = list.Count;
		int num2 = 0;
		if (_activeTab != MyAvatarPageTab.Eyes && _activeTab != MyAvatarPageTab.Face && text == "")
		{
			num++;
			num2 = 1;
		}
		int num3 = (int)MathHelper.Max(num, _previewsPerRow * _visiblePreviewRows);
		if (num3 % _previewsPerRow != 0)
		{
			num3 += _previewsPerRow - num3 % _previewsPerRow;
		}
		Group group = null;
		int num4 = num3 / _previewsPerRow;
		if (_activeTab != MyAvatarPageTab.Eyes && _activeTab != MyAvatarPageTab.Face && text == "")
		{
			group = new Group(Desktop, _parts)
			{
				LayoutMode = LayoutMode.Left
			};
			Interface.TryGetDocument("MainMenu/MyAvatar/EmptyPart.ui", out var document);
			UIFragment uIFragment = document.Instantiate(Desktop, group);
			uIFragment.Get<Button>("Button").Activating = delegate
			{
				SelectPart(GetProperty(_activeTab), null, null, matchHairColors: false);
			};
			if (selectedPartId == null)
			{
				Button button = uIFragment.Get<Button>("Button");
				Button.ButtonStyle style = button.Style;
				Button.ButtonStyle style2 = button.Style;
				Button.ButtonStyle style3 = button.Style;
				Button.ButtonStyleState obj = new Button.ButtonStyleState
				{
					Background = _previewBackgroundSelected
				};
				Button.ButtonStyleState hovered = obj;
				style3.Pressed = obj;
				style.Default = (style2.Hovered = hovered);
				new Element(Desktop, uIFragment.Get<Group>("Container"))
				{
					Anchor = _previewSelectedFrameSize,
					Background = _previewFrameBackgroundSelected
				};
			}
			else
			{
				new Element(Desktop, uIFragment.Get<Group>("Container")).Background = _previewFrameBackground;
			}
		}
		for (int i = 0; i < num3; i++)
		{
			int num5 = i / _previewsPerRow;
			int num6 = i - num2;
			if (num6 < 0)
			{
				continue;
			}
			if (group == null)
			{
				group = new Group(Desktop, _parts)
				{
					LayoutMode = LayoutMode.Left
				};
			}
			Group parent = new Group(Desktop, group)
			{
				Anchor = new Anchor
				{
					Width = _previewWidth,
					Height = _previewHeight,
					Left = ((i % _previewsPerRow != 0) ? _previewMargin : 0),
					Bottom = ((num5 != num4 - 1) ? _previewMargin : 0)
				}
			};
			if (num6 < list.Count)
			{
				CharacterPart part = list[num6];
				CharacterPartId previewId = GetCharacterPartIdForPreview(property, part, selectedPartId);
				PartPreviewComponent partPreviewComponent = new PartPreviewComponent(this, parent, num5)
				{
					Anchor = new Anchor
					{
						Width = _previewWidth,
						Height = _previewHeight
					},
					Style = _assetButtonStyle,
					IsSelected = (selectedPartId?.PartId == part.Id),
					MaskTexturePath = new UIPath("MainMenu/MyAvatar/PartMask.png"),
					Activating = delegate
					{
						if (Desktop.IsShiftKeyDown)
						{
							EditAsset(part.Id);
						}
						else
						{
							SelectPart(GetProperty(_activeTab), part, previewId, _matchHairColors);
						}
					}
				};
				if (selectedPartId?.PartId == part.Id)
				{
					new Element(Desktop, parent)
					{
						Anchor = _previewSelectedFrameSize,
						Background = _previewFrameBackgroundSelected
					};
				}
				else
				{
					new Element(Desktop, parent).Background = _previewFrameBackground;
				}
				bool updateRender = true;
				if (_previewCache.TryGetValue(part.Id, out var value))
				{
					partPreviewComponent.Texture = value;
					updateRender = false;
				}
				_previewComponents[part.Id] = partPreviewComponent;
				partPreviewComponent.Setup(property, part, previewId, _previewBackgroundColor, _previewBackgroundColorHovered, updateRender);
			}
			if (i % _previewsPerRow == _previewsPerRow - 1)
			{
				group = null;
			}
		}
		if (selectedPart != null)
		{
			List<KeyValuePair<string, string[]>> list2 = new List<KeyValuePair<string, string[]>>();
			CharacterPartStore characterPartStore = MainMenuView.Interface.App.CharacterPartStore;
			if (selectedPart.GradientSet != null && characterPartStore.GradientSets.TryGetValue(selectedPart.GradientSet, out var value2))
			{
				foreach (KeyValuePair<string, CharacterPartTintColor> gradient in value2.Gradients)
				{
					list2.Add(new KeyValuePair<string, string[]>(gradient.Key, gradient.Value.BaseColor));
				}
			}
			if (selectedPart.Variants != null)
			{
				if (selectedPart.Variants[selectedPartId.VariantId].Textures != null)
				{
					foreach (KeyValuePair<string, CharacterPartTexture> texture in selectedPart.Variants[selectedPartId.VariantId].Textures)
					{
						list2.Add(new KeyValuePair<string, string[]>(texture.Key, texture.Value.BaseColor));
					}
				}
			}
			else if (selectedPart.Textures != null)
			{
				foreach (KeyValuePair<string, CharacterPartTexture> texture2 in selectedPart.Textures)
				{
					list2.Add(new KeyValuePair<string, string[]>(texture2.Key, texture2.Value.BaseColor));
				}
			}
			BuildColorSelection(_colors, list2, selectedPartId.ColorId, delegate(string color)
			{
				SelectPart(GetProperty(_activeTab), selectedPart, new CharacterPartId(selectedPart.Id, selectedPartId.VariantId, color), _matchHairColors);
			});
			if (selectedPart.Variants != null)
			{
				List<DropdownBox.DropdownEntryInfo> entries = selectedPart.Variants.Select((KeyValuePair<string, CharacterPartVariant> variant) => new DropdownBox.DropdownEntryInfo(Desktop.Provider.GetText("characterCreator.variants." + variant.Key), variant.Key)).ToList();
				_variantsContainer.Visible = true;
				_variants.Entries = entries;
				_variants.Value = selectedPartId.VariantId;
				_variants.ValueChanged = delegate
				{
					string text2 = selectedPartId.ColorId;
					List<string> colorOptions = Interface.App.CharacterPartStore.GetColorOptions(selectedPart, _variants.Value);
					if (!colorOptions.Contains(text2))
					{
						text2 = colorOptions.First();
					}
					SelectPart(GetProperty(_activeTab), selectedPart, new CharacterPartId(selectedPart.Id, _variants.Value, text2), _matchHairColors);
				};
			}
			else
			{
				_variantsContainer.Visible = false;
			}
		}
		else
		{
			_colors.Clear();
			_variantsContainer.Visible = false;
		}
		UpdatePartPreviewVisibilities();
	}

	private void UpdatePartPreviewVisibilities()
	{
		int num = Desktop.ScaleRound(_previewHeight) + Desktop.ScaleRound(_previewMargin);
		int num2 = System.Math.Max((int)System.Math.Floor((double)_parts.ScaledScrollOffset.Y / (double)num) - 1, 0);
		int num3 = System.Math.Min(num2 + _visiblePreviewRows - 1 + 2, _parts.Children.Count - 1);
		foreach (PartPreviewComponent value in _previewComponents.Values)
		{
			if (value.Row >= num2 && value.Row <= num3)
			{
				if (!value.IsInView)
				{
					value.IsInView = true;
					value.Update();
				}
			}
			else if (value.IsInView)
			{
				value.IsInView = false;
			}
		}
	}

	private void UpdateBodyType()
	{
		ClientPlayerSkin editedSkin = Interface.App.MainMenu.EditedSkin;
		_bodyTypeMasculine.Children.Last().Visible = editedSkin.BodyType == CharacterBodyType.Masculine;
		_bodyTypeMasculine.Style = ((editedSkin.BodyType == CharacterBodyType.Masculine) ? _bodyTypeButtonSelectedStyle : _bodyTypeButtonStyle);
		_bodyTypeFeminine.Children.Last().Visible = editedSkin.BodyType == CharacterBodyType.Feminine;
		_bodyTypeFeminine.Style = ((editedSkin.BodyType == CharacterBodyType.Feminine) ? _bodyTypeButtonSelectedStyle : _bodyTypeButtonStyle);
	}

	private void UpdateSkinTones()
	{
		string skinTone2 = Interface.App.MainMenu.EditedSkin.SkinTone;
		BuildColorSelection(_skinTones, Interface.App.CharacterPartStore.GradientSets["Skin"].Gradients.Select((KeyValuePair<string, CharacterPartTintColor> tone) => new KeyValuePair<string, string[]>(tone.Key, tone.Value.BaseColor)).ToList(), skinTone2, delegate(string skinTone)
		{
			MainMenuView.MainMenu.SetCharacterAsset(PlayerSkinProperty.SkinTone, new CharacterPartId(skinTone));
			OnCharacterChanged();
		});
	}

	public MyAvatarPage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		MainMenuView = mainMenuView;
		_skinJsonPopup = new SkinJsonPopup(this);
		_failedToSyncDialogSetup = new ModalDialog.DialogSetup
		{
			Title = "ui.myAvatar.failedToSync",
			Text = "ui.myAvatar.tryAgainLater",
			Cancellable = false
		};
	}

	protected override void OnMounted()
	{
		SetTab(MyAvatarPageTab.General);
		UpdateElements();
		MainMenuView.ShowTopBar(showTopBar: true);
		_undoButton.Disabled = true;
		_undoButton.Find<Group>("Icon").Background = _undoIconDisabled;
		_redoButton.Disabled = true;
		_redoButton.Find<Group>("Icon").Background = _redoIconDisabled;
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
		foreach (Texture value in _previewCache.Values)
		{
			value.Dispose();
		}
		_previewCache.Clear();
		RenderCharacterPartPreviewCommandQueue.Clear();
		MainMenuView.MainMenu.ClearSkinEditHistory();
		if (MainMenuView.MainMenu.HasUnsavedSkinChanges())
		{
			MainMenuView.MainMenu.CancelCharacter();
		}
	}

	public void OnFailedToSync(Exception exception)
	{
		Interface.ModalDialog.Setup(_failedToSyncDialogSetup);
		Desktop.SetLayer(4, Interface.ModalDialog);
	}

	public void OnAssetsReloaded()
	{
		_reloadButton.Text = "Reload Assets";
		_reloadButton.Disabled = false;
		if (_reloadButton.IsMounted)
		{
			_reloadButton.Parent.Layout();
		}
	}

	public void OnSetCanUndoRedoSelection(bool canUndo, bool canRedo)
	{
		_undoButton.Find<Group>("Icon").Background = (canUndo ? _undoIcon : _undoIconDisabled);
		_undoButton.Disabled = !canUndo;
		_undoButton.Layout();
		_redoButton.Find<Group>("Icon").Background = (canRedo ? _redoIcon : _redoIconDisabled);
		_redoButton.Disabled = !canRedo;
		_redoButton.Layout();
	}

	private void SetTab(MyAvatarPageTab tab)
	{
		if (tab != _activeTab)
		{
			foreach (Texture value2 in _previewCache.Values)
			{
				value2.Dispose();
			}
			_previewCache.Clear();
			RenderCharacterPartPreviewCommandQueue.Clear();
		}
		_activeTab = tab;
		foreach (KeyValuePair<MyAvatarPageTab, Button> tabButton in _tabButtons)
		{
			string texturePath = $"MainMenu/MyAvatar/CategoryIcons/{tabButton.Key}.png";
			Button value = tabButton.Value;
			value.Style.Default = new Button.ButtonStyleState
			{
				Background = new PatchStyle(texturePath)
				{
					Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 127)
				}
			};
			value.Style.Hovered = new Button.ButtonStyleState
			{
				Background = new PatchStyle(texturePath)
				{
					Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 220)
				}
			};
			value.Style.Pressed = new Button.ButtonStyleState
			{
				Background = new PatchStyle(texturePath)
				{
					Color = UInt32Color.FromRGBA(byte.MaxValue, byte.MaxValue, byte.MaxValue, 200)
				}
			};
			value.Children[0].Visible = false;
		}
		_tabButtons[tab].Style.Default = new Button.ButtonStyleState
		{
			Background = new PatchStyle($"MainMenu/MyAvatar/CategoryIcons/{tab}Selected.png")
		};
		_tabButtons[tab].Style.Hovered = null;
		_tabButtons[tab].Style.Pressed = null;
		_tabButtons[tab].Children[0].Visible = true;
		_searchField.Value = "";
		SelectedTags.Clear();
		_categoryName.Text = Desktop.Provider.GetText($"ui.myAvatar.tabs.{_activeTab}");
		_basicAttributesContainer.Visible = tab == MyAvatarPageTab.General;
		_emoteListContainer.Visible = tab == MyAvatarPageTab.Emotes;
		_partListContainer.Visible = tab != 0 && tab != MyAvatarPageTab.Emotes;
		_parts.SetScroll(0, 0);
		_emotes.SetScroll(0, 0);
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/MyAvatar/MyAvatarPage.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_previewWidth = document.ResolveNamedValue<int>(Desktop.Provider, "PartPreviewWidth");
		_previewHeight = document.ResolveNamedValue<int>(Desktop.Provider, "PartPreviewHeight");
		_previewMargin = document.ResolveNamedValue<int>(Desktop.Provider, "PartPreviewMargin");
		_previewBackgroundColor = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "PartPreviewBackgroundColor");
		_previewBackgroundColorHovered = document.ResolveNamedValue<UInt32Color>(Desktop.Provider, "PartPreviewBackgroundColorHovered");
		_previewFrameBackground = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "PartPreviewFrameBackground");
		_previewFrameBackgroundSelected = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "PartPreviewSelectedFrameBackground");
		_previewBackgroundSelected = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "PartPreviewSelectedBackground");
		_previewsPerRow = document.ResolveNamedValue<int>(Desktop.Provider, "PartPreviewsPerRow");
		_previewSelectedFrameSize = document.ResolveNamedValue<Anchor>(Desktop.Provider, "PartPreviewSelectedFrameSize");
		_visiblePreviewRows = document.ResolveNamedValue<int>(Desktop.Provider, "PartPreviewRows");
		_assetButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "AssetButtonStyle");
		_emoteButtonSounds = document.ResolveNamedValue<ButtonSounds>(Desktop.Provider, "EmoteButtonSounds");
		_bodyTypeButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "BodyTypeButtonStyle");
		_bodyTypeButtonSelectedStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "BodyTypeButtonSelectedStyle");
		_randomizationLockedButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "RandomizationLockedButtonStyle");
		_randomizationUnlockedButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "RandomizationUnlockedButtonStyle");
		_matchHairColorsOnButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "MatchHairColorsOnButtonStyle");
		_matchHairColorsOffButtonStyle = document.ResolveNamedValue<Button.ButtonStyle>(Desktop.Provider, "MatchHairColorsOffButtonStyle");
		_lockedCharacterOptionsForRandomization.Clear();
		_matchHairColors = true;
		_bodyTypeRandomizationLock = uIFragment.Get<Button>("BodyTypeRandomizationLock");
		_bodyTypeRandomizationLock.Activating = delegate
		{
			ToggleRandomizationLock(PlayerSkinProperty.BodyType, _bodyTypeRandomizationLock);
		};
		_skinToneRandomizationLock = uIFragment.Get<Button>("SkinToneRandomizationLock");
		_skinToneRandomizationLock.Activating = delegate
		{
			ToggleRandomizationLock(PlayerSkinProperty.SkinTone, _skinToneRandomizationLock);
		};
		_partRandomizationLock = uIFragment.Get<Button>("PartRandomizationLock");
		_partRandomizationLock.Activating = delegate
		{
			ToggleRandomizationLock(GetProperty(_activeTab), _partRandomizationLock);
		};
		_matchHairColorsButton = uIFragment.Get<Button>("MatchHairColorsButton");
		_matchHairColorsButton.Activating = delegate
		{
			_matchHairColors = !_matchHairColors;
			_matchHairColorsButton.Style = (_matchHairColors ? _matchHairColorsOnButtonStyle : _matchHairColorsOffButtonStyle);
			_matchHairColorsButton.TooltipText = Desktop.Provider.GetText(_matchHairColors ? "ui.myAvatar.synchronizeHairColor.disable" : "ui.myAvatar.synchronizeHairColor.enable");
			_matchHairColorsButton.Layout();
		};
		_colorsContainer = uIFragment.Get<Group>("ColorsContainer");
		_partListContainer = uIFragment.Get<Group>("PartListContainer");
		_emoteListContainer = uIFragment.Get<Group>("EmoteListContainer");
		_basicAttributesContainer = uIFragment.Get<Group>("BasicAttributesContainer");
		_undoIcon = document.ResolveNamedValue<PatchStyle>(Interface, "UndoIcon");
		_undoIconDisabled = document.ResolveNamedValue<PatchStyle>(Interface, "UndoIconDisabled");
		_redoIcon = document.ResolveNamedValue<PatchStyle>(Interface, "RedoIcon");
		_redoIconDisabled = document.ResolveNamedValue<PatchStyle>(Interface, "RedoIconDisabled");
		_undoButton = uIFragment.Get<Button>("Undo");
		_undoButton.Activating = delegate
		{
			MainMenuView.MainMenu.UndoCharacterSkinChange();
		};
		_redoButton = uIFragment.Get<Button>("Redo");
		_redoButton.Activating = delegate
		{
			MainMenuView.MainMenu.RedoCharacterSkinChange();
		};
		_getJsonButton = uIFragment.Get<TextButton>("GetJson");
		_getJsonButton.Activating = delegate
		{
			Desktop.SetLayer(2, _skinJsonPopup);
		};
		_getJsonButton.Visible = true;
		_reloadButton = uIFragment.Get<TextButton>("Reload");
		_reloadButton.Activating = delegate
		{
			_reloadButton.Text = "Reloading...";
			_reloadButton.Disabled = true;
			_reloadButton.Parent.Layout();
			MainMenuView.MainMenu.ReloadCharacterAssets();
		};
		_reloadButton.Visible = true;
		_bodyTypeMasculine = uIFragment.Get<Button>("BodyTypeMasculine");
		_bodyTypeMasculine.Activating = delegate
		{
			SetProperty(PlayerSkinProperty.BodyType, "Masculine");
		};
		_bodyTypeFeminine = uIFragment.Get<Button>("BodyTypeFeminine");
		_bodyTypeFeminine.Activating = delegate
		{
			SetProperty(PlayerSkinProperty.BodyType, "Feminine");
		};
		uIFragment.Get<TextButton>("ResetOptions").Activating = delegate
		{
			MainMenuView.MainMenu.MakeEditedSkinNaked();
		};
		uIFragment.Get<Button>("Randomize").Activating = delegate
		{
			MainMenuView.MainMenu.RandomizeCharacter(_lockedCharacterOptionsForRandomization);
		};
		uIFragment.Get<TextButton>("DiscardChanges").Activating = delegate
		{
			MainMenuView.MainMenu.CancelCharacter();
			MainMenuView.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
		};
		uIFragment.Get<TextButton>("SaveChanges").Activating = delegate
		{
			MainMenuView.MainMenu.SaveCharacter();
			MainMenuView.MainMenu.Open(AppMainMenu.MainMenuPage.Home);
		};
		_categoryName = uIFragment.Get<Label>("CategoryName");
		_parts = uIFragment.Get<Group>("Parts");
		_parts.Scrolled = UpdatePartPreviewVisibilities;
		_searchField = uIFragment.Get<TextField>("SearchField");
		_searchField.ValueChanged = delegate
		{
			UpdatePartList();
			Layout();
		};
		_tabButtons.Clear();
		foreach (MyAvatarPageTab tab in Enum.GetValues(typeof(MyAvatarPageTab)))
		{
			_tabButtons[tab] = uIFragment.Get<Group>("Tab" + tab).Find<Button>("Button");
			_tabButtons[tab].Activating = delegate
			{
				SetTab(tab);
				UpdateElements();
				Layout();
			};
		}
		_colors = uIFragment.Get<Group>("Colors");
		_variantsContainer = uIFragment.Get<Group>("VariantsContainer");
		_variants = uIFragment.Get<DropdownBox>("Variants");
		_skinTones = uIFragment.Get<Group>("SkinTones");
		_emotes = uIFragment.Get<Group>("Emotes");
		_tags = uIFragment.Get<DropdownBox>("Tags");
		_tags.ValueChanged = delegate
		{
			SelectedTags.Clear();
			foreach (string selectedValue in _tags.SelectedValues)
			{
				SelectedTags.Add(selectedValue);
			}
			UpdatePartList();
			Layout();
		};
		SetTab(_activeTab);
		BuildEmoteList();
		if (MainMenuView.MainMenu.EditedSkin != null)
		{
			UpdateElements();
		}
		_skinJsonPopup.Build();
	}

	private void UpdateElements()
	{
		if (_activeTab == MyAvatarPageTab.General)
		{
			UpdateSkinTones();
			UpdateBodyType();
		}
		else
		{
			UpdateTags();
			UpdatePartList();
		}
	}

	private void ToggleRandomizationLock(PlayerSkinProperty property, Button button)
	{
		if (!_lockedCharacterOptionsForRandomization.Contains(property))
		{
			_lockedCharacterOptionsForRandomization.Add(property);
			button.Style = _randomizationLockedButtonStyle;
			button.TooltipText = Desktop.Provider.GetText("ui.myAvatar.unlockProperty");
			button.Layout();
		}
		else
		{
			_lockedCharacterOptionsForRandomization.Remove(property);
			button.Style = _randomizationUnlockedButtonStyle;
			button.TooltipText = Desktop.Provider.GetText("ui.myAvatar.lockProperty");
			button.Layout();
		}
	}

	private void BuildColorSelection(Group container, List<KeyValuePair<string, string[]>> colors, string selectedColor, Action<string> onSelect)
	{
		container.Clear();
		Group group = null;
		Interface.TryGetDocument("MainMenu/MyAvatar/ColorOption.ui", out var document);
		int num = document.ResolveNamedValue<int>(Desktop.Provider, "ColorsPerRow");
		int num2 = 0;
		foreach (KeyValuePair<string, string[]> color in colors)
		{
			if (group == null)
			{
				group = new Group(Desktop, container)
				{
					LayoutMode = LayoutMode.Left
				};
			}
			UIFragment uIFragment = document.Instantiate(Desktop, group);
			Button button = uIFragment.Get<Button>("Button");
			button.Anchor.Left = ((num2 % num != 0) ? 8 : 0);
			button.Activating = delegate
			{
				onSelect(color.Key);
			};
			Group parent = uIFragment.Get<Group>("Colors");
			string[] value = color.Value;
			foreach (string text in value)
			{
				new Element(Desktop, parent)
				{
					FlexWeight = 1,
					Background = new PatchStyle(UInt32Color.FromHexString(text))
				};
			}
			if (selectedColor == color.Key)
			{
				uIFragment.Get<Element>("SelectedHighlight").Visible = true;
			}
			if (num2 % num == num - 1)
			{
				group = null;
			}
			num2++;
		}
	}

	public void OnCharacterChanged()
	{
		UpdateElements();
		Layout();
	}

	private void EditAsset(string assetId)
	{
		MainMenuView.MainMenu.OpenAssetIdInCosmeticEditor("Cosmetics." + GetProperty(_activeTab), assetId);
	}

	private void SetProperty(PlayerSkinProperty property, string val)
	{
		MainMenuView.MainMenu.SetCharacterAsset(property, CharacterPartId.FromString(val));
		OnCharacterChanged();
	}

	private PlayerSkinProperty GetProperty(MyAvatarPageTab tab)
	{
		return tab switch
		{
			MyAvatarPageTab.FacialHair => PlayerSkinProperty.FacialHair, 
			MyAvatarPageTab.Eyebrows => PlayerSkinProperty.Eyebrows, 
			MyAvatarPageTab.Face => PlayerSkinProperty.Face, 
			MyAvatarPageTab.Eyes => PlayerSkinProperty.Eyes, 
			MyAvatarPageTab.Haircut => PlayerSkinProperty.Haircut, 
			MyAvatarPageTab.Pants => PlayerSkinProperty.Pants, 
			MyAvatarPageTab.Overpants => PlayerSkinProperty.Overpants, 
			MyAvatarPageTab.Undertop => PlayerSkinProperty.Undertop, 
			MyAvatarPageTab.Overtop => PlayerSkinProperty.Overtop, 
			MyAvatarPageTab.Shoes => PlayerSkinProperty.Shoes, 
			MyAvatarPageTab.Gloves => PlayerSkinProperty.Gloves, 
			MyAvatarPageTab.HeadAccessory => PlayerSkinProperty.HeadAccessory, 
			MyAvatarPageTab.FaceAccessory => PlayerSkinProperty.FaceAccessory, 
			MyAvatarPageTab.EarAccessory => PlayerSkinProperty.EarAccessory, 
			_ => throw new Exception("No property for tab " + tab), 
		};
	}

	private CharacterPartId GetSelectedPartId(PlayerSkinProperty property)
	{
		ClientPlayerSkin editedSkin = MainMenuView.MainMenu.EditedSkin;
		return property switch
		{
			PlayerSkinProperty.BodyType => new CharacterPartId(editedSkin.BodyType.ToString()), 
			PlayerSkinProperty.SkinTone => new CharacterPartId(editedSkin.SkinTone), 
			PlayerSkinProperty.FacialHair => editedSkin.FacialHair, 
			PlayerSkinProperty.Eyebrows => editedSkin.Eyebrows, 
			PlayerSkinProperty.Eyes => editedSkin.Eyes, 
			PlayerSkinProperty.Face => new CharacterPartId(editedSkin.Face, editedSkin.SkinTone), 
			PlayerSkinProperty.Haircut => editedSkin.Haircut, 
			PlayerSkinProperty.Pants => editedSkin.Pants, 
			PlayerSkinProperty.Overpants => editedSkin.Overpants, 
			PlayerSkinProperty.Undertop => editedSkin.Undertop, 
			PlayerSkinProperty.Overtop => editedSkin.Overtop, 
			PlayerSkinProperty.Shoes => editedSkin.Shoes, 
			PlayerSkinProperty.Gloves => editedSkin.Gloves, 
			PlayerSkinProperty.HeadAccessory => editedSkin.HeadAccessory, 
			PlayerSkinProperty.FaceAccessory => editedSkin.FaceAccessory, 
			PlayerSkinProperty.EarAccessory => editedSkin.EarAccessory, 
			_ => null, 
		};
	}

	private CharacterPartId GetCharacterPartIdForPreview(PlayerSkinProperty property, CharacterPart part, CharacterPartId selectedPartId)
	{
		if (selectedPartId != null && part.Id == selectedPartId.PartId)
		{
			return selectedPartId;
		}
		CharacterPartStore characterPartStore = MainMenuView.Interface.App.CharacterPartStore;
		switch (property)
		{
		case PlayerSkinProperty.Haircut:
		case PlayerSkinProperty.FacialHair:
		case PlayerSkinProperty.Eyebrows:
			if (part.GradientSet != null)
			{
				Dictionary<string, CharacterPartTintColor>.KeyCollection keys2 = characterPartStore.GradientSets[part.GradientSet].Gradients.Keys;
				if (selectedPartId != null && keys2.Contains(selectedPartId.ColorId))
				{
					return new CharacterPartId(part.Id, part.Variants?.First().Key, selectedPartId.ColorId);
				}
				ClientPlayerSkin editedSkin = Interface.App.MainMenu.EditedSkin;
				if (editedSkin.Haircut != null)
				{
					selectedPartId = editedSkin.Haircut;
				}
				else if (editedSkin.FacialHair != null)
				{
					selectedPartId = editedSkin.FacialHair;
				}
				else if (editedSkin.Eyebrows != null)
				{
					selectedPartId = editedSkin.Eyebrows;
				}
				if (selectedPartId != null && keys2.Contains(selectedPartId.ColorId))
				{
					return new CharacterPartId(part.Id, part.Variants?.First().Key, selectedPartId.ColorId);
				}
			}
			break;
		case PlayerSkinProperty.Eyes:
			if (selectedPartId == null)
			{
				break;
			}
			if (part.Textures != null && part.Textures.ContainsKey(selectedPartId.ColorId))
			{
				return new CharacterPartId(part.Id, null, selectedPartId.ColorId);
			}
			if (part.GradientSet != null)
			{
				Dictionary<string, CharacterPartTintColor>.KeyCollection keys = characterPartStore.GradientSets[part.GradientSet].Gradients.Keys;
				if (keys.Contains(selectedPartId.ColorId))
				{
					return new CharacterPartId(part.Id, null, selectedPartId.ColorId);
				}
			}
			break;
		}
		if (part.GradientSet != null)
		{
			if (!characterPartStore.GradientSets.TryGetValue(part.GradientSet, out var value))
			{
				throw new Exception($"Gradient set '{part.GradientSet}' in '{property}.{part.Id}' does not exist");
			}
			if (part.Variants != null)
			{
				return new CharacterPartId(part.Id, part.Variants.First().Key, value.Gradients.First().Key);
			}
			return new CharacterPartId(part.Id, value.Gradients.First().Key);
		}
		if (part.Variants != null)
		{
			return new CharacterPartId(part.Id, part.Variants.First().Key, part.Variants.First().Value.Textures.First().Key);
		}
		if (part.Textures != null)
		{
			return new CharacterPartId(part.Id, part.Textures.First().Key);
		}
		throw new Exception("Part has no texture defined");
	}

	private bool HasCharacterPartColor(CharacterPart part, string colorId, string variantId)
	{
		CharacterPartStore characterPartStore = MainMenuView.Interface.App.CharacterPartStore;
		if (part == null)
		{
			return false;
		}
		CharacterPartVariant value;
		if (variantId == null)
		{
			if (part.Textures != null)
			{
				return part.Textures.ContainsKey(colorId);
			}
		}
		else if (part.Variants != null && part.Variants.TryGetValue(variantId, out value))
		{
			return value.Textures.ContainsKey(colorId);
		}
		if (part.GradientSet != null && characterPartStore.GradientSets.TryGetValue(part.GradientSet, out var value2))
		{
			return value2.Gradients.ContainsKey(colorId);
		}
		return false;
	}

	private void AttemptToMatchTexture(string textureId, PlayerSkinProperty property)
	{
		string matchingPartId = GetMatchingPartId(textureId, property);
		if (matchingPartId != null)
		{
			MainMenuView.MainMenu.SetCharacterAsset(property, CharacterPartId.FromString(matchingPartId), updateInterface: false);
		}
	}

	private string GetMatchingPartId(string textureId, PlayerSkinProperty property)
	{
		CharacterPartId currentSelectedId = GetSelectedPartId(property);
		if (currentSelectedId != null)
		{
			CharacterPart characterPart = Interface.App.CharacterPartStore.GetParts(property).Find((CharacterPart p) => p.Id == currentSelectedId.PartId);
			if (characterPart.Variants != null)
			{
				if (HasCharacterPartColor(characterPart, textureId, currentSelectedId.VariantId))
				{
					return CharacterPartId.BuildString(characterPart.Id, currentSelectedId.VariantId, textureId);
				}
			}
			else if (HasCharacterPartColor(characterPart, textureId, null))
			{
				return CharacterPartId.BuildString(characterPart.Id, null, textureId);
			}
		}
		return null;
	}

	private void SelectPart(PlayerSkinProperty property, CharacterPart part, CharacterPartId id, bool matchHairColors)
	{
		if (part != null)
		{
			Debug.Assert(id.ColorId != null);
			Debug.Assert((part.Variants == null && id.VariantId == null) || (part.Variants != null && id.VariantId != null));
			if (matchHairColors)
			{
				switch (property)
				{
				case PlayerSkinProperty.Haircut:
					AttemptToMatchTexture(id.ColorId, PlayerSkinProperty.Eyebrows);
					AttemptToMatchTexture(id.ColorId, PlayerSkinProperty.FacialHair);
					break;
				case PlayerSkinProperty.FacialHair:
					AttemptToMatchTexture(id.ColorId, PlayerSkinProperty.Eyebrows);
					AttemptToMatchTexture(id.ColorId, PlayerSkinProperty.Haircut);
					break;
				case PlayerSkinProperty.Eyebrows:
					AttemptToMatchTexture(id.ColorId, PlayerSkinProperty.FacialHair);
					AttemptToMatchTexture(id.ColorId, PlayerSkinProperty.Haircut);
					break;
				}
			}
		}
		MainMenuView.MainMenu.SetCharacterAsset(property, id);
		OnCharacterChanged();
	}

	private void BuildEmoteList()
	{
		_emotes.Clear();
		foreach (Emote emote in Interface.App.CharacterPartStore.Emotes)
		{
			new TextButton(Desktop, _emotes)
			{
				Text = emote.Name,
				Anchor = new Anchor
				{
					Bottom = 8,
					Height = 44
				},
				Padding = new Padding
				{
					Horizontal = 14
				},
				Style = new TextButton.TextButtonStyle
				{
					Default = new TextButton.TextButtonStyleState
					{
						Background = new PatchStyle("Common/InputBox.png")
						{
							Border = 16
						},
						LabelStyle = new LabelStyle
						{
							VerticalAlignment = LabelStyle.LabelAlignment.Center
						}
					},
					Sounds = _emoteButtonSounds
				},
				Activating = delegate
				{
					MainMenuView.MainMenu.PlayCharacterEmote(emote.Id);
				}
			};
		}
	}
}
