using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Hypixel.ProtoPlus;
using HytaleClient.Data.Map;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.BuilderTools.Tools.Client;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools;

public class SelectionArea : IEnumerable<Vector3>, IEnumerable
{
	public enum SelectionRenderMode
	{
		LegacySelection,
		PlaySelection
	}

	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__31 : IEnumerator<Vector3>, IDisposable, IEnumerator
	{
		private int _003C_003E1__state;

		private Vector3 _003C_003E2__current;

		public SelectionArea _003C_003E4__this;

		private BoundingBox _003Cbounds_003E5__1;

		private float _003Cx_003E5__2;

		private float _003Cy_003E5__3;

		private float _003Cz_003E5__4;

		Vector3 IEnumerator<Vector3>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetEnumerator_003Ed__31(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				_003Cz_003E5__4++;
				goto IL_00db;
			}
			_003C_003E1__state = -1;
			if (!_003C_003E4__this.IsSelectionDefined())
			{
				return false;
			}
			_003Cbounds_003E5__1 = _003C_003E4__this.GetBoundsExclusiveMax();
			_003Cx_003E5__2 = _003Cbounds_003E5__1.Min.X;
			goto IL_0142;
			IL_00db:
			if (_003Cz_003E5__4 < _003Cbounds_003E5__1.Max.Z)
			{
				_003C_003E2__current = new Vector3(_003Cx_003E5__2, _003Cy_003E5__3, _003Cz_003E5__4);
				_003C_003E1__state = 1;
				return true;
			}
			_003Cy_003E5__3++;
			goto IL_010c;
			IL_010c:
			if (_003Cy_003E5__3 < _003Cbounds_003E5__1.Max.Y)
			{
				_003Cz_003E5__4 = _003Cbounds_003E5__1.Min.Z;
				goto IL_00db;
			}
			_003Cx_003E5__2++;
			goto IL_0142;
			IL_0142:
			if (_003Cx_003E5__2 < _003Cbounds_003E5__1.Max.X)
			{
				_003Cy_003E5__3 = _003Cbounds_003E5__1.Min.Y;
				goto IL_010c;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public Tuple<Vector3, Vector3, BoundingBox>[] SelectionData = new Tuple<Vector3, Vector3, BoundingBox>[8];

	public Vector3 Position1 = Vector3.NaN;

	public Vector3 Position2 = Vector3.NaN;

	public Vector3 SelectionSize;

	public Vector3 CenterPos;

	public const int SelectionCount = 8;

	public int SelectionIndex;

	public bool IsSelectionDirty = false;

	internal readonly SelectionToolRenderer Renderer;

	internal readonly BoxRenderer BoxRenderer;

	private readonly GameInstance _gameInstance;

	public SelectionRenderMode RenderMode = SelectionRenderMode.LegacySelection;

	public Vector3[] SelectionColors;

	public bool NeedsDrawing()
	{
		return IsSelectionDefined() || IsAnySelectionDefined();
	}

	public bool NeedsTextDrawing()
	{
		return NeedsDrawing();
	}

	internal SelectionArea(GameInstance gameInstance)
	{
		GraphicsDevice graphics = gameInstance.Engine.Graphics;
		_gameInstance = gameInstance;
		Renderer = new SelectionToolRenderer(graphics, gameInstance.App.Fonts.DefaultFontFamily.RegularFont);
		BoxRenderer = new BoxRenderer(graphics, graphics.GPUProgramStore.BasicProgram);
		SelectionColors = new Vector3[8] { graphics.WhiteColor, graphics.RedColor, graphics.GreenColor, graphics.BlueColor, graphics.CyanColor, graphics.MagentaColor, graphics.YellowColor, graphics.BlackColor };
	}

	public void Update()
	{
		if (IsSelectionDirty)
		{
			Renderer.UpdateSelection(Position1, Position2);
			IsSelectionDirty = false;
			int num = (int)MathHelper.Min(Position1.X, Position2.X);
			int num2 = (int)MathHelper.Min(Position1.Y, Position2.Y);
			int num3 = (int)MathHelper.Min(Position1.Z, Position2.Z);
			int num4 = (int)MathHelper.Max(Position1.X, Position2.X) + 1;
			int num5 = (int)MathHelper.Max(Position1.Y, Position2.Y) + 1;
			int num6 = (int)MathHelper.Max(Position1.Z, Position2.Z) + 1;
			int num7 = num4 - num;
			int num8 = num5 - num2;
			int num9 = num6 - num3;
			CenterPos = new Vector3((float)num + (float)num7 / 2f, (float)num2 + (float)num8 / 2f, (float)num3 + (float)num9 / 2f);
			SelectionSize = new Vector3(num7, num8, num9);
		}
	}

	public void DoDispose()
	{
		Renderer.Dispose();
		BoxRenderer.Dispose();
	}

	public BoundingBox GetBoundsExclusiveMax()
	{
		if (!IsSelectionDefined())
		{
			return new BoundingBox(Vector3.Zero, Vector3.Zero);
		}
		Vector3 min = Vector3.Min(Position1, Position2);
		Vector3 max = Vector3.Max(Position1, Position2) + Vector3.One;
		return new BoundingBox(min, max);
	}

	public BoundingBox GetBounds()
	{
		if (!IsSelectionDefined())
		{
			return new BoundingBox(Vector3.Zero, Vector3.Zero);
		}
		Vector3 min = Vector3.Min(Position1, Position2);
		Vector3 max = Vector3.Max(Position1, Position2);
		return new BoundingBox(min, max);
	}

	public Vector3 GetSize()
	{
		if (!IsSelectionDefined())
		{
			return Vector3.Zero;
		}
		return Vector3.Add(Vector3.Max(Position1, Position2) - Vector3.Min(Position1, Position2), Vector3.One);
	}

	public void OnSelectionChange()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		Vector3 vector = Vector3.Min(Position1, Position2);
		Vector3 vector2 = Vector3.Max(Position1, Position2);
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolSelectionUpdate((int)vector.X, (int)vector.Y, (int)vector.Z, (int)vector2.X, (int)vector2.Y, (int)vector2.Z));
	}

	public void UpdateSelection(Vector3 pos1, Vector3 pos2)
	{
		Position1 = Vector3.Min(pos1, pos2);
		Position2 = Vector3.Max(pos1, pos2);
		IsSelectionDirty = true;
	}

	public void ClearSelection()
	{
		Position1 = Vector3.NaN;
		Position2 = Vector3.NaN;
		_gameInstance.BuilderToolsModule.PlaySelection.Mode = PlaySelectionTool.EditMode.None;
		_gameInstance.BuilderToolsModule.PlaySelection.HoverMode = PlaySelectionTool.EditMode.None;
		_gameInstance.BuilderToolsModule.SelectionTool.Mode = SelectionTool.EditMode.None;
		_gameInstance.BuilderToolsModule.SelectionTool.HoverMode = SelectionTool.EditMode.None;
		SelectionData[SelectionIndex] = null;
	}

	public void Shift(Vector3 shiftAmount)
	{
		if (IsSelectionDefined())
		{
			Position1 += shiftAmount;
			Position2 += shiftAmount;
			IsSelectionDirty = true;
		}
	}

	public bool IsSelectionDefined()
	{
		return !Position1.IsNaN() && !Position2.IsNaN();
	}

	public bool IsAnySelectionDefined()
	{
		for (int i = 0; i < 8; i++)
		{
			if (SelectionData[i] != null)
			{
				return true;
			}
		}
		return false;
	}

	public void CycleSelectionIndex(bool forward = true)
	{
		int selectionIndex = ((!forward) ? ((SelectionIndex == 0) ? 7 : (SelectionIndex - 1)) : ((SelectionIndex != 7) ? (SelectionIndex + 1) : 0));
		SetSelectionIndex(selectionIndex);
	}

	public void SetSelectionIndex(int index)
	{
		if (index >= 0 && index <= 7 && SelectionIndex != index)
		{
			if (IsSelectionDefined())
			{
				BoundingBox item = BoundingBox.CreateFromPoints(new Vector3[2] { Position1, Position2 });
				item.Max += Vector3.One;
				SelectionData[SelectionIndex] = (IsSelectionDefined() ? new Tuple<Vector3, Vector3, BoundingBox>(Position1, Position2, item) : null);
			}
			SelectionIndex = index;
			if (SelectionData[SelectionIndex] == null)
			{
				ClearSelection();
			}
			else
			{
				Position1 = SelectionData[SelectionIndex].Item1;
				Position2 = SelectionData[SelectionIndex].Item2;
				IsSelectionDirty = true;
				OnSelectionChange();
			}
			string text = $"Selection set to #{SelectionIndex} - ";
			if (IsSelectionDefined())
			{
				Vector3 size = GetSize();
				text += $"[{size.X} x {size.Y} x {size.Z}]";
			}
			else
			{
				text += "Empty";
			}
			_gameInstance.Chat.Log(text);
		}
	}

	public void ListBlocks(bool clipobardOutput = false, string blockName = null)
	{
		if (!IsSelectionDefined())
		{
			_gameInstance.Chat.Log("Selection not defined");
			return;
		}
		int num = int.MaxValue;
		if (blockName != null)
		{
			ClientBlockType clientBlockTypeFromName = _gameInstance.MapModule.GetClientBlockTypeFromName(blockName);
			if (clientBlockTypeFromName == null)
			{
				_gameInstance.Chat.Log("Unable to find block type with id " + blockName);
				return;
			}
			num = clientBlockTypeFromName.Id;
		}
		if (num != int.MaxValue)
		{
			int num2 = 0;
			using (IEnumerator<Vector3> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Vector3 current = enumerator.Current;
					if (_gameInstance.MapModule.GetBlock(current, int.MaxValue) == num)
					{
						num2++;
					}
				}
			}
			Vector3 size = GetSize();
			float num3 = size.X * size.Y * size.Z;
			_gameInstance.Chat.Log($"{blockName}[{num}]: {num2} blocks | {(float)num2 / num3 * 100f}%");
			return;
		}
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		using (IEnumerator<Vector3> enumerator2 = GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				Vector3 current2 = enumerator2.Current;
				int block = _gameInstance.MapModule.GetBlock(current2, int.MaxValue);
				if (!dictionary.ContainsKey(block))
				{
					dictionary.Add(block, 1);
				}
				else
				{
					dictionary[block]++;
				}
			}
		}
		List<KeyValuePair<int, int>> list = dictionary.ToList();
		list.Sort((KeyValuePair<int, int> a, KeyValuePair<int, int> b) => b.Value.CompareTo(a.Value));
		Vector3 size2 = GetSize();
		float num4 = size2.X * size2.Y * size2.Z;
		string text = $"Selection [{size2.X} x {size2.Y} x {size2.Z}] {num4} total blocks{Environment.NewLine}";
		string text2 = "";
		if (clipobardOutput)
		{
			text2 += text;
		}
		else
		{
			_gameInstance.Chat.Log(text);
		}
		foreach (KeyValuePair<int, int> item in list)
		{
			string text3 = item.Key switch
			{
				int.MaxValue => "Undefined", 
				1 => "Unknown", 
				_ => _gameInstance.MapModule.ClientBlockTypes[item.Key].Name, 
			};
			string text4 = $"{text3}[{item.Key}]: {item.Value} blocks | {(float)item.Value / num4 * 100f}%";
			if (clipobardOutput)
			{
				text2 = text2 + text4 + Environment.NewLine;
			}
			else
			{
				_gameInstance.Chat.Log(text4);
			}
		}
		if (clipobardOutput)
		{
			SDL.SDL_SetClipboardText(text2);
			_gameInstance.Chat.Log("Data output to clipboard.");
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__31))]
	public IEnumerator<Vector3> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__31(0)
		{
			_003C_003E4__this = this
		};
	}
}
