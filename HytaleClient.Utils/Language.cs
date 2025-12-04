using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;

namespace HytaleClient.Utils;

public static class Language
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string FileExtension = ".lang";

	public static readonly string DefaultLanguage = "en-US";

	public static readonly string SystemLanguage = Thread.CurrentThread.CurrentCulture.Name;

	public static void Initialize()
	{
		Logger.Info("System language: {0}", SystemLanguage);
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
	}

	public static Dictionary<string, string> GetAvailableLanguages()
	{
		IEnumerable<DirectoryInfo> source = new DirectoryInfo(Paths.Language).EnumerateDirectories("*.*", SearchOption.TopDirectoryOnly);
		return source.ToDictionary((DirectoryInfo language) => language.Name, (DirectoryInfo language) => Parse(File.ReadAllLines(Path.Combine(language.FullName, "meta.lang")))["name"]);
	}

	public static IDictionary<string, string> Parse(string[] lines)
	{
		IDictionary<string, string> dictionary = new Dictionary<string, string>();
		bool flag = false;
		string key = null;
		foreach (string text in lines)
		{
			if (!string.IsNullOrEmpty(text) && !text.StartsWith("#") && (text.Contains("=") || flag))
			{
				string text2 = text;
				if (text2.EndsWith("\\"))
				{
					text2 = text2.Substring(0, text2.Length - 1);
				}
				if (flag)
				{
					dictionary[key] = dictionary[key] + "\n" + text2;
				}
				else
				{
					int num = text.IndexOf("=");
					key = text2.Substring(0, num).Trim();
					text2 = text2.Substring(num + 1).Trim();
					dictionary[key] = text2;
				}
				flag = text.EndsWith("\\");
			}
		}
		return dictionary;
	}

	private static void LoadLanguageFiles(string language, Dictionary<string, string> dict, string path, string prefix = "")
	{
		path = Path.Combine(path, language);
		if (!Directory.Exists(path))
		{
			Logger.Warn("Could not load language files from \"{0}\" as the directory doesn't exist.", path);
			return;
		}
		IEnumerable<string> enumerable = Directory.EnumerateFiles(path, "*.lang", SearchOption.AllDirectories);
		string text = path;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		Uri uri = new Uri(text + directorySeparatorChar);
		foreach (string item in enumerable)
		{
			string text2 = uri.MakeRelativeUri(new Uri(item)).ToString();
			string text3 = prefix + text2.Replace("/", ".").Substring(0, text2.Length - ".lang".Length) + ".";
			foreach (KeyValuePair<string, string> item2 in Parse(File.ReadAllLines(item)))
			{
				dict[text3 + item2.Key] = item2.Value;
			}
		}
	}

	public static string GetAutomaticLanguage()
	{
		string systemLanguage = SystemLanguage;
		return LanguageExists(systemLanguage) ? systemLanguage : GetFallback(systemLanguage);
	}

	public static bool LanguageExists(string language)
	{
		return Directory.Exists(Path.Combine(Paths.Language, language));
	}

	public static IDictionary<string, string> LoadServerLanguageFile(string filename, string language)
	{
		IDictionary<string, string> dictionary = Parse(File.ReadAllLines(Path.Combine(Paths.BuiltInAssets, "Server/Languages/" + DefaultLanguage + "/" + filename)));
		if (language == null)
		{
			language = GetAutomaticLanguage();
		}
		string path = Path.Combine(Paths.BuiltInAssets, "Server/Languages/" + language + "/" + filename);
		if (language != DefaultLanguage && File.Exists(path))
		{
			foreach (KeyValuePair<string, string> item in Parse(File.ReadAllLines(path)))
			{
				dictionary[item.Key] = item.Value;
			}
		}
		return dictionary;
	}

	public static Dictionary<string, string> LoadLanguage(string language)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string> { { "currentLocale", DefaultLanguage } };
		LoadLanguageFiles(DefaultLanguage, dictionary, Paths.Language, "ui.");
		LoadLanguageFiles(DefaultLanguage, dictionary, Path.Combine(Paths.BuiltInAssets, "Common", "Languages"));
		if (language == null)
		{
			language = GetAutomaticLanguage();
		}
		if (language != DefaultLanguage)
		{
			LoadLanguageFiles(language, dictionary, Paths.Language, "ui.");
			LoadLanguageFiles(language, dictionary, Path.Combine(Paths.BuiltInAssets, "Common", "Languages"));
			dictionary["currentLocale"] = language;
		}
		return dictionary;
	}

	private static string GetFallback(string language)
	{
		IDictionary<string, string> dictionary = Parse(File.ReadAllLines(Path.Combine(Paths.Language, "fallback.lang")));
		dictionary.TryGetValue(language, out var value);
		return value ?? DefaultLanguage;
	}
}
