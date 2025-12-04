using System;
using System.Collections.Generic;
using HytaleClient.Application.Services;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using NLog;

namespace HytaleClient.Interface.MainMenu.Pages;

internal class SharedSinglePlayerPage : InterfaceComponent
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly MainMenuView MainMenuView;

	private Group _container;

	private int _framesPerRow;

	private int _frameWidth;

	private int _frameHeight;

	private int _frameSpacing;

	private int _nameLabelHoverOffset;

	public SharedSinglePlayerPage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		MainMenuView = mainMenuView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/SharedSinglePlayer/SharedSinglePlayerPage.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("WorldsContainer");
		_framesPerRow = document.ResolveNamedValue<int>(Desktop.Provider, "FramesPerRow");
		_frameWidth = document.ResolveNamedValue<int>(Desktop.Provider, "FrameWidth");
		_frameHeight = document.ResolveNamedValue<int>(Desktop.Provider, "FrameHeight");
		_frameSpacing = document.ResolveNamedValue<int>(Desktop.Provider, "FrameSpacing");
		BuildWorldsList();
	}

	private void Queue(Guid worldId)
	{
		Interface.App.MainMenu.QueueForSharedSinglePlayerWorld(worldId);
	}

	private void BuildWorldsList()
	{
		Logger.Info("Building worlds list!");
		if (!base.IsMounted)
		{
			return;
		}
		List<ClientSharedSinglePlayerJoinableWorldWrapper> sharedSinglePlayerJoinableWorlds = Interface.App.HytaleServices.SharedSinglePlayerJoinableWorlds;
		if (sharedSinglePlayerJoinableWorlds == null)
		{
			return;
		}
		_container.Clear();
		Interface.TryGetDocument("MainMenu/SharedSinglePlayer/SharedSinglePlayerButton.ui", out var document);
		_nameLabelHoverOffset = document.ResolveNamedValue<int>(Desktop.Provider, "NameLabelHoverOffset");
		int num = 0;
		foreach (ClientSharedSinglePlayerJoinableWorldWrapper world in sharedSinglePlayerJoinableWorlds)
		{
			Logger.Info($"Creating a UI element for world {world}");
			if (!world.Hidden)
			{
				Group group = MakeButtonContainer(num);
				UIFragment uIFragment = document.Instantiate(Desktop, group);
				Button button = uIFragment.Get<Button>("Button");
				num++;
				button.Activating = (button.Find<TextButton>("PlayButton").Activating = delegate
				{
					Queue(world.WorldId);
				});
				button.MouseEntered = delegate
				{
					_container.Reorder(group);
					group.Find<Element>("LowerGlow").Visible = true;
					group.Find<Element>("UpperGlow").Visible = true;
					group.Find<Element>("PlayButton").Visible = true;
					group.Find<Element>("Name").Anchor.Bottom += _nameLabelHoverOffset;
					group.Layout();
				};
				button.MouseExited = delegate
				{
					group.Find<Element>("LowerGlow").Visible = false;
					group.Find<Element>("UpperGlow").Visible = false;
					group.Find<Element>("PlayButton").Visible = false;
					group.Find<Element>("Name").Anchor.Bottom -= _nameLabelHoverOffset;
					group.Layout();
				};
				uIFragment.Get<Label>("Name").Text = world.Name;
				Element element = uIFragment.Get<Element>("Image");
				element.Layout();
			}
		}
		AddCreateButton(MakeButtonContainer(num++), document);
		Group MakeButtonContainer(int index)
		{
			int value = index % _framesPerRow * (_frameWidth + _frameSpacing);
			int value2 = index / _framesPerRow * (_frameHeight + _frameSpacing);
			return new Group(Desktop, _container)
			{
				Anchor = new Anchor
				{
					Left = value,
					Top = value2,
					Width = _frameWidth,
					Height = _frameHeight
				}
			};
		}
	}

	private void AddCreateButton(Group group, Document doc)
	{
		UIFragment uIFragment = doc.Instantiate(Desktop, group);
		Button button = uIFragment.Get<Button>("Button");
		button.Activating = (button.Find<TextButton>("PlayButton").Activating = delegate
		{
			Interface.App.MainMenu.CreateSharedSinglePlayerWorld("new world");
		});
		button.MouseEntered = delegate
		{
			_container.Reorder(group);
			group.Find<Element>("LowerGlow").Visible = true;
			group.Find<Element>("UpperGlow").Visible = true;
			group.Find<Element>("PlayButton").Visible = true;
			group.Find<Element>("Name").Anchor.Bottom += _nameLabelHoverOffset;
			group.Layout();
		};
		button.MouseExited = delegate
		{
			group.Find<Element>("LowerGlow").Visible = false;
			group.Find<Element>("UpperGlow").Visible = false;
			group.Find<Element>("PlayButton").Visible = false;
			group.Find<Element>("Name").Anchor.Bottom -= _nameLabelHoverOffset;
			group.Layout();
		};
		uIFragment.Get<Label>("Name").Text = "Create a new world";
		Element element = uIFragment.Get<Element>("Image");
		element.Layout();
	}

	protected override void OnMounted()
	{
		MainMenuView.ShowTopBar(showTopBar: true);
		BuildWorldsList();
	}

	public void OnWorldsUpdated()
	{
		BuildWorldsList();
		Layout();
	}
}
