using System.Collections.Generic;
using Hypixel.ProtoPlus;
using HytaleClient.Core;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Graphics.Gizmos;
using HytaleClient.Graphics.Programs;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Math;
using HytaleClient.Protocol;
using SDL2;

namespace HytaleClient.InGame.Modules.BuilderTools.Tools.Client;

internal class EntityTool : ClientTool
{
	private static readonly BoundingBox _headBox = new BoundingBox(new Vector3(-0.25f, -0.25f, -0.25f), new Vector3(0.25f, 0.25f, 0.25f));

	private static readonly Vector3 _boxOffset = new Vector3(0.01f, 0.01f, 0.01f);

	private readonly RotationGizmo _rotationGizmo;

	private readonly TranslationGizmo _translationGizmo;

	private readonly BoxRenderer _boxRenderer;

	private readonly TextRenderer _textRenderer;

	private Entity _selectedEntity;

	private Entity _hoveredEntity;

	private Matrix _tempMatrix;

	private Matrix _drawMatrix;

	private Matrix _textMatrix;

	private Vector3 _textPosition = Vector3.Zero;

	private float _fillBlurThreshold;

	private Vector3 _lastEntityPosition = Vector3.Zero;

	private Vector3 _moveOffset = Vector3.Zero;

	private ToolMode _editMode = ToolMode.None;

	private float _targetDistance;

	private bool _lockToSurface;

	private bool _headSelected;

	private readonly Dictionary<Keybind, SDL_Scancode> _keybinds = new Dictionary<Keybind, SDL_Scancode>
	{
		{
			Keybind.Clone,
			(SDL_Scancode)73
		},
		{
			Keybind.Remove,
			(SDL_Scancode)76
		},
		{
			Keybind.ToggleFreeze,
			(SDL_Scancode)72
		},
		{
			Keybind.ToggleSurface,
			(SDL_Scancode)74
		}
	};

	public override string ToolId => "Entity";

	public EntityTool(GameInstance gameInstance)
		: base(gameInstance)
	{
		_rotationGizmo = new RotationGizmo(_graphics, _gameInstance.App.Fonts.DefaultFontFamily.RegularFont, OnRotationChange);
		_translationGizmo = new TranslationGizmo(_graphics, OnPositionChange);
		_boxRenderer = new BoxRenderer(_graphics, _graphics.GPUProgramStore.BasicProgram);
		_textRenderer = new TextRenderer(_graphics, _gameInstance.App.Fonts.DefaultFontFamily.RegularFont, ToolId);
	}

	protected override void DoDispose()
	{
		_boxRenderer.Dispose();
		_translationGizmo.Dispose();
		_rotationGizmo.Dispose();
		_textRenderer.Dispose();
	}

	public override void Update(float deltaTime)
	{
		Input input = _gameInstance.Input;
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		float targetBlockHitDistance = (_gameInstance.InteractionModule.HasFoundTargetBlock ? _gameInstance.InteractionModule.TargetBlockHit.Distance : 0f);
		_translationGizmo.Tick(lookRay);
		_rotationGizmo.Tick(lookRay, targetBlockHitDistance);
		_rotationGizmo.UpdateRotation(input.IsShiftHeld());
		if (_selectedEntity != null && _editMode == ToolMode.FreeMove)
		{
			Vector3 position = ((!_lockToSurface || !_gameInstance.InteractionModule.HasFoundTargetBlock) ? (lookRay.Position + lookRay.Direction * _targetDistance - _moveOffset) : _gameInstance.InteractionModule.TargetBlockHit.HitPosition);
			OnPositionChange(position);
		}
		_gameInstance.HitDetection.RaycastEntity(lookRay.Position, lookRay.Direction, 100f, checkOnlyTangibleEntities: false, out var entityHitData);
		_hoveredEntity = entityHitData.Entity;
		if (_hoveredEntity != null && _hoveredEntity.Type == Entity.EntityType.Character)
		{
			BoundingBox headBox = _headBox;
			headBox.Translate(_hoveredEntity.Position + new Vector3(0f, _hoveredEntity.EyeOffset, 0f));
			_headSelected = HitDetection.CheckRayBoxCollision(headBox, lookRay.Position, lookRay.Direction, out var _);
		}
		else
		{
			_headSelected = false;
		}
		_textRenderer.Text = GetDisplayText();
		if (_gameInstance.Input.IsAnyKeyHeld())
		{
			OnKeyDown();
		}
	}

	public override void Draw(ref Matrix viewProjectionMatrix)
	{
		base.Draw(ref viewProjectionMatrix);
		GLFunctions gL = _graphics.GL;
		gL.DepthMask(write: true);
		Vector3 cameraPosition = _gameInstance.SceneRenderer.Data.CameraPosition;
		if (_rotationGizmo.Visible)
		{
			_rotationGizmo.Draw(ref viewProjectionMatrix, _gameInstance.CameraModule.Controller, -cameraPosition);
		}
		if (_translationGizmo.Visible)
		{
			_translationGizmo.Draw(ref viewProjectionMatrix, -cameraPosition);
		}
		if ((_selectedEntity != null || _hoveredEntity != null) && (_editMode == ToolMode.None || _editMode == ToolMode.FreeMove))
		{
			Entity entity = _hoveredEntity;
			Vector3 outlineColor = _graphics.BlueColor;
			float quadOpacity = 0.3f;
			if (_selectedEntity != null)
			{
				entity = _selectedEntity;
				outlineColor = _graphics.RedColor;
				quadOpacity = 0.15f;
			}
			if (entity.ModelRenderer == null || (entity.NetworkId == _gameInstance.LocalPlayerNetworkId && _gameInstance.CameraModule.Controller.IsFirstPerson))
			{
				return;
			}
			BoundingBox hitbox = entity.Hitbox;
			hitbox.Min -= _boxOffset;
			hitbox.Max += _boxOffset;
			if (_headSelected)
			{
				_boxRenderer.Draw(entity.Position + new Vector3(0f, entity.EyeOffset, 0f) - cameraPosition, _headBox, viewProjectionMatrix, outlineColor, 0.5f, _graphics.WhiteColor, quadOpacity);
			}
			else
			{
				_boxRenderer.Draw(entity.Position - cameraPosition, hitbox, viewProjectionMatrix, outlineColor, 0.7f, _graphics.WhiteColor, quadOpacity);
			}
		}
		gL.DepthMask(write: false);
	}

	public override void DrawText(ref Matrix viewProjectionMatrix)
	{
		base.DrawText(ref viewProjectionMatrix);
		PrepareForTextDraw(ref viewProjectionMatrix);
		if (_editMode == ToolMode.RotateBody || _editMode == ToolMode.RotateHead)
		{
			_rotationGizmo.DrawText();
			return;
		}
		GLFunctions gL = _graphics.GL;
		TextProgram textProgram = _graphics.GPUProgramStore.TextProgram;
		textProgram.AssertInUse();
		textProgram.Position.SetValue(_textPosition);
		textProgram.FillBlurThreshold.SetValue(_fillBlurThreshold);
		textProgram.MVPMatrix.SetValue(ref _textMatrix);
		gL.DepthFunc(GL.ALWAYS);
		_textRenderer.Draw();
		gL.DepthFunc((!_graphics.UseReverseZ) ? GL.LEQUAL : GL.GEQUAL);
	}

	public override bool NeedsDrawing()
	{
		return _selectedEntity != null || _hoveredEntity != null;
	}

	public override bool NeedsTextDrawing()
	{
		return NeedsDrawing() || _editMode == ToolMode.RotateBody || _editMode == ToolMode.RotateHead;
	}

	private void PrepareForTextDraw(ref Matrix viewProjectionMatrix)
	{
		float scale = 0.2f / (float)_gameInstance.App.Fonts.DefaultFontFamily.RegularFont.BaseSize;
		int spread = _gameInstance.App.Fonts.DefaultFontFamily.RegularFont.Spread;
		float num = 1f / (float)spread;
		Vector3 vector = _gameInstance.LocalPlayer.Position + new Vector3(0f, _gameInstance.LocalPlayer.EyeOffset, 0f);
		Vector3 vector2 = ((_selectedEntity != null) ? _selectedEntity.Position : ((_hoveredEntity != null) ? _hoveredEntity.Position : _lastEntityPosition));
		vector2.Y = vector2.Y - 0.5f + ((!_headSelected) ? 0f : ((_selectedEntity != null) ? _selectedEntity.EyeOffset : _hoveredEntity.EyeOffset));
		_textPosition = vector2 - vector;
		float num2 = Vector3.Distance(vector2, vector);
		_fillBlurThreshold = MathHelper.Clamp(2f * num2 * 0.1f, 1f, spread) * num;
		Matrix.CreateTranslation(0f - _textRenderer.GetHorizontalOffset(TextRenderer.TextAlignment.Center), 0f - _textRenderer.GetVerticalOffset(TextRenderer.TextVerticalAlignment.Middle), 0f, out _tempMatrix);
		Matrix.CreateScale(scale, out _drawMatrix);
		Matrix.Multiply(ref _tempMatrix, ref _drawMatrix, out _drawMatrix);
		Vector3 rotation = _gameInstance.CameraModule.Controller.Rotation;
		Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, 0f, out _tempMatrix);
		Matrix.Multiply(ref _drawMatrix, ref _tempMatrix, out _drawMatrix);
		Matrix.AddTranslation(ref _drawMatrix, vector2.X, vector2.Y, vector2.Z);
		Matrix.Multiply(ref _drawMatrix, ref viewProjectionMatrix, out _textMatrix);
	}

	public override void OnInteraction(InteractionType interactionType, InteractionModule.ClickType clickType, InteractionContext context, bool firstRun)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Invalid comparison between Unknown and I4
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Invalid comparison between Unknown and I4
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		if (clickType == InteractionModule.ClickType.None)
		{
			return;
		}
		Input input = _gameInstance.Input;
		Ray lookRay = _gameInstance.CameraModule.GetLookRay();
		if (_rotationGizmo.Visible && (!input.IsAnyModifierHeld() || (int)interactionType == 1 || _rotationGizmo.InUse()))
		{
			_rotationGizmo.OnInteract(interactionType);
			if (!_rotationGizmo.Visible && (_editMode == ToolMode.RotateBody || _editMode == ToolMode.RotateHead))
			{
				_selectedEntity = null;
				_editMode = ToolMode.None;
			}
		}
		else if (_translationGizmo.Visible && (!input.IsAnyModifierHeld() || (int)interactionType == 1 || _translationGizmo.InUse()))
		{
			_translationGizmo.OnInteract(lookRay, interactionType);
			if (!_translationGizmo.Visible && _editMode == ToolMode.Translate)
			{
				_selectedEntity = null;
				_editMode = ToolMode.None;
			}
		}
		else if ((int)interactionType == 0)
		{
			if (_selectedEntity != null && !input.IsAnyModifierHeld())
			{
				_selectedEntity = null;
				_editMode = ToolMode.None;
			}
			else
			{
				if (_hoveredEntity == null)
				{
					return;
				}
				_selectedEntity = _hoveredEntity;
				if (input.IsAltHeld())
				{
					_translationGizmo.Hide();
					if (_headSelected)
					{
						_rotationGizmo.Show(_selectedEntity.Position + new Vector3(0f, _selectedEntity.EyeOffset, 0f), _selectedEntity.LookOrientation);
						_editMode = ToolMode.RotateHead;
					}
					else
					{
						_rotationGizmo.Show(_selectedEntity.Position, _selectedEntity.BodyOrientation);
						_editMode = ToolMode.RotateBody;
					}
				}
				else if (input.IsShiftHeld())
				{
					_rotationGizmo.Hide();
					if (_headSelected)
					{
						_translationGizmo.Show(_selectedEntity.Position, new Vector3(_selectedEntity.LookOrientation.Pitch, _selectedEntity.LookOrientation.Yaw, 0f));
					}
					else
					{
						_translationGizmo.Show(_selectedEntity.Position, _selectedEntity.BodyOrientation);
					}
					_editMode = ToolMode.Translate;
				}
				else
				{
					_editMode = ToolMode.FreeMove;
					BoundingBox hitbox = _hoveredEntity.Hitbox;
					hitbox.Translate(_hoveredEntity.Position);
					if (HitDetection.CheckRayBoxCollision(hitbox, lookRay.Position, lookRay.Direction, out var collision))
					{
						_moveOffset = collision.Position - _hoveredEntity.Position;
					}
				}
				_targetDistance = Vector3.Distance(lookRay.Position, _selectedEntity.Position + _moveOffset);
				_lastEntityPosition = _selectedEntity.Position;
			}
		}
		else
		{
			if (_selectedEntity != null)
			{
				_selectedEntity.SetPosition(_lastEntityPosition);
				OnPositionChange(_lastEntityPosition);
				_selectedEntity = null;
			}
			_editMode = ToolMode.None;
		}
	}

	protected override void OnActiveStateChange(bool newState)
	{
		if (!newState)
		{
			if (_editMode == ToolMode.RotateBody || _editMode == ToolMode.RotateHead)
			{
				_rotationGizmo.Hide();
			}
			if (_editMode == ToolMode.Translate)
			{
				_translationGizmo.Hide();
			}
			_selectedEntity = null;
			_editMode = ToolMode.None;
		}
	}

	private void OnKeyDown()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		Input input = _gameInstance.Input;
		if (input.ConsumeKey(_keybinds[Keybind.ToggleSurface]))
		{
			_lockToSurface = !_lockToSurface;
		}
		Entity entity = ((_selectedEntity == null) ? _hoveredEntity : _selectedEntity);
		if (entity != null)
		{
			if (input.ConsumeKey(_keybinds[Keybind.Remove]))
			{
				OnEntityAction((EntityToolAction)0, entity);
			}
			else if (input.ConsumeKey(_keybinds[Keybind.ToggleFreeze]))
			{
				OnEntityAction((EntityToolAction)2, entity);
			}
			else if (input.ConsumeKey(_keybinds[Keybind.Clone]))
			{
				OnEntityAction((EntityToolAction)1, entity);
			}
		}
	}

	private void OnRotationChange(Vector3 rotation)
	{
		if (_editMode == ToolMode.RotateBody)
		{
			OnMoveEntity(_selectedEntity, _selectedEntity.Position, rotation, _selectedEntity.LookOrientation);
		}
		if (_editMode == ToolMode.RotateHead)
		{
			OnMoveEntity(_selectedEntity, _selectedEntity.Position, _selectedEntity.BodyOrientation, rotation);
		}
	}

	private void OnPositionChange(Vector3 position)
	{
		if (_editMode == ToolMode.FreeMove || _editMode == ToolMode.Translate)
		{
			OnMoveEntity(_selectedEntity, position, _selectedEntity.BodyOrientation, _selectedEntity.LookOrientation);
		}
	}

	private void OnMoveEntity(Entity entity, Vector3 position, Vector3 bodyRotation, Vector3 headRotation)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_007a: Expected O, but got Unknown
		//IL_007a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		if (entity != null)
		{
			entity.PositionProgress = 1f;
			entity.SetTransform(position, bodyRotation, headRotation);
			BuilderToolSetEntityTransform packet = new BuilderToolSetEntityTransform(entity.NetworkId, new ModelTransform(new Position((double)position.X, (double)position.Y, (double)position.Z), new Direction(bodyRotation.Y, bodyRotation.X, bodyRotation.Z), new Direction(headRotation.Y, headRotation.X, headRotation.Z)));
			_gameInstance.Connection.SendPacket((ProtoPacket)(object)packet);
		}
	}

	private void OnEntityAction(EntityToolAction action, Entity entity)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		_gameInstance.Connection.SendPacket((ProtoPacket)new BuilderToolEntityAction(entity.NetworkId, action));
	}

	private string GetDisplayText()
	{
		string result = "";
		if (_selectedEntity == null && _hoveredEntity != null)
		{
			if (_gameInstance.Input.IsAltHeld())
			{
				result = ((!_headSelected) ? "Rotate" : "Rotate Head");
			}
			else if (_gameInstance.Input.IsShiftHeld())
			{
				result = "Translate";
			}
		}
		return result;
	}
}
