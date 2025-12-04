using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Config;

public class ConfigEditorState
{
	public Dictionary<PropertyPath, bool> UncollapsedProperties = new Dictionary<PropertyPath, bool>();

	public Point ScrollOffset;

	public PropertyPath? ActiveCategory;
}
