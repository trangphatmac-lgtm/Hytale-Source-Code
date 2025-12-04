using System;
using System.Linq;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.InGame.Hud;

internal class SpeedometerComponent : InterfaceComponent
{
	private struct MaxValues
	{
		public float X;

		public float Z;

		public float Y;

		public float LateralMagnitude;

		public float Magnitude;

		public void Reset()
		{
			X = 0f;
			Z = 0f;
			Y = 0f;
			LateralMagnitude = 0f;
			Magnitude = 0f;
		}
	}

	private const float MaxValuesUpdateInterval = 2f;

	private readonly InGameView _inGameView;

	private Label _xLabel;

	private Label _xMaxLabel;

	private Label _zLabel;

	private Label _zMaxLabel;

	private Label _yLabel;

	private Label _yMaxLabel;

	private Label _magnitudeLatLabel;

	private Label _magnitudeLatMaxLabel;

	private Label _magnitudeLabel;

	private Label _magnitudeMaxLabel;

	private float[] _xVelocityComparison = new float[2];

	private float[] _zVelocityComparison = new float[2];

	private float[] _yVelocityComparison = new float[2];

	private float[] _lateralMagnitudeComparison = new float[2];

	private float[] _magnitudeComparison = new float[2];

	private MaxValues _maxValues;

	private float _maxValuesUpdateTimer;

	public bool Enabled = false;

	public SpeedometerComponent(InGameView inGameView)
		: base(inGameView.Interface, inGameView.HudContainer)
	{
		_inGameView = inGameView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/Speedometer.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_xLabel = uIFragment.Get<Label>("X");
		_xMaxLabel = uIFragment.Get<Label>("XMax");
		_zLabel = uIFragment.Get<Label>("Z");
		_zMaxLabel = uIFragment.Get<Label>("ZMax");
		_yLabel = uIFragment.Get<Label>("Y");
		_yMaxLabel = uIFragment.Get<Label>("YMax");
		_magnitudeLatLabel = uIFragment.Get<Label>("MagnitudeLat");
		_magnitudeLatMaxLabel = uIFragment.Get<Label>("MagnitudeLatMax");
		_magnitudeLabel = uIFragment.Get<Label>("Magnitude");
		_magnitudeMaxLabel = uIFragment.Get<Label>("MagnitudeMax");
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Update);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Update);
	}

	private void Update(float deltaTime)
	{
		Vector3 velocity = _inGameView.InGame.Instance.CharacterControllerModule.MovementController.Velocity;
		double num = System.Math.Round(velocity.X, 3);
		double num2 = System.Math.Round(velocity.Z, 3);
		double num3 = System.Math.Round(velocity.Y, 3);
		float num4 = (float)System.Math.Sqrt((double)(velocity.X * velocity.X) + (double)(velocity.Z * velocity.Z));
		float num5 = (float)System.Math.Sqrt((double)(velocity.X * velocity.X) + (double)(velocity.Z * velocity.Z) + (double)(velocity.Y * velocity.Y));
		_xLabel.Text = $"{num:F}";
		_zLabel.Text = $"{num2:F}";
		_yLabel.Text = $"{num3:F}";
		_magnitudeLatLabel.Text = $"{num4:F}";
		_magnitudeLabel.Text = $"{num5:F}";
		_xVelocityComparison[0] = velocity.X;
		_xVelocityComparison[1] = _maxValues.X;
		_zVelocityComparison[0] = velocity.Z;
		_zVelocityComparison[1] = _maxValues.Z;
		_yVelocityComparison[0] = velocity.Y;
		_yVelocityComparison[1] = _maxValues.Y;
		_lateralMagnitudeComparison[0] = num4;
		_lateralMagnitudeComparison[1] = _maxValues.LateralMagnitude;
		_magnitudeComparison[0] = num5;
		_magnitudeComparison[1] = _maxValues.Magnitude;
		_maxValues.X = _xVelocityComparison.Select(System.Math.Abs).Max();
		_maxValues.Z = _zVelocityComparison.Select(System.Math.Abs).Max();
		float num6 = _yVelocityComparison.Min();
		float num7 = _yVelocityComparison.Max();
		_maxValues.Y = ((System.Math.Abs(num6) > num7) ? num6 : num7);
		_maxValues.LateralMagnitude = _lateralMagnitudeComparison.Select(System.Math.Abs).Max();
		_maxValues.Magnitude = _magnitudeComparison.Select(System.Math.Abs).Max();
		_maxValuesUpdateTimer += deltaTime;
		if (_maxValuesUpdateTimer > 2f)
		{
			_xMaxLabel.Text = $" ({_maxValues.X:F})";
			_zMaxLabel.Text = $" ({_maxValues.Z:F})";
			_yMaxLabel.Text = $" ({_maxValues.Y:F})";
			_magnitudeLatMaxLabel.Text = $" ({_maxValues.LateralMagnitude:F})";
			_magnitudeMaxLabel.Text = $" ({_maxValues.Magnitude:F})";
			_maxValues.Reset();
			_maxValuesUpdateTimer = 0f;
		}
		Layout();
	}
}
