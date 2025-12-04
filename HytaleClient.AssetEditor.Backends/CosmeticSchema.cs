using System;
using System.Collections.Generic;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Data.Characters;
using HytaleClient.Interface.UI.Elements;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Backends;

internal static class CosmeticSchema
{
	public static SchemaNode CreateHeadAccessorySchema(params string[] directory)
	{
		SchemaNode schemaNode = CreateCosmeticSchema(directory);
		schemaNode.Properties["HeadAccessoryType"] = new SchemaNode
		{
			Type = SchemaNode.NodeType.Dropdown,
			Enum = new string[3] { "Simple", "HalfCovering", "FullyCovering" },
			EnumTitles = new string[3],
			EnumDescriptions = new string[3],
			Title = "Head Accessory Type",
			Description = "This value defines how this accessory affects the haircut.\n\n<b>Simple:</b> This does not affect the haircut\n<b>HalfCovering:</b> This will replace the haircut of the player with a fallback version\n<b>FullyCovering:</b> This will only display the base of the haircut model"
		};
		return schemaNode;
	}

	public static SchemaNode CreateHaircutSchema(params string[] directory)
	{
		SchemaNode schemaNode = CreateCosmeticSchema(directory);
		schemaNode.Properties["HairType"] = new SchemaNode
		{
			Type = SchemaNode.NodeType.Dropdown,
			Enum = new string[3] { "Short", "Medium", "Long" },
			EnumTitles = new string[3],
			EnumDescriptions = new string[3],
			Title = "Hair Type",
			Description = "This property decides which haircut we are falling back to if the player has a head accessory of type HalfCovering and 'Requires Generic Haircut' is turned on."
		};
		schemaNode.Properties["RequiresGenericHaircut"] = new SchemaNode
		{
			Type = SchemaNode.NodeType.Checkbox,
			DefaultValue = JToken.op_Implicit(false),
			Title = "Requires Generic Haircut",
			Description = "If the player has a head accessory of type HalfCovering and this property is set to true, then the haircut will fallback to one of a set of predefined haircuts that don't interfere with the accessory model."
		};
		return schemaNode;
	}

	public static SchemaNode CreateCosmeticSchema(params string[] directory)
	{
		SchemaNode schemaNode = new SchemaNode
		{
			Type = SchemaNode.NodeType.Object,
			Properties = new Dictionary<string, SchemaNode>()
		};
		schemaNode.Properties.Add("Name", new SchemaNode
		{
			Type = SchemaNode.NodeType.Text,
			Description = "Name displayed in the User Interface"
		});
		schemaNode.Properties.Add("Model", new SchemaNode
		{
			Type = SchemaNode.NodeType.AssetFileDropdown,
			AllowedFileExtensions = new string[1] { "blockymodel" },
			AllowedDirectories = directory
		});
		schemaNode.Properties.Add("GreyscaleTexture", new SchemaNode
		{
			Type = SchemaNode.NodeType.AssetFileDropdown,
			Title = "Greyscale Texture",
			AllowedFileExtensions = new string[1] { "png" },
			AllowedDirectories = directory
		});
		schemaNode.Properties.Add("GradientSet", new SchemaNode
		{
			Type = SchemaNode.NodeType.AssetIdDropdown,
			Title = "Gradient Set",
			AssetType = "Cosmetics.GradientSet"
		});
		string[] names = Enum.GetNames(typeof(PlayerSkinProperty));
		schemaNode.Properties.Add("DisableCharacterPartCategory", new SchemaNode
		{
			Type = SchemaNode.NodeType.Dropdown,
			Title = "Disable Category",
			Description = "When a player selects this asset, they won't be able to select an asset from the category specified here and their currently selected asset from that specified category will be hidden.",
			Enum = names,
			EnumTitles = new string[names.Length],
			EnumDescriptions = new string[names.Length]
		});
		SchemaNode schemaNode2 = new SchemaNode();
		schemaNode2.Type = SchemaNode.NodeType.Map;
		schemaNode2.Key = new SchemaNode
		{
			Type = SchemaNode.NodeType.Text
		};
		schemaNode2.Value = new SchemaNode
		{
			Type = SchemaNode.NodeType.Object,
			Properties = new Dictionary<string, SchemaNode>
			{
				{
					"Texture",
					new SchemaNode
					{
						Type = SchemaNode.NodeType.AssetFileDropdown,
						AllowedFileExtensions = new string[1] { "png" },
						AllowedDirectories = directory
					}
				},
				{
					"BaseColor",
					new SchemaNode
					{
						Type = SchemaNode.NodeType.List,
						Title = "Preview Color",
						Description = "This is one or a combination of colors that will be displayed in the color selection UI.",
						IsCollapsedByDefault = false,
						Value = new SchemaNode
						{
							Type = SchemaNode.NodeType.Color,
							ColorFormat = ColorPicker.ColorFormat.Rgb
						}
					}
				}
			}
		};
		SchemaNode schemaNode3 = schemaNode2;
		schemaNode.Properties.Add("Textures", schemaNode3);
		schemaNode.Properties.Add("Variants", new SchemaNode
		{
			Type = SchemaNode.NodeType.Map,
			Key = new SchemaNode
			{
				Type = SchemaNode.NodeType.Text
			},
			Value = new SchemaNode
			{
				Type = SchemaNode.NodeType.Object,
				Properties = new Dictionary<string, SchemaNode>
				{
					{
						"Model",
						new SchemaNode
						{
							Type = SchemaNode.NodeType.AssetFileDropdown,
							AllowedFileExtensions = new string[1] { "blockymodel" },
							AllowedDirectories = directory
						}
					},
					{
						"GreyscaleTexture",
						new SchemaNode
						{
							Type = SchemaNode.NodeType.AssetFileDropdown,
							Title = "Greyscale Texture",
							AllowedFileExtensions = new string[1] { "png" },
							AllowedDirectories = directory
						}
					},
					{
						"Textures",
						schemaNode3.Clone(deep: true)
					}
				}
			}
		});
		schemaNode.Properties.Add("Tags", new SchemaNode
		{
			Type = SchemaNode.NodeType.List,
			IsCollapsedByDefault = false,
			Description = "Tags that can be used for filtering assets by the user.",
			Value = new SchemaNode
			{
				Type = SchemaNode.NodeType.Text,
				DataSet = "Cosmetics.Tags"
			}
		});
		schemaNode.Properties.Add("DefaultFor", new SchemaNode
		{
			Type = SchemaNode.NodeType.Dropdown,
			Enum = new string[3] { "None", "Masculine", "Feminine" },
			EnumTitles = new string[3],
			EnumDescriptions = new string[3],
			Title = "Default for body type",
			Description = "This option sets this asset as the default asset for a specific body type. Per body type there can only be a single default asset. This asset will be selected automatically when switching the body type. Only eyes and eyebrows make use of this property at the moment."
		});
		return schemaNode;
	}
}
