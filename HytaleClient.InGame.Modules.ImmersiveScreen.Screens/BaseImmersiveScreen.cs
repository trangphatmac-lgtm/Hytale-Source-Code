using System;
using HytaleClient.Core;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules.ImmersiveScreen.Screens;

internal abstract class BaseImmersiveScreen : Disposable
{
	protected GameInstance _gameInstance;

	private Vector3 _screenOffset;

	private bool _isBillboard;

	private Vector2 _screenDirection;

	private Vector2 _screenAutoRotationSpeed;

	private Vector2 _screenSizeInBlocks;

	private Vector2 _screenPixelsToBlockRatio;

	protected Matrix _mvpMatrix;

	public Vector3 BlockPosition { get; protected set; }

	public Vector2 ScreenSizeInPixels { get; private set; }

	public float MaxVisibilityDistance { get; protected set; }

	public float MaxSoundDistance { get; protected set; }

	public BaseImmersiveScreen(GameInstance gameInstance, Vector3 blockPosition, ViewScreen screen)
	{
		_gameInstance = gameInstance;
		BlockPosition = blockPosition;
		_screenOffset = new Vector3(screen.OffsetX, screen.OffsetY, screen.OffsetZ);
		_isBillboard = screen.UseBillboardRotation;
		_screenDirection = new Vector2(MathHelper.ToRadians(screen.Yaw), MathHelper.ToRadians(screen.Pitch));
		_screenAutoRotationSpeed = new Vector2(screen.YawRotateSpeed, screen.PitchRotateSpeed);
		_screenSizeInBlocks = new Vector2(screen.SizeX, screen.SizeY);
		int num = System.Math.Max(screen.Resolution, 1);
		float num2 = _screenSizeInBlocks.Y / _screenSizeInBlocks.X;
		ScreenSizeInPixels = ((num2 < 1f) ? new Vector2(num, (float)num * num2) : new Vector2((float)num / num2, num));
		_screenPixelsToBlockRatio = new Vector2(_screenSizeInBlocks.X / ScreenSizeInPixels.X, _screenSizeInBlocks.Y / ScreenSizeInPixels.Y);
		MaxSoundDistance = screen.MaxSoundDistance;
		MaxVisibilityDistance = screen.MaxVisibilityDistance;
	}

	public void Update(float deltaTime)
	{
		if (_isBillboard)
		{
			Vector3 vector = GetOffsetPosition() - _gameInstance.LocalPlayer.Position;
			vector.Normalize();
			_screenDirection = new Vector2((float)System.Math.Atan2(vector.X, vector.Z) + (float)System.Math.PI, 0f);
		}
		else
		{
			_screenDirection.X = MathHelper.WrapAngle(_screenDirection.X + MathHelper.ToRadians(_screenAutoRotationSpeed.X) * 60f * deltaTime);
			_screenDirection.Y = MathHelper.WrapAngle(_screenDirection.Y + MathHelper.ToRadians(_screenAutoRotationSpeed.Y) * 60f * deltaTime);
		}
	}

	public bool NeedsDrawing()
	{
		if (Vector3.Distance(_gameInstance.LocalPlayer.Position, GetOffsetPosition()) < MaxVisibilityDistance)
		{
			if (this is ImmersiveWebScreen && this != _gameInstance.ImmersiveScreenModule.ActiveWebScreen)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public void PrepareForDraw(ref Matrix viewProjectionMatrix)
	{
		if (!NeedsDrawing())
		{
			throw new Exception("PrepareForDraw called when it was not required. Please check with NeedsDrawing() first before calling this.");
		}
		Vector3 position = new Vector3(_screenSizeInBlocks.X / -2f, 0f, 0f);
		Vector3 position2 = GetOffsetPosition();
		Matrix.CreateFromYawPitchRoll(_screenDirection.X, _screenDirection.Y, 0f, out var result);
		Matrix.CreateTranslation(ref position, out var result2);
		Matrix.Multiply(ref result2, ref result, out result);
		Matrix.CreateTranslation(ref position2, out result2);
		Matrix.Multiply(ref result, ref result2, out result);
		Matrix.Multiply(ref result, ref viewProjectionMatrix, out result);
		Matrix.CreateScale(_screenPixelsToBlockRatio.X * ScreenSizeInPixels.X, _screenPixelsToBlockRatio.Y * ScreenSizeInPixels.Y, 1f, out _mvpMatrix);
		Matrix.Multiply(ref _mvpMatrix, ref result, out _mvpMatrix);
	}

	public abstract void Draw();

	public Vector3 GetOffsetPosition()
	{
		Vector3 vector = new Vector3(BlockPosition.X + 0.5f, BlockPosition.Y, BlockPosition.Z + 0.5f);
		if (_isBillboard)
		{
			return vector + _screenOffset;
		}
		Vector3 position = _screenOffset;
		Matrix.CreateFromYawPitchRoll(_screenDirection.X, _screenDirection.Y, 0f, out var result);
		Matrix.CreateTranslation(ref position, out var result2);
		Matrix.Multiply(ref result2, ref result, out result);
		return vector + result.Translation;
	}

	public Vector2 ViewOffsetToPixelPosition(Vector2 viewOffset)
	{
		return new Vector2((float)System.Math.Round(ScreenSizeInPixels.X - viewOffset.X * ScreenSizeInPixels.X), (float)System.Math.Round(viewOffset.Y * ScreenSizeInPixels.Y));
	}

	public bool CheckRayIntersection(Ray viewRay, out ViewPlaneIntersection intersection)
	{
		Vector3 offsetPosition = GetOffsetPosition();
		Vector3 position = viewRay.Position - offsetPosition;
		Vector3 position2 = new Vector3(_screenSizeInBlocks.X / 2f, 0f, 0f);
		Matrix.CreateFromYawPitchRoll(0f - _screenDirection.X, 0f - _screenDirection.Y, 0f, out var result);
		Matrix.CreateTranslation(ref position, out var result2);
		Matrix.Multiply(ref result2, ref result, out result2);
		Vector3 direction = Vector3.Transform(viewRay.Direction, result);
		Matrix.CreateTranslation(ref position2, out result);
		Matrix.Multiply(ref result2, ref result, out result2);
		Ray ray = new Ray(result2.Translation + offsetPosition, direction);
		intersection = default(ViewPlaneIntersection);
		if (ray.Direction.Z <= 0f)
		{
			float num = (ray.Position.Z - offsetPosition.Z) / (0f - ray.Direction.Z);
			Vector3 vector = new Vector3(ray.Position.X + num * ray.Direction.X, ray.Position.Y + num * ray.Direction.Y, ray.Position.Z + num * ray.Direction.Z);
			if (vector.Y >= offsetPosition.Y && vector.Y <= offsetPosition.Y + _screenSizeInBlocks.Y && vector.X >= offsetPosition.X && vector.X <= offsetPosition.X + _screenSizeInBlocks.X)
			{
				Vector2 pixelPos = new Vector2(1f - (vector.X - offsetPosition.X) / _screenSizeInBlocks.X, (vector.Y - offsetPosition.Y) / _screenSizeInBlocks.Y);
				Vector3 worldPos = viewRay.Position + viewRay.Direction * num;
				intersection = new ViewPlaneIntersection(worldPos, pixelPos);
				return true;
			}
		}
		return false;
	}
}
