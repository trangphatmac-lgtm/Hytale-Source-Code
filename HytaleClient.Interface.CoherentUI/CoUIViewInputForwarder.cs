using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Coherent.UI;
using HytaleClient.Core;
using HytaleClient.Math;
using HytaleClient.Utils;
using SDL2;

namespace HytaleClient.Interface.CoherentUI;

internal static class CoUIViewInputForwarder
{
	private static readonly Dictionary<SDL_Keycode, int> KeysMap;

	private static readonly KeyEventData KeyEventData;

	private static readonly MouseEventData MouseEventData;

	private static readonly byte[] TextBytes;

	static CoUIViewInputForwarder()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		KeyEventData = new KeyEventData();
		MouseEventData = new MouseEventData();
		TextBytes = new byte[256];
		KeysMap = new Dictionary<SDL_Keycode, int>(33);
		KeysMap[(SDL_Keycode)1073741897] = 45;
		KeysMap[(SDL_Keycode)1073741898] = 36;
		KeysMap[(SDL_Keycode)127] = 46;
		KeysMap[(SDL_Keycode)1073741901] = 35;
		KeysMap[(SDL_Keycode)1073741902] = 34;
		KeysMap[(SDL_Keycode)1073741903] = 39;
		KeysMap[(SDL_Keycode)1073741904] = 37;
		KeysMap[(SDL_Keycode)1073741905] = 40;
		KeysMap[(SDL_Keycode)1073741906] = 38;
		KeysMap[(SDL_Keycode)1073742049] = 160;
		KeysMap[(SDL_Keycode)1073742053] = 16;
		KeysMap[(SDL_Keycode)1073742048] = 17;
		KeysMap[(SDL_Keycode)1073742052] = 17;
		KeysMap[(SDL_Keycode)1073742050] = 18;
		KeysMap[(SDL_Keycode)1073742054] = 18;
		KeysMap[(SDL_Keycode)1073741881] = 20;
		KeysMap[(SDL_Keycode)1073741882] = 112;
		KeysMap[(SDL_Keycode)1073741883] = 113;
		KeysMap[(SDL_Keycode)1073741884] = 114;
		KeysMap[(SDL_Keycode)1073741885] = 115;
		KeysMap[(SDL_Keycode)1073741886] = 116;
		KeysMap[(SDL_Keycode)1073741887] = 117;
		KeysMap[(SDL_Keycode)1073741888] = 118;
		KeysMap[(SDL_Keycode)1073741889] = 119;
		KeysMap[(SDL_Keycode)1073741890] = 120;
		KeysMap[(SDL_Keycode)1073741891] = 121;
		KeysMap[(SDL_Keycode)1073741892] = 122;
		KeysMap[(SDL_Keycode)1073741893] = 123;
		KeysMap[(SDL_Keycode)13] = 13;
		KeysMap[(SDL_Keycode)8] = 8;
		KeysMap[(SDL_Keycode)1073741912] = 13;
		KeysMap[(SDL_Keycode)1073742051] = 91;
		KeysMap[(SDL_Keycode)1073742055] = 93;
		if (33 != KeysMap.Count)
		{
			throw new Exception("keysMap capacity should be updated.");
		}
	}

	public unsafe static void OnUserInput(WebView webView, SDL_Event evt, Window window)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		SDL_EventType type = evt.type;
		SDL_EventType val = type;
		if (val - 768 > 1)
		{
			if ((int)val != 771)
			{
				if (val - 1024 <= 3)
				{
					Point mousePosition = window.TransformSDLToViewportCoords(evt.button.x, evt.button.y);
					SendMouseEvent(mouseWheel: new Point(evt.wheel.x, evt.wheel.y), webView: webView, eventType: evt.type, mouseButton: evt.button.button, mousePosition: mousePosition);
				}
				return;
			}
			byte* ptr;
			for (ptr = &evt.text.text.FixedElementField; *ptr != 0; ptr++)
			{
			}
			int num = (int)(ptr - &evt.text.text.FixedElementField);
			Marshal.Copy((IntPtr)(&evt.text.text.FixedElementField), TextBytes, 0, num);
			string @string = Encoding.UTF8.GetString(TextBytes, 0, num);
			SendTextInputEvent(webView, @string);
		}
		else
		{
			SDL_Keycode sym = evt.key.keysym.sym;
			SDL_Scancode scancode = evt.key.keysym.scancode;
			SendKeyboardEvent(webView, evt.type, sym, scancode, evt.key.repeat, evt.key.keysym.mod);
		}
	}

	public static void SendKeyboardEvent(WebView webView, SDL_EventType eventType, SDL_Keycode keycode, SDL_Scancode scancode, byte repeat, SDL_Keymod keymod)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Invalid comparison between Unknown and I4
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected I4, but got Unknown
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Invalid comparison between Unknown and I4
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Invalid comparison between Unknown and I4
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Invalid comparison between Unknown and I4
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Invalid comparison between Unknown and I4
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Invalid comparison between Unknown and I4
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Invalid comparison between Unknown and I4
		KeyEventData.Type = (EventType)(((int)eventType == 768) ? 1 : 2);
		KeyEventData.IsAutoRepeat = repeat != 0;
		KeyEventData.IsNumPad = (int)keycode >= 1073741908 && (int)keycode <= 1073741923;
		SetModifiersState(KeyEventData.Modifiers, keymod);
		if ((int)scancode < 45 || (int)scancode > 56)
		{
			if (KeysMap.TryGetValue(keycode, out var value))
			{
				KeyEventData.KeyCode = value;
				switch (keycode - 1073742048)
				{
				case 2:
				case 6:
					KeyEventData.Modifiers.IsAltDown = (int)eventType == 768;
					break;
				case 0:
				case 4:
					KeyEventData.Modifiers.IsCtrlDown = (int)eventType == 768;
					break;
				case 3:
				case 7:
					KeyEventData.Modifiers.IsMetaDown = (int)eventType == 768;
					break;
				}
				webView.KeyEvent(KeyEventData);
			}
			else
			{
				KeyEventData.KeyCode = char.ToUpperInvariant((char)keycode);
				webView.KeyEvent(KeyEventData);
				if ((int)KeyEventData.Type == 1)
				{
					KeyEventData.Type = (EventType)3;
					if (!KeyEventData.Modifiers.IsCtrlDown)
					{
						if (KeyEventData.Modifiers.IsAltDown && BuildInfo.Platform != Platform.MacOS)
						{
							KeyEventData.KeyCode = char.ToLowerInvariant((char)KeyEventData.KeyCode);
							webView.KeyEvent(KeyEventData);
						}
						else if (KeyEventData.Modifiers.IsMetaDown)
						{
							KeyEventData.KeyCode = char.ToLowerInvariant((char)KeyEventData.KeyCode);
							webView.KeyEvent(KeyEventData);
						}
					}
				}
			}
		}
		if ((int)eventType == 768)
		{
			if ((int)keycode == 13 || (int)keycode == 1073741912)
			{
				KeyEventData.Type = (EventType)3;
				webView.KeyEvent(KeyEventData);
			}
		}
	}

	public static void SendTextInputEvent(WebView webView, string text)
	{
		KeyEventData.Type = (EventType)3;
		for (int i = 0; i < text.Length; i++)
		{
			KeyEventData.KeyCode = text[i];
			webView.KeyEvent(KeyEventData);
		}
	}

	public static void SendMouseEvent(WebView webView, SDL_EventType eventType, int mouseButton, Point mousePosition, Point mouseWheel)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Invalid comparison between Unknown and I4
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Expected I4, but got Unknown
		uint num = SDL.SDL_GetMouseState(IntPtr.Zero, IntPtr.Zero);
		MouseEventData.MouseModifiers.IsLeftButtonDown = (num & SDL.SDL_BUTTON_LMASK) != 0;
		MouseEventData.MouseModifiers.IsMiddleButtonDown = (num & SDL.SDL_BUTTON_MMASK) != 0;
		MouseEventData.MouseModifiers.IsRightButtonDown = (num & SDL.SDL_BUTTON_RMASK) != 0;
		SetModifiersState(MouseEventData.Modifiers, SDL.SDL_GetModState());
		MouseEventData.WheelX = 0f;
		MouseEventData.WheelY = 0f;
		if ((int)eventType == 1027)
		{
			if (MouseEventData.Modifiers.IsShiftDown)
			{
				MouseEventData.WheelX = mouseWheel.Y;
				MouseEventData.WheelY = mouseWheel.X;
			}
			else
			{
				MouseEventData.WheelY = mouseWheel.Y;
				MouseEventData.WheelX = mouseWheel.X;
			}
		}
		else
		{
			MouseEventData.X = mousePosition.X;
			MouseEventData.Y = mousePosition.Y;
		}
		switch ((uint)mouseButton)
		{
		case 1u:
			MouseEventData.Button = (MouseButton)0;
			break;
		case 2u:
			MouseEventData.Button = (MouseButton)1;
			break;
		case 3u:
			MouseEventData.Button = (MouseButton)2;
			break;
		}
		switch (eventType - 1024)
		{
		case 0:
			MouseEventData.Type = (EventType)0;
			break;
		case 1:
			MouseEventData.Type = (EventType)1;
			break;
		case 2:
			MouseEventData.Type = (EventType)2;
			break;
		case 3:
			MouseEventData.Type = (EventType)3;
			break;
		}
		webView.MouseEvent(MouseEventData);
	}

	public static void ResetMousePosition(WebView webView, Window window)
	{
		MouseEventData.Type = (EventType)0;
		MouseEventData.X = window.Viewport.Width / 2;
		MouseEventData.Y = window.Viewport.Height / 2;
		MouseEventData.MouseModifiers.IsLeftButtonDown = false;
		MouseEventData.MouseModifiers.IsMiddleButtonDown = false;
		MouseEventData.MouseModifiers.IsLeftButtonDown = false;
		MouseEventData.WheelX = 0f;
		MouseEventData.WheelY = 0f;
		webView.MouseEvent(MouseEventData);
	}

	private static void SetModifiersState(EventModifiersState coUImods, SDL_Keymod mods)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Invalid comparison between Unknown and I4
		bool flag = (mods & 0x200) != 0 || ((mods & 0x40) != 0 && (mods & 0x100) > 0);
		coUImods.IsNumLockOn = (mods & 0x1000) > 0;
		coUImods.IsCapsOn = (mods & 0x2000) > 0;
		coUImods.IsCtrlDown = (mods & 0xC0) != 0 && !flag;
		coUImods.IsAltDown = (mods & 0x300) != 0 && !flag;
		coUImods.IsShiftDown = (mods & 3) > 0;
		coUImods.IsMetaDown = (mods & 0xC00) > 0;
	}
}
