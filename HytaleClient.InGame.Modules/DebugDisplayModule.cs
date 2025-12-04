using System;
using System.Collections.Generic;
using HytaleClient.Graphics;
using HytaleClient.Math;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules;

internal class DebugDisplayModule : Module
{
	public class DebugMesh
	{
		public readonly DebugShape Shape;

		public readonly Matrix Matrix;

		public float Time;

		public readonly float InitialTime;

		public readonly Vector3 DebugColor;

		public readonly bool Fade;

		public DebugMesh(DebugShape shape, Matrix matrix, float time, Vector3 debugColor, bool fade)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			Shape = shape;
			Matrix = matrix;
			InitialTime = (Time = time);
			DebugColor = debugColor;
			Fade = fade;
		}
	}

	private readonly List<DebugMesh> _debugMeshes = new List<DebugMesh>();

	private Mesh _sphereMesh;

	private Mesh _cylinderMesh;

	private Mesh _coneMesh;

	private Mesh _cubeMesh;

	public bool ShouldDraw => _debugMeshes.Count > 0;

	public DebugDisplayModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		MeshProcessor.CreateSphere(ref _sphereMesh, 5, 8, 0.5f, 0);
		MeshProcessor.CreateCylinder(ref _cylinderMesh, 8, 0.5f, 0);
		MeshProcessor.CreateCone(ref _coneMesh, 8, 0.5f, 0);
		MeshProcessor.CreateSimpleBox(ref _cubeMesh);
	}

	protected override void DoDispose()
	{
		base.DoDispose();
		_sphereMesh.Dispose();
		_cylinderMesh.Dispose();
		_coneMesh.Dispose();
		_cubeMesh.Dispose();
	}

	public void AddForce(Vector3 position, Vector3 force, Vector3 color, float time, bool fade)
	{
		float num = (float)System.Math.Atan2(force.Z, force.X);
		float radians = (float)System.Math.Atan2(System.Math.Sqrt(force.X * force.X + force.Z * force.Z), force.Y);
		Matrix baseMatrix = Matrix.CreateRotationX(radians) * Matrix.CreateRotationY(0f - num + (float)System.Math.PI / 2f) * Matrix.CreateTranslation(position);
		AddArrow(baseMatrix, color, force.Length(), time, fade);
	}

	public void AddArrow(Matrix baseMatrix, Vector3 debugColor, float length, float time, bool fade)
	{
		length -= 0.3f;
		if (length > 0f)
		{
			Matrix matrix = Matrix.CreateScale(0.1f, length, 0.1f) * Matrix.CreateTranslation(new Vector3(0f, length * 0.5f, 0f)) * baseMatrix;
			Add((DebugShape)1, matrix, time, debugColor, fade);
		}
		Matrix matrix2 = Matrix.CreateScale(0.3f, 0.3f, 0.3f) * Matrix.CreateTranslation(new Vector3(0f, length + 0.15f, 0f)) * baseMatrix;
		Add((DebugShape)2, matrix2, time, debugColor, fade);
	}

	public void Add(DebugShape shape, Matrix matrix, float time, Vector3 debugColor, bool fade = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_debugMeshes.Add(new DebugMesh(shape, matrix, time, debugColor, fade));
	}

	public void Draw(GraphicsDevice graphics, GLFunctions gl, float delta, ref Vector3 cameraPosition, ref Matrix viewProjectionMatrix)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected I4, but got Unknown
		foreach (DebugMesh debugMesh in _debugMeshes)
		{
			Matrix matrix = debugMesh.Matrix * Matrix.CreateTranslation(-cameraPosition) * viewProjectionMatrix;
			graphics.GPUProgramStore.BasicProgram.MVPMatrix.SetValue(ref matrix);
			graphics.GPUProgramStore.BasicProgram.Opacity.SetValue(debugMesh.Fade ? (0.8f * (debugMesh.Time / debugMesh.InitialTime)) : 0.8f);
			graphics.GPUProgramStore.BasicProgram.Color.SetValue(debugMesh.DebugColor);
			ref Mesh sphereMesh = ref _sphereMesh;
			DebugShape shape = debugMesh.Shape;
			DebugShape val = shape;
			sphereMesh = (int)val switch
			{
				0 => ref _sphereMesh, 
				1 => ref _cylinderMesh, 
				2 => ref _coneMesh, 
				3 => ref _cubeMesh, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			gl.BindVertexArray(sphereMesh.VertexArray);
			gl.DrawElements(GL.TRIANGLES, sphereMesh.Count, GL.UNSIGNED_SHORT, IntPtr.Zero);
			gl.PolygonMode(GL.FRONT_AND_BACK, GL.LINE);
			graphics.GPUProgramStore.BasicProgram.Opacity.SetValue(debugMesh.Fade ? (debugMesh.Time / debugMesh.InitialTime) : 1f);
			graphics.GPUProgramStore.BasicProgram.Color.SetValue(graphics.BlackColor);
			gl.DrawElements(GL.TRIANGLES, sphereMesh.Count, GL.UNSIGNED_SHORT, IntPtr.Zero);
			gl.PolygonMode(GL.FRONT_AND_BACK, GL.FILL);
			debugMesh.Time -= delta;
		}
		_debugMeshes.RemoveAll((DebugMesh s) => s.Time <= 0f);
	}
}
