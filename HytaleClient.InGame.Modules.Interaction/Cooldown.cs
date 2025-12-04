using HytaleClient.Math;

namespace HytaleClient.InGame.Modules.Interaction;

public class Cooldown
{
	private float _cooldownMax;

	private float[] _charges;

	private float _remainingCooldown;

	private float _chargeTimer;

	private int _chargeCount;

	private bool _interruptRecharge;

	public Cooldown(float cooldownMax, float[] charges, bool interruptRecharge)
	{
		SetCooldownMax(cooldownMax);
		SetCharges(charges);
		ResetCharges();
		_interruptRecharge = interruptRecharge;
	}

	public void SetCooldownMax(float cooldownMax)
	{
		_cooldownMax = cooldownMax;
		if (_remainingCooldown > cooldownMax)
		{
			_remainingCooldown = cooldownMax;
		}
	}

	public void SetCharges(float[] charges)
	{
		_charges = charges;
		if (_chargeCount > charges.Length)
		{
			_chargeCount = charges.Length;
		}
	}

	public bool HasCooldown(bool deduct)
	{
		if (_remainingCooldown <= 0f && _chargeCount > 0)
		{
			if (deduct)
			{
				DeductCharge();
			}
			return false;
		}
		return true;
	}

	public float GetCooldownRemainingTime()
	{
		return _remainingCooldown;
	}

	public float GetCooldownMax()
	{
		return _cooldownMax;
	}

	public float GetChargeTimer()
	{
		return _chargeTimer;
	}

	public int GetChargeCount()
	{
		return _chargeCount;
	}

	public float[] GetCharges()
	{
		return _charges;
	}

	public bool HasMaxCharges()
	{
		return _chargeCount >= _charges.Length;
	}

	public void ResetCharges()
	{
		_chargeCount = _charges.Length;
	}

	public void ResetCooldown()
	{
		_remainingCooldown = _cooldownMax;
	}

	public void DeductCharge()
	{
		if (_chargeCount > 0)
		{
			_chargeCount--;
		}
		if (_interruptRecharge)
		{
			_chargeTimer = 0f;
		}
		ResetCooldown();
	}

	public bool Tick(float dt)
	{
		if (!HasMaxCharges())
		{
			float num = _charges[_chargeCount];
			_chargeTimer += dt;
			if (_chargeTimer >= num)
			{
				_chargeCount++;
				_chargeTimer = 0f;
			}
		}
		_remainingCooldown -= dt;
		return (HasMaxCharges() || _charges.Length <= 1) && _remainingCooldown <= 0f;
	}

	public float GetCooldown()
	{
		return _cooldownMax;
	}

	public bool InterruptRecharge()
	{
		return _interruptRecharge;
	}

	public void ReplenishCharge(int amount, bool interrupt)
	{
		_chargeCount = MathHelper.Clamp(_chargeCount + amount, 0, _charges.Length);
		if (interrupt && amount != 0)
		{
			_chargeTimer = 0f;
		}
	}

	public void IncreaseTime(float time)
	{
		_remainingCooldown = MathHelper.Clamp(_remainingCooldown + time, 0f, _cooldownMax);
	}

	public void IncreaseChargeTime(float time)
	{
		if (!HasMaxCharges() && _charges.Length > 1)
		{
			float max = _charges[_chargeCount];
			_chargeTimer = MathHelper.Clamp(_chargeTimer + time, 0f, max);
		}
	}
}
