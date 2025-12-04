using System.Collections.Generic;
using HytaleClient.Math;

namespace HytaleClient.Data.BlockyModels;

public struct NodeShape
{
	public bool Visible;

	public bool DoubleSided;

	public string ShadingMode;

	public string Type;

	public Vector3 Offset;

	public Vector3 Stretch;

	public NodeShapeSettings Settings;

	public IDictionary<string, FaceLayout> TextureLayout;
}
