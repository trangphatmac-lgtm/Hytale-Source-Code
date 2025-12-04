using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Hypixel.ProtoPlus;
using HytaleClient.Common.Memory;
using HytaleClient.Core;
using HytaleClient.Data.ClientInteraction.Client;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Map;
using HytaleClient.InGame.Commands;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.CharacterController;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Entities.Projectile;
using HytaleClient.InGame.Modules.Map;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using SDL2;

namespace HytaleClient.InGame.Modules;

internal class DebugCommandsModule : Module
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const int RowBlockCount = 32;

	private const int RowGap = 2;

	private const int ColumnGap = 3;

	private const int MaxRows = 32;

	private const int EdgePadding = 3;

	public DebugCommandsModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_gameInstance.RegisterCommand("help", _gameInstance.HelpCommand);
		_gameInstance.RegisterCommand("allblocks", AllBlocksCommand);
		_gameInstance.RegisterCommand("camoffset", CamOffsetCommand);
		_gameInstance.RegisterCommand("fog", FogCommand);
		_gameInstance.RegisterCommand("emotion", EmotionCommand);
		_gameInstance.RegisterCommand("playanimation", PlayCommand);
		_gameInstance.RegisterCommand("lod", LODCommand);
		_gameInstance.RegisterCommand("mapambientlight", MapAmbientLightCommand);
		_gameInstance.RegisterCommand("oit", OITCommand);
		_gameInstance.RegisterCommand("particle", ParticleCommand);
		_gameInstance.RegisterCommand("trail", TrailCommand);
		_gameInstance.RegisterCommand("perf", PerfCommand);
		_gameInstance.RegisterCommand("posdump", PosDumpCommand);
		_gameInstance.RegisterCommand("profiling", ProfilingCommand);
		_gameInstance.RegisterCommand("foliagefading", FoliageFadingCommand);
		_gameInstance.RegisterCommand("chunkdebug", ChunkDebugCommand);
		_gameInstance.RegisterCommand("mem", MemoryUsageCommand);
		_gameInstance.RegisterCommand("entitydebug", EntityDebugCommand);
		_gameInstance.RegisterCommand("entityeffect", EntityEffectCommand);
		_gameInstance.RegisterCommand("playernamedebug", PlayerNameDebugCommand);
		_gameInstance.RegisterCommand("entityuidebug", EntityUIDebugCommand);
		_gameInstance.RegisterCommand("chunkdump", ChunkDumpCommand);
		_gameInstance.RegisterCommand("crash", CrashCommand);
		_gameInstance.RegisterCommand("viewdistance", ViewDistanceCommand);
		_gameInstance.RegisterCommand("chunkymin", ChunkYMinCommand);
		_gameInstance.RegisterCommand("occlusion", Occlusion);
		_gameInstance.RegisterCommand("animation", AnimationCommand);
		_gameInstance.RegisterCommand("debugmap", DebugMap);
		_gameInstance.RegisterCommand("debugpixel", DebugPixelSetup);
		_gameInstance.RegisterCommand("light", LightSetup);
		_gameInstance.RegisterCommand("lbuffercompression", LightBufferCompressionSetup);
		_gameInstance.RegisterCommand("shadowmap", ShadowMappingSetup);
		_gameInstance.RegisterCommand("volsunshaft", VolumetricSunshaftSetup);
		_gameInstance.RegisterCommand("water", WaterTestSetup);
		_gameInstance.RegisterCommand("renderscale", ResolutionScaleSetup);
		_gameInstance.RegisterCommand("dof", DepthOfFieldSetup);
		_gameInstance.RegisterCommand("ssao", SSAOSetup);
		_gameInstance.RegisterCommand("bloom", BloomSetup);
		_gameInstance.RegisterCommand("blur", BlurSetup);
		_gameInstance.RegisterCommand("skyambient", SkyAmbientSetup);
		_gameInstance.RegisterCommand("caustics", UnderwaterCausticsSetup);
		_gameInstance.RegisterCommand("clouduvmotion", CloudsUVMotionSetup);
		_gameInstance.RegisterCommand("cloudshadow", CloudsShadowsSetup);
		_gameInstance.RegisterCommand("sky", Sky);
		_gameInstance.RegisterCommand("forcefield", ForceFieldSetup);
		_gameInstance.RegisterCommand("sharpen", SharpenSetup);
		_gameInstance.RegisterCommand("fxaa", FXAASetup);
		_gameInstance.RegisterCommand("taa", TAASetup);
		_gameInstance.RegisterCommand("distortion", DistortionSetup);
		_gameInstance.RegisterCommand("postfx", PostFXSetup);
		_gameInstance.RegisterCommand("movsettings", UpdateMovementSettings);
		_gameInstance.RegisterCommand("speedo", UpdateSpeedometer);
		_gameInstance.RegisterCommand("dmgindicatorangle", IndicatorAngle);
		_gameInstance.RegisterCommand("speed", SpeedCommand);
		_gameInstance.RegisterCommand("parallel", ParallelSetup);
		_gameInstance.RegisterCommand("test", TestSetup);
		_gameInstance.RegisterCommand("graphics", GraphicsSetup);
		_gameInstance.RegisterCommand("packetstats", PacketStats);
		_gameInstance.RegisterCommand("heartbeatsettings", UpdateHeartbeatSettings);
		_gameInstance.RegisterCommand("hitdetection", HitDetection);
		_gameInstance.RegisterCommand("renderplayers", RenderPlayers);
		_gameInstance.RegisterCommand("blockpreview", BlockPreview);
		_gameInstance.RegisterCommand("buildertool", BuilderTool);
		_gameInstance.RegisterCommand("forcetint", ForceTint);
		_gameInstance.RegisterCommand("warn", delegate
		{
			_gameInstance.App.DevTools.Warn("This is a warning");
		});
		_gameInstance.RegisterCommand("debugmove", DebugMove);
		_gameInstance.RegisterCommand("debugforce", delegate
		{
			ApplyForceInteraction.DebugDisplay = !ApplyForceInteraction.DebugDisplay;
			_gameInstance.Chat.Log($"Display local forces: {ApplyForceInteraction.DebugDisplay}");
		});
		_gameInstance.RegisterCommand("debugprojectile", delegate
		{
			PredictedProjectile.DebugPrediction = !PredictedProjectile.DebugPrediction;
			_gameInstance.Chat.Log($"Debug Projectile: {PredictedProjectile.DebugPrediction}");
		});
		_gameInstance.RegisterCommand("batcher", StressTestBatcher);
		_gameInstance.RegisterCommand("wireframe", WireframeCommand);
		_gameInstance.RegisterCommand("render", RenderSetup);
		_gameInstance.RegisterCommand("logdisposablesummary", LogDisposableSummaryCommand);
		_gameInstance.RegisterCommand("mem2", NativeMemoryUsageCommand);
	}

	[Usage("animation", new string[] { "[on|off]", "gpu_send_0 |gpu_send_1|gpu_send_2", "[list|reset|id] [slot]" })]
	private void AnimationCommand(string[] args)
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Expected O, but got Unknown
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0])
		{
		case "on":
			_gameInstance.Engine.AnimationSystem.SetEnabled(enable: true);
			_gameInstance.Chat.Log("Animations enabled");
			return;
		case "off":
			_gameInstance.Engine.AnimationSystem.SetEnabled(enable: false);
			_gameInstance.Chat.Log("Animations disabled");
			return;
		case "gpu_send_0":
			_gameInstance.Engine.AnimationSystem.SetTransferMethod(AnimationSystem.TransferMethod.Sequential);
			return;
		case "gpu_send_1":
			_gameInstance.Engine.AnimationSystem.SetTransferMethod(AnimationSystem.TransferMethod.ParallelSeparate);
			return;
		case "gpu_send_2":
			_gameInstance.Engine.AnimationSystem.SetTransferMethod(AnimationSystem.TransferMethod.ParallelInterleaved);
			return;
		case "list":
		{
			AnimationSlot slot = (AnimationSlot)0;
			if (args.Length > 1)
			{
				slot = (AnimationSlot)Enum.Parse(typeof(AnimationSlot), args[1].Trim(), ignoreCase: true);
			}
			List<string> animationList = _gameInstance.LocalPlayer.GetAnimationList(slot);
			_gameInstance.Chat.Log(((object)(AnimationSlot)(ref slot)).ToString() + " Animations: " + string.Join(", ", animationList.ToArray()));
			return;
		}
		}
		string text = args[0].Trim();
		AnimationSlot val = (AnimationSlot)0;
		if (text == "reset")
		{
			text = null;
		}
		if (args.Length > 1)
		{
			val = (AnimationSlot)Enum.Parse(typeof(AnimationSlot), args[1].Trim(), ignoreCase: true);
		}
		_gameInstance.InjectPacket((ProtoPacket)new PlayAnimation
		{
			EntityId = _gameInstance.LocalPlayerNetworkId,
			AnimationId = text,
			Slot = val
		});
		_gameInstance.Chat.Log((text == null) ? $"Resetting animation on slot {val}" : $"Playing Animation {text} on slot {val}");
	}

	[Usage("allblocks", new string[] { "[-c] [-r] [-w] [-s=f|b] [-o]" })]
	[Description("Displays all blocks currently loaded by the client.")]
	private void AllBlocksCommand(string[] args)
	{
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Invalid comparison between Unknown and I4
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Invalid comparison between Unknown and I4
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		bool flag = args.Contains("-c");
		bool flag2 = args.Contains("-r");
		bool flag3 = args.Contains("-w");
		bool flag4 = args.Contains("-s");
		bool sortBlocksForward = args.Contains("-s=f");
		bool flag5 = args.Contains("-s=b");
		bool flag6 = args.Contains("-o");
		if (sortBlocksForward || flag5)
		{
			flag4 = true;
		}
		Vector3 vector = new Vector3(32f, 32f, 32f);
		int num = (int)_gameInstance.LocalPlayer.Position.X;
		int num2 = (int)System.Math.Max(System.Math.Min(_gameInstance.LocalPlayer.Position.Y - 5f, ChunkHelper.Height - 10), 10f);
		int num3 = (int)_gameInstance.LocalPlayer.Position.Z;
		MapModule mapModule = _gameInstance.MapModule;
		int clientBlockIdFromName = mapModule.GetClientBlockIdFromName("Rock_Stone_Cobble");
		int num4 = mapModule.ClientBlockTypes.Count((ClientBlockType blockType) => blockType != null);
		int num5 = 0;
		if (flag || flag2 || flag6)
		{
			for (int i = 0; i < mapModule.ClientBlockTypes.Length; i++)
			{
				ClientBlockType clientBlockType = mapModule.ClientBlockTypes[i];
				if (clientBlockType == null || (flag3 && clientBlockType.FluidBlockId != 0 && (int)clientBlockType.CollisionMaterial != 2) || (flag2 && ((int)clientBlockType.RotationPitch != 0 || (int)clientBlockType.RotationYaw != 0)) || (flag && clientBlockType.FinalBlockyModel != null && clientBlockType.FinalBlockyModel.NodeCount == 1 && clientBlockType.FinalBlockyModel.AllNodes[0].Size == new Vector3(32f, 32f, 32f)) || (flag6 && clientBlockType.Name.Contains("|")))
				{
					num5++;
				}
			}
		}
		num4 -= num5;
		int num6 = (int)System.Math.Ceiling((double)num4 / 1024.0);
		int num7 = num4 % 32;
		int num8 = num4 / 32;
		int num9 = num6 * 32 + (num6 - 1) * 3 + 6;
		int num10 = 69;
		num -= num9 / 2;
		num3 -= num10 / 2;
		for (int j = 0; j < num9; j++)
		{
			for (int k = 0; k < num10; k++)
			{
				_gameInstance.MapModule.SetClientBlock(num + j, num2 - 1, num3 + k, clientBlockIdFromName);
				if ((j == 0 || k == 0 || j == num9 - 1 || k == num10 - 1) && ((j == num9 - 1 && k == 0) || (j == 0 && k == 0) || (j == 0 && k == num10 - 1) || (j == num9 - 1 && k == num10 - 1) || k == 0 || k == num10 - 1 || j == 0 || j == num9 - 1))
				{
					_gameInstance.MapModule.SetClientBlock(num + j, num2, num3 + k, clientBlockIdFromName);
				}
			}
		}
		ClientBlockType[] array = mapModule.ClientBlockTypes;
		if (flag4)
		{
			array = (ClientBlockType[])array.Clone();
			Array.Sort(array, delegate(ClientBlockType a, ClientBlockType b)
			{
				string[] array3 = a.Name.Split(new char[1] { '|' });
				string[] array4 = b.Name.Split(new char[1] { '|' });
				string[] array5 = array3[0].Split(new char[1] { '_' });
				string[] array6 = array4[0].Split(new char[1] { '_' });
				if (sortBlocksForward)
				{
					for (int m = 0; m < System.Math.Min(array5.Length, array6.Length); m++)
					{
						int num12 = string.Compare(array5[m], array6[m], StringComparison.InvariantCulture);
						if (num12 != 0)
						{
							return num12;
						}
					}
				}
				else
				{
					for (int n = 0; n < System.Math.Min(array5.Length, array6.Length); n++)
					{
						int num13 = string.Compare(array5[array5.Length - n - 1], array6[array6.Length - n - 1], StringComparison.InvariantCulture);
						if (num13 != 0)
						{
							return num13;
						}
					}
				}
				if (array5.Length != array6.Length)
				{
					return array5.Length.CompareTo(array6.Length);
				}
				if (array3.Length != array4.Length)
				{
					return array3.Length.CompareTo(array4.Length);
				}
				for (int num14 = 0; num14 < array3.Length; num14++)
				{
					int num15 = string.Compare(array3[num14], array4[num14], StringComparison.InvariantCulture);
					if (num15 != 0)
					{
						return num15;
					}
				}
				return 0;
			});
		}
		int num11 = 0;
		ClientBlockType[] array2 = array;
		foreach (ClientBlockType clientBlockType2 in array2)
		{
			if (clientBlockType2 != null && (!flag3 || clientBlockType2.FluidBlockId == 0 || (int)clientBlockType2.CollisionMaterial == 2) && (!flag2 || ((int)clientBlockType2.RotationPitch == 0 && (int)clientBlockType2.RotationYaw == 0)) && (!flag || clientBlockType2.FinalBlockyModel == null || clientBlockType2.FinalBlockyModel.NodeCount != 1 || !(clientBlockType2.FinalBlockyModel.AllNodes[0].Size == vector)) && (!flag6 || !clientBlockType2.Name.Contains("|")))
			{
				num11++;
				num6 = num11 / 1024;
				num7 = num11 % 32;
				num8 = num11 / 32;
				int j = num6 * 35 + num7 + 3;
				int k = num8 % 32 * 2 + 3;
				_gameInstance.MapModule.SetClientBlock(num + j, num2, num3 + k, clientBlockType2.Id);
			}
		}
		_gameInstance.Chat.Log($"{num4} block types placed, skipped {num5}");
	}

	[Usage("camoffset", new string[] { "[x] [y] [z]" })]
	[Description("Change the position offset of the camera.")]
	private void CamOffsetCommand(string[] args)
	{
		if (args.Length != 3)
		{
			throw new InvalidCommandUsage();
		}
		if (!float.TryParse(args[0], out var result))
		{
			throw new InvalidCommandUsage();
		}
		if (!float.TryParse(args[1], out var result2))
		{
			throw new InvalidCommandUsage();
		}
		if (!float.TryParse(args[2], out var result3))
		{
			throw new InvalidCommandUsage();
		}
		if (_gameInstance.CameraModule.Controller is ThirdPersonCameraController thirdPersonCameraController)
		{
			thirdPersonCameraController.PositionOffset = new Vector3(result, result2, result3);
		}
		else if (_gameInstance.CameraModule.Controller is FreeRotateCameraController freeRotateCameraController)
		{
			freeRotateCameraController.PositionOffset = new Vector3(result, result2, result3);
		}
		else
		{
			_gameInstance.Chat.Log("Can only be used in third-person and free rotate view!");
		}
	}

	[Usage("emotion", new string[] { "[Angry|Astonished|Fear|Laugh|Sad|Smile]" })]
	private void EmotionCommand(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		_gameInstance.LocalPlayer.SetEmotionAnimation(args[0]);
	}

	[Usage("playanimation", new string[] { "None|(<animationId> <systemId> <nodeId>)" })]
	private void PlayCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0];
		if (text == "Stop")
		{
			_gameInstance.LocalPlayer.RebuildRenderers(itemOnly: false);
			return;
		}
		string particleSystemId = ((args.Length > 1) ? args[1] : null);
		string nodeName = ((args.Length > 2) ? args[2] : null);
		_gameInstance.LocalPlayer.SetDebugAnimation(text, particleSystemId, nodeName);
	}

	[Usage("lod", new string[] { "[on|off|anim_on|anim_off||logic_on|logic_off|distance [start][range]|rotdistance [distance]]" })]
	[Description("Control the Level Of Detail mechanisms.")]
	private void LODCommand(string[] args)
	{
		if (args.Length > 3)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0])
		{
		case "on":
			_gameInstance.SetUseLOD(enable: true);
			break;
		case "off":
			_gameInstance.SetUseLOD(enable: false);
			break;
		case "anim_on":
			_gameInstance.UseAnimationLOD = true;
			break;
		case "anim_off":
			_gameInstance.UseAnimationLOD = false;
			break;
		case "logic_on":
			_gameInstance.Chat.Log("LOD enabled for logic");
			_gameInstance.EntityStoreModule.CurrentSetup.LogicalLoDUpdate = true;
			break;
		case "logic_off":
			_gameInstance.Chat.Log("LOD disabled for logic");
			_gameInstance.EntityStoreModule.CurrentSetup.LogicalLoDUpdate = false;
			break;
		case "distance":
		{
			uint result2 = 0u;
			if (uint.TryParse(args[1], out var result3))
			{
				if (args.Length > 2)
				{
					uint.TryParse(args[2], out result2);
				}
				_gameInstance.SetLODDistance(result3, result2);
				break;
			}
			throw new InvalidCommandUsage();
		}
		case "rotdistance":
		{
			if (float.TryParse(args[1], out var result))
			{
				_gameInstance.EntityStoreModule.CurrentSetup.DistanceToCameraBeforeRotation = result;
				break;
			}
			throw new InvalidCommandUsage();
		}
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.PrintLODState();
	}

	[Usage("fog", new string[] { "[dynamic|static|off| mood_[on|off]| dither_[on|off]| smooth_[on|off]]\n falloff [4-10]\n density [0-1]\n speed_factor [0-2]\n variation_scale [0-1]" })]
	[Description("Change current fog settings.")]
	private void FogCommand(string[] args)
	{
		if (args.Length < 1 || args.Length > 2)
		{
			throw new InvalidCommandUsage();
		}
		int num = 0;
		switch (args[num++].ToLower())
		{
		case "dynamic":
			_gameInstance.WeatherModule.ActiveFogMode = WeatherModule.FogMode.Dynamic;
			_gameInstance.UseFog(WeatherModule.FogMode.Dynamic);
			break;
		case "static":
			_gameInstance.WeatherModule.ActiveFogMode = WeatherModule.FogMode.Static;
			_gameInstance.UseFog(WeatherModule.FogMode.Static);
			break;
		case "off":
			_gameInstance.WeatherModule.ActiveFogMode = WeatherModule.FogMode.Off;
			_gameInstance.UseFog(WeatherModule.FogMode.Off);
			break;
		case "falloff":
		{
			if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result3))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetMoodFogHeightFalloffUnderwater(result3 * 0.01f);
			break;
		}
		case "density":
		{
			if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result5))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetMoodFogDensityUnderwater((float)System.Math.Exp(result5) - 1f);
			break;
		}
		case "variation_scale":
		{
			if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result2))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetMoodFogDensityVariationScale(result2);
			break;
		}
		case "speed_factor":
		{
			if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result6))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetMoodFogSpeedFactor(result6);
			break;
		}
		case "mood_on":
			_gameInstance.UseMoodFog(enable: true);
			break;
		case "mood_off":
			_gameInstance.UseMoodFog(enable: false);
			break;
		case "mood_sky_on":
			_gameInstance.UseMoodFogOnSky(enable: true);
			break;
		case "mood_sky_off":
			_gameInstance.UseMoodFogOnSky(enable: false);
			break;
		case "dither_on":
			_gameInstance.UseMoodFogDithering(enable: true);
			break;
		case "dither_off":
			_gameInstance.UseMoodFogDithering(enable: false);
			break;
		case "dither_sky_on":
			_gameInstance.UseMoodFogDitheringOnSkyAndClouds(enable: true);
			break;
		case "dither_sky_off":
			_gameInstance.UseMoodFogDitheringOnSkyAndClouds(enable: false);
			break;
		case "smooth_on":
			_gameInstance.UseMoodFogSmoothColor(enable: true);
			break;
		case "smooth_off":
			_gameInstance.UseMoodFogSmoothColor(enable: false);
			break;
		case "custom_on":
			_gameInstance.UseCustomMoodFog(enable: true);
			break;
		case "custom_off":
			_gameInstance.UseCustomMoodFog(enable: false);
			break;
		case "custom_density":
		{
			if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result4))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetMoodFogCustomDensity(result4);
			break;
		}
		case "custom_falloff":
		{
			if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetMoodFogHeightCustomHeightFalloff(result);
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.PrintFogState();
	}

	[Usage("mapambientlight", new string[] { "[0-0.5;default=0.05]" })]
	[Description("Change the ambient light applied to the world.")]
	private void MapAmbientLightCommand(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		if (!float.TryParse(args[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
		{
			throw new InvalidCommandUsage();
		}
		if (result < 0f || (double)result > 0.5)
		{
			throw new InvalidCommandUsage();
		}
		_gameInstance.MapModule.SetAmbientLight(result);
	}

	[Usage("entityeffect", new string[] { "add [name]", "remove [name]", "clear", "debug_display_on", "debug_display_off" })]
	[Description("Manipulate entity effects by adding, removing or clearing them.")]
	public void EntityEffectCommand(string[] args)
	{
		int num = 0;
		switch (args[num++].ToLower())
		{
		case "add":
		{
			string text2 = args[num++];
			if (!_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue(text2, out var value2))
			{
				_gameInstance.App.DevTools.Error("Could not find entity effect: " + text2);
				break;
			}
			PlayerEntity localPlayer = _gameInstance.LocalPlayer;
			int networkEffectIndex = value2;
			bool? infinite = true;
			localPlayer.AddEffect(networkEffectIndex, null, infinite);
			_gameInstance.Chat.Log("Entity effect (id: " + text2 + ") added!");
			break;
		}
		case "remove":
		{
			string text = args[num++];
			if (!_gameInstance.EntityStoreModule.EntityEffectIndicesByIds.TryGetValue(text, out var value))
			{
				_gameInstance.Chat.Error("Entity Effect not found for ID: " + text);
			}
			else if (_gameInstance.LocalPlayer.RemoveEffect(value))
			{
				_gameInstance.Chat.Log("Entity effect (id: " + text + ") removed!");
			}
			else
			{
				_gameInstance.Chat.Error("Entity has no effect: " + text);
			}
			break;
		}
		case "clear":
			_gameInstance.LocalPlayer.ClearEffects();
			break;
		case "debug_display_on":
			_gameInstance.EntityStoreModule.CurrentSetup.DisplayDebugCommandsOnEntityEffect = true;
			break;
		case "debug_display_off":
			_gameInstance.EntityStoreModule.CurrentSetup.DisplayDebugCommandsOnEntityEffect = false;
			break;
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("oit", new string[] { "off|wboit|wboit_e|poit|moit [1|2|4|8]", "lowres", "lowres_fixup_on|lowres_fixup_off" })]
	public void OITCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		int num = 0;
		switch (args[num++].ToLower())
		{
		case "chunks_on":
			_gameInstance.SetUseChunksOIT(enable: true);
			break;
		case "chunks_off":
			_gameInstance.SetUseChunksOIT(enable: false);
			break;
		case "lowres":
			_gameInstance.ChangeOITResolution();
			break;
		case "lowres_fixup_on":
			_gameInstance.UseOITEdgeFixup(fixupHalfRes: true, fixupQuarterRes: true);
			break;
		case "lowres_fixup_off":
			_gameInstance.UseOITEdgeFixup(fixupHalfRes: false, fixupQuarterRes: false);
			break;
		case "off":
			_gameInstance.SetupOIT(OrderIndependentTransparency.Method.None);
			break;
		case "wboit":
			_gameInstance.SetupOIT(OrderIndependentTransparency.Method.WBOIT);
			break;
		case "wboit_e":
			_gameInstance.SetupOIT(OrderIndependentTransparency.Method.WBOITExt);
			break;
		case "poit":
			_gameInstance.SetupOIT(OrderIndependentTransparency.Method.POIT);
			break;
		case "moit":
		{
			_gameInstance.SetupOIT(OrderIndependentTransparency.Method.MOIT);
			uint result = 1u;
			if (args.Length == 2 && uint.TryParse(args[1], out result) && !_gameInstance.SetupOITPrepassScale(result))
			{
				_gameInstance.Chat.Error(".oit " + args[0] + " - invalid parameter. \n Only valid params are 1,2,4,8.");
			}
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("particle", new string[] { "spawn [name] [quantity]", "remove [id]", "clear", "max [value]", "frustum_culling|distance_culling", "debug_overdraw|debug_quad|debug_bv|ztest" })]
	[Description("Experiment with the particle system.")]
	public void ParticleCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		int num = 0;
		string text = args[num++].ToLower();
		switch (text)
		{
		case "system_list":
			SDL.SDL_SetClipboardText(_gameInstance.ParticleSystemStoreModule.GetSystemsList());
			_gameInstance.Chat.Log("Placed ParticleSystem list in clipboard, ready to be pasted.");
			break;
		case "settings_stats":
		{
			_gameInstance.ParticleSystemStoreModule.GetSettingsStats(out var particleSystemSettingsCount, out var particleSettingsCount, out var keyframeArrayCount, out var keyframeArrayMaxSize, out var keyframeCount);
			_gameInstance.Chat.Log($"Particles settings stats:\n- particle system settings {particleSystemSettingsCount}\n- particle settings {particleSettingsCount}\n- keyframe array {keyframeArrayCount}\n- keyframe array max{keyframeArrayMaxSize}\n- keyframe {keyframeCount}");
			break;
		}
		case "pause":
			_gameInstance.ToggleParticleSimulationPaused();
			break;
		case "debug_bv":
			_gameInstance.ToggleDebugParticleBoundingVolume();
			_gameInstance.Chat.Log("BV color meaning:\n- grey: particle system not created\n- cyan: particle system created, not active\n- green: particle system created, and active");
			break;
		case "ztest":
			_gameInstance.ToggleDebugParticleZTest();
			break;
		case "debug_overdraw":
			_gameInstance.ToggleDebugParticleOverdraw();
			break;
		case "debug_tex":
		case "debug_quad":
			_gameInstance.ToggleDebugParticleTexture();
			break;
		case "debug_uvmotion":
			_gameInstance.ToggleDebugParticleUVMotion();
			break;
		case "lowres_on":
			_gameInstance.SetParticleLowResRenderingEnabled(enable: true);
			break;
		case "lowres_off":
			_gameInstance.SetParticleLowResRenderingEnabled(enable: false);
			break;
		case "culling":
		{
			bool flag = !_gameInstance.ParticleSystemStoreModule.FrustumCheck;
			_gameInstance.ParticleSystemStoreModule.FrustumCheck = flag;
			_gameInstance.ParticleSystemStoreModule.DistanceCheck = flag;
			string text2 = (flag ? "on" : "off");
			_gameInstance.Chat.Log("Particle frustum & distance Culling is " + text2 + "!");
			break;
		}
		case "frustum_culling":
		{
			_gameInstance.ParticleSystemStoreModule.FrustumCheck = !_gameInstance.ParticleSystemStoreModule.FrustumCheck;
			string text2 = (_gameInstance.ParticleSystemStoreModule.FrustumCheck ? "on" : "off");
			_gameInstance.Chat.Log("Particle frustum Culling is " + text2 + "!");
			break;
		}
		case "distance_culling":
		{
			_gameInstance.ParticleSystemStoreModule.DistanceCheck = !_gameInstance.ParticleSystemStoreModule.DistanceCheck;
			string text2 = (_gameInstance.ParticleSystemStoreModule.DistanceCheck ? "on" : "off");
			_gameInstance.Chat.Log("Particle distance Culling is " + text2 + "!");
			break;
		}
		case "proxy":
		{
			_gameInstance.ParticleSystemStoreModule.ProxyCheck = !_gameInstance.ParticleSystemStoreModule.ProxyCheck;
			string text2 = (_gameInstance.ParticleSystemStoreModule.ProxyCheck ? "on" : "off");
			_gameInstance.Chat.Log("Particle proxies are " + text2 + "!");
			break;
		}
		case "debug":
		case "spawn":
		{
			string systemId = args[num++];
			if (_gameInstance.ParticleSystemStoreModule.CheckSettingsExist(systemId))
			{
				float num4 = ((args.Length > num) ? float.Parse(args[num++], CultureInfo.InvariantCulture) : 1f);
				_gameInstance.ParticleSystemStoreModule.TrySpawnDebugSystem(systemId, _gameInstance.LocalPlayer.Position, text == "debug", (int)num4);
			}
			break;
		}
		case "clear_debug":
			_gameInstance.ParticleSystemStoreModule.ClearDebug();
			_gameInstance.Chat.Log("All Particle Debugs removed!");
			break;
		case "clear":
			_gameInstance.ParticleSystemStoreModule.Clear();
			_gameInstance.Chat.Log("All Particle Spawners removed!");
			break;
		case "max":
		{
			int num3 = int.Parse(args[num++], CultureInfo.InvariantCulture);
			_gameInstance.ParticleSystemStoreModule.SetMaxSpawnedSystems(num3);
			_gameInstance.Chat.Log($"Set MaxSpawnedSystems to {num3}");
			break;
		}
		case "remove":
		{
			int num2 = int.Parse(args[num++], CultureInfo.InvariantCulture);
			_gameInstance.ParticleSystemStoreModule.DespawnDebugSystem(num2);
			_gameInstance.Chat.Log($"Particle Spawner (id:{num2}) removed!");
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("trail", new string[] { "proxy" })]
	[Description("Experiment with the trail system.")]
	public void TrailCommand(string[] args)
	{
		if (args.Length == 0)
		{
			throw new InvalidCommandUsage();
		}
		int num = 0;
		string text = args[num++].ToLower();
		string text2 = text;
		string text3 = text2;
		if (text3 == "proxy")
		{
			_gameInstance.TrailStoreModule.ProxyCheck = !_gameInstance.TrailStoreModule.ProxyCheck;
			string text4 = (_gameInstance.TrailStoreModule.ProxyCheck ? "on" : "off");
			_gameInstance.Chat.Log("Trail proxies are " + text4 + "!");
			return;
		}
		throw new InvalidCommandUsage();
	}

	[Usage("perf", new string[] { })]
	[Description("Generate a performance report to your clipboard")]
	private void PerfCommand(string[] args)
	{
		if (args.Length != 0)
		{
			throw new InvalidCommandUsage();
		}
		string branchName = BuildInfo.BranchName;
		string arg = Marshal.PtrToStringAnsi(_gameInstance.Engine.Graphics.GL.GetString(GL.VENDOR));
		string arg2 = Marshal.PtrToStringAnsi(_gameInstance.Engine.Graphics.GL.GetString(GL.RENDERER));
		float num = (float)System.Math.Round(_gameInstance.TimeModule.GameDayProgressInHours, 2);
		int num2 = default(int);
		int num3 = default(int);
		SDL.SDL_GetWindowSize(_gameInstance.Engine.Window.Handle, ref num2, ref num3);
		SDL.SDL_SetClipboardText($"{_gameInstance.App.AuthManager.GetPlayerUuid()}\t{_gameInstance.App.Username}\t{branchName}\t" + $"{arg} {arg2}\t{_gameInstance.ProfilingModule.MeanFrameDuration}\t" + $"{_gameInstance.ProfilingModule.DrawnTriangles}\t{_gameInstance.ProfilingModule.DrawCallsCount}\t" + $"{num2} * {num3}\t" + $"{_gameInstance.LocalPlayer.Position}\t{_gameInstance.LocalPlayer.LookOrientation}\t" + $"{num}\t{_gameInstance.WeatherModule.CurrentWeather.Id}");
		_gameInstance.Chat.Log("Placed performance data in clipboard, ready to be pasted in a spreadsheet.");
	}

	[Description("Dumps position information to the chat.")]
	private void PosDumpCommand(string[] args)
	{
		ICameraController controller = _gameInstance.CameraModule.Controller;
		_gameInstance.Chat.Log("Camera Position: " + controller.Position.ToString());
		_gameInstance.Chat.Log("Camera Rotation: " + controller.Rotation.ToString());
		_gameInstance.Chat.Log("Camera Position Offset: " + controller.PositionOffset.ToString());
		_gameInstance.Chat.Log("Camera Rotation Offset: " + controller.RotationOffset.ToString());
		_gameInstance.Chat.Log("Camera Rotation MovementForceRotation: " + controller.MovementForceRotation.ToString());
		_gameInstance.Chat.Log("Camera Rotation IsFirstPerson: " + controller.IsFirstPerson);
		Entity attachedTo = controller.AttachedTo;
		_gameInstance.Chat.Log("Camera AttachedTo Position: " + attachedTo.Position.ToString());
		Chat chat = _gameInstance.Chat;
		Vector3 lookOrientation = attachedTo.LookOrientation;
		chat.Log("Camera AttachedTo Rotation: " + lookOrientation.ToString());
		_gameInstance.Chat.Log("Camera AttachedTo NetworkId: " + attachedTo.NetworkId);
		PlayerEntity localPlayer = _gameInstance.LocalPlayer;
		_gameInstance.Chat.Log("Local Player Position: " + localPlayer.Position.ToString());
		Chat chat2 = _gameInstance.Chat;
		lookOrientation = localPlayer.LookOrientation;
		chat2.Log("Local Player Rotation: " + lookOrientation.ToString());
		_gameInstance.Chat.Log("Local Player NetworkId: " + localPlayer.NetworkId);
	}

	[Usage("profiling", new string[] { "on|off [all|id_0 id_1 ... id_n]", "clear [all|id_0 id_1 ... id_n]" })]
	[Description("Change profiling modes and filter stages.")]
	private void ProfilingCommand(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		bool flag = _gameInstance.ProfilingModule.IsDetailedProfilingEnabled;
		bool flag2 = false;
		int i = 0;
		switch (args[i++].ToLower())
		{
		case "on":
			flag = true;
			break;
		case "off":
			flag = false;
			break;
		case "gpu":
			flag = true;
			_gameInstance.ProfilingModule.IsCPUOnlyRenderingProfilesEnabled = false;
			_gameInstance.ProfilingModule.IsPartialRenderingProfilesEnabled = true;
			break;
		case "cpu":
			flag = true;
			_gameInstance.ProfilingModule.IsCPUOnlyRenderingProfilesEnabled = true;
			_gameInstance.ProfilingModule.IsPartialRenderingProfilesEnabled = true;
			break;
		case "default":
			flag = true;
			_gameInstance.ProfilingModule.IsPartialRenderingProfilesEnabled = false;
			break;
		case "clear":
			flag2 = true;
			break;
		default:
			throw new InvalidCommandUsage();
		}
		Profiling profiling = _gameInstance.Engine.Profiling;
		bool flag3 = false;
		for (; i < args.Length; i++)
		{
			string text = args[i].ToLower();
			string text2 = text;
			int j;
			if (text2 == "all")
			{
				flag3 = true;
				if (_gameInstance.ProfilingModule.IsPartialRenderingProfilesEnabled)
				{
					for (j = 0; j < profiling.MeasureCount; j++)
					{
						if (profiling.GetMeasureInfo(j).HasGpuStats == !_gameInstance.ProfilingModule.IsCPUOnlyRenderingProfilesEnabled)
						{
							if (flag2)
							{
								profiling.ClearMeasure(j);
							}
							else
							{
								profiling.SetMeasureEnabled(j, flag);
							}
						}
					}
					continue;
				}
				for (j = 0; j < profiling.MeasureCount; j++)
				{
					if (flag2)
					{
						profiling.ClearMeasure(j);
					}
					else
					{
						profiling.SetMeasureEnabled(j, flag);
					}
				}
				continue;
			}
			if (args[i].Contains("-"))
			{
				string[] array = args[i].Split(new char[1] { '-' });
				int result;
				bool flag4 = int.TryParse(array[0], out result);
				int result2;
				bool flag5 = int.TryParse(array[1], out result2);
				flag3 = flag3 || (flag4 && flag5);
				if (!(flag4 && flag5))
				{
					continue;
				}
				for (j = result; j <= result2; j++)
				{
					if (flag2)
					{
						profiling.ClearMeasure(j);
					}
					else
					{
						profiling.SetMeasureEnabled(j, flag);
					}
				}
				continue;
			}
			bool flag6 = int.TryParse(args[i], out j);
			flag3 = flag3 || flag6;
			if (flag6)
			{
				if (flag2)
				{
					profiling.ClearMeasure(j);
				}
				else
				{
					profiling.SetMeasureEnabled(j, flag);
				}
			}
		}
		if (!flag3 || flag)
		{
			_gameInstance.ProfilingModule.IsDetailedProfilingEnabled = flag;
			if (!flag)
			{
				_gameInstance.ProfilingModule.IsPartialRenderingProfilesEnabled = flag;
			}
		}
	}

	[Usage("wireframe", new string[] { "off|on|entities|map_anim|map_opaque|map_alphatested|map_alphablended|sky" })]
	private void WireframeCommand(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "off":
			_gameInstance.Wireframe = GameInstance.WireframePass.Off;
			break;
		case "on":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnAll;
			break;
		case "entities":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnEntities;
			break;
		case "map_opaque":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnMapOpaque;
			break;
		case "map_alphatested":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnMapAlphaTested;
			break;
		case "map_anim":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnMapAnim;
			break;
		case "map_alphablended":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnMapAlphaBlend;
			break;
		case "sky":
			_gameInstance.Wireframe = GameInstance.WireframePass.OnSky;
			break;
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("foliagefading", new string[] { "on|off" })]
	[Description("Toggle foliage fading.")]
	private void FoliageFadingCommand(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		bool chunkUseFoliageFading;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			chunkUseFoliageFading = false;
		}
		else
		{
			chunkUseFoliageFading = true;
		}
		_gameInstance.SetChunkUseFoliageFading(chunkUseFoliageFading);
		_gameInstance.Chat.Log("Foliage fading: " + args[0]);
	}

	[Usage("chunkdebug", new string[] { "on|off" })]
	[Description("Generates a red lattice over chunk borders.")]
	private void ChunkDebugCommand(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		bool debugChunkBoundaries;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			debugChunkBoundaries = false;
		}
		else
		{
			debugChunkBoundaries = true;
		}
		_gameInstance.SetDebugChunkBoundaries(debugChunkBoundaries);
		_gameInstance.Chat.Log("Chunk debug view: " + args[0]);
	}

	[Description("Displays process memory.")]
	private void MemoryUsageCommand(string[] args)
	{
		Process currentProcess = Process.GetCurrentProcess();
		float num = (float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f;
		float num2 = (float)currentProcess.WorkingSet64 / 1024f / 1024f;
		float num3 = (float)currentProcess.PrivateMemorySize64 / 1024f / 1024f;
		_gameInstance.Chat.Log($"Managed: {num:f2} MB" + $"\nPhysical: {num2:f2} MB" + $"\nTotal committed: {num3:f2} MB");
		currentProcess.Dispose();
	}

	[Description("Displays native allocated memory.")]
	private void NativeMemoryUsageCommand(string[] args)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		BumpDebugData val = default(BumpDebugData);
		foreach (BumpDebugData value in NativeMemory.BumpDebug.Values)
		{
			val.AllocatedBytes += value.AllocatedBytes;
			val.UsedBytes += value.UsedBytes;
		}
		float num = (float)NativeMemory.NewAllocatorBytes / 1024f;
		float num2 = (float)val.AllocatedBytes / 1024f;
		float num3 = (float)val.UsedBytes / 1024f;
		float num4 = (float)(NativeMemory.NewAllocatorBytes + val.AllocatedBytes) / 1024f;
		_gameInstance.Chat.Log($"New: {num:f2} KB" + $"\nBump used/total: {num3:f2}/{num2:f2} KB" + $"\nTotal Native: {num4:f2} KB");
	}

	private void EntityDebugCommand(string[] args)
	{
		if (args.Length >= 1)
		{
			switch (args[0].ToLower())
			{
			case "ztest":
				_gameInstance.DebugEntitiesZTest = !_gameInstance.DebugEntitiesZTest;
				break;
			case "collided":
				_gameInstance.DebugCollisionOnlyCollided = !_gameInstance.DebugCollisionOnlyCollided;
				break;
			case "bounds":
				_gameInstance.EntityStoreModule.DebugInfoBounds = !_gameInstance.EntityStoreModule.DebugInfoBounds;
				break;
			default:
				throw new InvalidCommandUsage();
			}
		}
		else
		{
			_gameInstance.EntityStoreModule.DebugInfoNeedsDrawing = !_gameInstance.EntityStoreModule.DebugInfoNeedsDrawing;
		}
	}

	private void PlayerNameDebugCommand(string[] args)
	{
		_gameInstance.EntityStoreModule.CurrentSetup.DrawLocalPlayerName = !_gameInstance.EntityStoreModule.CurrentSetup.DrawLocalPlayerName;
	}

	[Description("Force drawing entity UI attachments.")]
	private void EntityUIDebugCommand(string[] args)
	{
		_gameInstance.EntityStoreModule.CurrentSetup.DebugUI = !_gameInstance.EntityStoreModule.CurrentSetup.DebugUI;
	}

	[Description("Dump chunk debug data to the log.")]
	private void ChunkDumpCommand(string[] args)
	{
		double num = System.Math.Round(_gameInstance.LocalPlayer.Position.X, 3);
		double num2 = System.Math.Round(_gameInstance.LocalPlayer.Position.Y, 3);
		double num3 = System.Math.Round(_gameInstance.LocalPlayer.Position.Z, 3);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"-- Start of chunk dump. Player feet at: ({num:##.000}, {num2:##.000}, {num3:##.000}), in column({_gameInstance.MapModule.StartChunkX}, {_gameInstance.MapModule.StartChunkZ}). View distance: {_gameInstance.App.Settings.ViewDistance}.");
		_gameInstance.MapModule.DoWithMapGeometryBuilderPaused(discardAllRenderedChunks: false, delegate
		{
			SpiralIterator spiralIterator = new SpiralIterator();
			spiralIterator.Initialize(_gameInstance.MapModule.StartChunkX, _gameInstance.MapModule.StartChunkZ, _gameInstance.MapModule.ViewRadius);
			HashSet<long> hashSet = new HashSet<long>();
			foreach (long item in spiralIterator)
			{
				if (!hashSet.Add(item))
				{
					throw new Exception("Spiral gave the same chunk column index twice.");
				}
			}
			List<long> allChunkColumnKeys = _gameInstance.MapModule.GetAllChunkColumnKeys();
			sb.AppendLine($"Chunk columns: {allChunkColumnKeys.Count}");
			sb.AppendLine($"Chunk update tasks in queue: {_gameInstance.MapModule.GetChunkUpdateTaskQueueCount()}");
			foreach (long item2 in allChunkColumnKeys)
			{
				ChunkColumn chunkColumn = _gameInstance.MapModule.GetChunkColumn(item2);
				sb.AppendLine($"  Chunk Column ({ChunkHelper.XOfChunkColumnIndex(item2)}, {ChunkHelper.ZOfChunkColumnIndex(item2)}) InSpiral: {hashSet.Contains(item2)}");
				for (int i = 0; i < ChunkHelper.ChunksPerColumn; i++)
				{
					Chunk chunk = chunkColumn.GetChunk(i);
					if (chunk == null)
					{
						sb.AppendLine($"    Chunk {i} — Not loaded");
					}
					else if (chunk.Rendered != null)
					{
						bool flag = _gameInstance.MapModule.IsChunkReadyForDraw(chunk.X, chunk.Y, chunk.Z);
						string arg = string.Format("{0}{1}", chunk.Rendered.RebuildState, (chunk.Rendered.UpdateTask != null) ? " HasUpdateTask" : "");
						string arg2 = (flag ? $" isReadyForDraw(BufferUpdateCount: {chunk.Rendered.BufferUpdateCount})" : "") ?? "";
						sb.AppendLine($"    Chunk {i} — Rendered {arg}{arg2}");
					}
					else
					{
						sb.AppendLine($"    Chunk {i} — Not rendered");
					}
				}
			}
		});
		sb.AppendLine("-- End of chunk dump.");
		Logger.Info<StringBuilder>(sb);
		_gameInstance.Chat.Log("Dumped loaded chunks to log!");
	}

	[Description("Crash the client.")]
	private void CrashCommand(string[] args)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			throw new Exception("Forcefully crashed client!");
		});
	}

	[Usage("viewdistance", new string[] { "<distance>" })]
	[Description("Change the view distance without using the settings.")]
	private void ViewDistanceCommand(string[] args)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		if (int.TryParse(args[0], out var result))
		{
			_gameInstance.App.Settings.ViewDistance = result;
			_gameInstance.Connection.SendPacket((ProtoPacket)new ViewRadius(result));
			_gameInstance.Chat.Log("View Distance set to: " + result);
		}
		else
		{
			_gameInstance.Chat.Log(args[0] + " is not a valid int!");
		}
	}

	[Usage("chunkymin", new string[] { "<y>" })]
	[Description("Only chunks above this Y will be rendered.")]
	private void ChunkYMinCommand(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		if (int.TryParse(args[0], out var result))
		{
			_gameInstance.MapModule.ChunkYMin = result;
			_gameInstance.MapModule.DoWithMapGeometryBuilderPaused(discardAllRenderedChunks: true, null);
		}
		else
		{
			_gameInstance.Chat.Log(args[0] + " is not a valid int!");
		}
	}

	[Usage("occlusion", new string[] { "[on|off|draw_on|draw_off|reproject_on|reproject_off|...]>" })]
	private void Occlusion(string[] args)
	{
		if (args.Length != 1 && args.Length != 4)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SetOcclusionCulling(enable: true);
			return;
		case "off":
			_gameInstance.SetOcclusionCulling(enable: false);
			return;
		case "draw_on":
			_gameInstance.DrawOcclusionMap = true;
			return;
		case "draw_off":
			_gameInstance.DrawOcclusionMap = false;
			return;
		case "debug_chunks_on":
			_gameInstance.DebugDrawOccludeeChunks = true;
			return;
		case "debug_chunks_off":
			_gameInstance.DebugDrawOccludeeChunks = false;
			return;
		case "debug_entities_on":
			_gameInstance.DebugDrawOccludeeEntities = true;
			return;
		case "debug_entities_off":
			_gameInstance.DebugDrawOccludeeEntities = false;
			return;
		case "debug_lights_on":
			_gameInstance.DebugDrawOccludeeLights = true;
			return;
		case "debug_lights_off":
			_gameInstance.DebugDrawOccludeeLights = false;
			return;
		case "debug_particles_on":
			_gameInstance.DebugDrawOccludeeParticles = true;
			return;
		case "debug_particles_off":
			_gameInstance.DebugDrawOccludeeParticles = false;
			return;
		case "for_entities_on":
			_gameInstance.UseOcclusionCullingForEntities = true;
			return;
		case "for_entities_off":
			_gameInstance.UseOcclusionCullingForEntities = false;
			return;
		case "for_entities_anim_on":
			_gameInstance.UseOcclusionCullingForEntitiesAnimations = true;
			return;
		case "for_entities_anim_off":
			_gameInstance.UseOcclusionCullingForEntitiesAnimations = false;
			return;
		case "for_lights_on":
			_gameInstance.UseOcclusionCullingForLights = true;
			return;
		case "for_lights_off":
			_gameInstance.UseOcclusionCullingForLights = false;
			return;
		case "for_particles_on":
			_gameInstance.UseOcclusionCullingForParticles = true;
			return;
		case "for_particles_off":
			_gameInstance.UseOcclusionCullingForParticles = false;
			return;
		case "player_on":
			_gameInstance.UseLocalPlayerOccluder = true;
			return;
		case "player_off":
			_gameInstance.UseLocalPlayerOccluder = false;
			return;
		case "plane_on":
			_gameInstance.UseChunkOccluderPlanes(enable: true);
			return;
		case "plane_off":
			_gameInstance.UseChunkOccluderPlanes(enable: false);
			return;
		case "opaque_on":
			_gameInstance.UseOpaqueChunkOccluders(enable: true);
			return;
		case "opaque_off":
			_gameInstance.UseOpaqueChunkOccluders(enable: false);
			return;
		case "alphatested_on":
			_gameInstance.UseAlphaTestedChunkOccluders(enable: true);
			return;
		case "alphatested_off":
			_gameInstance.UseAlphaTestedChunkOccluders(enable: false);
			return;
		case "reproject_on":
			_gameInstance.UseOcclusionCullingReprojection(enable: true);
			return;
		case "reproject_off":
			_gameInstance.UseOcclusionCullingReprojection(enable: false);
			return;
		case "fill_on":
			_gameInstance.UseOcclusionCullingReprojectionHoleFilling(enable: true);
			return;
		case "fill_off":
			_gameInstance.UseOcclusionCullingReprojectionHoleFilling(enable: false);
			return;
		case "break":
		{
			if (int.TryParse(args[1], out var result) && int.TryParse(args[2], out var result2) && int.TryParse(args[3], out var result3))
			{
				_gameInstance.MapModule.RegisterDestroyedBlock(result, result2, result3);
			}
			return;
		}
		}
		if (int.TryParse(args[0], out var result4))
		{
			_gameInstance.SetOpaqueOccludersCount(result4);
			return;
		}
		throw new InvalidCommandUsage();
	}

	[Usage("debugmap", new string[] { "[on|off|list|<map_name>]" })]
	private void DebugMap(string[] args)
	{
		string text = null;
		RenderTargetStore rTStore = _gameInstance.Engine.Graphics.RTStore;
		string text2 = args[0].ToLower();
		bool verticalDisplay = false;
		int num = 0;
		int num2 = 0;
		bool flag = false;
		switch (text2)
		{
		case "on":
			_gameInstance.DebugMap = true;
			break;
		case "off":
			_gameInstance.DebugMap = false;
			break;
		case "list":
			text = rTStore.GetDebugMapList();
			break;
		case "v":
			flag = true;
			verticalDisplay = true;
			break;
		case "h":
			flag = true;
			verticalDisplay = false;
			break;
		default:
		{
			string[] names = new string[1] { text2 };
			num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
			if (num2 == 1 && args.Length == 3)
			{
				string text3 = args[1];
				string text4 = text3;
				float result2;
				float result3;
				if (text4 == "scale")
				{
					float result = 1f;
					if (float.TryParse(args[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
					{
						rTStore.SetDebugMapScale(text2, result);
					}
				}
				else if (float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result2) && float.TryParse(args[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result3))
				{
					Rectangle viewport = _gameInstance.Engine.Window.Viewport;
					result2 = MathHelper.Clamp(result2, 0f, 1f);
					result3 = MathHelper.Clamp(result3, 0f, 1f);
					rTStore.SetDebugMapViewport(text2, result2, result3);
				}
			}
			if (num2 == 0)
			{
				_gameInstance.Chat.Log("Unknown map(s) name(s)");
			}
			break;
		}
		}
		if (flag)
		{
			switch (args[1])
			{
			case "preset_wboit":
			{
				string[] names = new string[3] { "blend_accu", "blend_weight", "blend_reveal" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_wboit_e":
			{
				string[] names = new string[4] { "blend_accu", "blend_weight", "blend_reveal", "blend_add" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_poit":
			{
				string[] names = new string[3] { "blend_accu", "blend_weight", "blend_beta" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_moit":
			{
				string[] names = new string[5] { "blend_moment", "blend_tod", "blend_accu", "blend_weight", "blend_reveal" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_wboit_lowres":
			{
				string[] names = new string[3] { "blend_accu_lowres", "blend_weight_lowres", "blend_reveal_lowres" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_wboit_e_lowres":
			{
				string[] names = new string[4] { "blend_accu_lowres", "blend_weight_lowres", "blend_reveal_lowres", "blend_add_lowres" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_poit_lowres":
			{
				string[] names = new string[3] { "blend_accu_lowres", "blend_weight_lowres", "blend_beta_lowres" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_moit_lowres":
			{
				string[] names = new string[5] { "blend_moment", "blend_tod", "blend_accu_lowres", "blend_weight_lowres", "blend_reveal_lowres" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_ssao":
			{
				string[] names = new string[2] { "ssao", "linear_z" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			case "preset_gbuffer":
			{
				string[] names = new string[4] { "gbuffer_albedo", "gbuffer_normal", "gbuffer_light", "gbuffer_sun" };
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			default:
			{
				num = args.Length - 1;
				string[] names = new string[num];
				for (int i = 0; i < num; i++)
				{
					string text5 = args[i + 1];
					names[i] = text5;
				}
				num2 = _gameInstance.SelectDebugMaps(names, verticalDisplay);
				break;
			}
			}
			if (num2 == 0)
			{
				_gameInstance.Chat.Log("Unknown map(s) name(s)");
			}
		}
		if (text != null)
		{
			_gameInstance.Chat.Log("Available maps:\n" + text);
		}
	}

	[Usage("debugpixel", new string[] { "[off|list|some_info_name]" })]
	private void DebugPixelSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = null;
		bool flag = false;
		string text2 = args[0].ToLower().Trim();
		string text3 = text2;
		string text4 = text3;
		if (!(text4 == "list"))
		{
			flag = ((!(text4 == "off")) ? _gameInstance.SetDebugPixelInfo(enable: true, text2) : _gameInstance.SetDebugPixelInfo(enable: false));
		}
		else
		{
			text = _gameInstance.GetDebugPixelInfoList();
		}
		if (text != null)
		{
			_gameInstance.Chat.Log("Available pixels info:\n" + text);
		}
		if (flag)
		{
			_gameInstance.Chat.Log("Debug pixel info changed to: " + args[0]);
		}
	}

	[Usage("light", new string[] { "[stencil_on|stencil_off|linear_on|linear_off|res_high|res_low|blend_add|blend_max|merge_on|merge_off|debug_on|debug_off|send_0|send_1]>" })]
	private void LightSetup(string[] args)
	{
		if (args.Length != 1 && args.Length != 4)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "debug_on":
			_gameInstance.DebugDrawLight = true;
			break;
		case "debug_off":
			_gameInstance.DebugDrawLight = false;
			break;
		case "linear_on":
			_gameInstance.SceneRenderer.SetLinearZForLight(enable: true);
			break;
		case "linear_off":
			_gameInstance.SceneRenderer.SetLinearZForLight(enable: false);
			break;
		case "res_high":
			_gameInstance.SceneRenderer.SetLightingResolution(SceneRenderer.LightingResolution.FULL);
			break;
		case "res_mix":
			_gameInstance.SceneRenderer.SetLightingResolution(SceneRenderer.LightingResolution.MIXED);
			break;
		case "res_low":
			_gameInstance.SceneRenderer.SetLightingResolution(SceneRenderer.LightingResolution.LOW);
			break;
		case "res_dynamic_on":
			_gameInstance.SceneRenderer.UseDynamicLightResolutionSelection = true;
			break;
		case "res_dynamic_off":
			_gameInstance.SceneRenderer.UseDynamicLightResolutionSelection = false;
			break;
		case "blend_add":
			_gameInstance.SceneRenderer.UseLightBlendMax = false;
			break;
		case "blend_max":
			_gameInstance.SceneRenderer.UseLightBlendMax = true;
			break;
		case "stencil_on":
			_gameInstance.SceneRenderer.ClassicDeferredLighting.UseStencilForOuterLights = true;
			break;
		case "stencil_off":
			_gameInstance.SceneRenderer.ClassicDeferredLighting.UseStencilForOuterLights = false;
			break;
		case "send_0":
			_gameInstance.SceneRenderer.ClassicDeferredLighting.LightDataTransferMethod = 0;
			break;
		case "send_1":
			_gameInstance.SceneRenderer.ClassicDeferredLighting.LightDataTransferMethod = 1;
			break;
		case "merge_on":
			_gameInstance.SceneRenderer.ClassicDeferredLighting.UseLightGroups = true;
			break;
		case "merge_off":
			_gameInstance.SceneRenderer.ClassicDeferredLighting.UseLightGroups = false;
			break;
		case "clustered_on":
			_gameInstance.UseClusteredLighting(enable: true);
			break;
		case "clustered_off":
			_gameInstance.UseClusteredLighting(enable: false);
			break;
		case "grid_custom_off":
			_gameInstance.UseClusteredLightingCustomZDistribution(enable: false);
			break;
		case "grid_direct_on":
			_gameInstance.UseClusteredLightingDirectAccess(enable: true);
			break;
		case "grid_direct_off":
			_gameInstance.UseClusteredLightingDirectAccess(enable: false);
			break;
		case "grid_refine_on":
			_gameInstance.UseClusteredLightingRefinedVoxelization(enable: true);
			break;
		case "grid_refine_off":
			_gameInstance.UseClusteredLightingRefinedVoxelization(enable: false);
			break;
		case "grid_gpumap_on":
			_gameInstance.UseClusteredLightingMappedGPUBuffers(enable: true);
			break;
		case "grid_gpumap_off":
			_gameInstance.UseClusteredLightingMappedGPUBuffers(enable: false);
			break;
		case "grid_pbo_on":
			_gameInstance.UseClusteredLightingPBO(enable: true);
			break;
		case "grid_pbo_off":
			_gameInstance.UseClusteredLightingPBO(enable: false);
			break;
		case "grid_doublebuffer_on":
			_gameInstance.UseClusteredLightingDoubleBuffering(enable: true);
			break;
		case "grid_doublebuffer_off":
			_gameInstance.UseClusteredLightingDoubleBuffering(enable: false);
			break;
		case "grid_debug_on":
			_gameInstance.SetDebugLightClusters(enable: true);
			break;
		case "grid_debug_off":
			_gameInstance.SetDebugLightClusters(enable: false);
			break;
		case "grid":
			try
			{
				uint width = uint.Parse(args[1]);
				uint height = uint.Parse(args[2]);
				uint depth = uint.Parse(args[3]);
				_gameInstance.SetClusteredLightingGridResolution(width, height, depth);
				break;
			}
			catch (Exception)
			{
				break;
			}
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("lbuffercompression", new string[] { "[on|off]" })]
	private void LightBufferCompressionSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetLightBufferCompression(enable: false);
		}
		else
		{
			_gameInstance.SetLightBufferCompression(enable: true);
		}
		_gameInstance.Chat.Log("LBuffer compression state : " + args[0]);
	}

	[Usage("shadowmap", new string[] { "on|off|", "dir_topdown|dir_sun|dir_custom [x,y,z]", "intensity [0.0-1.0]", "cascade [1|2|3|4]", "debug_cascade|debug_frustum|debug_frustum_split|debug_cascade_frustum" })]
	private void ShadowMappingSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		bool flag = true;
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SceneRenderer.SetSunShadowsEnabled(enable: true);
			break;
		case "off":
			_gameInstance.SceneRenderer.SetSunShadowsEnabled(enable: false);
			break;
		case "lod_backface_off":
			_gameInstance.SetUseShadowBackfaceLODDistance(enable: false);
			break;
		case "lod_backface_on":
			_gameInstance.SetUseShadowBackfaceLODDistance(enable: true);
			break;
		case "lod_backface_dist":
		{
			if (int.TryParse(args[1], out var result11))
			{
				_gameInstance.SetUseShadowBackfaceLODDistance(enable: true, result11);
			}
			break;
		}
		case "alphablended_on":
			_gameInstance.UseAlphaBlendedChunksSunShadows(enable: true);
			break;
		case "alphablended_off":
			_gameInstance.UseAlphaBlendedChunksSunShadows(enable: false);
			break;
		case "particle_on":
			_gameInstance.UseParticleSunShadows(enable: true);
			break;
		case "particle_off":
			_gameInstance.UseParticleSunShadows(enable: false);
			break;
		case "model_vfx":
			_gameInstance.SceneRenderer.ToggleSunShadowsWithModelVFXs();
			break;
		case "linearz_on":
			_gameInstance.SceneRenderer.SetSunShadowMappingUseLinearZ(enable: true);
			break;
		case "linearz_off":
			_gameInstance.SceneRenderer.SetSunShadowMappingUseLinearZ(enable: false);
			break;
		case "cache_on":
			_gameInstance.SceneRenderer.SetSunShadowMapCachingEnabled(enable: true);
			break;
		case "cache_off":
			_gameInstance.SceneRenderer.SetSunShadowMapCachingEnabled(enable: false);
			break;
		case "blur_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsBlurEnabled(enable: true);
			break;
		case "blur_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsBlurEnabled(enable: false);
			break;
		case "noise_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsNoiseEnabled(enable: true);
			break;
		case "noise_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsNoiseEnabled(enable: false);
			break;
		case "fade_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsFadingEnabled(enable: true);
			break;
		case "fade_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsFadingEnabled(enable: false);
			break;
		case "dir_topdown":
			_gameInstance.SceneRenderer.SetSunShadowsDirectionTopDown();
			break;
		case "dir_sun":
			_gameInstance.SceneRenderer.SetSunShadowsDirectionSun(useCleanBackFaces: false);
			break;
		case "dir_sun_clean":
			_gameInstance.SceneRenderer.SetSunShadowsDirectionSun(useCleanBackFaces: true);
			break;
		case "dir_custom":
		{
			if (args.Length == 4 && float.TryParse(args[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result8) && float.TryParse(args[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result9) && float.TryParse(args[3], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result10))
			{
				_gameInstance.SceneRenderer.SetSunShadowsDirectionCustom(new Vector3(result8, result9, result10));
				break;
			}
			flag = false;
			_gameInstance.Chat.Error(".shadowmap " + args[0] + " - invalid parameters.");
			break;
		}
		case "safe_angle_on":
			_gameInstance.SceneRenderer.SetSunShadowsSafeAngleEnabled(enable: true);
			break;
		case "safe_angle_off":
			_gameInstance.SceneRenderer.SetSunShadowsSafeAngleEnabled(enable: false);
			break;
		case "chunks_on":
			_gameInstance.SceneRenderer.SetSunShadowsWithChunks(enable: true);
			break;
		case "chunks_off":
			_gameInstance.SceneRenderer.SetSunShadowsWithChunks(enable: false);
			break;
		case "cull_underground_chunks_on":
			_gameInstance.CullUndergroundChunkShadowCasters = true;
			break;
		case "cull_underground_chunks_off":
			_gameInstance.CullUndergroundChunkShadowCasters = false;
			break;
		case "cull_underground_entities_on":
			_gameInstance.CullUndergroundEntityShadowCasters = true;
			break;
		case "cull_underground_entities_off":
			_gameInstance.CullUndergroundEntityShadowCasters = false;
			break;
		case "cull_small_entities_on":
			_gameInstance.CullSmallEntityShadowCasters = true;
			break;
		case "cull_small_entities_off":
			_gameInstance.CullSmallEntityShadowCasters = false;
			break;
		case "manual_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsManualModeEnabled(enable: true);
			break;
		case "manual_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsManualModeEnabled(enable: false);
			break;
		case "single_sample_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsWithSingleSampleEnabled(enable: true);
			break;
		case "single_sample_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsWithSingleSampleEnabled(enable: false);
			break;
		case "camera_bias_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsCameraBiasEnabled(enable: true);
			break;
		case "camera_bias_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsCameraBiasEnabled(enable: false);
			break;
		case "normal_bias_on":
			_gameInstance.SceneRenderer.SetDeferredShadowsNormalBiasEnabled(enable: true);
			break;
		case "normal_bias_off":
			_gameInstance.SceneRenderer.SetDeferredShadowsNormalBiasEnabled(enable: false);
			break;
		case "slope_scale_bias":
		{
			if (args.Length == 3 && float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result6) && float.TryParse(args[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result7))
			{
				_gameInstance.SceneRenderer.SetSunShadowsSlopeScaleBias(result6, result7);
				break;
			}
			flag = false;
			_gameInstance.Chat.Error(".shadowmap " + args[0] + " - invalid parameters.");
			break;
		}
		case "bias_1":
			_gameInstance.SceneRenderer.ToggleSunShadowsBiasMethod1();
			break;
		case "bias_2":
			_gameInstance.SceneRenderer.ToggleSunShadowsBiasMethod2();
			break;
		case "stable_on":
			_gameInstance.SceneRenderer.SetSunShadowMappingStableProjectionEnabled(enable: true);
			break;
		case "stable_off":
			_gameInstance.SceneRenderer.SetSunShadowMappingStableProjectionEnabled(enable: false);
			break;
		case "intensity":
		{
			if (float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result5) && result5 >= 0f && result5 <= 1f)
			{
				_gameInstance.SceneRenderer.SetSunShadowsIntensity(result5);
				break;
			}
			flag = false;
			_gameInstance.Chat.Error("Invalid intensity value provided.");
			break;
		}
		case "deferred_scale":
		{
			if (float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result4) && result4 > 0f && result4 <= 1f)
			{
				_gameInstance.SceneRenderer.SetDeferredShadowResolutionScale(result4);
				break;
			}
			flag = false;
			_gameInstance.Chat.Error("Invalid scale value provided.");
			break;
		}
		case "res":
		{
			if (uint.TryParse(args[1], out var result2))
			{
				uint result3 = 0u;
				uint.TryParse(args[2], out result3);
				_gameInstance.SceneRenderer.SetSunShadowMapResolution(result2, result3);
			}
			else
			{
				flag = false;
				_gameInstance.Chat.Error("Invalid resolution values provided.");
			}
			break;
		}
		case "cascade":
		{
			if (int.TryParse(args[1], out var result))
			{
				_gameInstance.SceneRenderer.SetSunShadowsCascadeCount(result);
				break;
			}
			flag = false;
			_gameInstance.Chat.Error("Invalid cascade count provided.");
			break;
		}
		case "cascade_smart":
			_gameInstance.SceneRenderer.SetSunShadowCastersSmartCascadeDispatchEnabled(!_gameInstance.SceneRenderer.UseSunShadowsSmartCascadeDispatch);
			break;
		case "kdop":
			_gameInstance.SceneRenderer.SetSunShadowsGlobalKDopEnabled(!_gameInstance.SceneRenderer.UseSunShadowsGlobalKDop);
			break;
		case "freeze":
			_gameInstance.SceneRenderer.ToggleFreeze();
			break;
		case "draw_instanced":
			_gameInstance.SceneRenderer.ToggleSunShadowCastersDrawInstanced();
			break;
		case "debug_cascade":
			_gameInstance.SceneRenderer.ToggleSunShadowMapCascadeDebug();
			break;
		case "debug_frustum":
			_gameInstance.SceneRenderer.ToggleCameraFrustumDebug();
			break;
		case "debug_frustum_split":
			_gameInstance.SceneRenderer.ToggleCameraFrustumSplitsDebug();
			break;
		case "debug_cascade_frustum":
			_gameInstance.SceneRenderer.ToggleShadowCascadeFrustumDebug();
			break;
		default:
			throw new InvalidCommandUsage();
		case "info":
		case "state":
			break;
		}
		if (flag)
		{
			string message = _gameInstance.SceneRenderer.WriteShadowMappingStateToString();
			_gameInstance.Chat.Log(message);
		}
	}

	[Usage("volsunshaft", new string[] { "on|off|", "strength <x>" })]
	private void VolumetricSunshaftSetup(string[] args)
	{
		if (args.Length < 1 || args.Length > 2)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.UseVolumetricSunshaft(enable: true);
			break;
		case "off":
			_gameInstance.UseVolumetricSunshaft(enable: false);
			break;
		case "strength":
		{
			if (float.TryParse(args[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
			{
				_gameInstance.PostEffectRenderer.SetVolumetricSunshaftStrength(result);
			}
			else
			{
				_gameInstance.Chat.Error(args[1] + " is not a valid number!");
			}
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Volumetric sunshafts " + args[0]);
	}

	[Usage("water", new string[] { "[0|1|2|3] - quality settings, for test!" })]
	private void WaterTestSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "0":
			_gameInstance.SetWaterQuality(0);
			break;
		case "1":
			_gameInstance.SetWaterQuality(1);
			break;
		case "2":
			_gameInstance.SetWaterQuality(2);
			break;
		case "3":
			_gameInstance.SetWaterQuality(3);
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Water quality state : " + args[0]);
	}

	[Usage("renderscale", new string[] { "<percentage>\n (default : 100)" })]
	private void ResolutionScaleSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		if (int.TryParse(args[0], out var result))
		{
			float resolutionScale = (float)result * 0.01f;
			if (_gameInstance.SetResolutionScale(resolutionScale))
			{
				_gameInstance.Chat.Log($"Render scale changed to {result}.");
			}
			else
			{
				_gameInstance.Chat.Log($"Invalid render scale factor {result}.\nMin-Max range is [{_gameInstance.ResolutionScaleMin * 100f} - {_gameInstance.ResolutionScaleMax * 100f}]");
			}
		}
		else
		{
			_gameInstance.Chat.Log("Invalid input : " + args[0]);
		}
	}

	[Usage("dof", new string[] { "[on | preset_1 | preset_2 | preset_3 | off | v1 | v2 | v2b | v3]" })]
	private void DepthOfFieldSetup(string[] args)
	{
		int num = 0;
		switch (args[num++].ToLower())
		{
		case "on":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.Chat.Log("depth of field is on.");
			return;
		case "preset_1":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetupDepthOfField(0f, 0f, 3f, 10f, 0f, 0.4f);
			_gameInstance.Chat.Log("depth of field preset 1");
			return;
		case "preset_2":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetupDepthOfField();
			_gameInstance.Chat.Log("depth of field preset 2");
			return;
		case "preset_3":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetupDepthOfField(2f, 15f, 1000f, 1000f, 0.5f, 0f);
			_gameInstance.Chat.Log("depth of field preset 3");
			return;
		case "off":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: false);
			_gameInstance.Chat.Log("depth of field is off.");
			return;
		case "v1":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetDepthOfFieldVersion(0);
			_gameInstance.Chat.Log("depth of field version 1");
			return;
		case "v2":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetDepthOfFieldVersion(1);
			_gameInstance.Chat.Log("depth of field version 2");
			return;
		case "v2b":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetDepthOfFieldVersion(2);
			_gameInstance.Chat.Log("depth of field version 2bis");
			return;
		case "v3":
			_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
			_gameInstance.PostEffectRenderer.SetDepthOfFieldVersion(3);
			_gameInstance.Chat.Log("depth of field version 3");
			return;
		}
		if (args.Length != 6)
		{
			throw new InvalidCommandUsage();
		}
		_gameInstance.PostEffectRenderer.UseDepthOfField(enable: true);
		bool flag = true;
		if (!float.TryParse(args[num - 1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
		{
			_gameInstance.Chat.Error(args[num - 1] + " is not a valid number!");
		}
		if (!float.TryParse(args[num], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result2))
		{
			_gameInstance.Chat.Error(args[num] + " is not a valid number!");
		}
		if (!float.TryParse(args[num + 1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result3))
		{
			_gameInstance.Chat.Error(args[num + 1] + " is not a valid number!");
		}
		if (!float.TryParse(args[num + 2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result4))
		{
			_gameInstance.Chat.Error(args[num + 2] + " is not a valid number!");
		}
		if (!float.TryParse(args[num + 3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result5))
		{
			_gameInstance.Chat.Error(args[num + 3] + " is not a valid number!");
		}
		if (!float.TryParse(args[num + 4], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result6))
		{
			_gameInstance.Chat.Error(args[num + 4] + " is not a valid number!");
		}
		_gameInstance.PostEffectRenderer.SetupDepthOfField(result, result2, result3, result4, result5, result6);
		_gameInstance.Chat.Log($"depth of field parameters set to {result} {result2} {result3} {result4} {result5} {result6}");
	}

	[Usage("ssao", new string[] { "on|off|blur_on|blur_off", "max [0.0-1.0]", "strength [0.0-10.0]", "radius [0.1-2.0]", "reset_params" })]
	private void SSAOSetup(string[] args)
	{
		if (args.Length < 1 || args.Length > 2)
		{
			throw new InvalidCommandUsage();
		}
		float result;
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: true);
			break;
		case "off":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: false);
			break;
		case "0":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: true, useTemporalFiltering: true, 0);
			break;
		case "1":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: true, useTemporalFiltering: true, 1);
			break;
		case "2":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: true, useTemporalFiltering: true, 2);
			break;
		case "temporal_on":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: true);
			break;
		case "temporal_off":
			_gameInstance.SceneRenderer.SetUseSSAO(useSSAO: true, useTemporalFiltering: false);
			break;
		case "blur_on":
			_gameInstance.SceneRenderer.UseSSAOBlur = true;
			break;
		case "blur_off":
			_gameInstance.SceneRenderer.UseSSAOBlur = false;
			break;
		case "reset_params":
			_gameInstance.SceneRenderer.ResetSSAOParameters();
			break;
		case "max":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SceneRenderer.SSAOParamOcclusionMax = result;
			break;
		case "strength":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SceneRenderer.SSAOParamOcclusionStrength = result;
			break;
		case "radius":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SceneRenderer.SSAOParamRadius = result;
			break;
		default:
			throw new InvalidCommandUsage();
		case "info":
		case "state":
			break;
		}
		string text = "SSAO state : " + args[0] + "\n";
		text += $".quality : {_gameInstance.SceneRenderer.SSAOQuality}\n";
		text += $".max : {_gameInstance.SceneRenderer.SSAOParamOcclusionMax}\n";
		text += $".strength: {_gameInstance.SceneRenderer.SSAOParamOcclusionStrength}\n";
		text += $".radius: {_gameInstance.SceneRenderer.SSAOParamRadius}";
		_gameInstance.Chat.Log(text);
	}

	[Usage("skyambient", new string[] { "[on|off|intensity X]\n (default : intensity = 0.12)" })]
	private void SkyAmbientSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SceneRenderer.SetUseSkyAmbient(enable: true);
			break;
		case "off":
			_gameInstance.SceneRenderer.SetUseSkyAmbient(enable: false);
			break;
		case "less_at_noon_on":
			_gameInstance.UseLessSkyAmbientAtNoon = true;
			break;
		case "less_at_noon_off":
			_gameInstance.UseLessSkyAmbientAtNoon = false;
			break;
		case "intensity":
		{
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetSkyAmbientIntensity(result);
			break;
		}
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Ambient from sky state : " + args[0]);
	}

	[Usage("bloom", new string[] { "[on {sun,fb,sunshaft,underwater} | off {sun,fb,sunshaft,underwater}] | intensity[0-1] | sun_intensity[0-1] | power[1-10] | sunshaft_scale[1-4] | sunshaft_intensity[0-0.5]" })]
	private void BloomSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		bool flag = true;
		int i = 0;
		switch (args[i++].ToLower())
		{
		case "on":
			_gameInstance.PostEffectRenderer.UseBloom(enable: true);
			for (; i < args.Length; i++)
			{
				switch (args[i].ToLower())
				{
				case "sun":
					_gameInstance.PostEffectRenderer.UseBloomOnSun(enable: true);
					break;
				case "moon":
					_gameInstance.PostEffectRenderer.UseBloomOnMoon(enable: true);
					break;
				case "fb":
					_gameInstance.PostEffectRenderer.UseBloomOnFullbright(enable: true);
					break;
				case "pow":
					_gameInstance.PostEffectRenderer.UseBloomOnFullscreen(enable: true);
					break;
				case "sunshaft":
					_gameInstance.PostEffectRenderer.UseBloomSunShaft(enable: true);
					break;
				case "underwater":
					_gameInstance.UseBloomUnderwater = true;
					break;
				default:
					flag = false;
					break;
				}
			}
			break;
		case "off":
			if (i == args.Length)
			{
				_gameInstance.PostEffectRenderer.UseBloom(enable: false);
				break;
			}
			for (; i < args.Length; i++)
			{
				switch (args[i].ToLower())
				{
				case "sun":
					_gameInstance.PostEffectRenderer.UseBloomOnSun(enable: false);
					break;
				case "moon":
					_gameInstance.PostEffectRenderer.UseBloomOnMoon(enable: false);
					break;
				case "fb":
					_gameInstance.PostEffectRenderer.UseBloomOnFullbright(enable: false);
					break;
				case "pow":
					_gameInstance.PostEffectRenderer.UseBloomOnFullscreen(enable: false);
					break;
				case "sunshaft":
					_gameInstance.PostEffectRenderer.UseBloomSunShaft(enable: false);
					break;
				case "underwater":
					_gameInstance.UseBloomUnderwater = false;
					break;
				default:
					flag = false;
					break;
				}
			}
			break;
		case "v0":
			_gameInstance.PostEffectRenderer.SetBloomVersion(0);
			break;
		case "v1":
			_gameInstance.PostEffectRenderer.SetBloomVersion(1);
			break;
		case "up":
		{
			if (!int.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result12))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetUpsampleMethod(result12);
			break;
		}
		case "down":
		{
			if (!int.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result8))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetDownsampleMethod(result8);
			break;
		}
		case "intensity":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result13))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetBloomGlobalIntensity(result13);
			break;
		}
		case "power":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result10))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetBloomPower(result10);
			break;
		}
		case "sunshaft_scale":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result6))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetSunshaftScaleFactor(result6);
			break;
		}
		case "sun_intensity":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result15))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetSunIntensity(result15);
			break;
		}
		case "sunshaft_intensity":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result11))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetSunshaftIntensity(result11);
			break;
		}
		case "pow_intensity":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result9))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.DefaultBloomIntensity = result9;
			_gameInstance.PostEffectRenderer.SetBloomOnPowIntensity(result9);
			break;
		}
		case "pow_power":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result7))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.DefaultBloomPower = result7;
			_gameInstance.PostEffectRenderer.SetBloomOnPowPower(result7);
			break;
		}
		case "underwater_intensity":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result16))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.UnderwaterBloomIntensity = result16;
			_gameInstance.PostEffectRenderer.SetBloomOnPowIntensity(result16);
			break;
		}
		case "underwater_power":
		{
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result14))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			_gameInstance.UnderwaterBloomPower = result14;
			_gameInstance.PostEffectRenderer.SetBloomOnPowPower(result14);
			break;
		}
		case "dither_on":
			_gameInstance.PostEffectRenderer.UseDitheringOnBloom(enable: true);
			break;
		case "dither_off":
			_gameInstance.PostEffectRenderer.UseDitheringOnBloom(enable: false);
			break;
		default:
		{
			if (!float.TryParse(args[i - 1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
			{
				_gameInstance.Chat.Error(args[i - 1] + " is not a valid number!");
				return;
			}
			if (!float.TryParse(args[i], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result2))
			{
				_gameInstance.Chat.Error(args[i] + " is not a valid number!");
				return;
			}
			if (!float.TryParse(args[i + 1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result3))
			{
				_gameInstance.Chat.Error(args[i + 1] + " is not a valid number!");
				return;
			}
			if (!float.TryParse(args[i + 2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result4))
			{
				_gameInstance.Chat.Error(args[i + 2] + " is not a valid number!");
				return;
			}
			if (!float.TryParse(args[i + 3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result5))
			{
				_gameInstance.Chat.Error(args[i + 3] + " is not a valid number!");
				return;
			}
			_gameInstance.PostEffectRenderer.SetBloomIntensities(result, result2, result3, result4, result5);
			break;
		}
		}
		if (flag)
		{
			string message = _gameInstance.PostEffectRenderer.PrintBloomState();
			_gameInstance.Chat.Log(message);
			return;
		}
		throw new InvalidCommandUsage();
	}

	[Usage("blur", new string[] { "[strong|normal|light|off]" })]
	private void BlurSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.PostEffectRenderer.UseBlur(enable: true);
			break;
		case "off":
			_gameInstance.PostEffectRenderer.UseBlur(enable: false);
			break;
		case "light":
			_gameInstance.PostEffectRenderer.SetBlurStrength(1);
			break;
		case "normal":
			_gameInstance.PostEffectRenderer.SetBlurStrength(2);
			break;
		case "strong":
			_gameInstance.PostEffectRenderer.SetBlurStrength(3);
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Blur : " + args[0]);
	}

	[Usage("caustics", new string[] { "[on|off|intensity X|scale X|distortion X]\n (default : intensity = 1.0, scale = 0.095, distortion = 0.05)" })]
	private void UnderwaterCausticsSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		float result;
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SetUseUnderwaterCaustics(enable: true);
			break;
		case "off":
			_gameInstance.SetUseUnderwaterCaustics(enable: false);
			break;
		case "intensity":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetUnderwaterCausticsIntensity(result);
			break;
		case "scale":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetUnderwaterCausticsScale(result);
			break;
		case "distortion":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetUnderwaterCausticsDistortion(result);
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.PrintUnderwaterCausticsParams();
	}

	[Usage("clouduvmotion", new string[] { "[scale X|strength X]\n (default : scale = 50, strength = 0.1)" })]
	private void CloudsUVMotionSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		string text3 = text2;
		float result;
		if (!(text3 == "scale"))
		{
			if (!(text3 == "strength"))
			{
				throw new InvalidCommandUsage();
			}
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetCloudsUVMotionStrength(result);
		}
		else
		{
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetCloudsUVMotionScale(result);
		}
		_gameInstance.PrintCloudsUVMotionParams();
	}

	[Usage("cloudshadow", new string[] { "[on|off|intensity X|scale X|blur X|speed X]\n (default : intensity = 0.25, scale = 0.005, blur = 3.5, speed = 1.0)" })]
	private void CloudsShadowsSetup(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		float result;
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SetUseCloudsShadows(enable: true);
			break;
		case "off":
			_gameInstance.SetUseCloudsShadows(enable: false);
			break;
		case "intensity":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetCloudsShadowsIntensity(result);
			break;
		case "scale":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetCloudsShadowsScale(result);
			break;
		case "blur":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetCloudsShadowsBlurriness(result);
			break;
		case "speed":
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.SetCloudsShadowsSpeed(result);
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.PrintCloudsShadowsParams();
	}

	[Usage("sky", new string[] { "[rotation 0-360 | sun_height (%)]" })]
	private void Sky(string[] args)
	{
		if (args.Length < 1)
		{
			throw new InvalidCommandUsage();
		}
		int num = 0;
		switch (args[num++].ToLower())
		{
		case "sun_height":
		{
			if (float.TryParse(args[num], out var result2))
			{
				result2 = MathHelper.Clamp(result2, 0f, 150f);
				_gameInstance.WeatherModule.SunHeight = 2f * (result2 / 100f);
				_gameInstance.Chat.Log($"Sun moved to {result2}% of default height");
			}
			else
			{
				_gameInstance.Chat.Error(args[num] + " is not a valid number!");
			}
			break;
		}
		case "rotation":
		{
			if (float.TryParse(args[num], out var result))
			{
				_gameInstance.WeatherModule.SkyRotation = Quaternion.CreateFromAxisAngle(Vector3.Down, MathHelper.ToRadians(result));
				_gameInstance.Chat.Log("Sky rotated by " + args[num] + " from default angle");
			}
			else
			{
				_gameInstance.Chat.Error(args[num] + " is not a valid number!");
			}
			break;
		}
		case "dither_on":
			_gameInstance.UseDitheringOnSky(enable: true);
			break;
		case "dither_off":
			_gameInstance.UseDitheringOnSky(enable: false);
			break;
		case "test":
			_gameInstance.UseSkyboxTest = !_gameInstance.UseSkyboxTest;
			break;
		default:
			throw new InvalidCommandUsage();
		}
	}

	[Usage("forcefield", new string[] { "[sphere|wall|anim_on|anim_off|distort_on|distort_off|color_on|color_off|off]" })]
	private void ForceFieldSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		bool flag = true;
		switch (args[0].ToLower())
		{
		case "box":
			_gameInstance.ForceFieldTest = 3;
			break;
		case "sphere":
			_gameInstance.ForceFieldTest = 2;
			break;
		case "wall":
			_gameInstance.ForceFieldTest = 1;
			break;
		case "anim_on":
			_gameInstance.ForceFieldOptionAnimation = true;
			break;
		case "anim_off":
			_gameInstance.ForceFieldOptionAnimation = false;
			break;
		case "outline_on":
			_gameInstance.ForceFieldOptionOutline = true;
			break;
		case "outline_off":
			_gameInstance.ForceFieldOptionOutline = false;
			break;
		case "distort_on":
			_gameInstance.ForceFieldOptionDistortion = true;
			break;
		case "distort_off":
			_gameInstance.ForceFieldOptionDistortion = false;
			break;
		case "color_on":
			_gameInstance.ForceFieldOptionColor = true;
			break;
		case "color_off":
			_gameInstance.ForceFieldOptionColor = false;
			break;
		case "off":
			_gameInstance.ForceFieldTest = 0;
			break;
		default:
		{
			if (int.TryParse(args[0], out var result))
			{
				_gameInstance.ForceFieldCount = result;
				break;
			}
			flag = false;
			throw new InvalidCommandUsage();
		}
		}
		if (flag)
		{
			_gameInstance.Chat.Log("Post-FXAA state : " + args[0]);
		}
	}

	[Usage("sharpen", new string[] { "[on|off]" })]
	private void SharpenSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (text2 == "off")
			{
				_gameInstance.PostEffectRenderer.UseFXAASharpened(enable: false);
			}
			else
			{
				if (!float.TryParse(args[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
				{
					throw new InvalidCommandUsage();
				}
				_gameInstance.PostEffectRenderer.UseFXAASharpened(enable: true, result);
			}
		}
		else
		{
			_gameInstance.PostEffectRenderer.UseFXAASharpened(enable: true);
		}
		_gameInstance.Chat.Log("Post-FXAA Sharpen state : " + args[0]);
	}

	[Usage("fxaa", new string[] { "[on|off]" })]
	private void FXAASetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.PostEffectRenderer.UseFXAA(enable: false);
		}
		else
		{
			_gameInstance.PostEffectRenderer.UseFXAA(enable: true);
		}
		_gameInstance.Chat.Log("Post-FXAA state : " + args[0]);
	}

	[Usage("taa", new string[] { "[on|off]" })]
	private void TAASetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.PostEffectRenderer.UseTemporalAA(enable: false);
		}
		else
		{
			_gameInstance.PostEffectRenderer.UseTemporalAA(enable: true);
		}
		_gameInstance.Chat.Log("Post-TAA state : " + args[0]);
	}

	[Usage("distortion", new string[] { "[on|off]" })]
	private void DistortionSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.PostEffectRenderer.UseDistortion(enable: false);
		}
		else
		{
			_gameInstance.PostEffectRenderer.UseDistortion(enable: true);
		}
		_gameInstance.Chat.Log("Distortion state : " + args[0]);
	}

	[Usage("postfx", new string[] { "[brightness x|contrast x] (default : brightness = 0; contrast = 1" })]
	private void PostFXSetup(string[] args)
	{
		if (args.Length != 2)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		float result;
		if (!(text2 == "brightness"))
		{
			if (!(text2 == "contrast"))
			{
				throw new InvalidCommandUsage();
			}
			if (!float.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.PostEffectRenderer.SetPostFXContrast(result);
		}
		else
		{
			if (!float.TryParse(args[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.PostEffectRenderer.SetPostFXBrightness(result);
		}
		_gameInstance.Chat.Log("Post-FX state : " + args[0] + " " + args[1]);
	}

	[Usage("dmgindicatorangle", new string[] { "[angleValueDegree]" })]
	private void IndicatorAngle(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		if (int.TryParse(args[0], out var result) && result >= 0)
		{
			_gameInstance.DamageEffectModule.AngleHideDamage = result;
		}
		else
		{
			_gameInstance.Chat.Log(args[0] + " is not a valid int or is not greater than or equal to 0!");
		}
	}

	[Usage("speed", new string[] { "[speed]" })]
	private void SpeedCommand(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		if (float.TryParse(args[0], out var result))
		{
			MovementController movementController = _gameInstance.CharacterControllerModule.MovementController;
			MovementSettings movementSettings = movementController.MovementSettings;
			movementController.SpeedMultiplier = System.Math.Min(movementSettings.MaxSpeedMultiplier, System.Math.Max(movementSettings.MinSpeedMultiplier, result));
			_gameInstance.Chat.Log($"Speed set to: {result}");
		}
		else
		{
			_gameInstance.Chat.Log("Invalid speed value: " + args[0]);
		}
	}

	[Usage("debugmove", new string[] { "[mode|off]" })]
	private void DebugMove(string[] args)
	{
		if (args.Length > 1)
		{
			throw new InvalidCommandUsage();
		}
		MovementController.DebugMovement debugMovement = MovementController.DebugMovement.None;
		if (args.Length == 1)
		{
			debugMovement = (MovementController.DebugMovement)Enum.Parse(typeof(MovementController.DebugMovement), args[0], ignoreCase: true);
		}
		MovementController.DebugMovementMode = debugMovement;
		_gameInstance.Chat.Log($"Debug movement mode set to: {debugMovement}");
	}

	[Usage("movsettings", new string[] { "[mass|dragCoefficient|jumpForce|swimJumpForce|acceleration|airDragMin|airDragMax|airDragMinSpeed|airDragMaxSpeed|airFrictionMin|airFrictionMax|airFrictionMinSpeed|airFrictionMaxSpeed|airSpeedMul|airControlMinSpeed|airControlMaxSpeed|airControlMinMultiplier|airControlMaxMultiplier|comboAirSpeedMul|runSpeedMul|baseSpeed|climbSpeed|groundDrag|jumpBufferDuration|jumpBufferMaxYVelocity|forwardWalkSpeedMul|backwardWalkSpeedMul|strafeWalkSpeedMul|forwardRunSpeedMul|strafeRunSpeedMul|strafeRunSpeedMul|forwardCrouchSpeedMul|backwardCrouchSpeedMul|strafeCrouchSpeedMul|forwardSprintSpeedMul|mariojumpfallforce|fallThreshold|fallEffectDuration|fallJumpForce|fallMomentumLoss|autoJumpObstacleEffectDuration|autoJumpObstacleSpeedLoss|autoJumpObstacleSprintSpeedLoss|autoJumpObstacleMaxAngle|autoJumpObstacleSprintEffectDuration|autoJumpDisableJumping] [value]" })]
	private void UpdateMovementSettings(string[] args)
	{
		if (args.Length == 0)
		{
			MovementSettings movementSettings = _gameInstance.CharacterControllerModule.MovementController.MovementSettings;
			string message = "Settings: \n" + $" - mass: {movementSettings.Mass}\n" + $" - dragCoefficient: {movementSettings.DragCoefficient}\n" + $" - jumpForce: {movementSettings.JumpForce}\n" + $" - swimJumpForce: {movementSettings.SwimJumpForce}\n" + $" - jumpBufferDuration: {movementSettings.JumpBufferDuration}\n" + $" - jumpBufferMaxYVelocity: {movementSettings.JumpBufferMaxYVelocity}\n" + $" - acceleration: {movementSettings.Acceleration}\n" + $" - airDragMin: {movementSettings.AirDragMin}\n" + $" - airDragMax: {movementSettings.AirDragMax}\n" + $" - airDragMinSpeed: {movementSettings.AirDragMinSpeed}\n" + $" - airDragMaxSpeed: {movementSettings.AirDragMaxSpeed}\n" + $" - airFrictionMin: {movementSettings.AirFrictionMin}\n" + $" - airFrictionMax: {movementSettings.AirFrictionMax}\n" + $" - airFrictionMinSpeed: {movementSettings.AirFrictionMinSpeed}\n" + $" - airFrictionMaxSpeed: {movementSettings.AirFrictionMaxSpeed}\n" + $" - airSpeedMul: {movementSettings.AirSpeedMultiplier}\n" + $" - airControlMinSpeed: {movementSettings.AirControlMinSpeed}\n" + $" - airControlMaxSpeed: {movementSettings.AirControlMaxSpeed}\n" + $" - airControlMinMultiplier: {movementSettings.AirControlMinMultiplier}\n" + $" - airControlMaxMultiplier: {movementSettings.AirControlMaxMultiplier}\n" + $" - comboAirSpeedMul: {movementSettings.ComboAirSpeedMultiplier}\n" + $" - baseSpeed: {movementSettings.BaseSpeed}\n" + $" - climbSpeed: {movementSettings.ClimbSpeed}\n" + $" - horizontalFlySpeed: {movementSettings.HorizontalFlySpeed}\n" + $" - verticalFlySpeed: {movementSettings.VerticalFlySpeed}\n" + $" - groundDrag: {_gameInstance.CharacterControllerModule.MovementController.DefaultBlockDrag}\n" + "\n\nTemporary settings: \n" + $" - forwardWalkSpeedMul: {movementSettings.ForwardWalkSpeedMultiplier}\n" + $" - backwardWalkSpeedMul: {movementSettings.BackwardWalkSpeedMultiplier}\n" + $" - strafeWalkSpeedMul: {movementSettings.StrafeWalkSpeedMultiplier}\n" + $" - forwardRunSpeedMul: {movementSettings.ForwardRunSpeedMultiplier}\n" + $" - backwardRunSpeedMul: {movementSettings.BackwardRunSpeedMultiplier}\n" + $" - strafeRunSpeedMul: {movementSettings.StrafeRunSpeedMultiplier}\n" + $" - forwardCrouchSpeedMul: {movementSettings.ForwardCrouchSpeedMultiplier}\n" + $" - backwardCrouchSpeedMul: {movementSettings.BackwardCrouchSpeedMultiplier}\n" + $" - strafeCrouchSpeedMul: {movementSettings.StrafeCrouchSpeedMultiplier}\n" + $" - forwardSprintSpeedMul: {movementSettings.ForwardSprintSpeedMultiplier}\n" + "\n\n" + $" - marioJumpFallForce: {movementSettings.MarioJumpFallForce}\n" + $" - fallEffectDuration: {movementSettings.FallEffectDuration}\n" + $" - fallJumpForce: {movementSettings.FallJumpForce}\n" + $" - fallMomentumLoss: {movementSettings.FallMomentumLoss}\n" + "\n\n" + $" - autoJumpObstacleEffectDuration: {movementSettings.AutoJumpObstacleEffectDuration}\n" + $" - autoJumpObstacleSpeedLoss: {movementSettings.AutoJumpObstacleSpeedLoss}\n" + $" - autoJumpObstacleSprintSpeedLoss: {movementSettings.AutoJumpObstacleSprintSpeedLoss}\n" + $" - autoJumpObstacleSprintEffectDuration: {movementSettings.AutoJumpObstacleSprintEffectDuration}\n" + $" - autoJumpObstacleMaxAngle: {movementSettings.AutoJumpObstacleMaxAngle}\n" + $" - autoJumpDisableJumping: {movementSettings.AutoJumpDisableJumping}\n";
			_gameInstance.Chat.Log(message);
			return;
		}
		string text = args[0].ToLower();
		bool flag = true;
		float result2;
		if (bool.TryParse(args[1], out var result))
		{
			string text2 = text;
			string text3 = text2;
			if (text3 == "autojumpdisablejumping")
			{
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AutoJumpDisableJumping = result;
			}
			else
			{
				flag = false;
			}
			if (flag)
			{
				_gameInstance.Chat.Log($"{text} changed to: {result}");
			}
		}
		else if (float.TryParse(args[1], out result2))
		{
			switch (text)
			{
			case "mass":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.Mass = result2;
				break;
			case "dragCoefficient":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.DragCoefficient = result2;
				break;
			case "jumpforce":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.JumpForce = result2;
				break;
			case "swimjumpforce":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.SwimJumpForce = result2;
				break;
			case "jumpbufferduration":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.JumpBufferDuration = result2;
				break;
			case "jumpbuffermaxyvelocity":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.JumpBufferMaxYVelocity = result2;
				break;
			case "acceleration":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.Acceleration = result2;
				break;
			case "airdragmin":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirDragMin = result2;
				break;
			case "airdragmax":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirDragMax = result2;
				break;
			case "airdragminspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirDragMinSpeed = result2;
				break;
			case "airdragmaxspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirDragMaxSpeed = result2;
				break;
			case "airfrictionmin":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirFrictionMin = result2;
				break;
			case "airfrictionmax":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirFrictionMax = result2;
				break;
			case "airfrictionminspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirFrictionMinSpeed = result2;
				break;
			case "airfrictionmaxspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirFrictionMaxSpeed = result2;
				break;
			case "airspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirSpeedMultiplier = result2;
				break;
			case "aircontrolminspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirControlMinSpeed = result2;
				break;
			case "aircontrolmaxspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirControlMaxSpeed = result2;
				break;
			case "aircontrolminmultiplier":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirControlMinMultiplier = result2;
				break;
			case "aircontrolmaxmultiplier":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AirControlMaxMultiplier = result2;
				break;
			case "comboairspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.ComboAirSpeedMultiplier = result2;
				break;
			case "basespeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.BaseSpeed = result2;
				break;
			case "climbspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.ClimbSpeed = result2;
				break;
			case "horizontalflyspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.HorizontalFlySpeed = result2;
				break;
			case "verticalflyspeed":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.VerticalFlySpeed = result2;
				break;
			case "grounddrag":
				_gameInstance.CharacterControllerModule.MovementController.DefaultBlockDrag = result2;
				break;
			case "forwardwalkspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.ForwardWalkSpeedMultiplier = result2;
				break;
			case "backwardwalkspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.BackwardWalkSpeedMultiplier = result2;
				break;
			case "strafewalkspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.StrafeWalkSpeedMultiplier = result2;
				break;
			case "forwardrunspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.ForwardRunSpeedMultiplier = result2;
				break;
			case "backwardrunspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.BackwardRunSpeedMultiplier = result2;
				break;
			case "straferunspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.StrafeRunSpeedMultiplier = result2;
				break;
			case "forwardcrouchspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.ForwardCrouchSpeedMultiplier = result2;
				break;
			case "backwardcrouchspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.BackwardCrouchSpeedMultiplier = result2;
				break;
			case "strafecrouchspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.StrafeCrouchSpeedMultiplier = result2;
				break;
			case "forwardsprintspeedmul":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.ForwardSprintSpeedMultiplier = result2;
				break;
			case "mariojumpfallforce":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.MarioJumpFallForce = result2;
				break;
			case "falleffectduration":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.FallEffectDuration = result2;
				break;
			case "falljumpforce":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.FallJumpForce = result2;
				break;
			case "fallmomentumloss":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.FallMomentumLoss = result2;
				break;
			case "autojumpobstacleeffectduration":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AutoJumpObstacleEffectDuration = result2;
				break;
			case "autojumpobstaclespeedloss":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AutoJumpObstacleSpeedLoss = result2;
				break;
			case "autojumpobstaclesprintspeedloss":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AutoJumpObstacleSprintSpeedLoss = result2;
				break;
			case "autojumpobstaclesprinteffectduration":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AutoJumpObstacleSprintEffectDuration = result2;
				break;
			case "autojumpobstaclemaxangle":
				_gameInstance.CharacterControllerModule.MovementController.MovementSettings.AutoJumpObstacleMaxAngle = result2;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				_gameInstance.Chat.Log($"{text} changed to: {result2}");
			}
		}
		if (flag)
		{
			return;
		}
		throw new InvalidCommandUsage();
	}

	[Usage("speedo", new string[] { "[on|off]>" })]
	private void UpdateSpeedometer(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.App.Interface.InGameView.SpeedometerComponent.Enabled = false;
		}
		else
		{
			_gameInstance.App.Interface.InGameView.SpeedometerComponent.Enabled = true;
		}
		_gameInstance.App.Interface.InGameView.UpdateSpeedometerVisibility(doLayout: true);
	}

	[Usage("render", new string[] { "[on|off|list] <name> - \ne.g. render off map_near" })]
	private void RenderSetup(string[] args)
	{
		if (args.Length > 2)
		{
			throw new InvalidCommandUsage();
		}
		string text2 = null;
		string text = args[0].ToLower();
		bool enable = false;
		switch (text)
		{
		case "on":
			enable = true;
			break;
		case "off":
			enable = false;
			break;
		case "pause":
			_gameInstance.RenderTimePaused = !_gameInstance.RenderTimePaused;
			break;
		case "list":
			text2 = string.Format("{0}", string.Join(",", _gameInstance.RenderPassNames));
			break;
		default:
			throw new InvalidCommandUsage();
		}
		if (text2 != null)
		{
			_gameInstance.Chat.Log("Available render passes:\n" + text2);
			return;
		}
		text = args[1].ToLower();
		if (text == "all")
		{
			for (int i = 0; i < _gameInstance.RenderPassNames.Length; i++)
			{
				_gameInstance.SetRenderPassEnabled((uint)i, enable);
			}
		}
		else if (_gameInstance.RenderPassNames.Contains(text))
		{
			int passId = Array.FindIndex(_gameInstance.RenderPassNames, (string item) => item == text);
			_gameInstance.SetRenderPassEnabled((uint)passId, enable);
		}
		else
		{
			_gameInstance.Chat.Log("Unknown render pass name");
		}
	}

	[Usage("parallel", new string[] { "on|off", "light_on|light_off", "particle_on|particle_off", "anim_on|anim_off" })]
	private void ParallelSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "on":
			_gameInstance.SceneRenderer.ClusteredLighting.UseParallelExecution(enable: true);
			_gameInstance.Engine.FXSystem.Particles.UseParallelExecution(enable: true);
			_gameInstance.Engine.AnimationSystem.UseParallelExecution(enable: true);
			break;
		case "off":
			_gameInstance.SceneRenderer.ClusteredLighting.UseParallelExecution(enable: false);
			_gameInstance.Engine.FXSystem.Particles.UseParallelExecution(enable: false);
			_gameInstance.Engine.AnimationSystem.UseParallelExecution(enable: false);
			break;
		case "light_on":
			_gameInstance.SceneRenderer.ClusteredLighting.UseParallelExecution(enable: true);
			break;
		case "light_off":
			_gameInstance.SceneRenderer.ClusteredLighting.UseParallelExecution(enable: false);
			break;
		case "particle_on":
			_gameInstance.Engine.FXSystem.Particles.UseParallelExecution(enable: true);
			break;
		case "particle_off":
			_gameInstance.Engine.FXSystem.Particles.UseParallelExecution(enable: false);
			break;
		case "anim_on":
			_gameInstance.Engine.AnimationSystem.UseParallelExecution(enable: true);
			break;
		case "anim_off":
			_gameInstance.Engine.AnimationSystem.UseParallelExecution(enable: false);
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("Parallel state : " + args[0]);
	}

	[Usage("test", new string[] { "[on|off]" })]
	private void TestSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "on"))
		{
			if (!(text2 == "off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.TestBranch = false;
		}
		else
		{
			_gameInstance.TestBranch = true;
		}
		_gameInstance.Chat.Log("Test state : " + args[0]);
	}

	[Usage("graphics", new string[] { "[mode_ingame|mode_cutscene|mode_trailer|mode_slowgpu]" })]
	private void GraphicsSetup(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		switch (args[0].ToLower())
		{
		case "mode_slowgpu":
			_gameInstance.SetRenderingOptions(ref _gameInstance.LowEndGPUMode);
			break;
		case "mode_ingame":
			_gameInstance.SetRenderingOptions(ref _gameInstance.IngameMode);
			break;
		case "mode_cutscene":
			_gameInstance.SetRenderingOptions(ref _gameInstance.CutscenesMode);
			break;
		case "mode_trailer":
			_gameInstance.SetRenderingOptions(ref _gameInstance.TrailerMode);
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log("graphics state : " + args[0]);
	}

	[Usage("packetstats", new string[] { "[reset]" })]
	private void PacketStats(string[] args)
	{
		ConnectionToServer.PacketStat[] packetStats = _gameInstance.Connection.PacketStats;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Packets Sent:");
		for (int i = 0; i < packetStats.Length; i++)
		{
			ConnectionToServer.PacketStat packetStat = packetStats[i];
			long sentCount = packetStat.SentCount;
			if (sentCount > 0)
			{
				long sentTotalSize = packetStat.SentTotalSize;
				stringBuilder.AppendLine($"\t{packetStat.Name} ({i})\n\t{sentCount} packets, Size: {sentTotalSize} bytes, Avg: {sentTotalSize / sentCount} bytes");
			}
		}
		stringBuilder.AppendLine("\nPackets Received:");
		for (int j = 0; j < packetStats.Length; j++)
		{
			ConnectionToServer.PacketStat packetStat2 = packetStats[j];
			long receivedCount = packetStat2.ReceivedCount;
			if (receivedCount > 0)
			{
				long receivedTotalElapsed = packetStat2.ReceivedTotalElapsed;
				stringBuilder.AppendLine($"\t{packetStat2.Name} ({j})\n\t\t{receivedCount} packets, Time: {TimeHelper.FormatTicks(receivedTotalElapsed)}, Avg: {TimeHelper.FormatTicks(receivedTotalElapsed / receivedCount)}");
			}
		}
		string text = stringBuilder.ToString();
		Logger.Info(text);
		if (args.Length == 1 && args[0] == "reset")
		{
			_gameInstance.Connection.ResetPacketStats();
			_gameInstance.Chat.Log("Reset packet Stats (Logged old stats to console)");
		}
		else
		{
			_gameInstance.Chat.Log(text);
		}
	}

	[Usage("heartbeatsettings", new string[] { "[healthAlertThreshold|minAlphaHealthBorder|maxAlphaHealthBorder|minVariance|maxVariance|lerpSpeed|resetSpeedHealthBorder] <value>" })]
	private void UpdateHeartbeatSettings(string[] args)
	{
		if (args.Length == 0)
		{
			string message = "Settings: \n" + $" - healthAlertThreshold: {_gameInstance.DamageEffectModule.HealthAlertThreshold}\n" + $" - minAlphaHealthBorder: {_gameInstance.DamageEffectModule.MinAlphaHealthBorder}\n" + $" - maxAlphaHealthBorder: {_gameInstance.DamageEffectModule.MaxAlphaHealthBorder}\n" + $" - minVariance: {_gameInstance.DamageEffectModule.MinVarianceHealthBorder}\n" + $" - maxVariance: {_gameInstance.DamageEffectModule.MaxVarianceHealthBorder}\n" + $" - lerpSpeed: {_gameInstance.DamageEffectModule.LerpSpeedHealthBorder}\n" + $" - resetSpeedHealthBorder: {_gameInstance.DamageEffectModule.ResetSpeedHealthBorder}\n";
			_gameInstance.Chat.Log(message);
			return;
		}
		string text = args[0];
		float result = 0f;
		if (args.Length > 1 && args[0] != "easing" && !float.TryParse(args[1], out result))
		{
			_gameInstance.Chat.Log(args[1] + " is not a valid number");
			throw new InvalidCommandUsage();
		}
		switch (text)
		{
		case "minAlphaHealthBorder":
			_gameInstance.DamageEffectModule.MinAlphaHealthBorder = result;
			break;
		case "maxAlphaHealthBorder":
			_gameInstance.DamageEffectModule.MaxAlphaHealthBorder = result;
			break;
		case "minVariance":
			_gameInstance.DamageEffectModule.MinVarianceHealthBorder = result;
			break;
		case "maxVariance":
			_gameInstance.DamageEffectModule.MaxVarianceHealthBorder = result;
			break;
		case "lerpSpeed":
			_gameInstance.DamageEffectModule.LerpSpeedHealthBorder = result;
			break;
		case "resetSpeedHealthBorder":
			_gameInstance.DamageEffectModule.ResetSpeedHealthBorder = result;
			break;
		default:
			throw new InvalidCommandUsage();
		}
		_gameInstance.Chat.Log($"{text} changed to: {result}");
	}

	[Usage("hitdetection", new string[] { "" })]
	private void HitDetection(string[] args)
	{
		_gameInstance.InteractionModule.ShowSelectorDebug = !_gameInstance.InteractionModule.ShowSelectorDebug;
		_gameInstance.InteractionModule.SelectorDebugMeshes.Clear();
		_gameInstance.Chat.Log($"Toggled hitdetection preview : {_gameInstance.InteractionModule.ShowSelectorDebug}");
	}

	private void RenderPlayers(string[] args)
	{
		_gameInstance.RenderPlayers = !_gameInstance.RenderPlayers;
		_gameInstance.Chat.Log($"Toggled player rendering : {_gameInstance.RenderPlayers}");
	}

	[Usage("blockpreview", new string[] { "[dither_on|dither_off]" })]
	private void BlockPreview(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "dither_on"))
		{
			if (!(text2 == "dither_off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.InteractionModule.BlockPreview.EnableDithering(enable: false);
		}
		else
		{
			_gameInstance.InteractionModule.BlockPreview.EnableDithering(enable: true);
		}
	}

	[Usage("buildertool", new string[] { "[highlight_on|highlight_off]" })]
	private void BuilderTool(string[] args)
	{
		if (args.Length != 1)
		{
			throw new InvalidCommandUsage();
		}
		string text = args[0].ToLower();
		string text2 = text;
		if (!(text2 == "highlight_on"))
		{
			if (!(text2 == "highlight_off"))
			{
				throw new InvalidCommandUsage();
			}
			_gameInstance.BuilderToolsModule.DrawHighlightAndUndergroundColor = false;
		}
		else
		{
			_gameInstance.BuilderToolsModule.DrawHighlightAndUndergroundColor = true;
		}
	}

	private void ForceTint(string[] args)
	{
		if (args.Length < 3)
		{
			ChunkGeometryBuilder.ForceTint = ChunkGeometryBuilder.NoTint;
		}
		else
		{
			ChunkGeometryBuilder.ForceTint = new ShortVector3((short)int.Parse(args[0]), (short)int.Parse(args[1]), (short)int.Parse(args[2]));
		}
	}

	private void StressTestBatcher(string[] args)
	{
		_gameInstance.App.Interface.InGameView.UpdateDebugStressVisibility();
	}

	private void LogDisposableSummaryCommand(string[] args)
	{
		Disposable.LogSummary(unfinalized: true);
		Disposable.LogSummary(unfinalized: false);
	}
}
