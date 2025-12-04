#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDL2;

namespace HytaleClient.Data.UserSettings;

internal sealed class Settings
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const int CurrentFormatVersion = 3;

	public int FormatVersion = 3;

	[JsonIgnore]
	public bool DidUndergoFormatMigration;

	public bool Fullscreen = false;

	public bool UseBorderlessForFullscreen = false;

	public bool DynamicUIScaling = true;

	public int StaticUIScale = 100;

	public bool Maximized = false;

	public bool VSync = true;

	public int FpsLimit = 240;

	public bool UnlimitedFps = false;

	public int ViewDistance = 192;

	public int FieldOfView = 70;

	public bool AutomaticRenderScale = true;

	public int RenderScale = 100;

	public int MaxChatMessages = 64;

	public BlockPlacementPreview.DisplayMode PlacementPreviewMode = BlockPlacementPreview.DisplayMode.Multipart;

	public ScreenResolution ScreenResolution = ScreenResolutions.DefaultScreenResolution;

	public bool DiagnosticMode = true;

	public InputBindings InputBindings;

	public MouseSettings MouseSettings = new MouseSettings();

	public AudioSettings AudioSettings = new AudioSettings();

	public MachinimaEditorSettings MachinimaEditorSettings = new MachinimaEditorSettings();

	public BuilderToolsSettings BuilderToolsSettings = new BuilderToolsSettings();

	public DebugSettings DebugSettings = new DebugSettings();

	public string Language;

	public ShortcutSettings ShortcutSettings = new ShortcutSettings();

	public int SavedCameraIndex = 0;

	public bool AutoJumpObstacle = true;

	public bool AutoJumpGap;

	public float JumpForceSpeedMultiplierStep = 4f;

	public float MaxJumpForceSpeedMultiplier = 30f;

	public int CreativeBlockInteractionDistance = 5;

	public bool BlockHealth;

	public bool BuilderMode;

	public bool UseBlockSubfaces = true;

	public bool DisplayBlockSubfaces = true;

	public bool DisplayBlockBoundaries = false;

	private int minimumInteractionDistance = 0;

	public int adventureInteractionDistance = 5;

	private int currentAdventureInteractionDistance = 5;

	public int creativeInteractionDistance = 30;

	private int currentCreativeInteractionDistance = 25;

	public bool BlockPlacementSupportValidation = true;

	public float ResetMouseSensitivityDuration = 1f;

	public bool _placeBlocksAtRange = true;

	public bool PlaceBlocksAtRangeInAdventureMode = false;

	public float PercentageOfPlaySelectionLengthGizmoShouldRender = 0.5f;

	public int MinPlaySelectGizmoSize = 1;

	public int MaxPlaySelectGizmoSize = 25;

	public bool Mantling = true;

	public float MinVelocityMantling = -16f;

	public float MaxVelocityMantling = 6f;

	public float MantlingCameraOffsetY = -1.6f;

	public float MantleBlockHeight = 0.8f;

	public bool SprintFovEffect = true;

	public float SprintFovIntensity = 1.175f;

	public bool ViewBobbingEffect = true;

	public float ViewBobbingIntensity = 1f;

	public bool CameraShakeEffect = true;

	public float FirstPersonCameraShakeIntensity = 1f;

	public float ThirdPersonCameraShakeIntensity = 1f;

	public bool SprintForce = true;

	public Easing.EasingType SprintAccelerationEasingType = Easing.EasingType.QuadInOut;

	public float SprintAccelerationDuration = 1.5f;

	public Easing.EasingType SprintDecelerationEasingType = Easing.EasingType.CubicOut;

	public float SprintDecelerationDuration = 1.5f;

	public bool UseNewFlyCamera = true;

	public Easing.EasingType SlideDecelerationEasingType = Easing.EasingType.QuintIn;

	public float SlideDecelerationDuration = 0.9f;

	public int StaminaLowAlertPercent = 20;

	public bool StaminaDebugInfo = false;

	public bool WeaponPullback = true;

	public bool ItemAnimationsClipGeometry = false;

	public bool ItemsClipGeometry = false;

	public bool UseOverrideFirstPersonAnimations = false;

	public int EntityUIMaxEntities = 8;

	public float EntityUIMaxDistance = 50f;

	public float EntityUIHideDelay = 1f;

	public float EntityUIFadeInDuration = 0.15f;

	public float EntityUIFadeOutDuration = 1f;

	public int PaintOperationsIgnoreHistoryLength = 5;

	public int BrushSpacingBlocks = 100;

	public bool EnableBrushSpacing = false;

	public float MountMinTurnRate = 180f;

	public float MountMaxTurnRate = 60f;

	public float MountSpeedMinTurnRate;

	public float MountSpeedMaxTurnRate = 12f;

	public Easing.EasingType MountForwardsAccelerationEasingType = Easing.EasingType.CircOut;

	public Easing.EasingType MountForwardsDecelerationEasingType = Easing.EasingType.QuadInOut;

	public Easing.EasingType MountBackwardsAccelerationEasingType = Easing.EasingType.CircOut;

	public Easing.EasingType MountBackwardsDecelerationEasingType = Easing.EasingType.CircOut;

	public float MountForwardsAccelerationDuration = 3f;

	public float MountForwardsDecelerationDuration = 3f;

	public float MountBackwardsAccelerationDuration = 0.2f;

	public float MountBackwardsDecelerationDuration = 2f;

	public float MountForcedAccelerationMultiplier = 2f;

	public float MountForcedDecelerationMultiplier = 2f;

	public bool MountRequireNewInput;

	private static readonly string SettingsPath = Path.Combine(Paths.UserData, "Settings.json");

	private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
	{
		Converters = { (JsonConverter)(object)new InputBindingJsonConverter() },
		Converters = { (JsonConverter)(object)new ShortcutSettingsJsonConverter() }
	};

	public int CurrentAdventureInteractionDistance
	{
		get
		{
			return PlaceBlocksAtRangeInAdventureMode ? currentAdventureInteractionDistance : minimumInteractionDistance;
		}
		set
		{
			currentAdventureInteractionDistance = value;
			currentAdventureInteractionDistance = MathHelper.Clamp(currentAdventureInteractionDistance, minimumInteractionDistance, adventureInteractionDistance);
		}
	}

	public int CurrentCreativeInteractionDistance
	{
		get
		{
			return _placeBlocksAtRange ? currentCreativeInteractionDistance : minimumInteractionDistance;
		}
		set
		{
			currentCreativeInteractionDistance = value;
			currentCreativeInteractionDistance = MathHelper.Clamp(currentCreativeInteractionDistance, minimumInteractionDistance, creativeInteractionDistance);
		}
	}

	public bool InteractionDistanceIsMinimum(GameMode currentMode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		if ((int)currentMode == 0)
		{
			if (PlaceBlocksAtRangeInAdventureMode)
			{
				return currentAdventureInteractionDistance == minimumInteractionDistance;
			}
			return true;
		}
		if ((int)currentMode == 1)
		{
			if (_placeBlocksAtRange)
			{
				return currentCreativeInteractionDistance == minimumInteractionDistance;
			}
			return true;
		}
		return false;
	}

	public bool PlaceBlocksAtRange(GameMode currentMode)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)currentMode == 0)
		{
			return PlaceBlocksAtRangeInAdventureMode;
		}
		return _placeBlocksAtRange;
	}

	private Settings()
	{
	}

	public static Settings MakeDefaults()
	{
		Settings settings = new Settings();
		settings.Initialize();
		return settings;
	}

	public static Settings Load()
	{
		string text;
		try
		{
			text = File.ReadAllText(SettingsPath, Encoding.UTF8);
		}
		catch (FileNotFoundException)
		{
			return MakeDefaults();
		}
		catch (Exception ex2)
		{
			Logger.Error(ex2, "Failed to load settings:");
			return MakeDefaults();
		}
		JObject val = JObject.Parse(text);
		MigrateParsedSettings(val);
		Settings settings = ((JToken)val).ToObject<Settings>(JsonSerializer.Create(SerializerSettings));
		if (settings.FormatVersion != 3)
		{
			Logger.Info<int, int>("Migrated settings from format version {0} to {1}", settings.FormatVersion, 3);
			settings.FormatVersion = 3;
			settings.DidUndergoFormatMigration = true;
		}
		settings.Initialize();
		return settings;
	}

	public static void MigrateParsedSettings(JObject parsedSettings)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		JToken val = default(JToken);
		if (parsedSettings.TryGetValue("formatVersion", ref val) || parsedSettings.TryGetValue("FormatVersion", ref val))
		{
			num = (int)(long)((JValue)val).Value;
		}
		if (num > 3)
		{
			throw new Exception("Invalid settings format version, found " + num + " but current is " + 3 + ".");
		}
		if (num == 0)
		{
			JToken val2 = default(JToken);
			JToken val3 = default(JToken);
			if (parsedSettings.TryGetValue("inputBindings", ref val2) && ((JObject)val2).TryGetValue("toggleHudVisibility", ref val3))
			{
				((JObject)val2).Add("switchHudVisibility", val3);
				((JObject)val2).Remove("toggleHudVisibility");
			}
			num = 1;
		}
		if (num == 1)
		{
			parsedSettings["VSync"] = parsedSettings["vsync"];
			parsedSettings.Remove("vsync");
			parsedSettings.Remove("DidUndergoFormatMigration");
			parsedSettings = RecursivelyCapitalize(parsedSettings);
			num = 2;
		}
		if (num == 2)
		{
			parsedSettings["InputBindings"][(object)"SwitchCameraMode"][(object)"Keycode"] = JToken.FromObject((object)(SDL_Keycode)118);
			num = 3;
		}
		static JObject RecursivelyCapitalize(JObject obj)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected O, but got Unknown
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			JObject val4 = new JObject();
			foreach (KeyValuePair<string, JToken> item in obj)
			{
				string text = item.Key[0].ToString().ToUpper() + item.Key.Substring(1);
				JToken val5 = item.Value;
				if (val5 is JObject)
				{
					val5 = (JToken)(object)RecursivelyCapitalize((JObject)val5);
				}
				val4[text] = val5;
			}
			return val4;
		}
	}

	private void Initialize()
	{
		if (InputBindings == null)
		{
			InputBindings = new InputBindings();
			InputBindings.ResetDefaults();
		}
		else
		{
			InputBindings.Setup();
		}
		AudioSettings.Initialize();
		if (DidUndergoFormatMigration)
		{
			Save();
		}
	}

	public void Save()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		Logger.Info("Saving settings...");
		string contents = JsonConvert.SerializeObject((object)this, (Formatting)1, SerializerSettings);
		File.WriteAllText(SettingsPath + ".new", contents);
		if (File.Exists(SettingsPath))
		{
			File.Replace(SettingsPath + ".new", SettingsPath, SettingsPath + ".bak");
		}
		else
		{
			File.Move(SettingsPath + ".new", SettingsPath);
		}
	}

	public Settings Clone()
	{
		Settings settings = new Settings();
		settings.PlacementPreviewMode = PlacementPreviewMode;
		settings.FormatVersion = FormatVersion;
		settings.Fullscreen = Fullscreen;
		settings.UseBorderlessForFullscreen = UseBorderlessForFullscreen;
		settings.DynamicUIScaling = DynamicUIScaling;
		settings.StaticUIScale = StaticUIScale;
		settings.Maximized = Maximized;
		settings.VSync = VSync;
		settings.FpsLimit = FpsLimit;
		settings.UnlimitedFps = UnlimitedFps;
		settings.ViewDistance = ViewDistance;
		settings.FieldOfView = FieldOfView;
		settings.SprintFovEffect = SprintFovEffect;
		settings.SprintFovIntensity = SprintFovIntensity;
		settings.ViewBobbingEffect = ViewBobbingEffect;
		settings.ViewBobbingIntensity = ViewBobbingIntensity;
		settings.CameraShakeEffect = CameraShakeEffect;
		settings.FirstPersonCameraShakeIntensity = FirstPersonCameraShakeIntensity;
		settings.ThirdPersonCameraShakeIntensity = ThirdPersonCameraShakeIntensity;
		settings.AutomaticRenderScale = AutomaticRenderScale;
		settings.RenderScale = RenderScale;
		settings.ScreenResolution = ScreenResolution;
		settings.MaxChatMessages = MaxChatMessages;
		settings.DiagnosticMode = DiagnosticMode;
		settings.InputBindings = InputBindings.Clone();
		settings.MouseSettings = MouseSettings.Clone();
		settings.AudioSettings = AudioSettings.Clone();
		settings.Language = Language;
		settings.ShortcutSettings = ShortcutSettings.Clone();
		settings.BuilderToolsSettings = BuilderToolsSettings.Clone();
		settings.DebugSettings = DebugSettings.Clone();
		settings.MachinimaEditorSettings = MachinimaEditorSettings.Clone();
		settings.AutoJumpObstacle = AutoJumpObstacle;
		settings.AutoJumpGap = AutoJumpGap;
		settings.JumpForceSpeedMultiplierStep = JumpForceSpeedMultiplierStep;
		settings.MaxJumpForceSpeedMultiplier = MaxJumpForceSpeedMultiplier;
		settings.CreativeBlockInteractionDistance = CreativeBlockInteractionDistance;
		settings.BuilderMode = BuilderMode;
		settings.adventureInteractionDistance = adventureInteractionDistance;
		settings.creativeInteractionDistance = creativeInteractionDistance;
		settings.CurrentAdventureInteractionDistance = CurrentAdventureInteractionDistance;
		settings.CurrentCreativeInteractionDistance = CurrentCreativeInteractionDistance;
		settings.Mantling = Mantling;
		settings.MinVelocityMantling = MinVelocityMantling;
		settings.MaxVelocityMantling = MaxVelocityMantling;
		settings.MantlingCameraOffsetY = MantlingCameraOffsetY;
		settings.MantleBlockHeight = MantleBlockHeight;
		settings.SprintForce = SprintForce;
		settings.SprintAccelerationEasingType = SprintAccelerationEasingType;
		settings.SprintAccelerationDuration = SprintAccelerationDuration;
		settings.SprintDecelerationEasingType = SprintDecelerationEasingType;
		settings.SprintDecelerationDuration = SprintDecelerationDuration;
		settings.StaminaLowAlertPercent = StaminaLowAlertPercent;
		settings.StaminaDebugInfo = StaminaDebugInfo;
		settings.WeaponPullback = WeaponPullback;
		settings.ItemAnimationsClipGeometry = ItemAnimationsClipGeometry;
		settings.ItemsClipGeometry = ItemsClipGeometry;
		settings.UseOverrideFirstPersonAnimations = UseOverrideFirstPersonAnimations;
		settings.UseNewFlyCamera = UseNewFlyCamera;
		settings.EntityUIMaxEntities = EntityUIMaxEntities;
		settings.EntityUIMaxDistance = EntityUIMaxDistance;
		settings.EntityUIHideDelay = EntityUIHideDelay;
		settings.EntityUIFadeInDuration = EntityUIFadeInDuration;
		settings.EntityUIFadeOutDuration = EntityUIFadeOutDuration;
		settings.SlideDecelerationDuration = SlideDecelerationDuration;
		settings.SlideDecelerationEasingType = SlideDecelerationEasingType;
		settings.UseBlockSubfaces = UseBlockSubfaces;
		settings.DisplayBlockSubfaces = DisplayBlockSubfaces;
		settings.DisplayBlockBoundaries = DisplayBlockBoundaries;
		settings._placeBlocksAtRange = _placeBlocksAtRange;
		settings.PlaceBlocksAtRangeInAdventureMode = PlaceBlocksAtRangeInAdventureMode;
		settings.BlockPlacementSupportValidation = BlockPlacementSupportValidation;
		settings.ResetMouseSensitivityDuration = ResetMouseSensitivityDuration;
		settings.PercentageOfPlaySelectionLengthGizmoShouldRender = PercentageOfPlaySelectionLengthGizmoShouldRender;
		settings.MinPlaySelectGizmoSize = MinPlaySelectGizmoSize;
		settings.MaxPlaySelectGizmoSize = MaxPlaySelectGizmoSize;
		settings.EnableBrushSpacing = EnableBrushSpacing;
		settings.BrushSpacingBlocks = BrushSpacingBlocks;
		settings.PaintOperationsIgnoreHistoryLength = PaintOperationsIgnoreHistoryLength;
		settings.MountMinTurnRate = MountMinTurnRate;
		settings.MountMaxTurnRate = MountMaxTurnRate;
		settings.MountSpeedMinTurnRate = MountSpeedMinTurnRate;
		settings.MountSpeedMaxTurnRate = MountSpeedMaxTurnRate;
		settings.MountForwardsAccelerationEasingType = MountForwardsAccelerationEasingType;
		settings.MountForwardsDecelerationEasingType = MountForwardsDecelerationEasingType;
		settings.MountBackwardsAccelerationEasingType = MountBackwardsAccelerationEasingType;
		settings.MountBackwardsDecelerationEasingType = MountBackwardsDecelerationEasingType;
		settings.MountForwardsAccelerationDuration = MountForwardsAccelerationDuration;
		settings.MountForwardsDecelerationDuration = MountForwardsDecelerationDuration;
		settings.MountBackwardsAccelerationDuration = MountBackwardsAccelerationDuration;
		settings.MountBackwardsDecelerationDuration = MountBackwardsDecelerationDuration;
		settings.MountForcedAccelerationMultiplier = MountForcedAccelerationMultiplier;
		settings.MountForcedDecelerationMultiplier = MountForcedDecelerationMultiplier;
		settings.MountRequireNewInput = MountRequireNewInput;
		return settings;
	}
}
