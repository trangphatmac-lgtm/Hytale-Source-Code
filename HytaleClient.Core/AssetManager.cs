using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using HytaleClient.Utils;
using NLog;

namespace HytaleClient.Core;

internal static class AssetManager
{
	private class AssetMetadata
	{
		public string CachedFileName;

		public int ServerFileReferences;

		public List<string> BuiltInFileNames;
	}

	private class HashCacheEntry
	{
		public long HashTime;

		public string Hash;
	}

	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private static readonly ConcurrentDictionary<string, AssetMetadata> ActivelyReferencedAssets = new ConcurrentDictionary<string, AssetMetadata>();

	private static readonly ConcurrentDictionary<string, long> CachedAssetsOnDisk = new ConcurrentDictionary<string, long>();

	private static volatile bool IsInitialized;

	private static readonly SHA256CryptoServiceProvider HashProvider = new SHA256CryptoServiceProvider();

	private const long CachedAssetMaxAgeSeconds = 2592000L;

	public static float BuiltInAssetsMetadataLoadProgress;

	public static bool IsAssetsDirectoryImmutable { get; private set; }

	public static int ActivelyReferencedAssetsCount => ActivelyReferencedAssets.Count;

	public static int BuiltInAssetsCount => ActivelyReferencedAssets.Count - CachedAssetsOnDisk.Count;

	public static int CachedAssetsCount => CachedAssetsOnDisk.Count;

	public static void Initialize(Engine engine, CancellationToken cancellationToken, out HashSet<string> newAssets)
	{
		if (File.Exists(Path.GetFullPath(Path.Combine(Paths.BuiltInAssets, "CommonAssetsIndex.hashes"))))
		{
			IsAssetsDirectoryImmutable = true;
		}
		LoadMetadataForBuiltInCommonAssets(engine, cancellationToken, out newAssets);
		LoadCachedAssetsIndex();
		IsInitialized = !cancellationToken.IsCancellationRequested;
	}

	public static byte[] GetBuiltInAsset(string assetPath)
	{
		if (ThreadHelper.IsMainThread())
		{
			Logger.Warn<string, string>("{0} was called from main thread: {1}", "GetBuiltInAsset", assetPath);
		}
		return File.ReadAllBytes(Path.Combine(Paths.BuiltInAssets, assetPath));
	}

	public static string GetAssetLocalPathUsingHash(string hash)
	{
		if (!ActivelyReferencedAssets.TryGetValue(hash, out var value))
		{
			throw new Exception("Failed to find asset with hash " + hash);
		}
		if (value.CachedFileName != null)
		{
			return value.CachedFileName;
		}
		return value.BuiltInFileNames[0];
	}

	public static byte[] GetAssetUsingHash(string hash, bool allowFromMainThread = false)
	{
		if (!allowFromMainThread && ThreadHelper.IsMainThread())
		{
			Logger.Warn<string, string>("{0} was called from main thread: {1}", "GetAssetUsingHash", hash);
		}
		return File.ReadAllBytes(GetAssetLocalPathUsingHash(hash));
	}

	public static void MarkAssetAsServerRequired(string name, string hash, out bool foundInCache)
	{
		if (!ActivelyReferencedAssets.TryGetValue(hash, out var value))
		{
			if (!CachedAssetsOnDisk.ContainsKey(hash))
			{
				foundInCache = false;
				return;
			}
			value = (ActivelyReferencedAssets[hash] = new AssetMetadata());
			value.CachedFileName = GetCachedAssetFilePathFromHash(hash);
			CachedAssetsOnDisk[hash] = TimeHelper.GetEpochSeconds();
		}
		string item = Path.Combine(Paths.BuiltInAssets, "Common", name);
		if (value.BuiltInFileNames == null || !value.BuiltInFileNames.Contains(item))
		{
			value.ServerFileReferences++;
		}
		foundInCache = true;
	}

	public static void UnloadServerRequiredAssets()
	{
		foreach (AssetMetadata value2 in ActivelyReferencedAssets.Values)
		{
			value2.ServerFileReferences = 0;
		}
		List<KeyValuePair<string, AssetMetadata>> list = ActivelyReferencedAssets.Where((KeyValuePair<string, AssetMetadata> x) => x.Value.BuiltInFileNames == null || x.Value.BuiltInFileNames.Count == 0).ToList();
		foreach (KeyValuePair<string, AssetMetadata> item in list)
		{
			ActivelyReferencedAssets.TryRemove(item.Key, out var _);
		}
	}

	public static void AddServerAssetToCache(string name, string hash)
	{
		CachedAssetsOnDisk[hash] = TimeHelper.GetEpochSeconds();
		if (!ActivelyReferencedAssets.TryGetValue(hash, out var value))
		{
			value = (ActivelyReferencedAssets[hash] = new AssetMetadata());
		}
		if (value.CachedFileName == null)
		{
			value.CachedFileName = GetCachedAssetFilePathFromHash(hash);
		}
		value.ServerFileReferences++;
		Logger.Info<string, int, string>("Added cached reference to asset {0}: {1}, {2}", hash, value.ServerFileReferences, name);
	}

	public static void RemoveServerAssetFromCache(string name, string hash)
	{
		if (!ActivelyReferencedAssets.TryGetValue(hash, out var value))
		{
			Logger.Warn<string, string>("Attempted to remove asset from cache that doesn't exist! {0} ({1})", name, hash);
			return;
		}
		string item = Path.Combine(Paths.BuiltInAssets, "Common", name);
		if (value.BuiltInFileNames == null || !value.BuiltInFileNames.Remove(item))
		{
			value.ServerFileReferences--;
		}
		if ((value.BuiltInFileNames == null || value.BuiltInFileNames.Count == 0) && value.ServerFileReferences == 0)
		{
			Logger.Info<string, string>("Removing asset {0} ({1}) from memory, no more references.", name, hash);
			ActivelyReferencedAssets.TryRemove(hash, out var _);
		}
		else if (Logger.IsInfoEnabled)
		{
			Logger.Info("Removed reference to asset {0} ({1}) but retained in memory due to other references! BuiltInFileNames: {2}, CachedReferences: {3}", new object[4]
			{
				name,
				hash,
				value.BuiltInFileNames?.Count ?? 0,
				value.ServerFileReferences
			});
		}
	}

	public static void Shutdown()
	{
		if (IsInitialized)
		{
			Logger.Info("Shutting down...");
			EvictCachedAssets();
			UpdateCachedAssetsIndex();
		}
	}

	public static string GetAssetCountInfo()
	{
		return $"Total Assets: {ActivelyReferencedAssets.Count}, BuiltIn: {ActivelyReferencedAssets.Count - CachedAssetsOnDisk.Count}, Cached: {CachedAssetsOnDisk.Count}";
	}

	private static void LoadMetadataForBuiltInCommonAssets(Engine engine, CancellationToken cancellationToken, out HashSet<string> updatedAssets)
	{
		updatedAssets = new HashSet<string>();
		string text = Path.Combine(Paths.BuiltInAssets, "Common");
		string text2 = Path.Combine(Paths.BuiltInAssets, "CommonAssetsIndex.hashes");
		string text3 = Path.Combine(Paths.BuiltInAssets, "CommonAssetsIndex.cache");
		Logger.Info("Loading built-in assets from {0}.", text);
		char[] separator = new char[1] { ' ' };
		File.Delete(Path.Combine(Paths.BuiltInAssets, text3 + ".tmp"));
		FileStream fileStream = null;
		Stopwatch stopwatch;
		try
		{
			fileStream = File.OpenRead(text2);
		}
		catch
		{
			Logger.Info("Could not find {0}, recomputing hashes for built-in assets.", text2);
			stopwatch = Stopwatch.StartNew();
			string[] fileNames = (from x in Directory.GetFiles(text, "*.*", SearchOption.AllDirectories)
				where !x.EndsWith(".sha") && !x.EndsWith(".hash")
				select x).ToArray();
			Dictionary<string, HashCacheEntry> dictionary = new Dictionary<string, HashCacheEntry>();
			string[] array = fileNames;
			foreach (string text4 in array)
			{
				string key = text4.Substring(text.Length + 1);
				dictionary[key] = null;
			}
			FileStream fileStream2 = null;
			try
			{
				fileStream2 = File.OpenRead(text3);
			}
			catch
			{
			}
			if (fileStream2 != null)
			{
				using (StreamReader streamReader = new StreamReader(fileStream2))
				{
					bool flag = false;
					string text5 = streamReader.ReadLine();
					if (text5 == "VERSION=1")
					{
						flag = true;
						text5 = streamReader.ReadLine();
					}
					while (text5 != null)
					{
						string[] array2 = text5.Split(separator, 3);
						string hash = array2[0];
						long num = long.Parse(array2[1]);
						string text6 = array2[2];
						if (!flag)
						{
							num = TimeHelper.GetEpochSeconds(DateTime.FromFileTimeUtc(num));
						}
						if (dictionary.ContainsKey(text6))
						{
							dictionary[text6] = new HashCacheEntry
							{
								HashTime = num,
								Hash = hash
							};
						}
						else
						{
							updatedAssets.Add(text6);
						}
						text5 = streamReader.ReadLine();
					}
				}
				fileStream2.Close();
			}
			int num2 = 0;
			int i;
			for (i = 0; i < fileNames.Length; i++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				string text7 = fileNames[i];
				string text8 = text7.Substring(text.Length + 1);
				try
				{
					long epochSeconds = TimeHelper.GetEpochSeconds(File.GetLastWriteTimeUtc(text7));
					HashCacheEntry hashCacheEntry = dictionary[text8];
					string hash2;
					if (hashCacheEntry == null || hashCacheEntry.HashTime != epochSeconds)
					{
						hash2 = ComputeHash(File.ReadAllBytes(text7));
						updatedAssets.Add(text8);
					}
					else
					{
						hash2 = hashCacheEntry.Hash;
						num2++;
					}
					dictionary[text8] = new HashCacheEntry
					{
						HashTime = epochSeconds,
						Hash = hash2
					};
					AddBuiltInFileNameReference(hash2, text7);
					engine.RunOnMainThread(engine, delegate
					{
						BuiltInAssetsMetadataLoadProgress = (float)i / (float)fileNames.Length;
					});
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to hash: " + text7);
					dictionary.Remove(text8);
				}
			}
			using (fileStream2 = File.Create(text3 + ".tmp"))
			{
				using StreamWriter streamWriter = new StreamWriter(fileStream2);
				streamWriter.WriteLine("VERSION=1");
				foreach (KeyValuePair<string, HashCacheEntry> item in dictionary)
				{
					streamWriter.WriteLine($"{item.Value.Hash} {item.Value.HashTime} {item.Key}");
				}
			}
			stopwatch.Stop();
			Logger.Info<long, int, int>("Spent {0}ms recomputing hashes for {1} built-in assets, reused {2} hashes from cache.", stopwatch.ElapsedMilliseconds, fileNames.Length, num2);
			return;
		}
		stopwatch = Stopwatch.StartNew();
		using (StreamReader streamReader2 = new StreamReader(fileStream))
		{
			while (true)
			{
				string text9 = streamReader2.ReadLine();
				if (text9 == null)
				{
					break;
				}
				string[] array3 = text9.Split(separator, 2);
				string hash3 = array3[0];
				string fileName = Path.Combine(text, array3[1]);
				AddBuiltInFileNameReference(hash3, fileName);
			}
		}
		stopwatch.Stop();
		Logger.Info<long, string>("Spent {0}ms loading hashes from {1}.", stopwatch.ElapsedMilliseconds, text2);
		fileStream.Close();
	}

	private static void AddBuiltInFileNameReference(string hash, string fileName)
	{
		if (!ActivelyReferencedAssets.TryGetValue(hash, out var value))
		{
			ConcurrentDictionary<string, AssetMetadata> activelyReferencedAssets = ActivelyReferencedAssets;
			AssetMetadata obj = new AssetMetadata
			{
				BuiltInFileNames = new List<string>()
			};
			value = obj;
			activelyReferencedAssets[hash] = obj;
		}
		value.BuiltInFileNames.Add(fileName);
	}

	public static HashSet<string> GetUpdatedAssets(string directoryPath, string cacheFilePath, CancellationToken cancellationToken)
	{
		HashSet<string> hashSet = new HashSet<string>();
		string[] array = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToArray();
		Dictionary<string, HashCacheEntry> dictionary = new Dictionary<string, HashCacheEntry>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			string key = text.Substring(directoryPath.Length + 1);
			dictionary[key] = null;
		}
		char[] separator = new char[1] { ' ' };
		FileStream fileStream = null;
		try
		{
			fileStream = File.OpenRead(cacheFilePath);
		}
		catch
		{
		}
		if (fileStream != null)
		{
			using (StreamReader streamReader = new StreamReader(fileStream))
			{
				bool flag = false;
				string text2 = streamReader.ReadLine();
				if (text2 == "VERSION=1")
				{
					flag = true;
					text2 = streamReader.ReadLine();
				}
				while (text2 != null)
				{
					string[] array3 = text2.Split(separator, 3);
					long num = long.Parse(array3[0]);
					string text3 = array3[1];
					if (!flag)
					{
						num = TimeHelper.GetEpochSeconds(DateTime.FromFileTimeUtc(num));
					}
					if (dictionary.ContainsKey(text3))
					{
						dictionary[text3] = new HashCacheEntry
						{
							HashTime = num
						};
					}
					else
					{
						hashSet.Add(text3);
					}
					text2 = streamReader.ReadLine();
				}
			}
			fileStream.Close();
		}
		for (int j = 0; j < array.Length; j++)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return hashSet;
			}
			string text4 = array[j];
			string text5 = text4.Substring(directoryPath.Length + 1);
			long epochSeconds = TimeHelper.GetEpochSeconds(File.GetLastWriteTimeUtc(text4));
			HashCacheEntry hashCacheEntry = dictionary[text5];
			if (hashCacheEntry == null || hashCacheEntry.HashTime != epochSeconds)
			{
				hashSet.Add(text5);
			}
			dictionary[text5] = new HashCacheEntry
			{
				HashTime = epochSeconds
			};
		}
		using (fileStream = File.Create(cacheFilePath))
		{
			using StreamWriter streamWriter = new StreamWriter(fileStream);
			streamWriter.WriteLine("VERSION=1");
			foreach (KeyValuePair<string, HashCacheEntry> item in dictionary)
			{
				streamWriter.WriteLine($"{item.Value.HashTime} {item.Key}");
			}
		}
		return hashSet;
	}

	private static void LoadCachedAssetsIndex()
	{
		string text = "CachedAssetsIndex.cache";
		Logger.Info("Loading cached assets from {0}...", Paths.CachedAssets);
		FileStream fileStream = null;
		try
		{
			fileStream = File.OpenRead(Path.Combine(Paths.CachedAssets, "..", text));
		}
		catch
		{
			Logger.Info("Could not find {0}.", text);
			return;
		}
		char[] separator = new char[1] { ' ' };
		using (StreamReader streamReader = new StreamReader(fileStream))
		{
			string text2 = streamReader.ReadLine();
			if (text2 != "VERSION=1")
			{
				Logger.Info<string, string>("Cached assets index in {0} has invalid header, found {1}.", Paths.CachedAssets, text2);
				return;
			}
			while (true)
			{
				text2 = streamReader.ReadLine();
				if (text2 == null)
				{
					break;
				}
				string[] array = text2.Split(separator, 2);
				string text3 = array[0];
				long value = long.Parse(array[1]);
				string cachedAssetFilePathFromHash = GetCachedAssetFilePathFromHash(text3);
				if (File.Exists(cachedAssetFilePathFromHash))
				{
					CachedAssetsOnDisk[text3] = value;
					if (ActivelyReferencedAssets.TryGetValue(text3, out var value2))
					{
						Logger.Info<string, string>("Linked BuiltIn asset to cached on disk path: {0}, {1}", text3, string.Join(", ", value2.BuiltInFileNames));
						value2.CachedFileName = cachedAssetFilePathFromHash;
					}
				}
			}
		}
		fileStream.Close();
	}

	private static void EvictCachedAssets()
	{
		long cutoffEpochSeconds = TimeHelper.GetEpochSeconds() - 2592000;
		KeyValuePair<string, long>[] array = CachedAssetsOnDisk.Where((KeyValuePair<string, long> x) => x.Value < cutoffEpochSeconds).ToArray();
		if (array.Length == 0)
		{
			return;
		}
		Logger.Info("Evicting {0} assets from cache.", array.Length);
		KeyValuePair<string, long>[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			KeyValuePair<string, long> keyValuePair = array2[i];
			CachedAssetsOnDisk.TryRemove(keyValuePair.Key, out var _);
			if (ActivelyReferencedAssets.TryGetValue(keyValuePair.Key, out var value2))
			{
				value2.CachedFileName = null;
				value2.ServerFileReferences = 0;
			}
			try
			{
				string text = Path.Combine(Paths.CachedAssets, keyValuePair.Key.Substring(0, 2));
				string path = Path.Combine(text, keyValuePair.Key.Substring(2));
				File.Delete(path);
				if (Directory.GetFiles(text).Length == 0)
				{
					Directory.Delete(text);
				}
			}
			catch
			{
				throw;
			}
		}
	}

	private static void UpdateCachedAssetsIndex()
	{
		string fullPath = Path.GetFullPath(Path.Combine(Paths.CachedAssets, "..", "CachedAssetsIndex.cache"));
		using FileStream stream = File.Open(fullPath, FileMode.Create);
		using StreamWriter streamWriter = new StreamWriter(stream);
		streamWriter.WriteLine("VERSION=1");
		foreach (KeyValuePair<string, long> item in CachedAssetsOnDisk)
		{
			streamWriter.WriteLine($"{item.Key} {item.Value}");
		}
	}

	private static string GetCachedAssetFilePathFromHash(string hash)
	{
		return Path.Combine(Paths.CachedAssets, hash.Substring(0, 2), hash.Substring(2));
	}

	public static string ComputeHash(byte[] data)
	{
		byte[] rawHash = HashProvider.ComputeHash(data);
		return HashBytesAsString(rawHash);
	}

	private static string HashBytesAsString(byte[] rawHash)
	{
		StringBuilder stringBuilder = new StringBuilder(rawHash.Length * 2);
		for (int i = 0; i < rawHash.Length; i++)
		{
			stringBuilder.Append(rawHash[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
