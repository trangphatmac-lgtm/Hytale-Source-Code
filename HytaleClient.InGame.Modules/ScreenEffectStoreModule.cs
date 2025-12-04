#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.InGame.Modules;

internal class ScreenEffectStoreModule : Module
{
	public class UniqueScreenEffect
	{
		public int Instances = 0;

		public ScreenEffectRenderer ScreenEffectRenderer;

		public UniqueScreenEffect(Engine engine)
		{
			ScreenEffectRenderer = new ScreenEffectRenderer(engine);
			ScreenEffectRenderer.Initialize();
			ScreenEffectRenderer.Color = new Vector4(1f, 1f, 1f, 0f);
			Instances++;
		}
	}

	private const float EntityEffectLerp = 3f;

	private QuadRenderer _quadRenderer;

	private List<string> _entityScreenEffectToRemove = new List<string>();

	public readonly ScreenEffectRenderer WeatherScreenEffectRenderer;

	public readonly Dictionary<string, UniqueScreenEffect> EntityScreenEffects = new Dictionary<string, UniqueScreenEffect>();

	private const int ScreenEffectDrawDefaultSize = 2;

	private const int ScreenEffectDrawGrowth = 2;

	public ScreenEffectRenderer[] ScreenEffectDraw = new ScreenEffectRenderer[2];

	public int ScreenEffectDrawCount { get; private set; } = 0;


	public ScreenEffectStoreModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		WeatherScreenEffectRenderer = new ScreenEffectRenderer(_gameInstance.Engine);
		BasicProgram basicProgram = gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
		_quadRenderer = new QuadRenderer(gameInstance.Engine.Graphics, basicProgram.AttribPosition, basicProgram.AttribTexCoords);
	}

	protected override void DoDispose()
	{
		_quadRenderer.Dispose();
		WeatherScreenEffectRenderer.Dispose();
		foreach (UniqueScreenEffect value in EntityScreenEffects.Values)
		{
			value.ScreenEffectRenderer.Dispose();
		}
	}

	public override void Initialize()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		WeatherScreenEffectRenderer.Initialize();
	}

	public void AddEntityScreenEffect(string assetPath)
	{
		if (EntityScreenEffects.TryGetValue(assetPath, out var value))
		{
			value.Instances++;
			return;
		}
		value = new UniqueScreenEffect(_gameInstance.Engine);
		if (!_gameInstance.HashesByServerAssetPath.TryGetValue(assetPath, out var value2))
		{
			_gameInstance.App.DevTools.Error("Missing entity screen effect asset " + assetPath);
			return;
		}
		value.ScreenEffectRenderer.RequestTextureUpdate(value2);
		EntityScreenEffects.Add(assetPath, value);
	}

	public void RemoveEntityScreenEffect(string assetPath)
	{
		if (EntityScreenEffects.TryGetValue(assetPath, out var value))
		{
			value.Instances--;
		}
	}

	public void Update(float deltaTime)
	{
		ScreenEffectDrawCount = 0;
		foreach (KeyValuePair<string, UniqueScreenEffect> entityScreenEffect in EntityScreenEffects)
		{
			if (entityScreenEffect.Value.Instances > 0)
			{
				if (entityScreenEffect.Value.ScreenEffectRenderer.IsScreenEffectTextureLoading)
				{
					entityScreenEffect.Value.ScreenEffectRenderer.Color.W = 0f;
				}
				else
				{
					entityScreenEffect.Value.ScreenEffectRenderer.Color.W = MathHelper.Lerp(entityScreenEffect.Value.ScreenEffectRenderer.Color.W, 1f, 3f * deltaTime);
				}
			}
			else
			{
				entityScreenEffect.Value.ScreenEffectRenderer.Color.W = MathHelper.Lerp(entityScreenEffect.Value.ScreenEffectRenderer.Color.W, 0f, 3f * deltaTime);
				if (entityScreenEffect.Value.ScreenEffectRenderer.Color.W <= 0.01f)
				{
					entityScreenEffect.Value.ScreenEffectRenderer.Color.W = 0f;
					_entityScreenEffectToRemove.Add(entityScreenEffect.Key);
				}
			}
			if (entityScreenEffect.Value.ScreenEffectRenderer.Color.W > 0f)
			{
				RequestDraw(entityScreenEffect.Value.ScreenEffectRenderer);
			}
		}
		for (int i = 0; i < _entityScreenEffectToRemove.Count; i++)
		{
			EntityScreenEffects[_entityScreenEffectToRemove[i]].ScreenEffectRenderer.Dispose();
			EntityScreenEffects.Remove(_entityScreenEffectToRemove[i]);
		}
		_entityScreenEffectToRemove.Clear();
		if (WeatherScreenEffectRenderer.HasTexture && WeatherScreenEffectRenderer.Color.W > 0f)
		{
			RequestDraw(WeatherScreenEffectRenderer);
		}
	}

	public void RequestDraw(ScreenEffectRenderer screenEffectRenderer)
	{
		if (ScreenEffectDrawCount == ScreenEffectDraw.Length)
		{
			Array.Resize(ref ScreenEffectDraw, ScreenEffectDraw.Length + 2);
		}
		ScreenEffectDraw[ScreenEffectDrawCount] = screenEffectRenderer;
		ScreenEffectDrawCount++;
	}

	[Obsolete]
	public bool NeedsDrawing()
	{
		return ScreenEffectDrawCount > 0;
	}

	public void Draw()
	{
		GLFunctions gL = _gameInstance.Engine.Graphics.GL;
		BasicProgram basicProgram = _gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram;
		basicProgram.MVPMatrix.SetValue(ref _gameInstance.Engine.Graphics.ScreenMatrix);
		for (int i = 0; i < ScreenEffectDrawCount; i++)
		{
			gL.BindTexture(GL.TEXTURE_2D, ScreenEffectDraw[i].ScreenEffectTexture);
			Vector4 color = ScreenEffectDraw[i].Color;
			basicProgram.Color.SetValue(color.X, color.Y, color.Z);
			basicProgram.Opacity.SetValue(color.W);
			_quadRenderer.Draw();
		}
	}
}
