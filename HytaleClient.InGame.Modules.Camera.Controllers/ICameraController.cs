using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using SDL2;

namespace HytaleClient.InGame.Modules.Camera.Controllers;

internal interface ICameraController
{
	float SpeedModifier { get; }

	bool AllowPitchControls { get; }

	bool DisplayCursor { get; }

	bool DisplayReticle { get; }

	bool SkipCharacterPhysics { get; }

	bool IsFirstPerson { get; }

	bool InteractFromEntity { get; }

	Vector3 MovementForceRotation { get; }

	Entity AttachedTo { get; }

	Vector3 AttachmentPosition { get; }

	Vector3 PositionOffset { get; }

	Vector3 RotationOffset { get; }

	Vector3 Position { get; }

	Vector3 Rotation { get; }

	Vector3 LookAt { get; }

	bool CanMove { get; }

	void Reset(GameInstance gameInstance, ICameraController previousCameraController);

	void Update(float deltaTime);

	void ApplyLook(float deltaTime, Vector2 lookOffset);

	void SetRotation(Vector3 rotation);

	void ApplyMove(Vector3 movementOffset);

	void OnMouseInput(SDL_Event evt);
}
