using HytaleClient.Core;
using SDL2;

namespace HytaleClient.Data.UserSettings;

internal class InputBinding
{
	public enum BindingType : byte
	{
		Keycode,
		MouseButton
	}

	public int Id;

	public BindingType Type;

	private SDL_Keycode _keycode;

	private Input.MouseButton _mouseButton;

	public string BoundInputLabel;

	public SDL_Keycode? Keycode
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _keycode;
		}
		set
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			if (value.HasValue)
			{
				Type = BindingType.Keycode;
				_keycode = value.Value;
				BoundInputLabel = SDL.SDL_GetKeyName(_keycode);
			}
		}
	}

	public Input.MouseButton? MouseButton
	{
		get
		{
			return _mouseButton;
		}
		set
		{
			if (value.HasValue)
			{
				Type = BindingType.MouseButton;
				_mouseButton = value.Value;
				BoundInputLabel = GetMouseBoundInputLabel(_mouseButton);
			}
		}
	}

	public InputBinding(InputBinding binding = null)
	{
		if (binding != null)
		{
			if (binding.Type == BindingType.Keycode)
			{
				Keycode = binding.Keycode;
			}
			else
			{
				MouseButton = binding.MouseButton;
			}
		}
	}

	public static InputBinding FromScancode(SDL_Scancode scancode)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return new InputBinding
		{
			Keycode = SDL.SDL_GetKeyFromScancode(scancode)
		};
	}

	public static InputBinding FromKeycode(SDL_Keycode keycode)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new InputBinding
		{
			Keycode = keycode
		};
	}

	public static InputBinding FromMouseButton(Input.MouseButton button)
	{
		return new InputBinding
		{
			MouseButton = button
		};
	}

	public static string GetMouseBoundInputLabel(Input.MouseButton mouseButton)
	{
		switch (mouseButton)
		{
		case Input.MouseButton.SDL_BUTTON_LEFT:
			return "Left Mouse";
		case Input.MouseButton.SDL_BUTTON_MIDDLE:
			return "Middle Mouse";
		case Input.MouseButton.SDL_BUTTON_RIGHT:
			return "Right Mouse";
		default:
		{
			int num = (int)mouseButton;
			return "Mouse " + num;
		}
		}
	}
}
