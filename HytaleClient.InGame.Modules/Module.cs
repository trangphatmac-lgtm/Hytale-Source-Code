using System;
using HytaleClient.Core;

namespace HytaleClient.InGame.Modules;

internal abstract class Module : Disposable
{
	protected GameInstance _gameInstance;

	public Module(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
	}

	public virtual void Initialize()
	{
	}

	[Obsolete]
	public virtual void Tick()
	{
		throw new Exception("Module.Tick should never be called directly!");
	}

	[Obsolete]
	public virtual void OnNewFrame(float deltaTime)
	{
		throw new Exception("Module.OnNewFrame should never be called directly!");
	}

	protected override void DoDispose()
	{
	}
}
