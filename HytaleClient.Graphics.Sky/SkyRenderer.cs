#define DEBUG
using System;
using System.Diagnostics;
using System.Threading;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics.Gizmos.Models;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Graphics.Sky;

internal class SkyRenderer : Disposable
{
	private readonly Vector3 CloudsSphereOffset = new Vector3(0f, -500f, 0f);

	private string[] _currentCloudsTextureChecksums = new string[4];

	public readonly GLTexture[] CloudsTextures = new GLTexture[4];

	public readonly Vector4[] CloudColors = new Vector4[4];

	public readonly float[] CloudOffsets = new float[4];

	private Matrix _cloudsMVPMatrix;

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const int SphereSegments = 20;

	private const int SphereRadius = 1000;

	private readonly Engine _engine;

	private readonly GLVertexArray _skyVertexArray;

	private readonly GLVertexArray _cloudsVertexArray;

	private readonly GLBuffer _sphereVerticesBuffer;

	private readonly GLBuffer _sphereIndicesBuffer;

	private int _sphereIndicesCount;

	public readonly GLTexture StarsTexture;

	private string _currentStarsTextureChecksum;

	private Matrix _tempMatrix;

	private Matrix _skyMVPMatrix;

	private GLVertexArray _horizonVertexArray;

	private GLBuffer _horizonVerticesBuffer;

	private GLBuffer _horizonIndicesBuffer;

	private int _horizonIndicesCount;

	private Matrix _horizonMVPMatrix;

	private const int SunSize = 102;

	private const int MoonSize = 102;

	public Matrix SunMVPMatrix;

	private Matrix _moonMVPMatrix;

	private readonly QuadRenderer _sunOrMoonRenderer;

	private bool _isSunTextureLoaded = false;

	private string _currentMoonTextureChecksum;

	public bool IsCloudsTextureLoading { get; private set; }

	public int CloudsTexturesCount { get; private set; }

	public bool IsStarsTextureLoading { get; private set; }

	public GLTexture SunTexture { get; private set; }

	public GLTexture MoonTexture { get; private set; }

	public bool IsMoonTextureLoading { get; private set; }

	private void InitializeClouds()
	{
		GLFunctions gL = _engine.Graphics.GL;
		for (int i = 0; i < 4; i++)
		{
			CloudsTextures[i] = gL.GenTexture();
			gL.BindTexture(GL.TEXTURE_2D, CloudsTextures[i]);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9985);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9729);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 10497);
			gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 10497);
		}
	}

	private void DisposeClouds()
	{
		GLFunctions gL = _engine.Graphics.GL;
		for (int i = 0; i < 4; i++)
		{
			gL.DeleteTexture(CloudsTextures[i]);
		}
	}

	public unsafe void RequestCloudsTextureUpdate(string[] targetCloudsChecksums, bool forceUpdate = false)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		GLFunctions gl = _engine.Graphics.GL;
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			_currentCloudsTextureChecksums[i] = targetCloudsChecksums[i];
			if (targetCloudsChecksums[i] != null)
			{
				num++;
			}
		}
		CloudsTexturesCount = num;
		if (num == 0 && !forceUpdate)
		{
			IsCloudsTextureLoading = false;
			return;
		}
		IsCloudsTextureLoading = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			Image[] cloudsImages = new Image[4];
			for (int j = 0; j < 4; j++)
			{
				if (targetCloudsChecksums[j] != null)
				{
					try
					{
						cloudsImages[j] = new Image(AssetManager.GetAssetUsingHash(targetCloudsChecksums[j]));
					}
					catch (Exception ex)
					{
						cloudsImages[j] = null;
						Logger.Error(ex, "Failed to load cloud texture: " + AssetManager.GetAssetLocalPathUsingHash(targetCloudsChecksums[j]));
					}
				}
			}
			_engine.RunOnMainThread(_engine, delegate
			{
				for (int k = 0; k < 4; k++)
				{
					if (targetCloudsChecksums[k] != _currentCloudsTextureChecksums[k])
					{
						return;
					}
				}
				for (int l = 0; l < 4; l++)
				{
					if (cloudsImages[l] != null)
					{
						gl.BindTexture(GL.TEXTURE_2D, CloudsTextures[l]);
						fixed (byte* ptr = cloudsImages[l].Pixels)
						{
							gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, cloudsImages[l].Width, cloudsImages[l].Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
						}
						gl.GenerateMipmap(GL.TEXTURE_2D);
					}
				}
				IsCloudsTextureLoading = false;
			});
		});
	}

	public void PrepareCloudsForDraw(ref Matrix viewRotationProjectionMatrix, ref Quaternion rotation)
	{
		Matrix.CreateFromQuaternion(ref rotation, out _tempMatrix);
		Matrix.CreateScale(1000f, out _cloudsMVPMatrix);
		Matrix.Multiply(ref _tempMatrix, ref _cloudsMVPMatrix, out _cloudsMVPMatrix);
		Matrix.AddTranslation(ref _cloudsMVPMatrix, CloudsSphereOffset.X, CloudsSphereOffset.Y, CloudsSphereOffset.Z);
		Matrix.Multiply(ref _cloudsMVPMatrix, ref viewRotationProjectionMatrix, out _cloudsMVPMatrix);
	}

	public void DrawClouds()
	{
		if (!IsCloudsTextureLoading)
		{
			CloudsProgram cloudsProgram = _engine.Graphics.GPUProgramStore.CloudsProgram;
			cloudsProgram.AssertInUse();
			cloudsProgram.MVPMatrix.SetValue(ref _cloudsMVPMatrix);
			GLFunctions gL = _engine.Graphics.GL;
			gL.BindVertexArray(_cloudsVertexArray);
			gL.DrawElements(GL.TRIANGLES, _sphereIndicesCount, GL.UNSIGNED_SHORT, (IntPtr)0);
		}
	}

	public SkyRenderer(Engine engine)
	{
		_engine = engine;
		GLFunctions gL = _engine.Graphics.GL;
		_sphereVerticesBuffer = gL.GenBuffer();
		_sphereIndicesBuffer = gL.GenBuffer();
		gL.BindVertexArray(GLVertexArray.None);
		gL.BindBuffer(GLVertexArray.None, GL.ARRAY_BUFFER, _sphereVerticesBuffer);
		gL.BindBuffer(GLVertexArray.None, GL.ELEMENT_ARRAY_BUFFER, _sphereIndicesBuffer);
		CreateSphereGeometry();
		StarsTexture = gL.GenTexture();
		_skyVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_skyVertexArray);
		gL.BindBuffer(_skyVertexArray, GL.ARRAY_BUFFER, _sphereVerticesBuffer);
		gL.BindBuffer(_skyVertexArray, GL.ELEMENT_ARRAY_BUFFER, _sphereIndicesBuffer);
		SkyProgram skyProgram = _engine.Graphics.GPUProgramStore.SkyProgram;
		gL.EnableVertexAttribArray(skyProgram.AttribPosition.Index);
		gL.VertexAttribPointer(skyProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, SkyAndCloudsVertex.Size, IntPtr.Zero);
		gL.EnableVertexAttribArray(skyProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(skyProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, SkyAndCloudsVertex.Size, (IntPtr)12);
		SunTexture = gL.GenTexture();
		MoonTexture = gL.GenTexture();
		_sunOrMoonRenderer = new QuadRenderer(_engine.Graphics, _engine.Graphics.GPUProgramStore.BasicProgram.AttribPosition, _engine.Graphics.GPUProgramStore.BasicProgram.AttribTexCoords);
		_cloudsVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_cloudsVertexArray);
		gL.BindBuffer(_cloudsVertexArray, GL.ARRAY_BUFFER, _sphereVerticesBuffer);
		gL.BindBuffer(_cloudsVertexArray, GL.ELEMENT_ARRAY_BUFFER, _sphereIndicesBuffer);
		CloudsProgram cloudsProgram = _engine.Graphics.GPUProgramStore.CloudsProgram;
		gL.EnableVertexAttribArray(cloudsProgram.AttribPosition.Index);
		gL.VertexAttribPointer(cloudsProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, SkyAndCloudsVertex.Size, IntPtr.Zero);
		gL.EnableVertexAttribArray(cloudsProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(cloudsProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, SkyAndCloudsVertex.Size, (IntPtr)12);
	}

	protected override void DoDispose()
	{
		GLFunctions gL = _engine.Graphics.GL;
		gL.DeleteTexture(StarsTexture);
		gL.DeleteBuffer(_sphereVerticesBuffer);
		gL.DeleteBuffer(_sphereIndicesBuffer);
		gL.DeleteVertexArray(_skyVertexArray);
		gL.DeleteVertexArray(_cloudsVertexArray);
		DisposeSunAndMoon();
		DisposeClouds();
		DisposeHorizon();
	}

	public void Initialize()
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		GLFunctions gL = _engine.Graphics.GL;
		gL.BindTexture(GL.TEXTURE_2D, StarsTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9729);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9729);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 10497);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 10497);
		InitializeSunAndMoon();
		InitializeClouds();
		InitializeHorizon();
	}

	public unsafe void RequestStarsTextureUpdate(string targetStarsTextureChecksum, bool forceUpdate = false)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!forceUpdate && targetStarsTextureChecksum == _currentStarsTextureChecksum)
		{
			return;
		}
		_currentStarsTextureChecksum = targetStarsTextureChecksum;
		GLFunctions gl = _engine.Graphics.GL;
		if (targetStarsTextureChecksum == null)
		{
			IsStarsTextureLoading = false;
			gl.BindTexture(GL.TEXTURE_2D, StarsTexture);
			fixed (byte* ptr = _engine.Graphics.TransparentPixel)
			{
				gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, 1, 1, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
			}
			return;
		}
		IsStarsTextureLoading = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			Image starsImage = null;
			try
			{
				starsImage = new Image(AssetManager.GetAssetUsingHash(targetStarsTextureChecksum));
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load star texture: " + AssetManager.GetAssetLocalPathUsingHash(targetStarsTextureChecksum));
			}
			_engine.RunOnMainThread(_engine, delegate
			{
				if (starsImage == null)
				{
					IsStarsTextureLoading = false;
				}
				else if (!(targetStarsTextureChecksum != _currentStarsTextureChecksum))
				{
					gl.BindTexture(GL.TEXTURE_2D, StarsTexture);
					fixed (byte* ptr2 = starsImage.Pixels)
					{
						gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, starsImage.Width, starsImage.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr2);
					}
					IsStarsTextureLoading = false;
				}
			});
		});
	}

	private unsafe void CreateSphereGeometry()
	{
		SkyAndCloudsVertex[] array = new SkyAndCloudsVertex[400];
		for (int i = 0; i < 20; i++)
		{
			float num = (float)i / 19f * (float)System.Math.PI;
			float y = (float)i / 19f;
			for (int j = 0; j < 20; j++)
			{
				int num2 = i * 20 + j;
				float num3 = (float)j / 19f * ((float)System.Math.PI * 2f);
				array[num2].Position = new Vector3((float)System.Math.Cos(num), (float)System.Math.Sin(num) * (float)System.Math.Sin(num3), (float)System.Math.Sin(num) * (float)System.Math.Cos(num3));
				float x = (float)j / 19f;
				array[num2].TextureCoordinates = new Vector2(x, y);
			}
		}
		int num4 = 19;
		_sphereIndicesCount = num4 * num4 * 6;
		ushort[] array2 = new ushort[_sphereIndicesCount];
		for (int k = 0; k < num4; k++)
		{
			for (int l = 0; l < num4; l++)
			{
				ushort num5 = (ushort)(k * 20 + l);
				ushort num6 = (ushort)(k * 20 + l + 1);
				ushort num7 = (ushort)((k + 1) * 20 + l + 1);
				ushort num8 = (ushort)((k + 1) * 20 + l);
				int num9 = (k * num4 + l) * 6;
				array2[num9] = num5;
				array2[num9 + 1] = num7;
				array2[num9 + 2] = num6;
				array2[num9 + 3] = num5;
				array2[num9 + 4] = num8;
				array2[num9 + 5] = num7;
			}
		}
		GLFunctions gL = _engine.Graphics.GL;
		fixed (SkyAndCloudsVertex* ptr = array)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(array.Length * SkyAndCloudsVertex.Size), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		fixed (ushort* ptr2 = array2)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_sphereIndicesCount * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
	}

	public void PrepareSkyForDraw(ref Matrix viewRotationProjectionMatrix)
	{
		Matrix.CreateScale(1000f, out _skyMVPMatrix);
		Matrix.Multiply(ref _skyMVPMatrix, ref viewRotationProjectionMatrix, out _skyMVPMatrix);
	}

	public void DrawSky()
	{
		SkyProgram skyProgram = _engine.Graphics.GPUProgramStore.SkyProgram;
		skyProgram.AssertInUse();
		_engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, StarsTexture);
		skyProgram.MVPMatrix.SetValue(ref _skyMVPMatrix);
		GLFunctions gL = _engine.Graphics.GL;
		gL.BindVertexArray(_skyVertexArray);
		gL.DrawElements(GL.TRIANGLES, _sphereIndicesCount, GL.UNSIGNED_SHORT, (IntPtr)0);
	}

	private unsafe void InitializeHorizon()
	{
		GLFunctions gL = _engine.Graphics.GL;
		PrimitiveModelData primitiveModelData = CylinderModel.BuildHollowModelData(1f, 1f, 8);
		_horizonVertexArray = gL.GenVertexArray();
		gL.BindVertexArray(_horizonVertexArray);
		_horizonVerticesBuffer = gL.GenBuffer();
		gL.BindBuffer(_horizonVertexArray, GL.ARRAY_BUFFER, _horizonVerticesBuffer);
		fixed (float* ptr = primitiveModelData.Vertices)
		{
			gL.BufferData(GL.ARRAY_BUFFER, (IntPtr)(primitiveModelData.Vertices.Length * 4), (IntPtr)ptr, GL.STATIC_DRAW);
		}
		_horizonIndicesBuffer = gL.GenBuffer();
		_horizonIndicesCount = primitiveModelData.Indices.Length;
		gL.BindBuffer(_horizonVertexArray, GL.ELEMENT_ARRAY_BUFFER, _horizonIndicesBuffer);
		fixed (ushort* ptr2 = primitiveModelData.Indices)
		{
			gL.BufferData(GL.ELEMENT_ARRAY_BUFFER, (IntPtr)(_horizonIndicesCount * 2), (IntPtr)ptr2, GL.STATIC_DRAW);
		}
		BasicProgram basicProgram = _engine.Graphics.GPUProgramStore.BasicProgram;
		gL.EnableVertexAttribArray(basicProgram.AttribPosition.Index);
		gL.VertexAttribPointer(basicProgram.AttribPosition.Index, 3, GL.FLOAT, normalized: false, 32, IntPtr.Zero);
		gL.EnableVertexAttribArray(basicProgram.AttribTexCoords.Index);
		gL.VertexAttribPointer(basicProgram.AttribTexCoords.Index, 2, GL.FLOAT, normalized: false, 32, (IntPtr)12);
	}

	private void DisposeHorizon()
	{
		GLFunctions gL = _engine.Graphics.GL;
		gL.DeleteBuffer(_horizonVerticesBuffer);
		gL.DeleteBuffer(_horizonIndicesBuffer);
		gL.DeleteVertexArray(_horizonVertexArray);
	}

	public void PrepareHorizonForDraw(ref Matrix viewRotationProjectionMatrix, Vector3 horizonPosition, Vector3 horizonScale)
	{
		Matrix.CreateScale(horizonScale.X, horizonScale.Y, horizonScale.Z, out _horizonMVPMatrix);
		Matrix.AddTranslation(ref _horizonMVPMatrix, horizonPosition.X, horizonPosition.Y, horizonPosition.Z);
		Matrix.Multiply(ref _horizonMVPMatrix, ref viewRotationProjectionMatrix, out _horizonMVPMatrix);
	}

	public void DrawHorizon()
	{
		BasicProgram basicProgram = _engine.Graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Opacity.AssertValue(1f);
		_engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, _engine.Graphics.WhitePixelTexture.GLTexture);
		basicProgram.MVPMatrix.SetValue(ref _horizonMVPMatrix);
		GLFunctions gL = _engine.Graphics.GL;
		gL.BindVertexArray(_horizonVertexArray);
		gL.DrawElements(GL.TRIANGLES, _horizonIndicesCount, GL.UNSIGNED_SHORT, (IntPtr)0);
	}

	private void InitializeSunAndMoon()
	{
		GLFunctions gL = _engine.Graphics.GL;
		gL.BindTexture(GL.TEXTURE_2D, SunTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
		gL.BindTexture(GL.TEXTURE_2D, MoonTexture);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, 9728);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, 33071);
		gL.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, 33071);
	}

	public unsafe void LoadSunTexture(string sunChecksum)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				Image sunImage = new Image(AssetManager.GetAssetUsingHash(sunChecksum));
				_engine.RunOnMainThread(_engine, delegate
				{
					GLFunctions gL = _engine.Graphics.GL;
					gL.BindTexture(GL.TEXTURE_2D, SunTexture);
					fixed (byte* ptr = sunImage.Pixels)
					{
						gL.TexImage2D(GL.TEXTURE_2D, 0, 6408, sunImage.Width, sunImage.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
					}
					_isSunTextureLoaded = true;
				});
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load sun texture: " + AssetManager.GetAssetLocalPathUsingHash(sunChecksum));
			}
		});
	}

	public unsafe void RequestMoonTextureUpdate(string targetMoonTextureChecksum, bool forceUpdate = false)
	{
		Debug.Assert(ThreadHelper.IsMainThread());
		if (!forceUpdate && targetMoonTextureChecksum == _currentMoonTextureChecksum)
		{
			return;
		}
		_currentMoonTextureChecksum = targetMoonTextureChecksum;
		GLFunctions gl = _engine.Graphics.GL;
		if (targetMoonTextureChecksum == null)
		{
			IsMoonTextureLoading = false;
			gl.BindTexture(GL.TEXTURE_2D, MoonTexture);
			fixed (byte* ptr = _engine.Graphics.TransparentPixel)
			{
				gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, 1, 1, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr);
			}
			return;
		}
		IsMoonTextureLoading = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			Image moonImage = null;
			try
			{
				moonImage = new Image(AssetManager.GetAssetUsingHash(targetMoonTextureChecksum));
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to load moon texture: " + AssetManager.GetAssetLocalPathUsingHash(targetMoonTextureChecksum));
			}
			_engine.RunOnMainThread(_engine, delegate
			{
				if (moonImage == null)
				{
					IsMoonTextureLoading = false;
				}
				else if (!(targetMoonTextureChecksum != _currentMoonTextureChecksum))
				{
					gl.BindTexture(GL.TEXTURE_2D, MoonTexture);
					fixed (byte* ptr2 = moonImage.Pixels)
					{
						gl.TexImage2D(GL.TEXTURE_2D, 0, 6408, moonImage.Width, moonImage.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, (IntPtr)ptr2);
					}
					IsMoonTextureLoading = false;
				}
			});
		});
	}

	private void DisposeSunAndMoon()
	{
		GLFunctions gL = _engine.Graphics.GL;
		gL.DeleteTexture(SunTexture);
		gL.DeleteTexture(MoonTexture);
		_sunOrMoonRenderer.Dispose();
	}

	public bool SunNeedsDrawing(Vector3 normalizedSunPosition, Vector3 cameraDirection, float sunScale)
	{
		return _isSunTextureLoaded && normalizedSunPosition.Y > -0.3f && Vector3.Dot(cameraDirection, normalizedSunPosition) > 0f - sunScale * 0.4f;
	}

	public void PrepareSunForDraw(ref Matrix viewRotationMatrix, ref Matrix projectionMatrix, Vector3 normalizedSunPosition, float sunScale)
	{
		Matrix.CreateTranslation(-0.5f, -0.5f, 0f, out SunMVPMatrix);
		Matrix.CreateScale(102f * sunScale, out _tempMatrix);
		Matrix.Multiply(ref SunMVPMatrix, ref _tempMatrix, out SunMVPMatrix);
		Vector3 vector = Vector3.Transform(normalizedSunPosition * 1000f, viewRotationMatrix);
		SunMVPMatrix.M41 += vector.X;
		SunMVPMatrix.M42 += vector.Y;
		SunMVPMatrix.M43 += vector.Z;
		Matrix.Multiply(ref SunMVPMatrix, ref projectionMatrix, out SunMVPMatrix);
	}

	public void DrawSun()
	{
		BasicProgram basicProgram = _engine.Graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		basicProgram.Opacity.AssertValue(1f);
		_engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, SunTexture);
		basicProgram.MVPMatrix.SetValue(ref SunMVPMatrix);
		_sunOrMoonRenderer.Draw();
	}

	public bool BackgroundSkyNeedDrawing(Vector3 normalizedSunPosition)
	{
		return normalizedSunPosition.Y > -0.85f;
	}

	public bool StarsNeedDrawing(Vector3 normalizedSunPosition)
	{
		return 0f - normalizedSunPosition.Y > -0.3f;
	}

	public bool MoonNeedsDrawing(Vector3 normalizedSunPosition, Vector3 cameraDirection, float moonScale)
	{
		return 0f - normalizedSunPosition.Y > -0.3f && !IsMoonTextureLoading && Vector3.Dot(cameraDirection, -normalizedSunPosition) > 0f - moonScale * 0.4f;
	}

	public void PrepareMoonForDraw(ref Matrix viewRotationMatrix, ref Matrix projectionMatrix, Vector3 normalizedSunPosition, float moonScale)
	{
		Matrix.CreateTranslation(-0.5f, -0.5f, 0f, out _moonMVPMatrix);
		Matrix.CreateScale(102f * moonScale, out _tempMatrix);
		Matrix.Multiply(ref _moonMVPMatrix, ref _tempMatrix, out _moonMVPMatrix);
		Vector3 vector = Vector3.Transform(-normalizedSunPosition * 1000f, viewRotationMatrix);
		_moonMVPMatrix.M41 += vector.X;
		_moonMVPMatrix.M42 += vector.Y;
		_moonMVPMatrix.M43 += vector.Z;
		Matrix.Multiply(ref _moonMVPMatrix, ref projectionMatrix, out _moonMVPMatrix);
	}

	public void DrawMoon()
	{
		BasicProgram basicProgram = _engine.Graphics.GPUProgramStore.BasicProgram;
		basicProgram.AssertInUse();
		_engine.Graphics.GL.AssertTextureBound(GL.TEXTURE0, MoonTexture);
		basicProgram.MVPMatrix.SetValue(ref _moonMVPMatrix);
		_sunOrMoonRenderer.Draw();
	}
}
