using System;
using Coherent.UI.Binding;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.InGame.Modules.Camera.Controllers;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Machinima.Settings;
using HytaleClient.InGame.Modules.Machinima.Track;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.InGame.Modules.Machinima.Actors;

[CoherentType]
internal class CameraActor : SceneActor, ICameraController
{
	private GameInstance _gameInstance;

	private Vector3 _rotation;

	public float FieldOfView = 70f;

	private float _previousFieldOfView = 70f;

	private PrimitiveModelRenderer _modelRenderer;

	private LineRenderer _lineRenderer;

	private Vector3 _cameraColor = new Vector3(0f, 1f, 1f);

	public float SpeedModifier { get; } = 1f;


	public bool AllowPitchControls => false;

	public bool DisplayCursor => false;

	public bool DisplayReticle => true;

	public bool SkipCharacterPhysics => false;

	public bool IsFirstPerson => false;

	public bool IsAttachedToCharacter => false;

	public bool CanMove => true;

	public Entity AttachedTo => null;

	[CoherentProperty("active")]
	public bool Active => _gameInstance.CameraModule.Controller == this;

	public Vector3 Offset { get; private set; }

	public Vector3 MovementForceRotation => _rotation;

	public Vector3 PositionOffset => Vector3.Zero;

	public Vector3 RotationOffset => Vector3.Zero;

	public new Vector3 Position => base.Position;

	public new Vector3 Rotation => _rotation;

	public Vector3 MovementLook => Look;

	public bool InteractFromEntity { get; }

	public Vector3 AttachmentPosition { get; }

	public Vector3 LookAt { get; }

	protected override ActorType GetActorType()
	{
		return ActorType.Camera;
	}

	public CameraActor(GameInstance gameInstance, string name)
		: base(gameInstance, name)
	{
		_gameInstance = gameInstance;
		_modelRenderer = new PrimitiveModelRenderer(gameInstance.Engine.Graphics, gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram);
		_modelRenderer.UpdateModelData(CameraModel.BuildModelData());
		_lineRenderer = new LineRenderer(gameInstance.Engine.Graphics, gameInstance.Engine.Graphics.GPUProgramStore.BasicProgram);
	}

	protected override void DoDispose()
	{
		_modelRenderer.Dispose();
		base.DoDispose();
	}

	public void SetRotation(Vector3 rotation)
	{
	}

	public void ApplyLook(float deltaTime, Vector2 look)
	{
	}

	public void OnMouseInput(SDL_Event evt)
	{
	}

	public void ApplyMove(Vector3 movementOffset)
	{
	}

	public void Reset(GameInstance gameInstance, ICameraController cameraController)
	{
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		if (!Active)
		{
			Vector3 position = Position;
			_modelMatrix = Matrix.Identity;
			Matrix.CreateRotationX(0f - Look.Roll, out _tempMatrix);
			Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
			Matrix.CreateFromYawPitchRoll(Look.Yaw + (float)System.Math.PI / 2f, 0f, Look.Pitch, out _tempMatrix);
			Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
			Matrix.CreateTranslation(ref position, out _tempMatrix);
			Matrix.Multiply(ref _modelMatrix, ref _tempMatrix, out _modelMatrix);
			float opacity = 0.4f;
			if (_gameInstance.MachinimaModule.ActiveActor == this)
			{
				opacity = 0.8f;
			}
			_modelRenderer.Draw(viewProjectionMatrix, _modelMatrix, _cameraColor, opacity);
			if (_gameInstance.MachinimaModule.ActiveActor == this && _gameInstance.MachinimaModule.ShowCameraFrustum)
			{
				DrawViewFrustum(ref viewProjectionMatrix);
			}
		}
	}

	private void DrawViewFrustum(ref Matrix viewProjectionMatrix)
	{
		float opacity = 0.5f;
		Vector3 whiteColor = _gameInstance.Engine.Graphics.WhiteColor;
		_gameInstance.Engine.Graphics.CreatePerspectiveMatrix(MathHelper.ToRadians(FieldOfView), (float)_gameInstance.Engine.Window.AspectRatio, 0.1f, 1024f, out var result);
		Matrix.CreateRotationX(0f - Look.X, out var result2);
		Matrix.CreateRotationY(0f - Look.Y, out var result3);
		Matrix.Multiply(ref result3, ref result2, out var result4);
		Matrix.CreateRotationZ(0f - Look.Z, out result3);
		Matrix.Multiply(ref result4, ref result3, out result4);
		Matrix.Multiply(ref result4, ref result, out var result5);
		Matrix matrix = Matrix.Invert(result5);
		Matrix matrix2 = Matrix.Invert(result4);
		Matrix matrix3 = Matrix.Invert(result);
		Vector3 vector = _gameInstance.LocalPlayer.Position - Position;
		Vector3 position = -Position;
		Matrix.CreateTranslation(ref position, out result3);
		Matrix.Multiply(ref result3, ref result4, out result2);
		Matrix.Multiply(ref result2, ref result, out var result6);
		Matrix invViewProjection = Matrix.Invert(result6);
		Vector3.ScreenToWorldRay(new Vector2(-1f, -1f), Position, invViewProjection, out var position2, out var direction);
		Vector3.ScreenToWorldRay(new Vector2(1f, -1f), Position, invViewProjection, out var position3, out var direction2);
		Vector3.ScreenToWorldRay(new Vector2(1f, 1f), Position, invViewProjection, out var position4, out var direction3);
		Vector3.ScreenToWorldRay(new Vector2(-1f, 1f), Position, invViewProjection, out var position5, out var direction4);
		int num = 5;
		Vector3 vector2 = Position + direction * num;
		Vector3 vector3 = Position + direction2 * num;
		Vector3 vector4 = Position + direction3 * num;
		Vector3 vector5 = Position + direction4 * num;
		_lineRenderer.UpdateLineData(new Vector3[5] { position2, position3, position4, position5, position2 });
		_lineRenderer.Draw(ref viewProjectionMatrix, whiteColor, opacity);
		_lineRenderer.UpdateLineData(new Vector3[2] { Position, vector2 });
		_lineRenderer.Draw(ref viewProjectionMatrix, whiteColor, opacity);
		_lineRenderer.UpdateLineData(new Vector3[2] { Position, vector3 });
		_lineRenderer.Draw(ref viewProjectionMatrix, whiteColor, opacity);
		_lineRenderer.UpdateLineData(new Vector3[2] { Position, vector4 });
		_lineRenderer.Draw(ref viewProjectionMatrix, whiteColor, opacity);
		_lineRenderer.UpdateLineData(new Vector3[2] { Position, vector5 });
		_lineRenderer.Draw(ref viewProjectionMatrix, whiteColor, opacity);
		_lineRenderer.UpdateLineData(new Vector3[5] { vector2, vector3, vector4, vector5, vector2 });
		_lineRenderer.Draw(ref viewProjectionMatrix, whiteColor, opacity);
	}

	public void Update(float deltaTime)
	{
	}

	public void SetState(bool newState)
	{
		if (newState && !Active)
		{
			if (_gameInstance.CameraModule.Controller is CameraActor)
			{
				_gameInstance.CameraModule.ResetCameraController();
			}
			_gameInstance.CameraModule.SetCustomCameraController(this);
			_previousFieldOfView = _gameInstance.ActiveFieldOfView;
			_gameInstance.SetFieldOfView(FieldOfView);
		}
		else if (!newState && Active)
		{
			_gameInstance.CameraModule.ResetCameraController();
			float previousFieldOfView = _previousFieldOfView;
			_previousFieldOfView = _gameInstance.ActiveFieldOfView;
			_gameInstance.SetFieldOfView(previousFieldOfView);
		}
	}

	public override void LoadKeyframe(TrackKeyframe keyframe)
	{
		base.LoadKeyframe(keyframe);
		_rotation = Look;
		Offset = Position - _gameInstance.LocalPlayer.Position;
		KeyframeSetting<float> setting = keyframe.GetSetting<float>("FieldOfView");
		if (setting != null)
		{
			FieldOfView = setting.Value;
			if (Active)
			{
				_previousFieldOfView = _gameInstance.ActiveFieldOfView;
				_gameInstance.SetFieldOfView(FieldOfView);
			}
		}
	}

	public override SceneActor Clone(GameInstance gameInstance)
	{
		SceneActor actor = new CameraActor(gameInstance, "clone");
		base.Track.CopyToActor(ref actor);
		return actor as CameraActor;
	}
}
