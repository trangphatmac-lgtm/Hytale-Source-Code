using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Entities;

internal class ClientModelVFX
{
	public enum EffectDirections
	{
		None,
		BottomUp,
		TopDown,
		ToCenter,
		FromCenter
	}

	public enum SwitchTo
	{
		Disappear,
		PostColor,
		Distortion
	}

	public enum LoopOptions
	{
		PlayOnce,
		Loop,
		LoopMirror
	}

	public Vector3 HighlightColor;

	public float HighlightThickness = 1f;

	public Vector2 NoiseScale;

	public float AnimationProgress;

	public int PackedModelVFXParams;

	public Vector2 NoiseScrollSpeed;

	public Vector4 PostColor;

	public int IdInTBO;

	public string Id;

	public bool TriggerAnimation;

	private float _modelVFXAnimationStartTime;

	public float AnimationDuration;

	public Vector2 AnimationRange;

	public LoopOptions LoopOption;

	public Easing.EasingType CurveType;

	public void UpdateCustomAnimation(float customRatio)
	{
		AnimationProgress = customRatio;
	}

	public void UpdateAnimation(float frametime)
	{
		if (TriggerAnimation)
		{
			_modelVFXAnimationStartTime = frametime;
			TriggerAnimation = false;
		}
		float num = 0f;
		float num2 = AnimationRange.Y - AnimationRange.X;
		switch (LoopOption)
		{
		case LoopOptions.PlayOnce:
		{
			float num3 = frametime - _modelVFXAnimationStartTime;
			if (num3 < AnimationDuration)
			{
				num = num3 * num2 / AnimationDuration + AnimationRange.X;
			}
			break;
		}
		case LoopOptions.Loop:
		{
			float num3 = (frametime - _modelVFXAnimationStartTime) % AnimationDuration;
			num = num3 * num2 / AnimationDuration + AnimationRange.X;
			break;
		}
		case LoopOptions.LoopMirror:
		{
			float num3 = (frametime - _modelVFXAnimationStartTime) % (AnimationDuration * 2f);
			if (num3 < AnimationDuration)
			{
				num = num3 * num2 / AnimationDuration + AnimationRange.X;
				break;
			}
			num = (num3 - AnimationDuration) * num2 / AnimationDuration + AnimationRange.X;
			num = AnimationRange.Y - num + AnimationRange.X;
			break;
		}
		}
		if (num != 0f)
		{
			AnimationProgress = Easing.Ease(CurveType, num);
		}
	}
}
