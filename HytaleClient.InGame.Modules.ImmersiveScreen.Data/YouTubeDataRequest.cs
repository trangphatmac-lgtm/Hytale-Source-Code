using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using HytaleClient.Core;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.ImmersiveScreen.Data;

internal class YouTubeDataRequest : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string ApiKey = "AIzaSyAYEB4Pku4uWRAJvQxou3Pq09tNUKbxstc";

	private const string ApiHost = "https://www.googleapis.com/youtube/v3";

	private bool _requested;

	private string _searchQuery;

	private string _nextPageToken;

	private JArray _searchData;

	private readonly Action<JObject, string> _callback;

	public YouTubeDataRequest(Action<JObject, string> callback)
	{
		_callback = callback;
	}

	public void GetVideo(string videoId)
	{
		if (_requested)
		{
			throw new Exception("YouTubeRequest instance can not be re-used for another request!");
		}
		_requested = true;
		_searchQuery = videoId;
		NameValueCollection parameters = new NameValueCollection
		{
			{ "key", "AIzaSyAYEB4Pku4uWRAJvQxou3Pq09tNUKbxstc" },
			{ "type", "video" },
			{ "part", "snippet" },
			{ "id", videoId },
			{ "maxResults", "1" }
		};
		HttpUtils.RequestJson("https://www.googleapis.com/youtube/v3/videos?" + HttpUtils.BuildQueryString(parameters), OnSearchResult);
	}

	public void SearchVideos(string query, int maxResults, string pageToken = null)
	{
		if (_requested)
		{
			throw new Exception("YouTubeRequest instance can not be re-used for another request!");
		}
		_requested = true;
		_searchQuery = query;
		NameValueCollection nameValueCollection = new NameValueCollection
		{
			{ "key", "AIzaSyAYEB4Pku4uWRAJvQxou3Pq09tNUKbxstc" },
			{ "type", "video" },
			{ "part", "snippet" },
			{ "q", query },
			{
				"maxResults",
				maxResults.ToString()
			}
		};
		if (pageToken != null)
		{
			nameValueCollection["pageToken"] = pageToken;
		}
		HttpUtils.RequestJson("https://www.googleapis.com/youtube/v3/search?" + HttpUtils.BuildQueryString(nameValueCollection), OnSearchResult);
	}

	private bool HandleErrors(JObject data, Exception ex, HttpWebResponse res)
	{
		if (ex != null)
		{
			Logger.Error<Exception>(ex);
			RunCallback(null, "unreachable");
			return true;
		}
		if (data == null)
		{
			Logger.Info<HttpStatusCode>("YouTube response is empty, status code is: {0}", res.StatusCode);
			return true;
		}
		if (data["error"] != null)
		{
			Logger.Info("YouTube responded with an error: {0}", ((object)data).ToString());
			RunCallback(null, "requestFailed");
			return true;
		}
		return false;
	}

	private void OnSearchResult(JObject data, Exception ex, HttpWebResponse res)
	{
		if (!HandleErrors(data, ex, res))
		{
			JToken obj = data["nextPageToken"];
			_nextPageToken = ((obj != null) ? Extensions.Value<string>((IEnumerable<JToken>)obj) : null);
			_searchData = data["items"].ToObject<JArray>();
			NameValueCollection parameters = new NameValueCollection
			{
				{ "key", "AIzaSyAYEB4Pku4uWRAJvQxou3Pq09tNUKbxstc" },
				{ "part", "id,contentDetails,statistics" },
				{
					"id",
					string.Join(",", ((IEnumerable<JToken>)_searchData).Select((JToken key, int value) => ((int)key[(object)"id"].Type == 8) ? Extensions.Value<string>((IEnumerable<JToken>)key[(object)"id"]) : Extensions.Value<string>((IEnumerable<JToken>)key[(object)"id"][(object)"videoId"])).ToArray())
				}
			};
			HttpUtils.RequestJson("https://www.googleapis.com/youtube/v3/videos?" + HttpUtils.BuildQueryString(parameters), OnSearchVideoResult);
		}
	}

	private void OnSearchVideoResult(JObject data, Exception ex, HttpWebResponse res)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Invalid comparison between Unknown and I4
		if (HandleErrors(data, ex, res))
		{
			return;
		}
		data["nextPageToken"] = JToken.op_Implicit(_nextPageToken);
		data["searchQuery"] = JToken.op_Implicit(_searchQuery);
		JArray val = data["items"].ToObject<JArray>();
		for (int i = 0; i < ((JContainer)val).Count; i++)
		{
			JToken val2 = val[i];
			foreach (JToken searchDatum in _searchData)
			{
				string text = (((int)searchDatum[(object)"id"].Type == 8) ? Extensions.Value<string>((IEnumerable<JToken>)searchDatum[(object)"id"]) : Extensions.Value<string>((IEnumerable<JToken>)searchDatum[(object)"id"][(object)"videoId"]));
				if (text == Extensions.Value<string>((IEnumerable<JToken>)val2[(object)"id"]))
				{
					data["items"][(object)i][(object)"snippet"] = (JToken)(object)searchDatum[(object)"snippet"].ToObject<JObject>();
					break;
				}
			}
		}
		RunCallback(data, null);
	}

	public void OnGetPopularVideos(int maxResults)
	{
		if (_requested)
		{
			throw new Exception("YouTubeRequest instance can not be re-used for another request!");
		}
		_requested = true;
		NameValueCollection parameters = new NameValueCollection
		{
			{ "key", "AIzaSyAYEB4Pku4uWRAJvQxou3Pq09tNUKbxstc" },
			{ "part", "id,contentDetails,statistics,snippet" },
			{ "chart", "mostPopular" },
			{
				"maxResults",
				maxResults.ToString()
			}
		};
		HttpUtils.RequestJson("https://www.googleapis.com/youtube/v3/videos?" + HttpUtils.BuildQueryString(parameters), delegate(JObject data, Exception ex, HttpWebResponse res)
		{
			if (!HandleErrors(data, ex, res))
			{
				RunCallback(data, null);
			}
		});
	}

	private void RunCallback(JObject data, string err)
	{
		if (!base.Disposed)
		{
			_callback(data, err);
		}
	}

	protected override void DoDispose()
	{
	}
}
