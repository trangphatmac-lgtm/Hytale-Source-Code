using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class AnchorTool : ClientTool
{
	private BoxRenderer _boxRenderer;

	private Vector3 _target;

	private bool _enabled;

	public override string ToolId => "SetAnchor";

	public AnchorTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_boxRenderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
	}

	public void ShowAnchor(Vector3 position)
	{
		_target = position;
		_enabled = true;
	}

	public void HideAnchors()
	{
		_enabled = false;
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		GLFunctions gL = _graphics.GL;
		Vector3 magentaColor = _graphics.MagentaColor;
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		gL.DepthFunc(GL.ALWAYS);
		_boxRenderer.Draw(_target - cameraPosition, new BoundingBox(Vector3.Zero, Vector3.One), viewProjectionMatrix, magentaColor, 0.4f, magentaColor, 0.03f);
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public override bool NeedsDrawing()
	{
		return _enabled;
	}
}
