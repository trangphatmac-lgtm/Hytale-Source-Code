using System;
using System.Collections.Generic;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.EntityStats;

public class ClientEntityStatValue
{
	private static ModifierTarget[] TARGETS = (ModifierTarget[])(object)new ModifierTarget[2]
	{
		default(ModifierTarget),
		(ModifierTarget)1
	};

	private float _value;

	public float Min;

	public float Max;

	public Dictionary<string, Modifier> Modifiers;

	public float Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = MathHelper.Clamp(value, Min, Max);
		}
	}

	public float AsPercentage()
	{
		return (Value - Min) / (Max - Min);
	}

	public void CalculateModifiers(ClientEntityStatType statType)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Invalid comparison between Unknown and I4
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Invalid comparison between Unknown and I4
		Min = statType.Min;
		Max = statType.Max;
		if (Modifiers != null)
		{
			for (int i = 0; i < TARGETS.Length; i++)
			{
				ModifierTarget val = TARGETS[i];
				bool flag = false;
				float num = 0f;
				bool flag2 = false;
				float num2 = 0f;
				foreach (Modifier value in Modifiers.Values)
				{
					if (value.Target != val)
					{
						continue;
					}
					CalculationType calculationType_ = value.CalculationType_;
					CalculationType val2 = calculationType_;
					if ((int)val2 != 0)
					{
						if ((int)val2 != 1)
						{
							throw new ArgumentOutOfRangeException();
						}
						flag2 = true;
						num2 += value.Amount;
					}
					else
					{
						flag = true;
						num += value.Amount;
					}
				}
				ModifierTarget val3 = val;
				ModifierTarget val4 = val3;
				if ((int)val4 != 0)
				{
					if ((int)val4 != 1)
					{
						throw new ArgumentOutOfRangeException();
					}
					if (flag)
					{
						Max += num;
					}
					if (flag2)
					{
						Max *= num2;
					}
				}
				else
				{
					if (flag)
					{
						Min += num;
					}
					if (flag2)
					{
						Min *= num2;
					}
				}
			}
		}
		_value = MathHelper.Clamp(_value, Min, Max);
	}
}
