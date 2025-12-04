using System;
using Hypixel.ProtoPlus;
using HytaleClient.Data.Items;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class LineTool : ClientTool
{
	private enum LineConnectionType
	{
		Continous,
		Split,
		Origin
	}

	private const string TypeArgKey = "LineType";

	private const string RadiusArgKey = "LineRadius";

	private const int SnapAngleDegrees = 45;

	private readonly BoxRenderer _boxRenderer;

	private readonly LineRenderer _lineRenderer;

	private readonly BoundingBox _blockBox;

	private readonly Vector3 _lineOffset;

	private int LineRadius;

	private LineConnectionType LineType;

	private bool _lineStarted;

	private bool _hasTarget;

	private Vector3 _origin;

	private Vector3 _target;

	public override string ToolId => "Line";

	public LineTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_boxRenderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_lineRenderer = new LineRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		Vector3 vector = new Vector3(0.05f, 0.05f, 0.05f);
		_blockBox = new BoundingBox(Vector3.Zero - vector, Vector3.One + vector);
		_lineOffset = Vector3.One * 0.5f;
	}

	protected override void DoDispose()
	{
		_boxRenderer.Dispose();
		_lineRenderer.Dispose();
	}

	public override void Update(float deltaTime)
	{
		if (base.BrushTarget.IsNaN())
		{
			_hasTarget = false;
		}
		else if (!(base.BrushTarget == _target))
		{
			_target = base.BrushTarget;
			_hasTarget = true;
			bool flag = _gameInstance.Input.IsShiftHeld();
			if (_lineStarted && flag)
			{
				Vector3 vector = Vector3.Normalize(_origin - _target);
				float num = Vector3.Distance(_origin, _target);
				float value = (float)System.Math.Asin(0f - vector.Y);
				float value2 = (float)System.Math.Atan2(vector.X, vector.Z);
				float interval = MathHelper.ToRadians(45f);
				float yaw = MathHelper.SnapValue(value2, interval);
				float pitch = MathHelper.SnapValue(value, interval);
				Matrix.CreateFromYawPitchRoll(yaw, pitch, 0f, out var result);
				Vector3 vector2 = Vector3.Transform(Vector3.Forward, result);
				_target = _origin + vector2 * num;
				_target = new Vector3((float)System.Math.Floor(_target.X), (float)System.Math.Floor(_target.Y), (float)System.Math.Floor(_target.Z));
			}
			if (_lineStarted)
			{
				_lineRenderer.UpdateLineData(new Vector3[2]
				{
					_origin + _lineOffset,
					_target + _lineOffset
				});
			}
		}
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		if (_hasTarget)
		{
			_boxRenderer.Draw(_target - cameraPosition, _blockBox, viewProjectionMatrix, _graphics.WhiteColor, 0.3f, _graphics.WhiteColor, 0.2f);
		}
		if (_lineStarted)
		{
			_boxRenderer.Draw(_origin - cameraPosition, _blockBox, viewProjectionMatrix, _graphics.WhiteColor, 0.3f, _graphics.WhiteColor, 0.2f);
		}
		if (_hasTarget && _lineStarted)
		{
			Vector3 vector = _origin - _target;
			Vector3 color = _graphics.WhiteColor;
			if (vector.X == 0f && vector.Z == 0f && vector.Y != 0f)
			{
				color = _graphics.GreenColor;
			}
			else if (vector.Y == 0f && vector.Z == 0f && vector.X != 0f)
			{
				color = _graphics.RedColor;
			}
			else if (vector.X == 0f && vector.Y == 0f && vector.Z != 0f)
			{
				color = _graphics.BlueColor;
			}
			_lineRenderer.Draw(ref _gameInstance.SceneRenderer.Data.ViewProjectionMatrix, color, 0.75f);
		}
	}

	public override bool NeedsDrawing()
	{
		return _hasTarget || _lineStarted;
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		if (clickType == InteractionModule.ClickType.None)
		{
			return;
		}
		if ((int)interactionType == 0)
		{
			_lineStarted = false;
			return;
		}
		if (_lineStarted)
		{
			OnLineAction(_origin, _target);
		}
		switch (LineType)
		{
		case LineConnectionType.Continous:
			if (!_lineStarted)
			{
				_lineStarted = true;
			}
			_origin = _target;
			break;
		case LineConnectionType.Split:
			if (!_lineStarted)
			{
				_lineStarted = true;
				_origin = _target;
			}
			else
			{
				_lineStarted = false;
			}
			break;
		case LineConnectionType.Origin:
			if (!_lineStarted)
			{
				_lineStarted = true;
				_origin = _target;
			}
			break;
		}
	}

	public override void OnToolItemChange(ClientItemStack itemStack)
	{
		BuilderTool toolFromItemStack = BuilderTool.GetToolFromItemStack(_gameInstance, itemStack);
		if (toolFromItemStack != null)
		{
			object obj;
			if (itemStack == null)
			{
				obj = null;
			}
			else
			{
				JObject metadata = itemStack.Metadata;
				obj = ((metadata != null) ? metadata["ToolData"] : null);
			}
			if (obj != null)
			{
				LineRadius = int.Parse(toolFromItemStack.GetItemArgValueOrDefault(ref itemStack, "LineRadius"));
				LineType = (LineConnectionType)Enum.Parse(typeof(LineConnectionType), toolFromItemStack.GetItemArgValueOrDefault(ref itemStack, "LineType"));
			}
		}
	}

	private void OnLineAction(Vector3 start, Vector3 end)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolLineAction((int)start.X, (int)start.Y, (int)start.Z, (int)end.X, (int)end.Y, (int)end.Z));
	}
}
