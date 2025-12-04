using System;
using System.Collections.Generic;
using HytaleClient.Data.UserSettings;
using SDL2;

namespace HytaleClient.Core;

internal class Input
{
	public enum MouseButton : uint
	{
		SDL_BUTTON_LEFT = 1u,
		SDL_BUTTON_MIDDLE,
		SDL_BUTTON_RIGHT,
		SDL_BUTTON_X1,
		SDL_BUTTON_X2
	}

	public struct KeyState
	{
		public bool Key;

		public bool Prev;

		public bool Down;

		public bool Up;
	}

	public struct KeyBehaviour
	{
		public bool Toggle;

		public int CopyToInputId;

		public int ReverseInputId;
	}

	private readonly Engine _engine;

	public bool KeyInputDisabled;

	public bool MouseInputDisabled = false;

	private readonly HashSet<SDL_Keycode> _keysSet = new HashSet<SDL_Keycode>();

	private readonly HashSet<SDL_Keycode> _keysDownSet = new HashSet<SDL_Keycode>();

	private readonly HashSet<SDL_Keycode> _keysHeldSet = new HashSet<SDL_Keycode>();

	private readonly HashSet<SDL_Keycode> _keysUpSet = new HashSet<SDL_Keycode>();

	private readonly HashSet<MouseButton> _mouseButtonsSet = new HashSet<MouseButton>();

	private readonly HashSet<MouseButton> _mouseButtonsDownSet = new HashSet<MouseButton>();

	private KeyState[] _keyStates;

	private InputBindings _inputBindings;

	private KeyBehaviour[] _keyBehaviours;

	public Input(Engine engine, InputBindings inputBindings)
	{
		_engine = engine;
		SetInputBindings(inputBindings);
	}

	public void SetInputBindings(InputBindings bindings)
	{
		_inputBindings = bindings;
		_keyStates = new KeyState[_inputBindings.AllBindings.Count];
		_keyBehaviours = new KeyBehaviour[_inputBindings.AllBindings.Count];
		_keyBehaviours[_inputBindings.ToggleCrouch.Id] = new KeyBehaviour
		{
			Toggle = true,
			ReverseInputId = _inputBindings.Crouch.Id
		};
		_keyBehaviours[_inputBindings.ToggleSprint.Id] = new KeyBehaviour
		{
			Toggle = true,
			CopyToInputId = _inputBindings.Sprint.Id
		};
		_keyBehaviours[_inputBindings.ToggleWalk.Id] = new KeyBehaviour
		{
			Toggle = true,
			CopyToInputId = _inputBindings.Walk.Id
		};
	}

	public void ResetKeys()
	{
		_keysSet.Clear();
		_keysDownSet.Clear();
		_keysHeldSet.Clear();
		_keysUpSet.Clear();
		for (int i = 0; i < _keyStates.Length; i++)
		{
			_keyStates[i] = default(KeyState);
		}
	}

	public void ResetMouseButtons()
	{
		_mouseButtonsSet.Clear();
		_mouseButtonsDownSet.Clear();
	}

	public void EndUserInput()
	{
		_keysDownSet.Clear();
		_keysUpSet.Clear();
	}

	public void OnUserInput(SDL_Event evt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Invalid comparison between Unknown and I4
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		if ((int)val <= 769)
		{
			if ((int)val == 768)
			{
				if (evt.key.repeat == 0)
				{
					_keysSet.Add(evt.key.keysym.sym);
					_keysDownSet.Add(evt.key.keysym.sym);
					_keysHeldSet.Add(evt.key.keysym.sym);
				}
				return;
			}
			if ((int)val == 769)
			{
				_keysSet.Remove(evt.key.keysym.sym);
				_keysHeldSet.Remove(evt.key.keysym.sym);
				_keysUpSet.Add(evt.key.keysym.sym);
				return;
			}
		}
		else
		{
			if ((int)val == 1025)
			{
				if (!MouseInputDisabled)
				{
					_mouseButtonsSet.Add((MouseButton)evt.button.button);
					_mouseButtonsDownSet.Add((MouseButton)evt.button.button);
				}
				return;
			}
			if ((int)val == 1026)
			{
				_mouseButtonsSet.Remove((MouseButton)evt.button.button);
				_mouseButtonsDownSet.Remove((MouseButton)evt.button.button);
				return;
			}
		}
		throw new ArgumentOutOfRangeException("evt", ((object)(SDL_EventType)(ref evt.type)).ToString());
	}

	public void UpdateBindings()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _inputBindings.AllBindings.Count; i++)
		{
			InputBinding inputBinding = _inputBindings.AllBindings[i];
			KeyState keyState = _keyStates[inputBinding.Id];
			KeyBehaviour keyBehaviour = _keyBehaviours[inputBinding.Id];
			keyState.Prev = keyState.Key;
			if (inputBinding.Keycode.HasValue)
			{
				keyState.Key = _keysHeldSet.Contains(inputBinding.Keycode.Value);
				keyState.Down = _keysDownSet.Contains(inputBinding.Keycode.Value);
				keyState.Up = _keysUpSet.Contains(inputBinding.Keycode.Value);
			}
			if (keyBehaviour.Toggle)
			{
				keyState.Key = (keyState.Down ? (!keyState.Prev) : keyState.Prev);
			}
			if (keyBehaviour.CopyToInputId != 0)
			{
				KeyState keyState2 = _keyStates[keyBehaviour.CopyToInputId];
				keyState2.Prev |= keyState.Prev;
				keyState2.Key |= keyState.Key;
				keyState2.Down |= keyState.Down;
				keyState2.Up |= keyState.Up;
				_keyStates[keyBehaviour.CopyToInputId] = keyState2;
			}
			if (keyBehaviour.ReverseInputId != 0)
			{
				KeyState keyState3 = _keyStates[keyBehaviour.ReverseInputId];
				keyState3.Prev ^= keyState.Prev;
				keyState3.Key ^= keyState.Key;
				keyState3.Down ^= keyState.Down;
				keyState3.Up ^= keyState.Up;
				_keyStates[keyBehaviour.ReverseInputId] = keyState3;
			}
			_keyStates[inputBinding.Id] = keyState;
		}
	}

	public static bool EventMatchesBinding(SDL_Event evt, InputBinding binding)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Invalid comparison between Unknown and I4
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		if (binding.Type == InputBinding.BindingType.Keycode)
		{
			return ((int)evt.type == 768 || (int)evt.type == 769) && evt.key.keysym.sym == binding.Keycode.Value;
		}
		return ((int)evt.type == 1025 || (int)evt.type == 1026) && evt.button.button == (byte)binding.MouseButton.Value;
	}

	public bool ConsumeBinding(InputBinding binding, bool ignoreKeyInputDisabled = false)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (binding.Type == InputBinding.BindingType.Keycode)
		{
			if (KeyInputDisabled && !ignoreKeyInputDisabled)
			{
				return false;
			}
			return _keysSet.Remove(binding.Keycode.Value);
		}
		if (!_engine.Window.IsMouseLocked)
		{
			return false;
		}
		return _mouseButtonsSet.Remove(binding.MouseButton.Value);
	}

	public bool ConsumeKey(SDL_Scancode scancode, bool ignoreKeyInputDisabled = false)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (KeyInputDisabled && !ignoreKeyInputDisabled)
		{
			return false;
		}
		return _keysSet.Remove(SDL.SDL_GetKeyFromScancode(scancode));
	}

	public bool CanConsumeBinding(InputBinding binding, bool ignoreKeyInputDisabled = false)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (binding.Type == InputBinding.BindingType.Keycode)
		{
			if (KeyInputDisabled && !ignoreKeyInputDisabled)
			{
				return false;
			}
			return _keysSet.Contains(binding.Keycode.Value);
		}
		if (!_engine.Window.IsMouseLocked || MouseInputDisabled)
		{
			return false;
		}
		return _mouseButtonsSet.Contains(binding.MouseButton.Value);
	}

	public bool IsBindingHeld(InputBinding binding, bool ignoreKeyInputDisabled = false)
	{
		if (binding.Type == InputBinding.BindingType.Keycode)
		{
			if (KeyInputDisabled && !ignoreKeyInputDisabled)
			{
				return false;
			}
			return _keyStates[binding.Id].Key;
		}
		if (!_engine.Window.IsMouseLocked)
		{
			return false;
		}
		return _mouseButtonsDownSet.Contains(binding.MouseButton.Value);
	}

	public bool IsBindingDown(InputBinding binding, bool ignoreKeyInputDisabled = false)
	{
		if (binding.Type == InputBinding.BindingType.Keycode)
		{
			if (KeyInputDisabled && !ignoreKeyInputDisabled)
			{
				return false;
			}
			return _keyStates[binding.Id].Down;
		}
		if (!_engine.Window.IsMouseLocked)
		{
			return false;
		}
		return _mouseButtonsDownSet.Contains(binding.MouseButton.Value);
	}

	public bool IsBindingUp(InputBinding binding, bool ignoreKeyInputDisabled = false)
	{
		if (binding.Type == InputBinding.BindingType.Keycode)
		{
			if (KeyInputDisabled && !ignoreKeyInputDisabled)
			{
				return false;
			}
			return _keyStates[binding.Id].Up;
		}
		if (!_engine.Window.IsMouseLocked)
		{
			return false;
		}
		throw new NotSupportedException("Mouse Up event not supported.");
	}

	public bool IsKeyHeld(SDL_Scancode scancode, bool ignoreKeyInputDisabled = false)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (KeyInputDisabled && !ignoreKeyInputDisabled)
		{
			return false;
		}
		return _keysHeldSet.Contains(SDL.SDL_GetKeyFromScancode(scancode));
	}

	public bool IsAnyKeyHeld(bool ignoreKeyInputDisabled = false)
	{
		if (KeyInputDisabled && !ignoreKeyInputDisabled)
		{
			return false;
		}
		return _keysHeldSet.Count > 0;
	}

	public bool IsShiftHeld()
	{
		return IsKeyHeld((SDL_Scancode)225) || IsKeyHeld((SDL_Scancode)229);
	}

	public bool IsAltHeld()
	{
		return IsKeyHeld((SDL_Scancode)226) || IsKeyHeld((SDL_Scancode)230);
	}

	public bool IsCtrlHeld()
	{
		return IsKeyHeld((SDL_Scancode)224) || IsKeyHeld((SDL_Scancode)228);
	}

	public bool IsAnyModifierHeld()
	{
		return IsShiftHeld() || IsAltHeld() || IsCtrlHeld();
	}

	public bool IsOnlyShiftHeld()
	{
		return IsShiftHeld() && !IsAltHeld() && !IsCtrlHeld();
	}

	public bool IsOnlyAltHeld()
	{
		return IsAltHeld() && !IsShiftHeld() && !IsCtrlHeld();
	}

	public bool IsOnlyCtrlHeld()
	{
		return IsCtrlHeld() && !IsShiftHeld() && !IsAltHeld();
	}

	public bool IsMouseButtonDown(MouseButton button)
	{
		return _mouseButtonsDownSet.Contains(button);
	}
}
