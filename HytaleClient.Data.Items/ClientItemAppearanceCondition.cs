using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.EntityStats;
using HytaleClient.Graphics.Particles;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

internal class ClientItemAppearanceCondition
{
	public class Data
	{
		public int EntityStatIndex;

		public int ConditionIndex;

		public Entity.EntityParticle[] EntityParticles;

		public Data(int entityStatIndex, int conditionIndex)
		{
			EntityStatIndex = entityStatIndex;
			ConditionIndex = conditionIndex;
		}
	}

	public ModelParticleSettings[] Particles;

	public ModelParticleSettings[] FirstPersonParticles;

	public string ModelId;

	public BlockyModel Model;

	public string Texture;

	public FloatRange Condition;

	public ValueType Type;

	public string ModelVFXId;

	public bool CanApplyCondition(ClientEntityStatValue entityStat)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		float num = 0f;
		ValueType type = Type;
		ValueType val = type;
		if ((int)val != 0)
		{
			if ((int)val != 1)
			{
			}
			num = entityStat.Value;
		}
		else
		{
			num = entityStat.Value / entityStat.Max * 100f;
		}
		return Condition.Includes(num);
	}
}
