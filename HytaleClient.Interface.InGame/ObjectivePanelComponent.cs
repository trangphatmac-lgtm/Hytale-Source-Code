using System;
using System.Collections.Generic;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Protocol;

namespace HytaleClient.Interface.InGame;

internal class ObjectivePanelComponent : InterfaceComponent
{
	private class ObjectiveUI
	{
		public Element ObjectiveElement { get; }

		public List<Element> TaskElements { get; }

		public ObjectiveUI(Element objectiveElement)
		{
			ObjectiveElement = objectiveElement;
			TaskElements = new List<Element>();
		}
	}

	public readonly InGameView InGameView;

	private readonly Dictionary<Guid, Objective> _objectivesByGuid = new Dictionary<Guid, Objective>();

	private readonly Dictionary<Guid, ObjectiveUI> _objectiveUIsByGuid = new Dictionary<Guid, ObjectiveUI>();

	private Document _objectiveDocument;

	private Document _taskDocument;

	private PatchStyle _inProgressIconStyle;

	private PatchStyle _completeIconStyle;

	private LabelStyle _inProgressTaskDescriptionStyle;

	private LabelStyle _completeTaskDescriptionStyle;

	private LabelStyle _inProgressTaskCompletionStyle;

	private LabelStyle _completeTaskCompletionStyle;

	private Group _objectivePanel;

	public bool HasObjectives => _objectivesByGuid.Count > 0;

	public ObjectivePanelComponent(InGameView view)
		: base(view.Interface, view.HudContainer)
	{
		InGameView = view;
		Interface.RegisterForEventFromEngine<Objective>("objectives.updateObjective", OnAddUpdateObjective);
		Interface.RegisterForEventFromEngine<Guid>("objectives.removeObjective", OnRemoveObjective);
		Interface.RegisterForEventFromEngine<Guid, int, ObjectiveTask>("objectives.updateTask", OnUpdateTask);
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("InGame/Hud/ObjectivePanelObjectiveSlot.ui", out _objectiveDocument);
		Interface.TryGetDocument("InGame/Hud/ObjectiveCommon.ui", out var document);
		_inProgressIconStyle = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "TaskIconInProgress");
		_completeIconStyle = document.ResolveNamedValue<PatchStyle>(Desktop.Provider, "TaskIconComplete");
		_inProgressTaskDescriptionStyle = document.ResolveNamedValue<LabelStyle>(Desktop.Provider, "TaskDescInProgress");
		_completeTaskDescriptionStyle = document.ResolveNamedValue<LabelStyle>(Desktop.Provider, "TaskLabelComplete");
		Interface.TryGetDocument("InGame/Hud/ObjectivePanelTask.ui", out _taskDocument);
		_inProgressTaskCompletionStyle = _taskDocument.ResolveNamedValue<LabelStyle>(Desktop.Provider, "PanelTaskCompletionInProgress");
		_completeTaskCompletionStyle = _taskDocument.ResolveNamedValue<LabelStyle>(Desktop.Provider, "PanelTaskCompletionComplete");
		Interface.TryGetDocument("InGame/Hud/ObjectivePanel.ui", out var document2);
		UIFragment uIFragment = document2.Instantiate(Desktop, this);
		_objectivePanel = uIFragment.Get<Group>("ObjectivePanel");
		_objectiveUIsByGuid.Clear();
		foreach (Objective value in _objectivesByGuid.Values)
		{
			AddPanelObjective(value);
		}
	}

	private void OnAddUpdateObjective(Objective objective)
	{
		if (!_objectivesByGuid.ContainsKey(objective.ObjectiveUuid))
		{
			_objectivesByGuid.Add(objective.ObjectiveUuid, objective);
			AddPanelObjective(objective);
		}
		else
		{
			_objectivesByGuid[objective.ObjectiveUuid] = objective;
			UpdatePanelObjective(objective);
		}
		if (base.IsMounted)
		{
			Layout();
		}
	}

	private void AddPanelObjective(Objective objective)
	{
		UIFragment uIFragment = _objectiveDocument.Instantiate(Desktop, _objectivePanel);
		uIFragment.Get<Label>("ObjectiveTitle").Text = Desktop.Provider.GetText(objective.ObjectiveTitleKey);
		ObjectiveUI objectiveUI = new ObjectiveUI(uIFragment.RootElements[0]);
		_objectiveUIsByGuid.Add(objective.ObjectiveUuid, objectiveUI);
		AddTasks(objective, objectiveUI);
		InGameView.UpdateObjectivePanelVisibility(doLayout: true);
	}

	private void UpdatePanelObjective(Objective objective)
	{
		ObjectiveUI objectiveUI = _objectiveUIsByGuid[objective.ObjectiveUuid];
		objectiveUI.ObjectiveElement.Find<Group>("Tasks").Clear();
		objectiveUI.TaskElements.Clear();
		AddTasks(objective, objectiveUI);
	}

	private void AddTasks(Objective objective, ObjectiveUI objectiveUI)
	{
		Group root = objectiveUI.ObjectiveElement.Find<Group>("Tasks");
		ObjectiveTask[] tasks = objective.Tasks;
		foreach (ObjectiveTask val in tasks)
		{
			Element item = AddTask(val.TaskDescriptionKey, val.CurrentCompletion, val.CompletionNeeded, root);
			objectiveUI.TaskElements.Add(item);
		}
	}

	private Element AddTask(string taskKey, int currentCompletion, int completionNeeded, Element root)
	{
		UIFragment uIFragment = _taskDocument.Instantiate(Desktop, root);
		uIFragment.Get<Label>("TaskKey").Text = Desktop.Provider.GetText(taskKey);
		uIFragment.Get<Label>("TaskCompletion").Text = Desktop.Provider.FormatNumber(currentCompletion) + "/" + Desktop.Provider.FormatNumber(completionNeeded);
		SetTaskStyle(uIFragment.RootElements[0], currentCompletion == completionNeeded);
		return uIFragment.RootElements[0];
	}

	private void SetTaskStyle(Element taskElement, bool isComplete)
	{
		taskElement.Find<Group>("TaskIcon").Background = (isComplete ? _completeIconStyle : _inProgressIconStyle);
		taskElement.Find<Label>("TaskKey").Style = (isComplete ? _completeTaskDescriptionStyle : _inProgressTaskDescriptionStyle);
		taskElement.Find<Label>("TaskCompletion").Style = (isComplete ? _completeTaskCompletionStyle : _inProgressTaskCompletionStyle);
	}

	private void OnRemoveObjective(Guid objectiveUuid)
	{
		if (_objectiveUIsByGuid.TryGetValue(objectiveUuid, out var value))
		{
			_objectivePanel.Remove(value.ObjectiveElement);
			_objectiveUIsByGuid.Remove(objectiveUuid);
			_objectivesByGuid.Remove(objectiveUuid);
			InGameView.UpdateObjectivePanelVisibility();
			if (base.IsMounted)
			{
				Layout();
			}
		}
	}

	private void OnUpdateTask(Guid objectiveUuid, int taskIndex, ObjectiveTask task)
	{
		if (_objectiveUIsByGuid.TryGetValue(objectiveUuid, out var value))
		{
			_objectivesByGuid[objectiveUuid].Tasks[taskIndex] = task;
			Element element = value.TaskElements[taskIndex];
			element.Find<Label>("TaskCompletion").Text = task.CurrentCompletion + "/" + task.CompletionNeeded;
			SetTaskStyle(element, task.CurrentCompletion == task.CompletionNeeded);
			if (base.IsMounted)
			{
				Layout();
			}
		}
	}

	public void ResetState()
	{
		_objectivePanel.Clear();
		_objectiveUIsByGuid.Clear();
		_objectivesByGuid.Clear();
	}
}
