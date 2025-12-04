using System;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Brush;

internal class BrushAxisLockPlane : Disposable
{
	public enum EditMode
	{
		None,
		Translate,
		Rotate
	}

	public enum Gizmo
	{
		Translation,
		Rotation
	}

	private readonly GameInstance _gameInstance;

	private readonly RotationGizmo _rotationGizmo;

	private readonly TranslationGizmo _translationGizmo;

	private readonly BrushAxisLockPlaneRenderer _planeRenderer;

	private bool _isCurrentlyInteractingWithPlane;

	public bool Enabled { get; private set; } = false;


	public EditMode Mode { get; private set; } = EditMode.None;


	public Vector3 Position { get; private set; } = Vector3.Zero;


	public Matrix Rotation { get; private set; } = Matrix.Identity;


	public Vector3 Normal { get; private set; } = Vector3.Forward;


	public BrushAxisLockPlane(GameInstance gameInstance)
	{
		_gameInstance = gameInstance;
		GraphicsDevice graphics = gameInstance.Engine.Graphics;
		_rotationGizmo = new RotationGizmo(graphics, gameInstance.App.Fonts.DefaultFontFamily.RegularFont, OnRotationChange);
		_translationGizmo = new TranslationGizmo(graphics, OnPositionChange);
		_planeRenderer = new BrushAxisLockPlaneRenderer(graphics);
	}

	protected override void DoDispose()
	{
		_rotationGizmo.Dispose();
		_translationGizmo.Dispose();
		_planeRenderer.Dispose();
	}

	private void UpdatePlane()
	{
		Normal = Vector3.TransformNormal(Vector3.Forward, Rotation);
		_planeRenderer.UpdatePlane(Position, Rotation);
	}

	public void Update(float deltaTime)
	{
		if (Enabled)
		{
			Ray lookRay = _gameInstance.CameraModule.GetLookRay();
			float targetBlockHitDistance = (_gameInstance.InteractionModule.HasFoundTargetBlock ? _gameInstance.InteractionModule.TargetBlockHit.Distance : 0f);
			_translationGizmo.Tick(lookRay);
			_rotationGizmo.Tick(lookRay, targetBlockHitDistance);
			_rotationGizmo.UpdateRotation(snapValue: true);
		}
	}

	public void Draw(ref Matrix viewProjectionMatrix)
	{
		if (Enabled)
		{
			SceneRenderer.SceneData data = _gameInstance.SceneRenderer.Data;
			if (_rotationGizmo.Visible)
			{
				_rotationGizmo.Draw(ref viewProjectionMatrix, _gameInstance.CameraModule.Controller, -data.CameraPosition);
			}
			if (_translationGizmo.Visible)
			{
				_translationGizmo.Draw(ref viewProjectionMatrix, -data.CameraPosition);
			}
			_planeRenderer.Draw(ref viewProjectionMatrix, -_gameInstance.SceneRenderer.Data.CameraPosition, Vector3.One, 1f);
		}
	}

	public bool OnInteract(InteractionType interactionType, InteractionModule.ClickType clickType, bool firstRun)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled)
		{
			return false;
		}
		bool result = PerformInteractions(interactionType, clickType, firstRun);
		if (_isCurrentlyInteractingWithPlane)
		{
			if (clickType == InteractionModule.ClickType.None)
			{
				_isCurrentlyInteractingWithPlane = false;
			}
			return true;
		}
		return result;
	}

	private bool PerformInteractions(InteractionType interactionType, InteractionModule.ClickType clickType, bool firstRun)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (CancelTransform(interactionType, firstRun))
		{
			return true;
		}
		if (TranslationGizmoInteraction(interactionType, clickType, firstRun))
		{
			return true;
		}
		if (RotationGizmoInteraction(interactionType, clickType, firstRun))
		{
			return true;
		}
		return false;
	}

	private bool CancelTransform(InteractionType interactionType, bool firstRun)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)interactionType != 1)
		{
			return false;
		}
		if (!firstRun)
		{
			return false;
		}
		if (Mode == EditMode.Rotate || Mode == EditMode.Translate)
		{
			_isCurrentlyInteractingWithPlane = true;
			ExitGizmoTransformMode();
			return true;
		}
		return false;
	}

	private bool TranslationGizmoInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, bool firstRun)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (Mode != EditMode.Translate)
		{
			return false;
		}
		if (!firstRun && clickType != InteractionModule.ClickType.None)
		{
			return false;
		}
		_isCurrentlyInteractingWithPlane = true;
		_translationGizmo.OnInteract(_gameInstance.CameraModule.GetLookRay(), interactionType);
		if (clickType == InteractionModule.ClickType.None)
		{
			_translationGizmo.Show(EpsilonFloorVector3(Position), Vector3.Forward);
		}
		return true;
	}

	private bool RotationGizmoInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, bool firstRun)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (Mode != EditMode.Rotate)
		{
			return false;
		}
		if (!firstRun && clickType != InteractionModule.ClickType.None)
		{
			return false;
		}
		_isCurrentlyInteractingWithPlane = true;
		_rotationGizmo.OnInteract(interactionType);
		return true;
	}

	private void OnRotationChange(Vector3 rotation)
	{
		Rotation = Matrix.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
		UpdatePlane();
	}

	private void OnPositionChange(Vector3 translatedTo)
	{
		Position = EpsilonFloorVector3(translatedTo);
		UpdatePlane();
	}

	private void EnterAxisLockMode(Gizmo gizmo)
	{
		Enabled = true;
		Mode = ((gizmo != Gizmo.Rotation) ? EditMode.Translate : EditMode.Rotate);
		_gameInstance.Chat.Log("Entered Brush Plane Lock Mode");
		_gameInstance.BuilderToolsModule.Brush.useServerRaytrace = false;
		Vector3 value = new Vector3(MathHelper.SnapRadianTo90Degrees(_gameInstance.LocalPlayer.LookOrientation.X), MathHelper.SnapRadianTo90Degrees(_gameInstance.LocalPlayer.LookOrientation.Y), MathHelper.SnapRadianTo90Degrees(_gameInstance.LocalPlayer.LookOrientation.Z));
		_rotationGizmo.Show(Position, value);
		_rotationGizmo.Hide();
		Rotation = Matrix.CreateFromYawPitchRoll(value.Yaw, value.Pitch, value.Roll);
		UpdateGizmoPosition(gizmo);
	}

	private void ExitGizmoTransformMode()
	{
		Mode = EditMode.None;
		_rotationGizmo.Hide();
		_translationGizmo.Hide();
	}

	private void SwapAxisLockMode()
	{
		if (Mode == EditMode.Rotate)
		{
			_rotationGizmo.Hide();
			UpdateGizmoPosition(Gizmo.Translation, updateUsingPlaneIntersection: true);
		}
		else if (Mode == EditMode.Translate)
		{
			_translationGizmo.Hide();
			UpdateGizmoPosition(Gizmo.Rotation, updateUsingPlaneIntersection: true);
		}
	}

	private void UpdateGizmoPosition(Gizmo gizmo, bool updateUsingPlaneIntersection = false)
	{
		Vector3 translatedTo = _gameInstance.InteractionModule.TargetBlockHit.BlockPosition;
		if (updateUsingPlaneIntersection)
		{
			translatedTo = GetIntersectionPointOnPlane();
		}
		if (translatedTo.IsNaN())
		{
			translatedTo = Vector3.Floor(_gameInstance.LocalPlayer.Position);
		}
		OnPositionChange(translatedTo);
		if (gizmo == Gizmo.Rotation)
		{
			_rotationGizmo.Show(Position);
		}
		else
		{
			_translationGizmo.Show(Position, Vector3.Forward);
		}
		if (gizmo == Gizmo.Rotation)
		{
			Mode = EditMode.Rotate;
		}
		else
		{
			Mode = EditMode.Translate;
		}
	}

	private Vector3 GetIntersectionPointOnPlane()
	{
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		if (HitDetection.CheckRayPlaneIntersection(Position, Normal, lookRay.Position, lookRay.Direction, out var intersection, forwardOnly: true))
		{
			return intersection;
		}
		return Vector3.NaN;
	}

	public void OnKeyDown()
	{
		if (_gameInstance.Input.ConsumeKey((SDL_Scancode)8))
		{
			if (!Enabled)
			{
				EnterAxisLockMode(Gizmo.Translation);
			}
			if (Mode == EditMode.None || Mode == EditMode.Translate)
			{
				UpdateGizmoPosition(Gizmo.Translation, updateUsingPlaneIntersection: true);
			}
			else if (Mode == EditMode.Rotate)
			{
				SwapAxisLockMode();
			}
		}
		if (_gameInstance.Input.ConsumeKey((SDL_Scancode)21))
		{
			if (!Enabled)
			{
				EnterAxisLockMode(Gizmo.Rotation);
			}
			else if (Mode == EditMode.None || Mode == EditMode.Rotate)
			{
				UpdateGizmoPosition(Gizmo.Rotation, updateUsingPlaneIntersection: true);
			}
			else if (Mode == EditMode.Translate)
			{
				SwapAxisLockMode();
			}
		}
		if (!_gameInstance.Input.IsShiftHeld() && _gameInstance.Input.ConsumeKey((SDL_Scancode)29))
		{
			Disable();
		}
	}

	private static Vector3 EpsilonFloorVector3(Vector3 vector3, float epsilon = 0.1f)
	{
		return new Vector3((int)System.Math.Floor(vector3.X + epsilon), (int)System.Math.Floor(vector3.Y + epsilon), (int)System.Math.Floor(vector3.Z + epsilon));
	}

	public void Disable()
	{
		if (Enabled)
		{
			ExitGizmoTransformMode();
			_gameInstance.BuilderToolsModule.Brush.useServerRaytrace = true;
			Enabled = false;
			_gameInstance.Chat.Log("Exited Brush Plane Lock Mode");
		}
	}

	public Vector3 GetPosition()
	{
		return Position;
	}

	public Matrix GetRotation()
	{
		return Rotation;
	}

	public Vector3 GetNormal()
	{
		return Normal;
	}

	public bool IsEnabled()
	{
		return Enabled;
	}

	public EditMode GetMode()
	{
		return Mode;
	}
}
