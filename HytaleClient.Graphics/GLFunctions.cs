#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HytaleClient.Graphics.Programs;
using HytaleClient.Math;
using NLog;
using SDL2;

namespace HytaleClient.Graphics;

public class GLFunctions
{
	[AttributeUsage(AttributeTargets.Field)]
	public class OptionalAttribute : Attribute
	{
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public int DrawCallsCount = 0;

	public int DrawnVertices = 0;

	private static glDebugProc DebugCallbackDelegate = DebugCallback;

	public readonly glGetError GetError;

	[Optional]
	public readonly glDebugMessageCallback DebugMessageCallbackARB;

	[Optional]
	public readonly glDebugMessageControl DebugMessageControlARB;

	public readonly glHint Hint;

	public readonly glGenQueries Internal_GenQueries;

	public readonly glDeleteQueries Internal_DeleteQueries;

	public readonly glQueryCounter Internal_QueryCounter;

	public readonly glBeginQuery Internal_BeginQuery;

	public readonly glEndQuery Internal_EndQuery;

	public readonly glGetQueryObjectuiv Internal_GetQueryObjectuiv;

	public readonly glGetQueryObjectui64v Internal_glGetQueryObjectui64v;

	public readonly glGetIntegerv GetIntegerv;

	public readonly glGetFloatv GetFloatv;

	public readonly glGetFramebufferAttachmentParameteriv GetFramebufferAttachmentParameteriv;

	public readonly glTransformFeedbackVaryings TransformFeedbackVaryings;

	public readonly glBeginTransformFeedback BeginTransformFeedback;

	public readonly glEndTransformFeedback EndTransformFeedback;

	public readonly glEnable Internal_Enable;

	public readonly glDisable Internal_Disable;

	public readonly glDepthFunc Internal_DepthFunc;

	public readonly glDepthMask Internal_DepthMask;

	public readonly glClearDepth ClearDepth;

	public readonly glColorMask ColorMask;

	public readonly glStencilFunc Internal_StencilFunc;

	public readonly glStencilOp Internal_StencilOp;

	public readonly glStencilFuncSeparate StencilFuncSeparate;

	public readonly glStencilOpSeparate StencilOpSeparate;

	public readonly glStencilMask StencilMask;

	public readonly glClearStencil ClearStencil;

	public readonly glBlendFunc Internal_BlendFunc;

	public readonly glBlendFuncSeparate Internal_BlendFuncSeparate;

	public readonly glBlendFunci BlendFunci;

	public readonly glBlendFuncSeparatei BlendFuncSeparatei;

	public readonly glBlendEquation Internal_BlendEquation;

	public readonly glBlendEquationSeparate Internal_BlendEquationSeparate;

	public readonly glCullFace Internal_CullFace;

	public readonly glPointSize PointSize;

	public readonly glClearColor ClearColor;

	public readonly glClear Clear;

	public readonly glClearBufferfv ClearBufferfv;

	public readonly glViewport Internal_Viewport;

	public readonly glPolygonMode PolygonMode;

	public readonly glPolygonOffset PolygonOffset;

	public readonly glScissor Scissor;

	public readonly glActiveTexture Internal_ActiveTexture;

	public readonly glGenTextures Internal_GenTextures;

	public readonly glDeleteTextures Internal_DeleteTextures;

	public readonly glBindTexture Internal_BindTexture;

	public readonly glTexBuffer TexBuffer;

	public readonly glTexParameteri TexParameteri;

	public readonly glTexParameterf TexParameterf;

	public readonly glTexParameterfv TexParameterfv;

	public readonly glTexImage1D TexImage1D;

	public readonly glTexImage2D TexImage2D;

	public readonly glTexImage3D TexImage3D;

	public readonly glGenerateMipmap GenerateMipmap;

	public readonly glTexSubImage2D TexSubImage2D;

	public readonly glTexSubImage3D TexSubImage3D;

	public readonly glCopyTexSubImage2D CopyTexSubImage2D;

	public readonly glGetTexImage GetTexImage;

	public readonly glTexImage2DMultisample TexImage2DMultisample;

	public readonly glGenSamplers Internal_GenSamplers;

	public readonly glDeleteSamplers Internal_DeleteSamplers;

	public readonly glBindSampler Internal_BindSampler;

	public readonly glSamplerParameteri Internal_SamplerParameteri;

	public readonly glGenFramebuffers Internal_GenFramebuffers;

	public readonly glDeleteFramebuffers Internal_DeleteFramebuffers;

	public readonly glBindFramebuffer Internal_BindFramebuffer;

	public readonly glFramebufferTexture2D Internal_FramebufferTexture2D;

	public readonly glCheckFramebufferStatus CheckFramebufferStatus;

	public readonly glDrawBuffer DrawBuffer;

	public readonly glDrawBuffers DrawBuffers;

	public readonly glBlitFramebuffer BlitFramebuffer;

	public readonly glGenVertexArrays Internal_GenVertexArrays;

	public readonly glDeleteVertexArrays Internal_DeleteVertexArrays;

	public readonly glBindVertexArray Internal_BindVertexArray;

	public readonly glGenBuffers Internal_GenBuffers;

	public readonly glDeleteBuffers Internal_DeleteBuffers;

	public readonly glBindBuffer Internal_BindBuffer;

	public readonly glBindBufferBase BindBufferBase;

	public readonly glBindBufferRange BindBufferRange;

	public readonly glBufferData BufferData;

	public readonly glBufferSubData BufferSubData;

	public readonly glGetBufferSubData GetBufferSubData;

	public readonly glMapBufferRange MapBufferRange;

	public readonly glUnmapBuffer UnmapBuffer;

	public readonly glCreateShader CreateShader;

	public readonly glShaderSource ShaderSource;

	public readonly glCompileShader CompileShader;

	public readonly glDeleteShader DeleteShader;

	public readonly glGetShaderiv GetShaderiv;

	public readonly glGetShaderInfoLog GetShaderInfoLog;

	public readonly glCreateProgram CreateProgram;

	public readonly glDeleteProgram DeleteProgram;

	public readonly glAttachShader AttachShader;

	public readonly glDetachShader DetachShader;

	public readonly glBindAttribLocation BindAttribLocation;

	public readonly glLinkProgram LinkProgram;

	public readonly glUseProgram Internal_UseProgram;

	public readonly glGetProgramiv GetProgramiv;

	public readonly glGetProgramInfoLog GetProgramInfoLog;

	public readonly glGetUniformBlockIndex GetUniformBlockIndex;

	public readonly glUniformBlockBinding UniformBlockBinding;

	public readonly glGetUniformLocation GetUniformLocation;

	public readonly glUniform1i Uniform1i;

	public readonly glUniform1iv Uniform1iv;

	public readonly glUniform2i Uniform2i;

	public readonly glUniform3i Uniform3i;

	public readonly glUniform4i Uniform4i;

	public readonly glUniform1f Uniform1f;

	public readonly glUniform2f Uniform2f;

	public readonly glUniform3f Uniform3f;

	public readonly glUniform4f Uniform4f;

	public readonly glUniform1fv Uniform1fv;

	public readonly glUniform2fv Uniform2fv;

	public readonly glUniform3fv Uniform3fv;

	public readonly glUniform4fv Uniform4fv;

	public readonly glUniformMatrix4fv UniformMatrix4fv;

	public readonly glGetAttribLocation Internal_GetAttribLocation;

	public readonly glEnableVertexAttribArray EnableVertexAttribArray;

	public readonly glVertexAttribPointer Internal_VertexAttribPointer;

	public readonly glVertexAttribIPointer Internal_VertexAttribIPointer;

	public readonly glVertexAttribI2i VertexAttribI2i;

	public readonly glDrawArrays Internal_DrawArrays;

	public readonly glDrawElements Internal_DrawElements;

	public readonly glDrawArraysInstanced Internal_DrawArraysInstanced;

	public readonly glDrawElementsInstanced Internal_DrawElementsInstanced;

	public readonly glReadBuffer ReadBuffer;

	public readonly glReadPixels ReadPixels;

	public readonly glFenceSync FenceSync;

	public readonly glDeleteSync DeleteSync;

	public readonly glGetSynciv GetSynciv;

	public readonly glGetString GetString;

	public readonly glFlush Flush;

	private Rectangle _viewport;

	private uint _activeVertexArray;

	private const int MaxTextureUnits = 21;

	private readonly uint[] _boundTextureIds = new uint[21];

	private readonly uint[] _boundSamplerIds = new uint[21];

	private uint _activeTextureUnit;

	private uint _activeFramebufferId;

	private readonly Dictionary<GL, bool> _capStates = new Dictionary<GL, bool>
	{
		{
			GL.CULL_FACE,
			false
		},
		{
			GL.BLEND,
			false
		},
		{
			GL.DEPTH_TEST,
			false
		},
		{
			GL.STENCIL_TEST,
			false
		},
		{
			GL.SCISSOR_TEST,
			false
		},
		{
			GL.POLYGON_OFFSET_FILL,
			false
		},
		{
			GL.RASTERIZER_DISCARD,
			false
		}
	};

	private bool _depthMask;

	private GL _depthFunc;

	private GL _blendSourceRGB;

	private GL _blendDestinationRGB;

	private GL _blendSourceAlpha;

	private GL _blendDestinationAlpha;

	private GL _blendEquationModeRGB;

	private GL _blendEquationModeAlpha;

	private GL _stencilFunc;

	private int _stencilFuncVal;

	private uint _stencilFuncMask;

	private GL _stencilOpSFail;

	private GL _stencilOpDPFail;

	private GL _stencilOpDPPass;

	private GL _cullFace;

	public void ResetDrawCallStats()
	{
		DrawCallsCount = 0;
		DrawnVertices = 0;
	}

	public GLFunctions()
	{
		bool flag = true;
		bool flag2 = false;
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			Type fieldType = fieldInfo.FieldType;
			if (!fieldType.Name.StartsWith("gl"))
			{
				continue;
			}
			IntPtr intPtr = SDL.SDL_GL_GetProcAddress(fieldType.Name);
			if (intPtr == IntPtr.Zero)
			{
				Logger.Warn("{0} is not supported.", fieldType.Name);
				string text = SDL.SDL_GetError();
				if (text != "")
				{
					Logger.Warn("SDL_GetError(): \"{0}\"", text);
				}
				SDL.SDL_ClearError();
				if (fieldInfo.GetCustomAttributes(typeof(OptionalAttribute), inherit: false).Length == 0)
				{
					flag2 = true;
				}
				else if (fieldType.Name.StartsWith("glDebug"))
				{
					flag = false;
				}
			}
			else
			{
				fieldInfo.SetValue(this, Marshal.GetDelegateForFunctionPointer(intPtr, fieldType));
			}
		}
		if (flag2)
		{
			throw new Exception("Failed to find one or more required GL functions. View log for more info!");
		}
		if (flag)
		{
			DebugMessageControlARB(GL.DONT_CARE, GL.DONT_CARE, GL.DONT_CARE, 0, IntPtr.Zero, enabled: true);
			DebugMessageControlARB(GL.DONT_CARE, GL.DEBUG_TYPE_OTHER_ARB, GL.DEBUG_SEVERITY_LOW_ARB, 0, IntPtr.Zero, enabled: false);
			DebugMessageControlARB(GL.DONT_CARE, GL.DEBUG_TYPE_OTHER_ARB, GL.DEBUG_SEVERITY_NOTIFICATION_ARB, 0, IntPtr.Zero, enabled: false);
			DebugMessageCallbackARB(DebugCallbackDelegate, IntPtr.Zero);
		}
	}

	private static void DebugCallback(GL source, GL type, uint id, GL severity, int length, IntPtr message, IntPtr userParam)
	{
		if (id != 131186 && id != 131218 && source != GL.DEBUG_SOURCE_SHADER_COMPILER_ARB)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(Marshal.PtrToStringAnsi(message));
			stringBuilder.AppendLine("\tSource: " + source);
			stringBuilder.AppendLine("\tType: " + type);
			stringBuilder.AppendLine("\tID: " + id);
			stringBuilder.AppendLine("\tSeverity: " + severity);
			Logger.Info<StringBuilder>(stringBuilder);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CheckError(string headerMessage)
	{
		GL gL = GetError();
		if (gL != 0)
		{
			Logger.Warn(headerMessage);
		}
		while (gL != 0)
		{
			Logger.Warn("GL_" + gL.ToString() + " - ");
			gL = GetError();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Viewport(int x, int y, int width, int height)
	{
		_viewport = new Rectangle(x, y, width, height);
		Internal_Viewport(x, y, width, height);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Viewport(Rectangle viewport)
	{
		_viewport = viewport;
		Internal_Viewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
	}

	public void AssertViewport(int x, int y, int width, int height)
	{
		AssertViewport(new Rectangle(x, y, width, height));
	}

	public void AssertViewport(Rectangle viewport)
	{
		Debug.Assert(_viewport == viewport, $"Expected viewport to be ({viewport.X}, {viewport.Y}, {viewport.Width}, {viewport.Height}) but was ({_viewport.X}, {_viewport.Y}, {_viewport.Width}, {_viewport.Height})");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UseProgram(GPUProgram program)
	{
		Internal_UseProgram(program.ProgramId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetAttribLocation(GPUProgram program, string name)
	{
		return Internal_GetAttribLocation(program.ProgramId, name);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLVertexArray GenVertexArray()
	{
		Internal_GenVertexArrays(1, out var arrays);
		return new GLVertexArray(arrays);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindVertexArray(GLVertexArray vertexArray)
	{
		_activeVertexArray = vertexArray.InternalId;
		Internal_BindVertexArray(vertexArray.InternalId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DeleteVertexArray(GLVertexArray vertexArray)
	{
		uint arrays = vertexArray.InternalId;
		Internal_DeleteVertexArrays(1, ref arrays);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLBuffer GenBuffer()
	{
		Internal_GenBuffers(1, out var buffers);
		return new GLBuffer(buffers);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DeleteBuffer(GLBuffer buffer)
	{
		uint buffers = buffer.InternalId;
		Internal_DeleteBuffers(1, ref buffers);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindBuffer(GLVertexArray array, GL target, GLBuffer buffer)
	{
		Debug.Assert(_activeVertexArray == array.InternalId, $"Unexpected vertex array is bound, expected {array.InternalId} but found ${_activeVertexArray}.");
		Internal_BindBuffer(target, buffer.InternalId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindBuffer(GL target, GLBuffer buffer)
	{
		Internal_BindBuffer(target, buffer.InternalId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void VertexAttribPointer(uint index, int size, GL type, bool normalized, int stride, IntPtr pointer)
	{
		Internal_VertexAttribPointer(index, size, type, normalized, stride, pointer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void VertexAttribIPointer(uint index, int size, GL type, int stride, IntPtr pointer)
	{
		Internal_VertexAttribIPointer(index, size, type, stride, pointer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLFramebuffer GenFramebuffer()
	{
		Internal_GenFramebuffers(1, out var framebuffers);
		return new GLFramebuffer(framebuffers);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindFramebuffer(GL target, GLFramebuffer framebuffer)
	{
		_activeFramebufferId = framebuffer.InternalId;
		Internal_BindFramebuffer(target, framebuffer.InternalId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void FramebufferTexture2D(GL target, GL attachment, GL textarget, GLTexture texture, int level)
	{
		Internal_FramebufferTexture2D(target, attachment, textarget, texture.InternalId, level);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DeleteFramebuffer(GLFramebuffer framebuffer)
	{
		uint framebuffers = framebuffer.InternalId;
		Internal_DeleteFramebuffers(1, ref framebuffers);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ActiveTexture(GL textureUnit)
	{
		_activeTextureUnit = (uint)(textureUnit - 33984);
		Internal_ActiveTexture(textureUnit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AssertActiveTexture(GL textureUnit)
	{
		Debug.Assert(textureUnit - 33984 == (GL)_activeTextureUnit, $"Expected textureUnit must be GL.TEXTURE{textureUnit}, but it's GL.TEXTURE{_activeTextureUnit}");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLTexture GenTexture()
	{
		Internal_GenTextures(1, out var textures);
		return new GLTexture(textures);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindTexture(GL target, GLTexture texture)
	{
		_boundTextureIds[_activeTextureUnit] = texture.InternalId;
		Internal_BindTexture(target, texture.InternalId);
	}

	public void AssertTextureBound(GL textureUnit, GLTexture texture)
	{
		Debug.Assert(textureUnit >= GL.TEXTURE0, "textureUnit must be GL.TEXTURE0 or bigger");
		uint num = (uint)(textureUnit - 33984);
		uint num2 = _boundTextureIds[num];
		Debug.Assert(num2 == texture.InternalId, $"Expected texture unit {num} to be bound to texture {texture.InternalId}, found {num2} instead.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DeleteTexture(GLTexture texture)
	{
		uint textures = texture.InternalId;
		Internal_DeleteTextures(1, ref textures);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLSampler GenSampler()
	{
		Internal_GenSamplers(1, out var samplers);
		return new GLSampler(samplers);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindSampler(uint unit, GLSampler sampler)
	{
		_boundSamplerIds[_activeTextureUnit] = sampler.InternalId;
		Internal_BindSampler(unit, sampler.InternalId);
	}

	public void AssertSamplerBound(uint unit, GLSampler sampler)
	{
		uint num = _boundTextureIds[unit];
		Debug.Assert(num == sampler.InternalId, $"Expected texture unit {unit} to be bound to texture {sampler.InternalId}, found {num} instead.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DeleteSampler(GLSampler sampler)
	{
		uint samplers = sampler.InternalId;
		Internal_DeleteSamplers(1, ref samplers);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SamplerParameteri(GLSampler sampler, GL pname, GL param)
	{
		uint internalId = sampler.InternalId;
		Internal_SamplerParameteri(internalId, pname, (int)param);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public GLQuery GenQuery()
	{
		Internal_GenQueries(1, out var queries);
		return new GLQuery(queries);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DeleteQuery(GLQuery query)
	{
		uint queries = query.InternalId;
		Internal_DeleteQueries(1, ref queries);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void QueryCounter(GLQuery query, GL target)
	{
		uint internalId = query.InternalId;
		Internal_QueryCounter(internalId, target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BeginQuery(GL target, GLQuery query)
	{
		uint internalId = query.InternalId;
		Internal_BeginQuery(target, internalId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EndQuery(GL target)
	{
		Internal_EndQuery(target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetQueryObjectuiv(GLQuery query, GL name, out uint param)
	{
		Debug.Assert(name == GL.QUERY_RESULT || name == GL.QUERY_RESULT_AVAILABLE, $"Expected names are GL.QUERY_RESULT and GL.QUERY_RESULT_AVAILABLE , found {name} instead.");
		uint internalId = query.InternalId;
		Internal_GetQueryObjectuiv(internalId, name, out param);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void GetQueryObjectui64v(GLQuery query, GL name, out ulong param)
	{
		Debug.Assert(name == GL.QUERY_RESULT || name == GL.QUERY_RESULT_AVAILABLE, $"Expected names are GL.QUERY_RESULT and GL.QUERY_RESULT_AVAILABLE , found {name} instead.");
		uint internalId = query.InternalId;
		Internal_glGetQueryObjectui64v(internalId, name, out param);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawArrays(GL mode, int first, int count)
	{
		DrawCallsCount++;
		DrawnVertices += count;
		Internal_DrawArrays(mode, first, count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawElements(GL mode, int count, GL type, IntPtr indices)
	{
		DrawCallsCount++;
		DrawnVertices += count;
		Internal_DrawElements(mode, count, type, indices);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawArraysInstanced(GL mode, int first, int count, int instancecount)
	{
		DrawCallsCount++;
		DrawnVertices += count * instancecount;
		Internal_DrawArraysInstanced(mode, first, count, instancecount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawElementsInstanced(GL mode, int count, GL type, IntPtr indices, int instancecount)
	{
		DrawCallsCount++;
		DrawnVertices += count * instancecount;
		Internal_DrawElementsInstanced(mode, count, type, indices, instancecount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetCapState(GL cap, bool enabled)
	{
		Debug.Assert(_capStates.ContainsKey(cap), $"GL.{cap} isn't a known capability.");
		_capStates[cap] = enabled;
		if (enabled)
		{
			Internal_Enable(cap);
		}
		else
		{
			Internal_Disable(cap);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Enable(GL cap)
	{
		SetCapState(cap, enabled: true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Disable(GL cap)
	{
		SetCapState(cap, enabled: false);
	}

	private void AssertCapState(GL cap, bool shouldBeEnabled)
	{
		Debug.Assert(_capStates.ContainsKey(cap), $"GL.{cap} isn't a known capability.");
		string arg = (shouldBeEnabled ? "enabled" : "disabled");
		Debug.Assert(_capStates[cap] == shouldBeEnabled, $"Expected GL.{cap} to be {arg} but it wasn't.");
	}

	public void AssertEnabled(GL cap)
	{
		AssertCapState(cap, shouldBeEnabled: true);
	}

	public void AssertDisabled(GL cap)
	{
		AssertCapState(cap, shouldBeEnabled: false);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DepthFunc(GL func)
	{
		_depthFunc = func;
		Internal_DepthFunc(func);
	}

	public void AssertDepthFunc(GL func)
	{
		Debug.Assert(_depthFunc == func, $"Expected glDepthFunc to be ({func}) but it wasn't.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DepthMask(bool write)
	{
		_depthMask = write;
		Internal_DepthMask(write);
	}

	public void AssertDepthMask(bool write)
	{
		Debug.Assert(_depthMask == write, $"Expected glDepthMask to be ({write}) but it wasn't.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlendFunc(GL sfactor, GL dfactor)
	{
		_blendSourceRGB = sfactor;
		_blendSourceAlpha = sfactor;
		_blendDestinationRGB = dfactor;
		_blendDestinationAlpha = dfactor;
		Internal_BlendFunc(sfactor, dfactor);
	}

	public void AssertBlendFunc(GL sfactor, GL dfactor)
	{
		Debug.Assert(_blendSourceRGB == sfactor && _blendSourceAlpha == sfactor && _blendDestinationRGB == dfactor && _blendDestinationAlpha == dfactor, $"Expected glBlendFunc to be (GL.{sfactor}, GL.{dfactor}) but it (GL.{_blendSourceRGB}, GL.{_blendDestinationRGB}) .");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlendFuncSeparate(GL srcRGB, GL dstRGB, GL srcAlpha, GL dstAlpha)
	{
		_blendSourceRGB = srcRGB;
		_blendDestinationRGB = dstRGB;
		_blendSourceAlpha = srcAlpha;
		_blendDestinationAlpha = dstAlpha;
		Internal_BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
	}

	public void AssertBlendFuncSeparate(GL srcRGB, GL dstRGB, GL srcAlpha, GL dstAlpha)
	{
		Debug.Assert(_blendSourceRGB == srcRGB && _blendDestinationRGB == dstRGB && _blendSourceAlpha == srcAlpha && _blendDestinationAlpha == dstAlpha, $"Expected glBlendFuncSeparate to be (GL.{srcRGB}, GL.{dstRGB}, GL.{srcAlpha}, GL.{dstAlpha}, ) but it was (GL.{_blendSourceRGB}, GL.{_blendDestinationRGB}, GL.{_blendSourceAlpha}, GL.{_blendDestinationAlpha}) .");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlendEquation(GL mode)
	{
		_blendEquationModeRGB = mode;
		_blendEquationModeAlpha = mode;
		Internal_BlendEquation(mode);
	}

	public void AssertBlendEquation(GL mode)
	{
		Debug.Assert(_blendEquationModeRGB == mode && _blendEquationModeAlpha == mode, $"Expected glBlendFunc to be (GL.{mode}) but it was (GL.{_blendEquationModeRGB}) .");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlendEquationSeparate(GL modeRGB, GL modeAlpha)
	{
		_blendEquationModeRGB = modeRGB;
		_blendEquationModeAlpha = modeAlpha;
		Internal_BlendEquationSeparate(modeRGB, modeAlpha);
	}

	public void AssertBlendEquationSeparate(GL modeRGB, GL modeAlpha)
	{
		Debug.Assert(_blendEquationModeRGB == modeRGB && _blendEquationModeAlpha == modeAlpha, $"Expected glBlendFunc to be (GL.{modeRGB}, GL.{modeAlpha}) but it wasn't.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StencilFunc(GL func, int val, uint mask)
	{
		_stencilFunc = func;
		_stencilFuncVal = val;
		_stencilFuncMask = mask;
		Internal_StencilFunc(func, val, mask);
	}

	public void AssertStencilFunc(GL func, int val, uint mask)
	{
		Debug.Assert(_stencilFunc == func && _stencilFuncVal == val && _stencilFuncMask == mask, $"Expected glStencilFunc to be (GL.{func}, GL.{val}, , GL.{mask}) but it wasn't.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StencilOp(GL sfail, GL dpfail, GL dppass)
	{
		_stencilOpSFail = sfail;
		_stencilOpDPFail = dpfail;
		_stencilOpDPPass = dppass;
		Internal_StencilOp(sfail, dpfail, dppass);
	}

	public void AssertStencilOp(GL sfail, GL dpfail, GL dppass)
	{
		Debug.Assert(_stencilOpSFail == sfail && _stencilOpDPFail == dpfail && _stencilOpDPPass == dppass, $"Expected glStencilOp to be (GL.{sfail}, GL.{dpfail}, , GL.{dppass}) but it wasn't.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CullFace(GL mode)
	{
		_cullFace = mode;
		Internal_CullFace(mode);
	}

	public void AssertCullFace(GL mode)
	{
		Debug.Assert(_cullFace == mode, $"Expected glCullFace to be (GL.{mode}) but it wasn't.");
	}
}
