using System.Collections.Generic;
using HytaleClient.InGame;
using HytaleClient.InGame.Modules.Interaction;
using HytaleClient.Protocol;
using NLog;

namespace HytaleClient.Data.ClientInteraction;

internal class ClientRootInteraction
{
	public class OperationsBuilder
	{
		private List<Operation> _operationList = new List<Operation>();

		public Label CreateLabel()
		{
			return new Label(_operationList.Count);
		}

		public Label CreateUnresolvedLabel()
		{
			return new Label(int.MinValue);
		}

		public void ResolveLabel(Label label)
		{
			label.Index = _operationList.Count;
		}

		public void Jump(Label target)
		{
			_operationList.Add(new JumpOperation(target));
		}

		public void AddOperation(int index)
		{
			_operationList.Add(new DefaultOperation(index));
		}

		public void AddOperation(int index, params Label[] labels)
		{
			_operationList.Add(new LabelOperation(new DefaultOperation(index), labels));
		}

		public Operation[] Build()
		{
			return _operationList.ToArray();
		}
	}

	public class Label
	{
		public int Index;

		public Label(int index)
		{
			Index = index;
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}", "Index", Index);
		}
	}

	public interface Operation
	{
		WaitForDataFrom GetWaitForDataFrom(GameInstance gameInstance);

		void Tick(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context);

		void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context);

		bool TryGetRules(GameInstance gameInstance, out ClientInteraction.ClientInteractionRules rules, out HashSet<int> tags);

		void Revert(GameInstance gameInstance, InteractionType type, InteractionContext context);

		void MatchServer(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context);
	}

	public interface InteractionWrapper : Operation
	{
		ClientInteraction GetInteraction(InteractionModule module);
	}

	public class JumpOperation : Operation
	{
		public readonly Label Target;

		public JumpOperation(Label target)
		{
			Target = target;
		}

		public WaitForDataFrom GetWaitForDataFrom(GameInstance gameInstance)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			return (WaitForDataFrom)2;
		}

		public bool TryGetRules(GameInstance gameInstance, out ClientInteraction.ClientInteractionRules rules, out HashSet<int> tags)
		{
			rules = null;
			tags = null;
			return false;
		}

		public void Tick(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			context.OperationCounter = Target.Index;
			context.State.State = (InteractionState)0;
		}

		public void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
		{
		}

		public void Revert(GameInstance gameInstance, InteractionType type, InteractionContext context)
		{
		}

		public void MatchServer(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			context.OperationCounter = Target.Index;
			context.State.State = (InteractionState)0;
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}", "Target", Target);
		}
	}

	public class LabelOperation : InteractionWrapper, Operation
	{
		public readonly InteractionWrapper Inner;

		public readonly Label[] Labels;

		public LabelOperation(InteractionWrapper inner, Label[] labels)
		{
			Inner = inner;
			Labels = labels;
		}

		public WaitForDataFrom GetWaitForDataFrom(GameInstance gameInstance)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			return Inner.GetWaitForDataFrom(gameInstance);
		}

		public void Tick(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			context.Labels = Labels;
			Inner.Tick(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		}

		public bool TryGetRules(GameInstance gameInstance, out ClientInteraction.ClientInteractionRules rules, out HashSet<int> tags)
		{
			return Inner.TryGetRules(gameInstance, out rules, out tags);
		}

		public void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			context.Labels = Labels;
			Inner.Handle(gameInstance, firstRun, time, type, context);
		}

		public void Revert(GameInstance gameInstance, InteractionType type, InteractionContext context)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			context.Labels = Labels;
			Inner.Revert(gameInstance, type, context);
		}

		public void MatchServer(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			context.Labels = Labels;
			Inner.MatchServer(gameInstance, clickType, hasAnyButtonClick, type, context);
		}

		public ClientInteraction GetInteraction(InteractionModule module)
		{
			return Inner.GetInteraction(module);
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}, {2}: {3}", "Inner", Inner, "Labels", Labels);
		}
	}

	public class DefaultOperation : InteractionWrapper, Operation
	{
		public readonly int InteractionId;

		public DefaultOperation(int interactionId)
		{
			InteractionId = interactionId;
		}

		public WaitForDataFrom GetWaitForDataFrom(GameInstance gameInstance)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			return gameInstance.InteractionModule.Interactions[InteractionId].Interaction.WaitForDataFrom_;
		}

		public bool TryGetRules(GameInstance gameInstance, out ClientInteraction.ClientInteractionRules rules, out HashSet<int> tags)
		{
			ClientInteraction clientInteraction = gameInstance.InteractionModule.Interactions[InteractionId];
			rules = clientInteraction.Rules;
			tags = clientInteraction.Tags;
			return true;
		}

		public ClientInteraction GetInteraction(InteractionModule module)
		{
			return module.Interactions[InteractionId];
		}

		public void Tick(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, bool firstRun, float time, InteractionType type, InteractionContext context)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			gameInstance.InteractionModule.Interactions[InteractionId].Tick(gameInstance, clickType, hasAnyButtonClick, firstRun, time, type, context);
		}

		public void Handle(GameInstance gameInstance, bool firstRun, float time, InteractionType type, InteractionContext context)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			gameInstance.InteractionModule.Interactions[InteractionId].Handle(gameInstance, firstRun, time, type, context);
		}

		public void Revert(GameInstance gameInstance, InteractionType type, InteractionContext context)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			gameInstance.InteractionModule.Interactions[InteractionId].Revert(gameInstance, type, context);
		}

		public void MatchServer(GameInstance gameInstance, InteractionModule.ClickType clickType, bool hasAnyButtonClick, InteractionType type, InteractionContext context)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			gameInstance.InteractionModule.Interactions[InteractionId].MatchServer(gameInstance, clickType, hasAnyButtonClick, type, context);
		}

		public override string ToString()
		{
			return string.Format("{0}: {1}", "InteractionId", InteractionId);
		}
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly string Id;

	public readonly int Index;

	public readonly RootInteraction RootInteraction;

	public Operation[] Operations;

	public readonly ClientInteraction.ClientInteractionRules Rules;

	public readonly HashSet<int> Tags;

	public ClientRootInteraction(int index, RootInteraction root)
	{
		Id = root.Id;
		Index = index;
		RootInteraction = root;
		Rules = new ClientInteraction.ClientInteractionRules(root.Rules);
		Tags = ((root.Tags != null) ? new HashSet<int>(root.Tags) : new HashSet<int>());
	}

	public void Build(InteractionModule module)
	{
		if (module.Interactions == null)
		{
			return;
		}
		OperationsBuilder operationsBuilder = new OperationsBuilder();
		int[] interactions = RootInteraction.Interactions;
		foreach (int num in interactions)
		{
			if (num == int.MinValue)
			{
				Logger.Error($"Root interaction {Index} contains an undefined interaction.");
				return;
			}
			ClientInteraction clientInteraction = module.Interactions[num];
			clientInteraction.Compile(module, operationsBuilder);
		}
		Operations = operationsBuilder.Build();
	}
}
