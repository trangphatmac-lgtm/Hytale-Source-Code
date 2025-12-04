using System;
using System.Collections.Generic;
using System.Reflection;
using HytaleClient.Core;
using Newtonsoft.Json;
using SDL2;

namespace HytaleClient.Data.UserSettings;

internal class InputBindings
{
	public InputBinding MoveForwards;

	public InputBinding MoveBackwards;

	public InputBinding StrafeLeft;

	public InputBinding StrafeRight;

	public InputBinding Jump;

	public InputBinding Crouch;

	public InputBinding ToggleCrouch;

	public InputBinding Sprint;

	public InputBinding ToggleSprint;

	public InputBinding Walk;

	public InputBinding ToggleWalk;

	public InputBinding FlyUp;

	public InputBinding FlyDown;

	public InputBinding Chat;

	public InputBinding Command;

	public InputBinding ShowPlayerList;

	public InputBinding ShowUtilitySlotSelector;

	public InputBinding ShowConsumableSlotSelector;

	public InputBinding ToggleBuilderToolsLegend;

	public InputBinding OpenInventory;

	public InputBinding OpenMachinimaEditor;

	public InputBinding OpenMap;

	public InputBinding OpenToolsSettings;

	public InputBinding OpenAssetEditor;

	public InputBinding OpenDevTools;

	public InputBinding DropItem;

	public InputBinding SwitchCameraMode;

	public InputBinding ActivateCameraRotation;

	public InputBinding ToggleProfiling;

	public InputBinding SwitchHudVisibility;

	public InputBinding ToggleFullscreen;

	public InputBinding TakeScreenshot;

	public InputBinding DecreaseSpeedMultiplier;

	public InputBinding IncreaseSpeedMultiplier;

	public InputBinding ToggleCreativeCollision;

	public InputBinding ToggleFlyCamera;

	public InputBinding ToggleFlyCameraControlTarget;

	public InputBinding BlockInteractAction;

	public InputBinding PrimaryItemAction;

	public InputBinding SecondaryItemAction;

	public InputBinding TertiaryItemAction;

	public InputBinding Ability1ItemAction;

	public InputBinding Ability2ItemAction;

	public InputBinding Ability3ItemAction;

	public InputBinding PickBlock;

	public InputBinding UndoItemAction;

	public InputBinding RedoItemAction;

	public InputBinding AddRemoveFavoriteMaterialItemAction;

	public InputBinding DismountAction;

	public InputBinding HotbarSlot1;

	public InputBinding HotbarSlot2;

	public InputBinding HotbarSlot3;

	public InputBinding HotbarSlot4;

	public InputBinding HotbarSlot5;

	public InputBinding HotbarSlot6;

	public InputBinding HotbarSlot7;

	public InputBinding HotbarSlot8;

	public InputBinding HotbarSlot9;

	public InputBinding ToolPaintBrush;

	public InputBinding ToolSculptBrush;

	public InputBinding ToolSelectionTool;

	public InputBinding ToolPaste;

	public InputBinding ToolLine;

	public InputBinding TogglePreRotationMode;

	public InputBinding TogglePostRotationMode;

	public InputBinding NextRotationAxis;

	public InputBinding PreviousRotationAxis;

	public InputBinding AlternatePlaySculptBrushModeModifier;

	public InputBinding NextBrushLockAxisOrPlane;

	public InputBinding NextBrushLockMode;

	public InputBinding UsePaintModeForBrush;

	public InputBinding SelectBlockFromSet;

	public InputBinding PastePreview;

	[JsonIgnore]
	public List<InputBinding> AllBindings;

	public void ResetDefaults()
	{
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType == typeof(InputBinding))
			{
				fieldInfo.SetValue(this, null);
			}
		}
		Setup();
	}

	public void Setup()
	{
		if (SDL.SDL_WasInit(32u) == 0)
		{
			throw new Exception("The SDL video subsystem must be initialized to access keyboard layout");
		}
		MoveForwards = MoveForwards ?? InputBinding.FromScancode((SDL_Scancode)26);
		MoveBackwards = MoveBackwards ?? InputBinding.FromScancode((SDL_Scancode)22);
		StrafeLeft = StrafeLeft ?? InputBinding.FromScancode((SDL_Scancode)4);
		StrafeRight = StrafeRight ?? InputBinding.FromScancode((SDL_Scancode)7);
		OpenMachinimaEditor = OpenMachinimaEditor ?? InputBinding.FromScancode((SDL_Scancode)17);
		OpenInventory = OpenInventory ?? InputBinding.FromScancode((SDL_Scancode)43);
		OpenAssetEditor = OpenAssetEditor ?? InputBinding.FromScancode((SDL_Scancode)11);
		OpenDevTools = OpenDevTools ?? InputBinding.FromScancode((SDL_Scancode)53);
		DropItem = DropItem ?? new InputBinding();
		Jump = Jump ?? InputBinding.FromKeycode((SDL_Keycode)32);
		Crouch = Crouch ?? InputBinding.FromKeycode((SDL_Keycode)1073742048);
		ToggleCrouch = ToggleCrouch ?? new InputBinding();
		Sprint = Sprint ?? InputBinding.FromKeycode((SDL_Keycode)1073742049);
		ToggleSprint = ToggleSprint ?? new InputBinding();
		Walk = Walk ?? new InputBinding();
		ToggleWalk = ToggleWalk ?? new InputBinding();
		FlyUp = FlyUp ?? new InputBinding(Jump);
		FlyDown = FlyDown ?? new InputBinding(Crouch);
		Chat = Chat ?? InputBinding.FromKeycode((SDL_Keycode)13);
		Command = Command ?? InputBinding.FromKeycode((SDL_Keycode)47);
		ShowPlayerList = ShowPlayerList ?? InputBinding.FromKeycode((SDL_Keycode)112);
		ShowUtilitySlotSelector = ShowUtilitySlotSelector ?? InputBinding.FromScancode((SDL_Scancode)29);
		ShowConsumableSlotSelector = ShowConsumableSlotSelector ?? InputBinding.FromScancode((SDL_Scancode)27);
		ToggleBuilderToolsLegend = ToggleBuilderToolsLegend ?? InputBinding.FromKeycode((SDL_Keycode)108);
		OpenMap = OpenMap ?? InputBinding.FromKeycode((SDL_Keycode)109);
		OpenToolsSettings = OpenToolsSettings ?? InputBinding.FromKeycode((SDL_Keycode)98);
		SwitchCameraMode = SwitchCameraMode ?? InputBinding.FromKeycode((SDL_Keycode)118);
		ActivateCameraRotation = ActivateCameraRotation ?? InputBinding.FromKeycode((SDL_Keycode)99);
		ToggleProfiling = ToggleProfiling ?? InputBinding.FromKeycode((SDL_Keycode)1073741888);
		SwitchHudVisibility = SwitchHudVisibility ?? InputBinding.FromKeycode((SDL_Keycode)1073741889);
		ToggleFullscreen = ToggleFullscreen ?? InputBinding.FromKeycode((SDL_Keycode)1073741892);
		TakeScreenshot = TakeScreenshot ?? InputBinding.FromKeycode((SDL_Keycode)1073741893);
		DecreaseSpeedMultiplier = DecreaseSpeedMultiplier ?? InputBinding.FromKeycode((SDL_Keycode)1073741882);
		IncreaseSpeedMultiplier = IncreaseSpeedMultiplier ?? InputBinding.FromKeycode((SDL_Keycode)1073741883);
		ToggleCreativeCollision = ToggleCreativeCollision ?? InputBinding.FromKeycode((SDL_Keycode)1073741884);
		ToggleFlyCamera = ToggleFlyCamera ?? InputBinding.FromKeycode((SDL_Keycode)1073741885);
		ToggleFlyCameraControlTarget = ToggleFlyCameraControlTarget ?? InputBinding.FromKeycode((SDL_Keycode)106);
		BlockInteractAction = BlockInteractAction ?? InputBinding.FromKeycode((SDL_Keycode)102);
		PrimaryItemAction = PrimaryItemAction ?? InputBinding.FromMouseButton(Input.MouseButton.SDL_BUTTON_LEFT);
		SecondaryItemAction = SecondaryItemAction ?? InputBinding.FromMouseButton(Input.MouseButton.SDL_BUTTON_RIGHT);
		TertiaryItemAction = TertiaryItemAction ?? InputBinding.FromKeycode((SDL_Keycode)107);
		Ability1ItemAction = Ability1ItemAction ?? InputBinding.FromKeycode((SDL_Keycode)113);
		Ability2ItemAction = Ability2ItemAction ?? InputBinding.FromKeycode((SDL_Keycode)101);
		Ability3ItemAction = Ability3ItemAction ?? InputBinding.FromKeycode((SDL_Keycode)114);
		PickBlock = PickBlock ?? InputBinding.FromMouseButton(Input.MouseButton.SDL_BUTTON_MIDDLE);
		HotbarSlot1 = HotbarSlot1 ?? InputBinding.FromKeycode((SDL_Keycode)49);
		HotbarSlot2 = HotbarSlot2 ?? InputBinding.FromKeycode((SDL_Keycode)50);
		HotbarSlot3 = HotbarSlot3 ?? InputBinding.FromKeycode((SDL_Keycode)51);
		HotbarSlot4 = HotbarSlot4 ?? InputBinding.FromKeycode((SDL_Keycode)52);
		HotbarSlot5 = HotbarSlot5 ?? InputBinding.FromKeycode((SDL_Keycode)53);
		HotbarSlot6 = HotbarSlot6 ?? InputBinding.FromKeycode((SDL_Keycode)54);
		HotbarSlot7 = HotbarSlot7 ?? InputBinding.FromKeycode((SDL_Keycode)55);
		HotbarSlot8 = HotbarSlot8 ?? InputBinding.FromKeycode((SDL_Keycode)56);
		HotbarSlot9 = HotbarSlot9 ?? InputBinding.FromKeycode((SDL_Keycode)57);
		ToolPaintBrush = ToolPaintBrush ?? InputBinding.FromKeycode((SDL_Keycode)1073741913);
		ToolSculptBrush = ToolSculptBrush ?? InputBinding.FromKeycode((SDL_Keycode)1073741914);
		ToolSelectionTool = ToolSelectionTool ?? InputBinding.FromKeycode((SDL_Keycode)1073741915);
		ToolPaste = ToolPaste ?? InputBinding.FromKeycode((SDL_Keycode)1073741916);
		ToolLine = ToolLine ?? InputBinding.FromKeycode((SDL_Keycode)1073741917);
		UndoItemAction = UndoItemAction ?? InputBinding.FromKeycode((SDL_Keycode)122);
		RedoItemAction = RedoItemAction ?? InputBinding.FromKeycode((SDL_Keycode)121);
		AddRemoveFavoriteMaterialItemAction = AddRemoveFavoriteMaterialItemAction ?? new InputBinding();
		DismountAction = DismountAction ?? InputBinding.FromKeycode((SDL_Keycode)102);
		NextRotationAxis = NextRotationAxis ?? InputBinding.FromKeycode((SDL_Keycode)103);
		PreviousRotationAxis = PreviousRotationAxis ?? InputBinding.FromKeycode((SDL_Keycode)98);
		TogglePreRotationMode = TogglePreRotationMode ?? InputBinding.FromKeycode((SDL_Keycode)116);
		TogglePostRotationMode = TogglePostRotationMode ?? InputBinding.FromKeycode((SDL_Keycode)121);
		AlternatePlaySculptBrushModeModifier = AlternatePlaySculptBrushModeModifier ?? InputBinding.FromKeycode((SDL_Keycode)1073742049);
		NextBrushLockAxisOrPlane = NextBrushLockAxisOrPlane ?? InputBinding.FromKeycode((SDL_Keycode)46);
		NextBrushLockMode = NextBrushLockMode ?? InputBinding.FromKeycode((SDL_Keycode)44);
		UsePaintModeForBrush = UsePaintModeForBrush ?? InputBinding.FromKeycode((SDL_Keycode)117);
		SelectBlockFromSet = SelectBlockFromSet ?? InputBinding.FromKeycode((SDL_Keycode)113);
		PastePreview = PastePreview ?? InputBinding.FromKeycode((SDL_Keycode)101);
		FieldInfo[] fields = GetType().GetFields();
		AllBindings = new List<InputBinding>();
		for (int i = 0; i < fields.Length; i++)
		{
			FieldInfo fieldInfo = fields[i];
			if (fieldInfo.FieldType == typeof(InputBinding))
			{
				InputBinding inputBinding = (InputBinding)fieldInfo.GetValue(this);
				inputBinding.Id = i;
				AllBindings.Add(inputBinding);
			}
		}
	}

	public InputBinding GetHotbarSlot(int slot)
	{
		return slot switch
		{
			0 => HotbarSlot1, 
			1 => HotbarSlot2, 
			2 => HotbarSlot3, 
			3 => HotbarSlot4, 
			4 => HotbarSlot5, 
			5 => HotbarSlot6, 
			6 => HotbarSlot7, 
			7 => HotbarSlot8, 
			8 => HotbarSlot9, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public InputBindings Clone()
	{
		InputBindings inputBindings = new InputBindings();
		inputBindings.AllBindings = new List<InputBinding>();
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.FieldType == typeof(InputBinding))
			{
				fieldInfo.SetValue(inputBindings, fieldInfo.GetValue(this));
				inputBindings.AllBindings.Add((InputBinding)fieldInfo.GetValue(this));
			}
		}
		return inputBindings;
	}
}
