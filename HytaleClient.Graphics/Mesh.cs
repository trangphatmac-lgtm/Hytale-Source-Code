namespace HytaleClient.Graphics;

internal struct Mesh
{
	public int Count;

	public GLVertexArray VertexArray;

	public GLBuffer VerticesBuffer;

	public GLBuffer IndicesBuffer;

	private static GLFunctions _gl;

	public bool UseIndices => IndicesBuffer != GLBuffer.None;

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public void CreateGPUData(bool useIndices = true)
	{
		Count = 0;
		VertexArray = _gl.GenVertexArray();
		VerticesBuffer = _gl.GenBuffer();
		IndicesBuffer = (useIndices ? _gl.GenBuffer() : GLBuffer.None);
	}

	public void Dispose()
	{
		if (IndicesBuffer != GLBuffer.None)
		{
			_gl.DeleteBuffer(IndicesBuffer);
		}
		_gl.DeleteBuffer(VerticesBuffer);
		_gl.DeleteVertexArray(VertexArray);
		IndicesBuffer = GLBuffer.None;
		VerticesBuffer = GLBuffer.None;
		VertexArray = GLVertexArray.None;
	}
}
