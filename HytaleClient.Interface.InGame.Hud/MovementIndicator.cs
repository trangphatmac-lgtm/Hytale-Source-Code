using HytaleClient.Data.Entities;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.InGame.Hud;

internal class MovementIndicator : InterfaceComponent
{
	private enum State
	{
		Sliding,
		Crouching,
		Flying,
		Running,
		Sprinting,
		SprintSwimming,
		Swimming,
		Walking,
		Rolling
	}

	private State _previousState;

	private Group _icon;

	private PatchStyle _iconCrouch;

	private PatchStyle _iconSlide;

	private PatchStyle _iconRoll;

	private PatchStyle _iconFly;

	private PatchStyle _iconSprint;

	private PatchStyle _iconSprintSwim;

	private PatchStyle _iconSwim;

	private PatchStyle _iconWalk;

	private PatchStyle _iconRun;

	public MovementIndicator(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/MovementIndicator/MovementIndicator.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_icon = uIFragment.Get<Group>("Icon");
		_iconCrouch = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconCrouch").Value);
		_iconSlide = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconSlide").Value);
		_iconRoll = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconRoll").Value);
		_iconFly = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconFly").Value);
		_iconSprint = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconSprint").Value);
		_iconSprintSwim = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconSprintSwim").Value);
		_iconSwim = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconSwim").Value);
		_iconWalk = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconWalk").Value);
		_iconRun = new PatchStyle(document.ResolveNamedValue<UIPath>(Desktop.Provider, "IconRun").Value);
		UpdateIcon(State.Running, doLayout: false);
	}

	public void Update(ClientMovementStates movementStates)
	{
		State state = (movementStates.IsFlying ? State.Flying : (movementStates.IsSwimming ? (movementStates.IsSprinting ? State.SprintSwimming : State.Swimming) : (movementStates.IsRolling ? State.Rolling : ((!movementStates.IsSliding) ? (movementStates.IsCrouching ? State.Crouching : ((!movementStates.IsSprinting) ? (movementStates.IsWalking ? State.Walking : State.Running) : State.Sprinting)) : State.Sliding))));
		if (state != _previousState)
		{
			UpdateIcon(state);
		}
		_previousState = state;
	}

	private void UpdateIcon(State state, bool doLayout = true)
	{
		switch (state)
		{
		case State.Sliding:
			_icon.Background = _iconSlide;
			break;
		case State.Rolling:
			_icon.Background = _iconRoll;
			break;
		case State.Crouching:
			_icon.Background = _iconCrouch;
			break;
		case State.Flying:
			_icon.Background = _iconFly;
			break;
		case State.Running:
			_icon.Background = _iconRun;
			break;
		case State.Sprinting:
			_icon.Background = _iconSprint;
			break;
		case State.SprintSwimming:
			_icon.Background = _iconSprintSwim;
			break;
		case State.Swimming:
			_icon.Background = _iconSwim;
			break;
		case State.Walking:
			_icon.Background = _iconWalk;
			break;
		}
		if (doLayout)
		{
			Layout();
		}
	}
}
