using System;
using System.IO;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.AssetEditor.Interface.MainMenu;
using HytaleClient.Interface;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Utils;

namespace HytaleClient.AssetEditor.Interface;

internal class AssetEditorInterface : BaseInterface
{
	public readonly AssetEditorOverlay AssetEditor;

	public readonly AssetEditorStartupView StartupView;

	public readonly AssetEditorMainMenuView MainMenuView;

	public readonly SettingsModal SettingsModal;

	public readonly AssetEditorApp App;

	public readonly ToastNotifications Notifications;

	private Element _currentView;

	public AssetEditorInterface(AssetEditorApp app, bool isDevMode)
		: base(app.Engine, app.Fonts, app.CoUIManager, Path.Combine(Paths.EditorData, "Interface"), isDevMode)
	{
		App = app;
		AssetEditor = new AssetEditorOverlay(this, Desktop);
		MainMenuView = new AssetEditorMainMenuView(this);
		StartupView = new AssetEditorStartupView(Desktop, null);
		Notifications = new ToastNotifications(Desktop, null);
		SettingsModal = new SettingsModal(this);
	}

	public void OnWindowFocusChanged()
	{
		AssetEditor.OnWindowFocusChanged();
	}

	public void OnAppStageChanged()
	{
		Element element = App.Stage switch
		{
			AssetEditorApp.AppStage.Editor => AssetEditor, 
			AssetEditorApp.AppStage.MainMenu => MainMenuView, 
			AssetEditorApp.AppStage.Startup => StartupView, 
			_ => throw new NotSupportedException(), 
		};
		if (_currentView != element)
		{
			Desktop.ClearAllLayers();
			Desktop.SetLayer(0, element);
			Notifications.Parent?.Remove(Notifications);
			element.Add(Notifications);
			_currentView = element;
			_currentView.Layout();
		}
	}

	protected override void Build()
	{
		AssetEditor.Build();
		MainMenuView.Build();
		SettingsModal.Build();
	}
}
