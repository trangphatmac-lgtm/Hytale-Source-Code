using System;
using HytaleClient.Core;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal abstract class ClientTool : Disposable
{
	protected readonly GameInstance _gameInstance;

	protected readonly BuilderToolsModule _builderTools;

	protected readonly GraphicsDevice _graphics;

	private bool _isActive;

	public abstract string ToolId { get; }

	protected Vector3 BrushTarget => _builderTools.BrushTargetPosition;

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		private set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnActiveStateChange(_isActive);
			}
		}
	}

	public ClientTool(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		_builderTools = gameInstance.BuilderToolsModule;
		_graphics = gameInstance.Engine.Graphics;
	}

	public void SetActive(ClientItemStack itemStack)
	{
		IsActive = true;
		OnToolItemChange(itemStack);
	}

	public void SetInactive()
	{
		IsActive = false;
	}

	protected override void DoDispose()
	{
	}

	public virtual void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
	}

	public virtual void Update(float deltaTime)
	{
	}

	protected virtual void OnActiveStateChange(bool newState)
	{
	}

	public virtual void OnToolItemChange(ClientItemStack toolItem)
	{
	}

	public virtual bool NeedsDrawing()
	{
		return false;
	}

	public virtual bool NeedsTextDrawing()
	{
		return false;
	}

	public virtual void Draw(ref Matrix viewProjectionMatrix)
	{
		if (!NeedsDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
	}

	public virtual void DrawText(ref Matrix viewProjectionMatrix)
	{
		if (!NeedsTextDrawing())
		{
			throw new Exception("Draw called when it was not required. Please check with NeedsTextDrawing() first before calling this.");
		}
	}
}
