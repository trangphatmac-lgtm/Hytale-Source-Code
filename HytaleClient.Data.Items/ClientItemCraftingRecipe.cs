using Coherent.UI.Binding;
using HytaleClient.Protocol;

namespace HytaleClient.Data.Items;

[CoherentType]
public class ClientItemCraftingRecipe
{
	[CoherentType]
	public class ClientCraftingMaterial
	{
		[CoherentProperty("itemId")]
		public readonly string ItemId;

		[CoherentProperty("resourceTypeId")]
		public readonly string ResourceTypeId;

		[CoherentProperty("quantity")]
		public readonly int Quantity;

		public ClientCraftingMaterial(CraftingMaterial craftingMaterial)
		{
			ItemId = craftingMaterial.ItemId;
			ResourceTypeId = craftingMaterial.ResourceTypeId;
			Quantity = craftingMaterial.Quantity;
		}
	}

	[CoherentType]
	public class ClientBenchRequirement
	{
		[CoherentProperty("type")]
		public readonly BenchType Type;

		[CoherentProperty("id")]
		public readonly string Id;

		[CoherentProperty("categories")]
		public readonly string[] Categories;

		public ClientBenchRequirement(BenchRequirement benchRequirement)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			Type = benchRequirement.Type;
			Id = benchRequirement.Id;
			Categories = benchRequirement.Categories;
		}
	}

	[CoherentProperty("benchRequirement")]
	public readonly ClientBenchRequirement[] BenchRequirement;

	[CoherentProperty("input")]
	public readonly ClientCraftingMaterial[] Input;

	[CoherentProperty("output")]
	public readonly ClientCraftingMaterial[] Output;

	[CoherentProperty("timeSeconds")]
	public readonly float TimeSeconds;

	[CoherentProperty("knowledgeRequired")]
	public readonly bool KnowledgeRequired;

	public ClientItemCraftingRecipe(CraftingRecipe recipe)
	{
		if (recipe.BenchRequirement_ != null)
		{
			BenchRequirement = new ClientBenchRequirement[recipe.BenchRequirement_.Length];
			for (int i = 0; i < recipe.BenchRequirement_.Length; i++)
			{
				BenchRequirement[i] = new ClientBenchRequirement(recipe.BenchRequirement_[i]);
			}
		}
		if (recipe.Input != null)
		{
			Input = new ClientCraftingMaterial[recipe.Input.Length];
			for (int j = 0; j < recipe.Input.Length; j++)
			{
				Input[j] = new ClientCraftingMaterial(recipe.Input[j]);
			}
		}
		else
		{
			Input = new ClientCraftingMaterial[0];
		}
		if (recipe.Output != null)
		{
			Output = new ClientCraftingMaterial[recipe.Output.Length];
			for (int k = 0; k < recipe.Output.Length; k++)
			{
				Output[k] = new ClientCraftingMaterial(recipe.Output[k]);
			}
		}
		else
		{
			Output = new ClientCraftingMaterial[0];
		}
		TimeSeconds = recipe.TimeSeconds;
		KnowledgeRequired = recipe.KnowledgeRequired;
	}
}
