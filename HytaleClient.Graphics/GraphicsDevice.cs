using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HytaleClient.Core;
using HytaleClient.Graphics.Batcher2D;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using NLog;
using SDL2;

namespace HytaleClient.Graphics;

public class GraphicsDevice : Disposable
{
	public enum GPUVendor
	{
		Intel,
		Nvidia,
		AMD,
		Other
	}

	public struct GPUInfos
	{
		public GPUVendor Vendor;

		public string Renderer;

		public string Version;
	}

	public struct GPUMemory
	{
		public int Capacity;

		public int AvailableAtStartup;

		public int AvailableNow;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private GPUMemory _videoMemory;

	private const int GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX = 36935;

	private const int GPU_MEMORY_INFO_TOTAL_AVAILABLE_MEMORY_NVX = 36936;

	private const int GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX = 36937;

	private const int GPU_MEMORY_INFO_EVICTION_COUNT_NVX = 36938;

	private const int GPU_MEMORY_INFO_EVICTED_MEMORY_NVX = 36939;

	private const int VBO_FREE_MEMORY_ATI = 34811;

	private const int TEXTURE_FREE_MEMORY_ATI = 34812;

	private const int RENDERBUFFER_FREE_MEMORY_ATI = 34813;

	public readonly GLFunctions GL;

	internal readonly GPUProgramStore GPUProgramStore;

	internal readonly RenderTargetStore RTStore;

	public readonly GLSampler SamplerLinearMipmapLinearA;

	public readonly GLSampler SamplerLinearMipmapLinearB;

	public readonly Texture WhitePixelTexture;

	public readonly byte[] TransparentPixel = new byte[4];

	public readonly Vector3 WhiteColor = Vector3.One;

	public readonly Vector3 BlackColor = Vector3.Zero;

	public readonly Vector3 RedColor = new Vector3(1f, 0f, 0f);

	public readonly Vector3 GreenColor = new Vector3(0f, 1f, 0f);

	public readonly Vector3 BlueColor = new Vector3(0f, 0f, 1f);

	public readonly Vector3 CyanColor = new Vector3(0f, 1f, 1f);

	public readonly Vector3 MagentaColor = new Vector3(1f, 0f, 1f);

	public readonly Vector3 YellowColor = new Vector3(1f, 1f, 0f);

	public Matrix ScreenMatrix = Matrix.CreateTranslation(0f, 0f, -1f) * Matrix.CreateOrthographicOffCenter(0f, 1f, 0f, 1f, 0.1f, 1000f);

	public readonly QuadRenderer ScreenQuadRenderer;

	public readonly FullscreenTriangleRenderer ScreenTriangleRenderer;

	public readonly HytaleClient.Graphics.Batcher2D.Batcher2D Batcher2D;

	public readonly Cursors Cursors;

	public const int ColorComponentSize = 8;

	public const int DepthSize = 24;

	public const int StencilSize = 8;

	public const int EnableDoubleBuffer = 1;

	public readonly int MaxTextureSize;

	public readonly int MaxUniformBlockSize;

	public readonly int MaxTextureImageUnits;

	public const int MaxDeferredLights = 1024;

	public readonly int OcclusionMapWidth = 512;

	public readonly int OcclusionMapHeight = 256;

	private bool _useReverseZ;

	private int[] _tmp = new int[4];

	public bool UseDeferredLight = true;

	public bool UseDownsampledZ = false;

	public bool UseLowResDeferredLighting = false;

	public bool UseLinearZ = false;

	public bool UseLinearZForLight = false;

	public GPUInfos GPUInfo { get; private set; }

	public bool IsGPULowEnd { get; private set; }

	public bool SupportsDrawBuffersBlend { get; private set; }

	public GPUMemory VideoMemory => _videoMemory;

	public int CpuCoreCount { get; private set; }

	public int RamSize { get; private set; }

	public bool UseReverseZ
	{
		get
		{
			return _useReverseZ;
		}
		set
		{
			if (value)
			{
				GL.DepthFunc(HytaleClient.Graphics.GL.GEQUAL);
				GL.ClearDepth(0.0);
			}
			else
			{
				GL.DepthFunc(HytaleClient.Graphics.GL.LEQUAL);
				GL.ClearDepth(1.0);
			}
			_useReverseZ = value;
		}
	}

	[DllImport("nvapi64.dll", EntryPoint = "fake")]
	private static extern int LoadNvApi64();

	[DllImport("nvapi.dll", EntryPoint = "fake")]
	private static extern int LoadNvApi32();

	public static void TryForceDedicatedNvGraphics()
	{
		try
		{
			if (Environment.Is64BitProcess)
			{
				LoadNvApi64();
			}
			else
			{
				LoadNvApi32();
			}
		}
		catch
		{
		}
	}

	public int UpdateAvailableGPUMemory()
	{
		int num = 0;
		switch (GPUInfo.Vendor)
		{
		case GPUVendor.Nvidia:
			GL.GetIntegerv((GL)36937u, _tmp);
			num = _tmp[0];
			break;
		case GPUVendor.AMD:
			GL.GetIntegerv((GL)34812u, _tmp);
			num = _tmp[0];
			break;
		}
		_videoMemory.AvailableNow = num;
		return num;
	}

	public void CheckGPUMemoryStatsAtStartup()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Invalid comparison between Unknown and I4
		StringBuilder stringBuilder = new StringBuilder();
		switch (GPUInfo.Vendor)
		{
		case GPUVendor.Nvidia:
			if ((int)SDL.SDL_GL_ExtensionSupported("GL_NVX_gpu_memory_info") != 1)
			{
				stringBuilder.AppendLine("Expected GL extension GL_NVX_gpu_memory_info is not available.");
			}
			GL.GetIntegerv((GL)36935u, _tmp);
			stringBuilder.AppendLine($"Video memory max: {_tmp[0]}");
			_videoMemory.Capacity = _tmp[0];
			GL.GetIntegerv((GL)36936u, _tmp);
			stringBuilder.AppendLine($"Video memory total available: {_tmp[0]}");
			GL.GetIntegerv((GL)36937u, _tmp);
			stringBuilder.AppendLine($"Video memory current available: {_tmp[0]}");
			_videoMemory.AvailableAtStartup = _tmp[0];
			_videoMemory.AvailableNow = _tmp[0];
			GL.GetIntegerv((GL)36938u, _tmp);
			stringBuilder.AppendLine($"Video memory eviction count: {_tmp[0]}");
			GL.GetIntegerv((GL)36939u, _tmp);
			stringBuilder.AppendLine($"Video memory evicted: {_tmp[0]}");
			break;
		case GPUVendor.AMD:
			if ((int)SDL.SDL_GL_ExtensionSupported("GL_ATI_meminfo") != 1)
			{
				stringBuilder.AppendLine("Expected GL extension GL_ATI_meminfo is not available.");
			}
			GL.GetIntegerv((GL)34812u, _tmp);
			stringBuilder.AppendLine($"Video memory current available: {_tmp[0]}");
			_videoMemory.AvailableAtStartup = _tmp[0];
			_videoMemory.AvailableNow = _tmp[0];
			break;
		}
		Logger.Info<StringBuilder>(stringBuilder);
	}

	public static void SetupGLAttributes()
	{
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)0, 8));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)1, 8));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)2, 8));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)3, 8));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)6, 24));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)7, 8));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)5, 1));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)17, 3));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)18, 3));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)21, 1));
		AssertNoSDLError(SDL.SDL_GL_SetAttribute((SDL_GLattr)20, 1));
	}

	private static void AssertNoSDLError(int result)
	{
		if (result == -1)
		{
			Exception ex = new Exception("SDL_GetError: " + SDL.SDL_GetError());
			throw ex;
		}
	}

	public GraphicsDevice(Window window, bool allowBatcher2dToGrow = false)
	{
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Invalid comparison between Unknown and I4
		CpuCoreCount = SDL.SDL_GetCPUCount();
		RamSize = SDL.SDL_GetSystemRAM();
		IntPtr intPtr = SDL.SDL_GL_CreateContext(window.Handle);
		if (intPtr == IntPtr.Zero)
		{
			throw new Exception("Could not create GL context: " + SDL.SDL_GetError());
		}
		GL = new GLFunctions();
		Mesh.InitializeGL(GL);
		MeshProcessor.InitializeGL(GL);
		RenderTarget.InitializeGL(GL);
		Uniform.InitializeGL(GL);
		UniformBufferObject.InitializeGL(GL);
		GPUTimer.InitializeGL(GL);
		GPUProgram.InitializeGL(GL);
		GPUBuffer.InitializeGL(GL);
		GPUBufferTexture.InitializeGL(GL);
		Texture.InitializeGL(GL);
		GL.Hint(HytaleClient.Graphics.GL.FRAGMENT_SHADER_DERIVATIVE_HINT, HytaleClient.Graphics.GL.FASTEST);
		string text = Marshal.PtrToStringAnsi(GL.GetString(HytaleClient.Graphics.GL.VENDOR)).ToLower();
		GPUInfos gPUInfo = default(GPUInfos);
		if (text.Contains("intel"))
		{
			gPUInfo.Vendor = GPUVendor.Intel;
		}
		else if (text.Contains("nvidia"))
		{
			gPUInfo.Vendor = GPUVendor.Nvidia;
		}
		else if (text.Contains("amd") || text.Contains("ati"))
		{
			gPUInfo.Vendor = GPUVendor.AMD;
		}
		else
		{
			gPUInfo.Vendor = GPUVendor.Other;
		}
		gPUInfo.Version = Marshal.PtrToStringAnsi(GL.GetString(HytaleClient.Graphics.GL.VERSION));
		gPUInfo.Renderer = Marshal.PtrToStringAnsi(GL.GetString(HytaleClient.Graphics.GL.RENDERER));
		GPUInfo = gPUInfo;
		CheckGPUMemoryStatsAtStartup();
		IsGPULowEnd = gPUInfo.Vendor != GPUVendor.Nvidia && gPUInfo.Vendor != GPUVendor.AMD;
		SupportsDrawBuffersBlend = (int)SDL.SDL_GL_ExtensionSupported("GL_ARB_draw_buffers_blend") == 1;
		int[] temp = new int[1];
		int num = GetFramebufferParam(HytaleClient.Graphics.GL.BACK_LEFT, HytaleClient.Graphics.GL.FRAMEBUFFER_ATTACHMENT_RED_SIZE);
		int num2 = GetFramebufferParam(HytaleClient.Graphics.GL.BACK_LEFT, HytaleClient.Graphics.GL.FRAMEBUFFER_ATTACHMENT_GREEN_SIZE);
		int num3 = GetFramebufferParam(HytaleClient.Graphics.GL.BACK_LEFT, HytaleClient.Graphics.GL.FRAMEBUFFER_ATTACHMENT_BLUE_SIZE);
		int num4 = GetFramebufferParam(HytaleClient.Graphics.GL.BACK_LEFT, HytaleClient.Graphics.GL.FRAMEBUFFER_ATTACHMENT_ALPHA_SIZE);
		int num5 = GetFramebufferParam(HytaleClient.Graphics.GL.DEPTH, HytaleClient.Graphics.GL.FRAMEBUFFER_ATTACHMENT_DEPTH_SIZE);
		int num6 = GetFramebufferParam(HytaleClient.Graphics.GL.STENCIL, HytaleClient.Graphics.GL.FRAMEBUFFER_ATTACHMENT_STENCIL_SIZE);
		GL.GetIntegerv(HytaleClient.Graphics.GL.DOUBLEBUFFER, temp);
		int num7 = temp[0];
		if (num != 8)
		{
			Logger.Warn<int, int>("SDL_GL_RED_SIZE should be {0} but is {1}", 8, num);
		}
		if (num2 != 8)
		{
			Logger.Warn<int, int>("SDL_GL_GREEN_SIZE should be {0} but is {1}", 8, num2);
		}
		if (num3 != 8)
		{
			Logger.Warn<int, int>("SDL_GL_BLUE_SIZE should be {0} but is {1}", 8, num3);
		}
		if (num4 != 8)
		{
			Logger.Warn<int, int>("SDL_GL_ALPHA_SIZE should be {0} but is {1}", 8, num4);
		}
		if (num5 != 24)
		{
			Logger.Warn<int, int>("SDL_GL_DEPTH_SIZE should be {0} but is {1}", 24, num5);
		}
		if (num6 != 8)
		{
			Logger.Warn<int, int>("SDL_GL_STENCIL_SIZE should be {0} but is {1}", 8, num6);
		}
		if (num7 != 1)
		{
			Logger.Warn<int, int>("SDL_GL_DOUBLEBUFFER should be {0} but is {1}", 1, num7);
		}
		GL.GetIntegerv(HytaleClient.Graphics.GL.MAX_VERTEX_UNIFORM_COMPONENTS, temp);
		if (temp[0] < 1024)
		{
			throw new Exception($"Hardware not supported, MAX_VERTEX_UNIFORM_COMPONENTS is {temp[0]} but should be at least {1024}");
		}
		GL.GetIntegerv(HytaleClient.Graphics.GL.MAX_UNIFORM_BLOCK_SIZE, temp);
		if (temp[0] < 16384)
		{
			throw new Exception($"Hardware not supported, MAX_UNIFORM_BLOCK_SIZE is {temp[0]} but should be at least {16384}");
		}
		MaxUniformBlockSize = temp[0];
		GL.GetIntegerv(HytaleClient.Graphics.GL.MAX_TEXTURE_IMAGE_UNITS, temp);
		if (temp[0] < 16)
		{
			throw new Exception($"Hardware not supported, MAX_TEXTURE_IMAGE_UNITS is {temp[0]} but should be at least {16}");
		}
		MaxTextureImageUnits = temp[0];
		GL.GetIntegerv(HytaleClient.Graphics.GL.MAX_TEXTURE_SIZE, temp);
		if (temp[0] < 8192)
		{
			throw new Exception($"Hardware not supported, MAX_TEXTURE_SIZE is {temp[0]} but should be at least {8192}");
		}
		MaxTextureSize = temp[0];
		UseReverseZ = false;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("");
		stringBuilder.AppendLine("System informations");
		stringBuilder.AppendLine($" CPU core count: {CpuCoreCount}");
		stringBuilder.AppendLine($" RAM size: {RamSize} MB");
		stringBuilder.AppendLine(" OpenGL driver: " + gPUInfo.Version);
		stringBuilder.AppendLine(" GPU Renderer: " + gPUInfo.Renderer);
		stringBuilder.AppendLine($" GPU Memory Total: {_videoMemory.Capacity / 1024} MB");
		stringBuilder.AppendLine($" GPU Memory Available: {_videoMemory.AvailableAtStartup / 1024} MB");
		Logger.Info<StringBuilder>(stringBuilder);
		GL.ClearStencil(1);
		GPUProgramStore = new GPUProgramStore(this);
		int width = window.Viewport.Width;
		int height = window.Viewport.Height;
		RTStore = new RenderTargetStore(this, width, height, new Vector2(2048f, 2048f), CascadedShadowMapping.DefaultDeferredShadowResolutionScale, SceneRenderer.DefaultSSAOResolutionScale);
		SamplerLinearMipmapLinearA = GL.GenSampler();
		GL.SamplerParameteri(SamplerLinearMipmapLinearA, HytaleClient.Graphics.GL.TEXTURE_WRAP_S, HytaleClient.Graphics.GL.CLAMP_TO_EDGE);
		GL.SamplerParameteri(SamplerLinearMipmapLinearA, HytaleClient.Graphics.GL.TEXTURE_WRAP_T, HytaleClient.Graphics.GL.CLAMP_TO_EDGE);
		GL.SamplerParameteri(SamplerLinearMipmapLinearA, HytaleClient.Graphics.GL.TEXTURE_MIN_FILTER, HytaleClient.Graphics.GL.LINEAR_MIPMAP_LINEAR);
		GL.SamplerParameteri(SamplerLinearMipmapLinearA, HytaleClient.Graphics.GL.TEXTURE_MAG_FILTER, HytaleClient.Graphics.GL.LINEAR);
		SamplerLinearMipmapLinearB = GL.GenSampler();
		GL.SamplerParameteri(SamplerLinearMipmapLinearB, HytaleClient.Graphics.GL.TEXTURE_WRAP_S, HytaleClient.Graphics.GL.CLAMP_TO_EDGE);
		GL.SamplerParameteri(SamplerLinearMipmapLinearB, HytaleClient.Graphics.GL.TEXTURE_WRAP_T, HytaleClient.Graphics.GL.CLAMP_TO_EDGE);
		GL.SamplerParameteri(SamplerLinearMipmapLinearB, HytaleClient.Graphics.GL.TEXTURE_MIN_FILTER, HytaleClient.Graphics.GL.LINEAR_MIPMAP_LINEAR);
		GL.SamplerParameteri(SamplerLinearMipmapLinearB, HytaleClient.Graphics.GL.TEXTURE_MAG_FILTER, HytaleClient.Graphics.GL.LINEAR);
		WhitePixelTexture = new Texture(Texture.TextureTypes.Texture2D);
		WhitePixelTexture.CreateTexture2D(1, 1, new byte[4] { 255, 255, 255, 255 });
		ScreenQuadRenderer = new QuadRenderer(this, GPUProgramStore.BasicProgram.AttribPosition, GPUProgramStore.BasicProgram.AttribTexCoords);
		ScreenQuadRenderer.UpdateUVs(1f, 0f, 0f, 1f);
		ScreenTriangleRenderer = new FullscreenTriangleRenderer(this);
		Batcher2D = new HytaleClient.Graphics.Batcher2D.Batcher2D(this, allowBatcher2dToGrow);
		Cursors = new Cursors();
		int GetFramebufferParam(GL attachment, GL param)
		{
			GL.GetFramebufferAttachmentParameteriv(HytaleClient.Graphics.GL.DRAW_FRAMEBUFFER, attachment, param, temp);
			return temp[0];
		}
	}

	protected override void DoDispose()
	{
		GPUProgramStore.Release();
		GL.DeleteSampler(SamplerLinearMipmapLinearB);
		GL.DeleteSampler(SamplerLinearMipmapLinearA);
		RTStore.Dispose();
		Cursors.Dispose();
		Batcher2D.Dispose();
		ScreenTriangleRenderer.Dispose();
		ScreenQuadRenderer.Dispose();
		WhitePixelTexture.Dispose();
		Texture.ReleaseGL();
		GPUBufferTexture.ReleaseGL();
		GPUBuffer.ReleaseGL();
		GPUProgram.ReleaseGL();
		GPUTimer.ReleaseGL();
		UniformBufferObject.ReleaseGL();
		Uniform.ReleaseGL();
		RenderTarget.ReleaseGL();
		MeshProcessor.ReleaseGL();
		Mesh.ReleaseGL();
	}

	public void CreatePerspectiveMatrix(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (UseReverseZ)
		{
			Matrix.CreatePerspectiveFieldOfViewReverseZ(fieldOfView, aspectRatio, nearPlaneDistance, out result);
		}
		else
		{
			Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneDistance, farPlaneDistance, out result);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SaveColorMask()
	{
		GL.GetIntegerv(HytaleClient.Graphics.GL.COLOR_WRITEMASK, _tmp);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RestoreColorMask()
	{
		GL.ColorMask(_tmp[0] == 1, _tmp[1] == 1, _tmp[2] == 1, _tmp[3] == 1);
	}

	public void SetVSyncEnabled(bool enabled)
	{
		if (enabled)
		{
			SDL.SDL_GL_SetSwapInterval(1);
		}
		else
		{
			SDL.SDL_GL_SetSwapInterval(0);
		}
	}
}
