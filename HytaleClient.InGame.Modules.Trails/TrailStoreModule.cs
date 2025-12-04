#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.FX;
using HytaleClient.Graphics.Trails;
using HytaleClient.Math;
using HytaleClient.Networking;
using HytaleClient.Protocol;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules.Trails;

internal class TrailStoreModule : Module
{
	private Dictionary<string, TrailSettings> _trailSettingsById = new Dictionary<string, TrailSettings>();

	public bool ProxyCheck = true;

	public TrailStoreModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		_gameInstance.Engine.FXSystem.Trails.InitializeFunction(UpdateTrailLighting);
	}

	public bool TrySpawnTrailProxy(string trailId, out TrailProxy trailProxy, bool isLocalPlayer = false)
	{
		trailProxy = null;
		if (!_trailSettingsById.TryGetValue(trailId, out var value))
		{
			_gameInstance.App.DevTools.Error("Could not find trail settings: " + trailId);
			return false;
		}
		Vector2 textureAltasInverseSize = new Vector2(1f / (float)_gameInstance.FXModule.TextureAtlas.Width, 1f / (float)_gameInstance.FXModule.TextureAtlas.Height);
		bool flag = _gameInstance.Engine.FXSystem.Trails.TrySpawnTrail(value, textureAltasInverseSize, out trailProxy, isLocalPlayer);
		if (!flag)
		{
			_gameInstance.App.DevTools.Error("Failed to spawn Trail '" + trailId + "' : max trails count limit reached.");
		}
		return flag;
	}

	private void UpdateTrailLighting(Trail trail)
	{
		if (trail.LightInfluence > 0f && !_gameInstance.MapModule.Disposed)
		{
			Vector4 lightColorAtBlockPosition = _gameInstance.MapModule.GetLightColorAtBlockPosition((int)System.Math.Floor(trail.Position.X), (int)System.Math.Floor(trail.Position.Y), (int)System.Math.Floor(trail.Position.Z));
			lightColorAtBlockPosition = Vector4.Lerp(Vector4.One, lightColorAtBlockPosition, trail.LightInfluence);
			trail.UpdateLight(lightColorAtBlockPosition);
		}
	}

	protected override void DoDispose()
	{
		_gameInstance.Engine.FXSystem.Trails.DisposeFunction();
	}

	public void Update(Vector3 cameraPosition)
	{
		_gameInstance.Engine.FXSystem.Trails.UpdateProxies(cameraPosition, ProxyCheck);
	}

	public void PrepareTrails(Dictionary<string, Trail> networkTrails, out Dictionary<string, PacketHandler.TextureInfo> upcomingTextureInfo, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingTextureInfo = new Dictionary<string, PacketHandler.TextureInfo>();
		foreach (Trail value3 in networkTrails.Values)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			if (value3.Texture == null || !_gameInstance.HashesByServerAssetPath.TryGetValue(value3.Texture, out var value))
			{
				_gameInstance.App.DevTools.Error("Missing trail texture: " + value3.Texture + " for trail " + value3.Id);
			}
			else
			{
				if (upcomingTextureInfo.TryGetValue(value, out var value2))
				{
					continue;
				}
				value2 = new PacketHandler.TextureInfo
				{
					Checksum = value
				};
				if (Image.TryGetPngDimensions(AssetManager.GetAssetLocalPathUsingHash(value), out value2.Width, out value2.Height))
				{
					upcomingTextureInfo[value] = value2;
					if (value2.Width % 32 != 0 || value2.Height % 32 != 0 || value2.Width < 32 || value2.Height < 32)
					{
						_gameInstance.App.DevTools.Error($"Texture width/height must be a multiple of 32 and at least 32x32: {value3.Texture} ({value2.Width}x{value2.Height})");
					}
				}
			}
		}
	}

	public void UpdateTextures()
	{
		foreach (TrailSettings value3 in _trailSettingsById.Values)
		{
			if (!_gameInstance.HashesByServerAssetPath.TryGetValue(value3.Texture, out var value) || !_gameInstance.FXModule.ImageLocations.TryGetValue(value, out var value2))
			{
				_gameInstance.App.DevTools.Error("Failed to update trail texture: " + value3.Texture + " for trail " + value3.Id);
			}
			else
			{
				value3.ImageLocation = value2;
			}
		}
	}

	public void SetupTrailSettings(Dictionary<string, Trail> networkTrails)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		foreach (KeyValuePair<string, Trail> networkTrail in networkTrails)
		{
			string key = networkTrail.Key;
			Trail value = networkTrail.Value;
			TrailSettings trailSettings = new TrailSettings();
			TrailProtocolInitializer.Initialize(value, ref trailSettings);
			if (trailSettings.Texture == null)
			{
				continue;
			}
			if (!_gameInstance.HashesByServerAssetPath.ContainsKey(trailSettings.Texture))
			{
				_gameInstance.App.DevTools.Error("Failed to find trail texture: " + trailSettings.Texture + " for trail " + key);
				if (_trailSettingsById.ContainsKey(key))
				{
					_trailSettingsById.Remove(key);
				}
			}
			else
			{
				_trailSettingsById[key] = trailSettings;
			}
		}
	}
}
