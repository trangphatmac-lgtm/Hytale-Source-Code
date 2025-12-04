using HytaleClient.Data.EntityUI;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Interface.InGame.EntityUI;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.Interface.InGame;

internal class EntityUIContainer : Group
{
	private readonly InGameView _inGameView;

	private EntityUIDrawTask[] _drawTasks = new EntityUIDrawTask[100];

	private int _drawTasksCount;

	private int _entitiesCount;

	private const int DrawTasksDefaultSize = 100;

	private const int DrawTasksGrowth = 50;

	public CombatTextUIComponentRenderer CombatTextUIComponentRenderer;

	public EntityStatUIComponentRenderer EntityStatUIComponentRenderer;

	private ClientEntityUIComponent[] _components => _inGameView.InGame.Instance.ServerSettings.EntityUIComponents;

	public EntityUIContainer(Desktop desktop, InGameView inGameView)
		: base(desktop, inGameView)
	{
		_inGameView = inGameView;
		CombatTextUIComponentRenderer = new CombatTextUIComponentRenderer(inGameView);
		EntityStatUIComponentRenderer = new EntityStatUIComponentRenderer(inGameView);
	}

	protected override void OnMounted()
	{
		Desktop.RegisterAnimationCallback(Animate);
	}

	protected override void OnUnmounted()
	{
		Desktop.UnregisterAnimationCallback(Animate);
	}

	private void Animate(float deltaTime)
	{
		CombatTextUIComponentRenderer.Animate(deltaTime);
		EntityStatUIComponentRenderer.Animate(deltaTime);
	}

	public void Build()
	{
		Clear();
		CombatTextUIComponentRenderer.Build(this);
		EntityStatUIComponentRenderer.Build(this);
	}

	public void RegisterDrawTasksForEntity(ref Matrix transformationMatrix, Entity entity, float distanceToCamera)
	{
		ArrayUtils.GrowArrayIfNecessary(ref _drawTasks, _drawTasksCount, 50);
		int drawTasksCount = _drawTasksCount;
		int[] uIComponents = entity.UIComponents;
		foreach (int id in uIComponents)
		{
			if (entity.TryGetUIComponent(id, out var component) && !component.Unknown)
			{
				component.RegisterDrawTasksForEntity(entity, transformationMatrix, distanceToCamera, _entitiesCount, ref _drawTasks, ref _drawTasksCount);
			}
		}
		if (_drawTasksCount > drawTasksCount)
		{
			_entitiesCount++;
		}
	}

	protected override void PrepareForDrawContent()
	{
		for (int num = _drawTasksCount - 1; num >= 0; num--)
		{
			EntityUIDrawTask task = _drawTasks[num];
			ClientEntityUIComponent clientEntityUIComponent = _components[task.ComponentId];
			Desktop.Batcher2D.SetOpacityOverride(task.Opacity);
			Desktop.Batcher2D.SetTransformationMatrix(task.TransformationMatrix);
			clientEntityUIComponent.PrepareForDraw(task);
		}
		_drawTasksCount = 0;
		_entitiesCount = 0;
		Desktop.Batcher2D.SetOpacityOverride(null);
		Desktop.Batcher2D.SetTransformationMatrix(Matrix.Identity);
	}
}
