using System;
using System.Collections.Generic;
using HytaleClient.Application.Services;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using NLog;

namespace HytaleClient.Interface.MainMenu.Pages;

internal class MinigamesPage : InterfaceComponent
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly Dictionary<string, TextureArea> _downloadedGameTextureAreas = new Dictionary<string, TextureArea>();

	private readonly Dictionary<string, ExternalTextureLoader> _gameTextureLoaders = new Dictionary<string, ExternalTextureLoader>();

	public readonly MainMenuView MainMenuView;

	private Group _container;

	private int _framesPerRow;

	private int _frameWidth;

	private int _frameHeight;

	private int _frameSpacing;

	private int _nameLabelHoverOffset;

	public MinigamesPage(MainMenuView mainMenuView)
		: base(mainMenuView.Interface, null)
	{
		MainMenuView = mainMenuView;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/Minigames/MinigamesPage.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("MinigamesContainer");
		_framesPerRow = document.ResolveNamedValue<int>(Desktop.Provider, "FramesPerRow");
		_frameWidth = document.ResolveNamedValue<int>(Desktop.Provider, "FrameWidth");
		_frameHeight = document.ResolveNamedValue<int>(Desktop.Provider, "FrameHeight");
		_frameSpacing = document.ResolveNamedValue<int>(Desktop.Provider, "FrameSpacing");
		BuildGamesList();
	}

	public void DisposeGameTextures()
	{
		foreach (TextureArea value in _downloadedGameTextureAreas.Values)
		{
			value.Texture.Dispose();
		}
		_downloadedGameTextureAreas.Clear();
	}

	private void Queue(string joinKey)
	{
		Interface.App.MainMenu.QueueForMinigame(joinKey);
	}

	private void BuildGamesList()
	{
		if (!base.IsMounted)
		{
			return;
		}
		List<ClientGameWrapper> games = Interface.App.HytaleServices.Games;
		if (games == null)
		{
			return;
		}
		_container.Clear();
		Interface.TryGetDocument("MainMenu/Minigames/MinigameButton.ui", out var document);
		_nameLabelHoverOffset = document.ResolveNamedValue<int>(Desktop.Provider, "NameLabelHoverOffset");
		int num = 0;
		foreach (ClientGameWrapper game in games)
		{
			if (!game.Display)
			{
				continue;
			}
			Group group = MakeButtonContainer(num);
			UIFragment uIFragment = document.Instantiate(Desktop, group);
			Button button = uIFragment.Get<Button>("Button");
			num++;
			button.Activating = (button.Find<TextButton>("PlayButton").Activating = delegate
			{
				Queue(game.JoinKey);
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
			uIFragment.Get<Label>("Name").Text = game.DefaultName;
			Element imageElt = uIFragment.Get<Element>("Image");
			if (_downloadedGameTextureAreas.TryGetValue(game.ImageUrl, out var value))
			{
				imageElt.Background = new PatchStyle(value);
				continue;
			}
			if (!_gameTextureLoaders.TryGetValue(game.ImageUrl, out var value2))
			{
				ExternalTextureLoader externalTextureLoader2 = (_gameTextureLoaders[game.ImageUrl] = ExternalTextureLoader.FromUrl(Interface.App, game.ImageUrl));
				value2 = externalTextureLoader2;
				value2.OnComplete += delegate(object sender, TextureArea downloadedTextureArea)
				{
					_gameTextureLoaders.Remove(game.ImageUrl);
					_downloadedGameTextureAreas.Add(game.ImageUrl, downloadedTextureArea);
				};
				value2.OnFailure += delegate(object sender, Exception exception)
				{
					_gameTextureLoaders.Remove(game.ImageUrl);
					Logger.Error(exception, "Failed to load game image from {0}.", new object[1] { game.ImageUrl });
				};
			}
			value2.OnComplete += delegate(object sender, TextureArea downloadedTextureArea)
			{
				imageElt.Background = new PatchStyle(downloadedTextureArea);
				imageElt.Layout();
			};
		}
		Group MakeButtonContainer(int index)
		{
			int value3 = index % _framesPerRow * (_frameWidth + _frameSpacing);
			int value4 = index / _framesPerRow * (_frameHeight + _frameSpacing);
			return new Group(Desktop, _container)
			{
				Anchor = new Anchor
				{
					Left = value3,
					Top = value4,
					Width = _frameWidth,
					Height = _frameHeight
				}
			};
		}
	}

	protected override void OnMounted()
	{
		MainMenuView.ShowTopBar(showTopBar: true);
		BuildGamesList();
	}

	protected override void OnUnmounted()
	{
		foreach (ExternalTextureLoader value in _gameTextureLoaders.Values)
		{
			value.Cancel();
		}
		_gameTextureLoaders.Clear();
	}

	public void OnGamesUpdated()
	{
		BuildGamesList();
		if (base.IsMounted)
		{
			_container.Layout();
		}
	}
}
