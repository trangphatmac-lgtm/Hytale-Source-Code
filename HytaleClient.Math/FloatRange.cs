namespace HytaleClient.Math;

internal struct FloatRange
{
	private float _inclusiveMin;

	private float _inclusiveMax;

	public FloatRange(float inclusiveMin, float inclusiveMax)
	{
		_inclusiveMin = inclusiveMin;
		_inclusiveMax = inclusiveMax;
	}

	public bool Includes(float value)
	{
		return value >= _inclusiveMin && value <= _inclusiveMax;
	}

	public float Clamp(float value)
	{
		return MathHelper.Clamp(value, _inclusiveMin, _inclusiveMax);
	}
}
