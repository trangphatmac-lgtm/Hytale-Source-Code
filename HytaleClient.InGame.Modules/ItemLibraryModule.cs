#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Data.BlockyModels;
using HytaleClient.Data.Items;
using HytaleClient.Graphics;
using HytaleClient.InGame.Modules.Entities;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.InGame.Modules;

internal class ItemLibraryModule : Module
{
	private class IconTextureInfo
	{
		public string Name;

		public string Checksum;

		public int Width;

		public int Height;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private Dictionary<string, ClientItemPlayerAnimations> _itemPlayerAnimationsById;

	public ClientItemPlayerAnimations DefaultItemPlayerAnimations;

	public Dictionary<string, ClientIcon> ItemIcons;

	private Dictionary<string, ClientItemBase> _items;

	private readonly Dictionary<string, BlockyModel> _modelsByChecksum = new Dictionary<string, BlockyModel>();

	private readonly ConcurrentDictionary<string, BlockyAnimation> _animationsByChecksum = new ConcurrentDictionary<string, BlockyAnimation>();

	public Dictionary<string, ClientResourceType> ResourceTypes { get; private set; }

	public ItemLibraryModule(GameInstance gameInstance)
		: base(gameInstance)
	{
	}

	public void PrepareItems(Dictionary<string, ItemBase> networkItems, Dictionary<string, Point> entitiesImageLocations, ref Dictionary<string, ClientItemBase> upcomingItems, CancellationToken cancellationToken)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		_modelsByChecksum.Clear();
		_animationsByChecksum.Clear();
		foreach (ItemBase networkItem in networkItems.Values)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			if (networkItem == null)
			{
				continue;
			}
			ClientItemBase itemBase = new ClientItemBase();
			ClientItemBaseProtocolInitializer.Parse(networkItem, _gameInstance.EntityStoreModule.NodeNameManager, ref itemBase);
			if (itemBase.BlockId == 0)
			{
				string value;
				if (networkItem.Model == null)
				{
					_gameInstance.App.DevTools.Error("Missing model for item " + networkItem.Id);
					itemBase.Model = new BlockyModel(0);
				}
				else if (!_gameInstance.HashesByServerAssetPath.TryGetValue(networkItem.Model, out value))
				{
					_gameInstance.App.DevTools.Error("Missing model asset: " + networkItem.Model + " for item " + networkItem.Id);
					itemBase.Model = new BlockyModel(0);
				}
				else
				{
					if (!_modelsByChecksum.TryGetValue(value, out var value2))
					{
						try
						{
							value2 = new BlockyModel(BlockyModel.MaxNodeCount);
							BlockyModelInitializer.Parse(AssetManager.GetAssetUsingHash(value), _gameInstance.EntityStoreModule.NodeNameManager, ref value2);
							_modelsByChecksum[value] = value2;
						}
						catch (Exception innerException)
						{
							throw new Exception("Failed to parse BlockyModel for item: " + networkItem.Id + ", Model: " + networkItem.Model + " (" + value + ")", innerException);
						}
					}
					itemBase.Model = value2.Clone();
					if (networkItem.ItemAppearanceConditions != null)
					{
						PrepareItemAppearanceConditions(networkItem, itemBase, value2, entitiesImageLocations);
					}
				}
				itemBase.Model.SetAtlasIndex(1);
				if (entitiesImageLocations != null && itemBase.Model != null)
				{
					PrepareItemUV(itemBase, entitiesImageLocations);
				}
				if (networkItem.Animation != null)
				{
					if (!_gameInstance.HashesByServerAssetPath.TryGetValue(networkItem.Animation, out var value3))
					{
						_gameInstance.App.DevTools.Error("Missing animated asset: " + networkItem.Animation + " for item " + networkItem.Id);
					}
					else
					{
						itemBase.Animation = _animationsByChecksum.GetOrAdd(value3, delegate(string x)
						{
							try
							{
								BlockyAnimation blockyAnimation2 = new BlockyAnimation();
								BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(x), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation2);
								return blockyAnimation2;
							}
							catch (Exception ex2)
							{
								Logger.Error(ex2, "Failed to parse BlockyAnimation for item: " + networkItem.Id + ", Animation: " + networkItem.Animation);
								return (BlockyAnimation)null;
							}
						});
					}
				}
			}
			if (networkItem.DroppedItemAnimation != null)
			{
				if (!_gameInstance.HashesByServerAssetPath.TryGetValue(networkItem.DroppedItemAnimation, out var value4))
				{
					_gameInstance.App.DevTools.Error("Missing animated asset: " + networkItem.DroppedItemAnimation + " for item " + networkItem.Id);
				}
				else
				{
					itemBase.DroppedItemAnimation = _animationsByChecksum.GetOrAdd(value4, delegate(string x)
					{
						try
						{
							BlockyAnimation blockyAnimation = new BlockyAnimation();
							BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(x), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation);
							return blockyAnimation;
						}
						catch (Exception ex)
						{
							Logger.Error(ex, "Failed to parse BlockyAnimation for item: " + networkItem.Id + ", Animation: " + networkItem.DroppedItemAnimation);
							return (BlockyAnimation)null;
						}
					});
				}
			}
			upcomingItems[networkItem.Id] = itemBase;
		}
	}

	private void PrepareItemAppearanceConditions(ItemBase networkItem, ClientItemBase clientItem, BlockyModel baseModel, Dictionary<string, Point> entitiesImageLocations)
	{
		foreach (KeyValuePair<int, ItemAppearanceCondition[]> itemAppearanceCondition in networkItem.ItemAppearanceConditions)
		{
			BlockyModel[] array = new BlockyModel[itemAppearanceCondition.Value.Length];
			for (int i = 0; i < itemAppearanceCondition.Value.Length; i++)
			{
				ItemAppearanceCondition val = itemAppearanceCondition.Value[i];
				BlockyModel blockyModel = null;
				if (val.Model != null)
				{
					if (_gameInstance.HashesByServerAssetPath.TryGetValue(val.Model, out var value))
					{
						if (!_modelsByChecksum.TryGetValue(value, out var value2))
						{
							try
							{
								value2 = new BlockyModel(BlockyModel.MaxNodeCount);
								BlockyModelInitializer.Parse(AssetManager.GetAssetUsingHash(value), _gameInstance.EntityStoreModule.NodeNameManager, ref value2);
								_modelsByChecksum[value] = value2;
							}
							catch (Exception innerException)
							{
								throw new Exception("Failed to parse BlockyModel for item: " + networkItem.Id + ", Model: " + val.Model + " (" + value + ")", innerException);
							}
						}
						blockyModel = value2.Clone();
					}
					else
					{
						blockyModel = baseModel.Clone();
					}
				}
				else
				{
					blockyModel = baseModel.Clone();
				}
				blockyModel.SetAtlasIndex(1);
				if (entitiesImageLocations != null && val.Texture != null)
				{
					Point value4;
					if (!_gameInstance.HashesByServerAssetPath.TryGetValue(val.Texture, out var value3))
					{
						_gameInstance.App.DevTools.Error("Missing texture asset: " + val.Texture + " for item " + networkItem.Id);
					}
					else if (!entitiesImageLocations.TryGetValue(value3, out value4))
					{
						_gameInstance.App.DevTools.Error("Cannot use " + val.Texture + " as texture for item " + networkItem.Id);
					}
					else
					{
						blockyModel.OffsetUVs(value4);
					}
				}
				clientItem.ItemAppearanceConditions[itemAppearanceCondition.Key][i].Model = blockyModel;
				array[i] = blockyModel;
			}
		}
	}

	public void PrepareItemIconAtlas(Dictionary<string, ItemBase> networkItems, out Dictionary<string, ClientIcon> icons, out byte[] pixels, out int width, out int height, CancellationToken cancellationToken)
	{
		Dictionary<string, IconTextureInfo> dictionary = new Dictionary<string, IconTextureInfo>();
		icons = new Dictionary<string, ClientIcon>();
		pixels = null;
		width = 2048;
		height = 64;
		foreach (ItemBase value3 in networkItems.Values)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			if (value3?.Icon == null)
			{
				continue;
			}
			if (_gameInstance.HashesByServerAssetPath.TryGetValue(value3.Icon, out var value))
			{
				if (!dictionary.TryGetValue(value3.Icon, out var value2))
				{
					value2 = new IconTextureInfo
					{
						Checksum = value,
						Name = value3.Icon
					};
					string assetLocalPathUsingHash = AssetManager.GetAssetLocalPathUsingHash(value);
					if (Image.TryGetPngDimensions(assetLocalPathUsingHash, out value2.Width, out value2.Height))
					{
						dictionary[value3.Icon] = value2;
						continue;
					}
					_gameInstance.App.DevTools.Error("Failed to get PNG dimensions for: " + value3.Icon + ", " + assetLocalPathUsingHash + " (" + value + ")");
				}
			}
			else
			{
				_gameInstance.App.DevTools.Error("Missing icon: " + value3.Icon + " for item " + value3.Id);
			}
		}
		List<IconTextureInfo> list = new List<IconTextureInfo>(dictionary.Values);
		list.Sort((IconTextureInfo a, IconTextureInfo b) => b.Height.CompareTo(a.Height));
		Point point = new Point(0, 0);
		int num = 0;
		foreach (IconTextureInfo item in list)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			int num2 = System.Math.Min(64, System.Math.Min(item.Width, item.Height));
			if (point.X + num2 > width)
			{
				point.X = 0;
				point.Y = num;
			}
			while (point.Y + num2 > height)
			{
				height <<= 1;
			}
			icons[item.Name] = new ClientIcon(point.X, point.Y, num2);
			num = System.Math.Max(num, point.Y + num2);
			point.X += num2;
		}
		pixels = new byte[width * height * 4];
		point = Point.Zero;
		foreach (IconTextureInfo item2 in list)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			try
			{
				Image image = new Image(AssetManager.GetAssetUsingHash(item2.Checksum));
				ClientIcon clientIcon = icons[item2.Name];
				for (int i = 0; i < clientIcon.Size; i++)
				{
					int dstOffset = ((clientIcon.Y + i) * width + clientIcon.X) * 4;
					Buffer.BlockCopy(image.Pixels, i * clientIcon.Size * 4, pixels, dstOffset, clientIcon.Size * 4);
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Faile to load icon texture: " + AssetManager.GetAssetLocalPathUsingHash(item2.Checksum));
			}
		}
	}

	public void PrepareItemUVs(ref Dictionary<string, ClientItemBase> upcomingItems, Dictionary<string, Point> entitiesImageLocations, CancellationToken cancellationToken)
	{
		foreach (ClientItemBase value in upcomingItems.Values)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			if (value.Model != null)
			{
				PrepareItemUV(value, entitiesImageLocations);
			}
			if (value.ItemAppearanceConditions != null)
			{
				PrepareItemAppearanceConditionUV(value, entitiesImageLocations);
			}
		}
	}

	private void PrepareItemUV(ClientItemBase item, Dictionary<string, Point> entitiesImageLocations)
	{
		Point value2;
		if (!_gameInstance.HashesByServerAssetPath.TryGetValue(item.Texture, out var value))
		{
			_gameInstance.App.DevTools.Error("Missing texture asset: " + item.Texture + " for item " + item.Id);
		}
		else if (!entitiesImageLocations.TryGetValue(value, out value2))
		{
			_gameInstance.App.DevTools.Error("Cannot use " + item.Texture + " as texture for item " + item.Id);
		}
		else
		{
			item.Model.OffsetUVs(value2);
		}
	}

	private void PrepareItemAppearanceConditionUV(ClientItemBase item, Dictionary<string, Point> entitiesImageLocations)
	{
		foreach (KeyValuePair<int, ClientItemAppearanceCondition[]> itemAppearanceCondition in item.ItemAppearanceConditions)
		{
			for (int i = 0; i < itemAppearanceCondition.Value.Length; i++)
			{
				ClientItemAppearanceCondition clientItemAppearanceCondition = itemAppearanceCondition.Value[i];
				if (clientItemAppearanceCondition.Texture != null)
				{
					if (!_gameInstance.HashesByServerAssetPath.TryGetValue(clientItemAppearanceCondition.Texture, out var value))
					{
						_gameInstance.App.DevTools.Error("Failed to load entity effect texture: " + clientItemAppearanceCondition.Texture);
						return;
					}
					if (!entitiesImageLocations.TryGetValue(value, out var value2))
					{
						_gameInstance.App.DevTools.Error("Cannot use " + clientItemAppearanceCondition.Texture + " as an entity effect texture");
						return;
					}
					clientItemAppearanceCondition.Model.OffsetUVs(value2);
				}
			}
		}
	}

	public void PrepareItemPlayerAnimations(Dictionary<string, ItemPlayerAnimations> networkItemAnimations, out Dictionary<string, ClientItemPlayerAnimations> upcomingItemPlayerAnimationsById)
	{
		Debug.Assert(!ThreadHelper.IsMainThread());
		upcomingItemPlayerAnimationsById = new Dictionary<string, ClientItemPlayerAnimations>();
		foreach (ItemPlayerAnimations value in networkItemAnimations.Values)
		{
			ClientItemPlayerAnimations clientItemPlayerAnimations = new ClientItemPlayerAnimations(value);
			foreach (KeyValuePair<string, ItemAnimation> animation in value.Animations)
			{
				clientItemPlayerAnimations.Animations[animation.Key] = LoadItemAnimation(value.Id + "/" + animation.Key, animation.Value, value.PullbackConfig, value.UseFirstPersonOverride);
			}
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "RunBackward", "Run");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "Sprint", "Run");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "CrouchWalk", "Run");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "CrouchWalkBackward", "CrouchWalk");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "JumpRun", "JumpWalk");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "JumpSprint", "JumpRun");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "Jump", "JumpWalk");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimBackward", "Swim");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimFast", "Swim");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimFloat", "SwimSink");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimIdle", "SwimSink");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimDive", "Swim");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimDiveFast", "SwimDive");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimDiveBackward", "SwimDive");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SwimJump", "JumpWalk");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "FluidIdle", "Idle");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "FluidWalk", "Run");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "FluidWalkBackward", "RunBackward");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "FluidRun", "Sprint");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "CrouchSlide", "CrouchWalk");
			SetAnimationFallback(clientItemPlayerAnimations.Animations, "SafetyRoll", "CrouchWalk");
			upcomingItemPlayerAnimationsById[value.Id] = clientItemPlayerAnimations;
		}
	}

	public void SetupItemPlayerAnimations(Dictionary<string, ClientItemPlayerAnimations> animationsById)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_004d: Expected O, but got Unknown
		Debug.Assert(ThreadHelper.IsMainThread());
		_itemPlayerAnimationsById = animationsById;
		if (!animationsById.TryGetValue("Default", out DefaultItemPlayerAnimations))
		{
			DefaultItemPlayerAnimations = new ClientItemPlayerAnimations(new ItemPlayerAnimations
			{
				Id = "Default",
				WiggleWeights_ = new WiggleWeights()
			});
		}
	}

	public void SetupItemIcons(Dictionary<string, ClientIcon> icons, byte[] pixels, int width, int height)
	{
		Texture texture = new Texture(Texture.TextureTypes.Texture2D);
		texture.CreateTexture2D(width, height, pixels, 0, GL.LINEAR_MIPMAP_LINEAR, GL.LINEAR);
		ItemIcons = icons;
		_gameInstance.App.Interface.InGameView.OnItemIconsUpdated(texture);
	}

	public void SetupResourceTypes(Dictionary<string, ClientResourceType> resourceTypes)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		ResourceTypes = resourceTypes;
	}

	public void SetupItems(Dictionary<string, ClientItemBase> items)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		_items = items;
		LinkItemPlayerAnimations();
	}

	public void LinkItemPlayerAnimations()
	{
		foreach (ClientItemBase value2 in _items.Values)
		{
			if (value2.PlayerAnimationsId != null && _itemPlayerAnimationsById.TryGetValue(value2.PlayerAnimationsId, out var value))
			{
				value2.PlayerAnimations = value;
				continue;
			}
			_gameInstance.App.DevTools.Error("Missing playerAnimationsId for item " + value2.Id);
			value2.PlayerAnimations = DefaultItemPlayerAnimations;
		}
	}

	public bool GetItemPlayerAnimation(string id, out ClientItemPlayerAnimations ret)
	{
		return _itemPlayerAnimationsById.TryGetValue(id, out ret);
	}

	public EntityAnimation LoadItemAnimation(string itemId, ItemAnimation animation, ItemPullbackConfiguration pullbackConfig, bool useFirstPersonOverride)
	{
		string thirdPerson = animation.ThirdPerson;
		string thirdPersonMoving = animation.ThirdPersonMoving;
		string thirdPersonFace = animation.ThirdPersonFace;
		string firstPerson = animation.FirstPerson;
		string firstPersonOverride = animation.FirstPersonOverride;
		float speed = ((animation.Speed != 0f) ? animation.Speed : 1f);
		float blendingDuration = animation.BlendingDuration * 60f;
		bool looping = animation.Looping;
		bool keepPreviousFirstPersonAnimation = animation.KeepPreviousFirstPersonAnimation;
		bool clipsGeometry = animation.ClipsGeometry;
		return LoadAnimation(itemId, thirdPerson, thirdPersonMoving, thirdPersonFace, firstPerson, firstPersonOverride, speed, blendingDuration, looping, keepPreviousFirstPersonAnimation, pullbackConfig, clipsGeometry, useFirstPersonOverride);
	}

	private EntityAnimation LoadAnimation(string itemId, string thirdPersonPath, string thirdPersonMovingPath, string thirdPersonFacePath, string firstPersonPath, string firstPersonOverridePath, float speed, float blendingDuration, bool looping, bool keepPreviousFirstPersonAnimation, ItemPullbackConfiguration pullbackConfig, bool clipsGeometry, bool useFirstPersonOverride)
	{
		BlockyAnimation data = null;
		if (thirdPersonPath != null)
		{
			if (_gameInstance.HashesByServerAssetPath.TryGetValue(thirdPersonPath, out var value))
			{
				data = _animationsByChecksum.GetOrAdd(value, delegate(string x)
				{
					try
					{
						BlockyAnimation blockyAnimation5 = new BlockyAnimation();
						BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(x), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation5);
						return blockyAnimation5;
					}
					catch (Exception ex5)
					{
						Logger.Error(ex5, "Failed to parse BlockyAnimation for item: " + itemId + ", Animation: " + thirdPersonPath);
						return (BlockyAnimation)null;
					}
				});
			}
			else
			{
				_gameInstance.App.DevTools.Error("Failed to load third person animation: " + thirdPersonPath + " for item: " + itemId);
			}
		}
		else if (!itemId.Contains("Default"))
		{
			_gameInstance.App.DevTools.Error("Missing third person animation for item: " + itemId);
		}
		BlockyAnimation movingData = null;
		if (thirdPersonMovingPath != null)
		{
			if (_gameInstance.HashesByServerAssetPath.TryGetValue(thirdPersonMovingPath, out var value2))
			{
				movingData = _animationsByChecksum.GetOrAdd(value2, delegate(string x)
				{
					try
					{
						BlockyAnimation blockyAnimation4 = new BlockyAnimation();
						BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(x), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation4);
						return blockyAnimation4;
					}
					catch (Exception ex4)
					{
						Logger.Error(ex4, "Failed to parse BlockyAnimation for item: " + itemId + ", Animation: " + thirdPersonMovingPath);
						return (BlockyAnimation)null;
					}
				});
			}
			else
			{
				_gameInstance.App.DevTools.Error("Failed to load third person moving animation: " + thirdPersonMovingPath + " for item: " + itemId);
			}
		}
		BlockyAnimation faceData = null;
		if (thirdPersonFacePath != null)
		{
			if (_gameInstance.HashesByServerAssetPath.TryGetValue(thirdPersonFacePath, out var value3))
			{
				faceData = _animationsByChecksum.GetOrAdd(value3, delegate(string x)
				{
					try
					{
						BlockyAnimation blockyAnimation3 = new BlockyAnimation();
						BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(x), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation3);
						return blockyAnimation3;
					}
					catch (Exception ex3)
					{
						Logger.Error(ex3, "Failed to parse BlockyAnimation for item: " + itemId + ", Animation: " + thirdPersonFacePath);
						return (BlockyAnimation)null;
					}
				});
			}
			else
			{
				_gameInstance.App.DevTools.Error("Failed to load face animation: " + thirdPersonFacePath + " for item: " + itemId);
			}
		}
		BlockyAnimation firstPersonData = null;
		if (firstPersonPath != null)
		{
			if (_gameInstance.HashesByServerAssetPath.TryGetValue(firstPersonPath, out var firstPersonAnimationChecksum))
			{
				firstPersonData = _animationsByChecksum.GetOrAdd(firstPersonAnimationChecksum, delegate
				{
					try
					{
						BlockyAnimation blockyAnimation2 = new BlockyAnimation();
						BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(firstPersonAnimationChecksum), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation2);
						return blockyAnimation2;
					}
					catch (Exception ex2)
					{
						Logger.Error(ex2, "Failed to parse BlockyAnimation for item: " + itemId + ", Animation: " + firstPersonPath);
						return (BlockyAnimation)null;
					}
				});
			}
			else
			{
				_gameInstance.App.DevTools.Error("Failed to load first person animation: " + firstPersonPath + " for item: " + itemId);
			}
		}
		BlockyAnimation firstPersonOverrideData = null;
		if (useFirstPersonOverride && firstPersonOverridePath != null)
		{
			if (_gameInstance.HashesByServerAssetPath.TryGetValue(firstPersonOverridePath, out var firstPersonOverrideAnimationChecksum))
			{
				firstPersonOverrideData = _animationsByChecksum.GetOrAdd(firstPersonOverrideAnimationChecksum, delegate
				{
					try
					{
						BlockyAnimation blockyAnimation = new BlockyAnimation();
						BlockyAnimationInitializer.Parse(AssetManager.GetAssetUsingHash(firstPersonOverrideAnimationChecksum), _gameInstance.EntityStoreModule.NodeNameManager, ref blockyAnimation);
						return blockyAnimation;
					}
					catch (Exception ex)
					{
						Logger.Error(ex, "Failed to parse BlockyAnimation for item: " + itemId + ", Animation: " + firstPersonPath);
						return (BlockyAnimation)null;
					}
				});
			}
			else
			{
				_gameInstance.App.DevTools.Error("Failed to load first person override animation: " + firstPersonPath + " for item: " + itemId);
			}
		}
		return new EntityAnimation(data, speed, blendingDuration, looping, keepPreviousFirstPersonAnimation, 0u, 0f, Array.Empty<int>(), 0, movingData, faceData, firstPersonData, firstPersonOverrideData, pullbackConfig, clipsGeometry);
	}

	private void SetAnimationFallback(Dictionary<string, EntityAnimation> animations, string source, string fallback)
	{
		if (!animations.ContainsKey(source) && animations.TryGetValue(fallback, out var value))
		{
			animations.Add(source, new EntityAnimation(value));
		}
	}

	public Dictionary<string, ClientItemBase> GetItems()
	{
		return _items;
	}

	public ClientItemBase GetItem(string itemId)
	{
		if (itemId == null || !_items.TryGetValue(itemId, out var value))
		{
			return null;
		}
		return value;
	}

	public ClientResourceType GetResourceType(string resourceTypeId)
	{
		if (resourceTypeId == null || !ResourceTypes.TryGetValue(resourceTypeId, out var value))
		{
			return null;
		}
		return value;
	}
}
