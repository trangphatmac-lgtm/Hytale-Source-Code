using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace HytaleClient.Graphics.Programs;

public abstract class GPUProgram
{
	public enum ShaderCodeDumpPolicy
	{
		Never,
		OnError,
		Always
	}

	protected struct ShaderResource
	{
		public string FileName;

		public DateTime LastLoadTime;

		public List<string> Includes;
	}

	protected struct AttribBindingInfo
	{
		public readonly uint Index;

		public readonly string Name;

		public AttribBindingInfo(uint index, string name)
		{
			Index = index;
			Name = name;
		}
	}

	protected static readonly NumberFormatInfo DecimalPointFormatting = new NumberFormatInfo
	{
		NumberDecimalSeparator = ".",
		NumberGroupSeparator = ""
	};

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static ShaderCodeDumpPolicy _dumpPolicy;

	private static string _shaderResourceAssemblyPath;

	private static string _shaderResourcePath;

	private static string _shaderCodeDumpPath;

	private const int MaxIncludeDepth = 5;

	protected readonly string _variationName;

	protected ShaderResource _vertexShaderResource;

	protected ShaderResource _geometryShaderResource;

	protected ShaderResource _fragmentShaderResource;

	protected static GLFunctions _gl;

	private static uint _fallbackVertexShader;

	private static uint _fallbackGeometryShader;

	private static uint _fallbackFragmentShader;

	private static uint _fallbackProgram;

	public uint ProgramId { get; private set; }

	public static void InitializeGL(GLFunctions gl)
	{
		_gl = gl;
	}

	public static void ReleaseGL()
	{
		_gl = null;
	}

	public static void SetShaderCodeDumpPolicy(ShaderCodeDumpPolicy policy)
	{
		_dumpPolicy = policy;
	}

	public static void SetResourcePaths(string shaderResourceAssemblyPath, string shaderResourcePath, string shaderCodeDumpPath)
	{
		_shaderResourceAssemblyPath = shaderResourceAssemblyPath;
		_shaderResourcePath = shaderResourcePath;
		_shaderCodeDumpPath = shaderCodeDumpPath;
	}

	protected static bool IsResourceBindingLayoutValid<T>(T layout)
	{
		bool flag = true;
		object obj = layout;
		FieldInfo[] fields = typeof(T).GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			FieldInfo[] fields2 = typeof(T).GetFields();
			foreach (FieldInfo fieldInfo2 in fields2)
			{
				flag = flag && (fieldInfo.Name == fieldInfo2.Name || (byte)fieldInfo.GetValue(obj) != (byte)fieldInfo2.GetValue(obj));
			}
		}
		return flag;
	}

	protected GPUProgram(string vertexShaderFileName, string geometryShaderFileName, string fragmentShaderFileName, string variationName = null)
	{
		_variationName = ((variationName != null) ? variationName : GetType().Name);
		_vertexShaderResource.FileName = vertexShaderFileName;
		_geometryShaderResource.FileName = geometryShaderFileName;
		_fragmentShaderResource.FileName = fragmentShaderFileName;
	}

	protected GPUProgram(string vertexShaderFileName, string fragmentShaderFileName, string variationName = null)
		: this(vertexShaderFileName, null, fragmentShaderFileName, variationName)
	{
	}

	public virtual bool Initialize()
	{
		Logger.Info<string, string>("Building GPU Program \"{0} (class: {1})\"", _variationName, GetType().Name);
		return true;
	}

	public virtual void Release()
	{
		_vertexShaderResource.Includes?.Clear();
		_geometryShaderResource.Includes?.Clear();
		_fragmentShaderResource.Includes?.Clear();
		_gl.DeleteProgram(ProgramId);
	}

	public bool Reset(bool forceReset = true)
	{
		if (!forceReset && !HasChangedSinceLastCompile())
		{
			return true;
		}
		Release();
		return Initialize();
	}

	protected virtual void InitUniforms()
	{
	}

	public static bool CreateFallbacks()
	{
		_fallbackVertexShader = CompileShader(GL.VERTEX_SHADER, "#version 150 core\n uniform mat4 uMVPMatrix; in vec3 vertPosition; void main() { gl_Position = uMVPMatrix * vec4(vertPosition, 1.0); }", 0, 0);
		_fallbackGeometryShader = 0u;
		_fallbackFragmentShader = CompileShader(GL.FRAGMENT_SHADER, "#version 150 core\n out vec4 outColor; void main() { outColor = vec4(1,0,0,1); }", 0, 0);
		_fallbackProgram = _gl.CreateProgram();
		_gl.AttachShader(_fallbackProgram, _fallbackVertexShader);
		_gl.AttachShader(_fallbackProgram, _fallbackFragmentShader);
		return LinkProgram("Fallback Program", ref _fallbackProgram);
	}

	public static void DestroyFallbacks()
	{
		_gl.DeleteShader(_fallbackVertexShader);
		_gl.DeleteShader(_fallbackFragmentShader);
	}

	public void AssertInUse()
	{
		int[] array = new int[1];
		_gl.GetIntegerv(GL.CURRENT_PROGRAM, array);
		if (ProgramId != array[0])
		{
			Logger.Info<string, string>("Program {0} (class:{1}) isn't current!", _variationName, GetType().Name);
		}
	}

	protected bool MakeProgram(uint vertexShader, List<AttribBindingInfo> attribLocations = null, bool ignoreMissingUniforms = false, string[] transformFeedbackVaryings = null)
	{
		return MakeProgram((int)vertexShader, -1, -1, attribLocations, ignoreMissingUniforms, transformFeedbackVaryings);
	}

	protected bool MakeProgram(uint vertexShader, uint fragmentShader, List<AttribBindingInfo> attribLocations = null, bool ignoreMissingUniforms = false, string[] transformFeedbackVaryings = null)
	{
		return MakeProgram((int)vertexShader, -1, (int)fragmentShader, attribLocations, ignoreMissingUniforms, transformFeedbackVaryings);
	}

	protected bool MakeProgram(uint vertexShader, uint geometryShader, uint fragmentShader, List<AttribBindingInfo> attribLocations = null, bool ignoreMissingUniforms = false, string[] transformFeedbackVaryings = null)
	{
		return MakeProgram((int)vertexShader, (int)geometryShader, (int)fragmentShader, attribLocations, ignoreMissingUniforms, transformFeedbackVaryings);
	}

	private bool MakeProgram(int vertexShader, int geometryShader, int fragmentShader, List<AttribBindingInfo> attribLocations = null, bool ignoreMissingUniforms = false, string[] transformFeedbackVaryings = null)
	{
		bool flag = vertexShader != 0 && geometryShader != 0 && fragmentShader != 0;
		if (flag)
		{
			ProgramId = _gl.CreateProgram();
			if (attribLocations != null)
			{
				foreach (AttribBindingInfo attribLocation2 in attribLocations)
				{
					_gl.BindAttribLocation(ProgramId, attribLocation2.Index, attribLocation2.Name);
				}
			}
			_gl.AttachShader(ProgramId, (uint)vertexShader);
			if (geometryShader != -1)
			{
				_gl.AttachShader(ProgramId, (uint)geometryShader);
			}
			if (fragmentShader != -1)
			{
				_gl.AttachShader(ProgramId, (uint)fragmentShader);
			}
			if (transformFeedbackVaryings != null)
			{
				_gl.TransformFeedbackVaryings(ProgramId, transformFeedbackVaryings.Length, transformFeedbackVaryings, GL.INTERLEAVED_ATTRIBS);
			}
			uint program = ProgramId;
			flag = LinkProgram(GetType().Name, ref program);
			ProgramId = program;
			if (vertexShader > 0)
			{
				_gl.DeleteShader((uint)vertexShader);
			}
			if (geometryShader > 0)
			{
				_gl.DeleteShader((uint)geometryShader);
			}
			if (fragmentShader > 0)
			{
				_gl.DeleteShader((uint)fragmentShader);
			}
			FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.FieldType == typeof(UniformBufferObject))
				{
					string text = "ubo" + fieldInfo.Name;
					uint num = _gl.GetUniformBlockIndex(ProgramId, text);
					if (num == uint.MaxValue)
					{
						Logger.Warn("- Could not find uniform buffer object {0}.", text);
						fieldInfo.SetValue(this, new UniformBufferObject(this, num, text));
					}
					fieldInfo.SetValue(this, new UniformBufferObject(this, num, text));
				}
				else if (fieldInfo.FieldType == typeof(Uniform))
				{
					string text2 = "u" + fieldInfo.Name;
					int num2 = _gl.GetUniformLocation(ProgramId, text2);
					if (num2 == -1)
					{
						Logger.Warn("- Could not find uniform {0}.", text2);
						fieldInfo.SetValue(this, new Uniform(-1, text2, this));
					}
					fieldInfo.SetValue(this, new Uniform(num2, text2, this));
				}
				else if (fieldInfo.FieldType == typeof(Attrib))
				{
					string text3 = "vert" + fieldInfo.Name.Substring("Attrib".Length);
					int attribLocation = _gl.GetAttribLocation(this, text3);
					if (attribLocation == -1)
					{
						Logger.Error("- Could not find attrib {0}.", text3);
						fieldInfo.SetValue(this, new Attrib(uint.MaxValue, text3));
						flag = false;
					}
					fieldInfo.SetValue(this, new Attrib((uint)attribLocation, text3));
				}
			}
			InitUniforms();
		}
		return flag;
	}

	private static bool LinkProgram(string programName, ref uint program)
	{
		bool result = true;
		_gl.LinkProgram(program);
		int param = 0;
		_gl.GetProgramiv(program, GL.LINK_STATUS, out param);
		if (param == 0)
		{
			_gl.GetProgramiv(program, GL.INFO_LOG_LENGTH, out var param2);
			byte[] array = new byte[param2];
			_gl.GetProgramInfoLog(program, param2, out param2, array);
			string @string = Encoding.UTF8.GetString(array);
			Logger.Warn(@string);
			Logger.Warn<uint, string>("- Program {0} {1} failed to link!", program, programName);
			Logger.Warn("- Using Fallback program instead!");
			program = _fallbackProgram;
			result = false;
		}
		return result;
	}

	public bool ResetUniforms()
	{
		uint program = ProgramId;
		bool flag = LinkProgram(GetType().Name, ref program);
		ProgramId = program;
		FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (!(fieldInfo.FieldType != typeof(Uniform)))
			{
				Uniform uniform = (Uniform)fieldInfo.GetValue(this);
				uniform.Reset();
				fieldInfo.SetValue(this, uniform);
			}
		}
		if (flag)
		{
			InitUniforms();
		}
		return flag;
	}

	private string GetShaderFullPath(string shaderName)
	{
		return Path.GetFullPath(Path.Combine(_shaderResourcePath, shaderName));
	}

	private string GetShaderSource(string shaderName, out DateTime lastLoadtime)
	{
		string result;
		try
		{
			string shaderFullPath = GetShaderFullPath(shaderName);
			StreamReader streamReader = new StreamReader(shaderFullPath);
			result = streamReader.ReadToEnd();
			streamReader.Close();
			lastLoadtime = File.GetLastWriteTimeUtc(shaderFullPath);
		}
		catch (Exception innerException)
		{
			throw new Exception("- GetShaderSource failed! Could not load the source code for '" + shaderName + "'", innerException);
		}
		return result;
	}

	protected void DumpShaderCodeToFile(string shaderName, string shaderSource)
	{
		string fullPath = Path.GetFullPath(_shaderCodeDumpPath);
		if (!Directory.Exists(fullPath))
		{
			Directory.CreateDirectory(fullPath);
		}
		string path = "Full_" + _variationName + "_" + shaderName;
		string path2 = Path.Combine(fullPath, path);
		using FileStream stream = File.Open(path2, FileMode.Create);
		using StreamWriter streamWriter = new StreamWriter(stream);
		streamWriter.Write(shaderSource);
	}

	protected bool HasChangedSinceLastCompile()
	{
		return false || HasChangedSinceLastCompile(_vertexShaderResource) || HasChangedSinceLastCompile(_geometryShaderResource) || HasChangedSinceLastCompile(_fragmentShaderResource);
	}

	private bool HasChangedSinceLastCompile(ShaderResource shaderResource)
	{
		bool flag = false;
		if (shaderResource.FileName != null)
		{
			flag = File.GetLastWriteTimeUtc(GetShaderFullPath(shaderResource.FileName)) > shaderResource.LastLoadTime;
			if (!flag)
			{
				for (int i = 0; i < shaderResource.Includes?.Count; i++)
				{
					if (flag)
					{
						break;
					}
					flag = File.GetLastWriteTimeUtc(GetShaderFullPath(shaderResource.Includes[i])) > shaderResource.LastLoadTime;
				}
			}
		}
		return flag;
	}

	protected uint CompileVertexShader(Dictionary<string, string> defines = null)
	{
		return CompileShaderResource(ref _vertexShaderResource, GL.VERTEX_SHADER, defines);
	}

	protected uint CompileGeometryShader(Dictionary<string, string> defines = null)
	{
		return CompileShaderResource(ref _geometryShaderResource, GL.GEOMETRY_SHADER, defines);
	}

	protected uint CompileFragmentShader(Dictionary<string, string> defines = null)
	{
		return CompileShaderResource(ref _fragmentShaderResource, GL.FRAGMENT_SHADER, defines);
	}

	private uint CompileShaderResource(ref ShaderResource shaderResource, GL shaderType, Dictionary<string, string> defines = null)
	{
		string shaderSource = GetShaderSource(shaderResource.FileName, out shaderResource.LastLoadTime);
		shaderSource = InjectDefines(shaderResource.FileName, shaderSource, defines, out var defineLineCount);
		string regex = "#include\\s*\"(?<File>[^\"]*)\"";
		int includeLineCount = 0;
		shaderSource = InjectIncludes(ref shaderResource, shaderSource, regex, 0, ref includeLineCount);
		uint num = CompileShader(shaderType, shaderSource, defineLineCount, includeLineCount);
		bool flag = num == 0;
		if (flag)
		{
			Logger.Warn("ERROR : Shader compilation failed for resource {0}.", shaderResource.FileName);
		}
		if (_dumpPolicy == ShaderCodeDumpPolicy.Always || (_dumpPolicy == ShaderCodeDumpPolicy.OnError && flag))
		{
			string shaderName = shaderResource.FileName + shaderType switch
			{
				GL.FRAGMENT_SHADER => ".frag", 
				GL.VERTEX_SHADER => ".vert", 
				_ => ".geom", 
			};
			DumpShaderCodeToFile(shaderName, shaderSource);
		}
		return num;
	}

	protected string InjectDefines(string shaderName, string shaderSource, Dictionary<string, string> defines, out int defineLineCount)
	{
		defineLineCount = 0;
		if (defines != null)
		{
			string text = "//##################################### BEGIN: CPU - DEFINES #####################################\n";
			string text2 = "//##################################### END: CPU - DEFINES #####################################\n";
			string text3 = "";
			foreach (KeyValuePair<string, string> define in defines)
			{
				text3 = text3 + "#define " + define.Key + " " + define.Value + "\n";
			}
			text3 = text + text3 + text2;
			defineLineCount = defines.Count + 2;
			int num = shaderSource.LastIndexOf("#extension");
			if (num == -1)
			{
				shaderSource.LastIndexOf("#version");
			}
			if (num == -1)
			{
				num = 0;
			}
			num = shaderSource.IndexOf('\n', num);
			return shaderSource.Insert(num + 1, text3);
		}
		return shaderSource;
	}

	protected string InjectIncludes(ref ShaderResource shaderResource, string source, string regex, int includeDepth, ref int includeLineCount)
	{
		includeDepth++;
		if (includeDepth > 5)
		{
			throw new Exception("Too many includes!");
		}
		MatchCollection matchCollection = Regex.Matches(source, regex);
		foreach (Match item in matchCollection)
		{
			string value = item.Groups["File"].Value;
			string shaderSource = GetShaderSource(value, out var lastLoadtime);
			shaderResource.LastLoadTime = ((shaderResource.LastLoadTime < lastLoadtime) ? lastLoadtime : shaderResource.LastLoadTime);
			if (shaderResource.Includes == null)
			{
				shaderResource.Includes = new List<string>();
			}
			shaderResource.Includes.Add(value);
			string text = "//##################################### BEGIN: " + value + " #####################################\n";
			string text2 = "//##################################### END: " + value + " #####################################\n";
			shaderSource = text + shaderSource + text2;
			includeLineCount += shaderSource.Split(new char[1] { '\n' }).Length - 1;
			shaderSource = InjectIncludes(ref shaderResource, shaderSource, regex, includeDepth, ref includeLineCount);
			source = source.Replace(item.Value, shaderSource);
		}
		return source;
	}

	protected static string PatchCompileErrorMessage(string errorMessage, string sourceCode, int definesLineCount, int includeLineCount)
	{
		int injectedLinesCount = includeLineCount + definesLineCount;
		string pattern = "\\((?<line>\\d+)\\)";
		string pattern2 = "ERROR: (?<fileid>\\d+):(?<line>\\d+)";
		string[] sourceCodeLines = sourceCode.Split(new char[1] { '\n' });
		string text = errorMessage;
		text = Regex.Replace(errorMessage, pattern, (Match match) => string.Format("({0}) [line patched as {1}, error in \"{2}\"]\n =>", match.Groups["line"].ToString(), int.Parse(match.Groups["line"].ToString()) - injectedLinesCount, sourceCodeLines[int.Parse(match.Groups["line"].ToString()) - 1].Replace('\r', ' ')));
		text = Regex.Replace(text, pattern2, (Match match) => string.Format("ERROR: {0}: {1} [line patched as {2}, error in \"{3}\"]\n =>", match.Groups["fileid"].ToString(), match.Groups["line"].ToString(), int.Parse(match.Groups["line"].ToString()) - injectedLinesCount, sourceCodeLines[int.Parse(match.Groups["line"].ToString()) - 1].Replace('\r', ' ')));
		return $"- Following GLSL error message was patched : {definesLineCount} lines for #define and {includeLineCount} lines for #include\n" + text;
	}

	protected static uint CompileShader(GL shaderType, string source, int definesLineCount, int includeLineCount)
	{
		uint num = _gl.CreateShader(shaderType);
		int length = source.Length;
		_gl.ShaderSource(num, 1, ref source, ref length);
		_gl.CompileShader(num);
		int param = 0;
		_gl.GetShaderiv(num, GL.COMPILE_STATUS, out param);
		if (param == 0)
		{
			_gl.GetShaderiv(num, GL.INFO_LOG_LENGTH, out var param2);
			byte[] array = new byte[param2];
			_gl.GetShaderInfoLog(num, param2, out param2, array);
			string @string = Encoding.UTF8.GetString(array);
			@string = PatchCompileErrorMessage(@string, source, definesLineCount, includeLineCount);
			Logger.Warn(@string);
			Logger.Warn("- {0} compilation failed, using fallback since shader hot reloading is on.", shaderType.ToString());
			switch (shaderType)
			{
			case GL.VERTEX_SHADER:
				num = _fallbackVertexShader;
				break;
			case GL.GEOMETRY_SHADER:
				num = _fallbackGeometryShader;
				break;
			case GL.FRAGMENT_SHADER:
				num = _fallbackFragmentShader;
				break;
			default:
				throw new NotImplementedException();
			}
			num = 0u;
		}
		return num;
	}
}
