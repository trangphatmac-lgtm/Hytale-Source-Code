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

internal class TwitchDataRequest : Disposable
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const string ApiClientId = "f9lmcos9wdqjyzqz9jy6bs672msxgk6";

	private const string ApiHost = "https://api.twitch.tv/helix";

	private const string ApiHostV5 = "https://api.twitch.tv/kraken";

	private bool _requested;

	private readonly Action<JObject, string> _callback;

	public TwitchDataRequest(Action<JObject, string> callback)
	{
		_callback = callback;
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
			Logger.Info<HttpStatusCode>("Twitch response is empty, status code: {0}", res.StatusCode);
			RunCallback(null, "unreachable");
			return true;
		}
		if (data["error"] != null)
		{
			Logger.Info("Twitch responded with an error: {0}", ((object)data).ToString());
			RunCallback(null, "requestFailed");
			return true;
		}
		return false;
	}

	public void GetPopularStreams(int maxResults)
	{
		if (_requested)
		{
			throw new Exception("TwitchDataRequest instance can not be re-used for another request!");
		}
		_requested = true;
		WebHeaderCollection headers = new WebHeaderCollection { "Client-ID: f9lmcos9wdqjyzqz9jy6bs672msxgk6" };
		HttpWebRequest req = HttpUtils.CreateRequest("https://api.twitch.tv/helix/streams");
		req.Headers = headers;
		HttpUtils.RequestJson(req, null, delegate(JObject data, Exception ex, HttpWebResponse res)
		{
			if (!HandleErrors(data, ex, res))
			{
				JArray streams = Extensions.Value<JArray>((IEnumerable<JToken>)data["data"]);
				string[] value = ((IEnumerable<JToken>)streams).Select((JToken stream) => "id=" + Extensions.Value<string>((IEnumerable<JToken>)stream[(object)"game_id"])).Distinct().ToArray();
				req = HttpUtils.CreateRequest("https://api.twitch.tv/helix/games?" + string.Join("&", value));
				req.Headers = headers;
				HttpUtils.RequestJson(req, null, delegate(JObject data2, Exception ex2, HttpWebResponse res2)
				{
					if (!HandleErrors(data2, ex2, res2))
					{
						JArray val = Extensions.Value<JArray>((IEnumerable<JToken>)data2["data"]);
						foreach (JToken item in streams)
						{
							if (item[(object)"game_id"] != null)
							{
								foreach (JToken item2 in val)
								{
									if (Extensions.Value<string>((IEnumerable<JToken>)item2[(object)"id"]) == Extensions.Value<string>((IEnumerable<JToken>)item[(object)"game_id"]))
									{
										item[(object)"game_title"] = item2[(object)"name"];
										break;
									}
								}
							}
						}
						RunCallback(data, null);
					}
				});
			}
		});
	}

	public void SearchStreams(string query, int maxResults, int page)
	{
		if (_requested)
		{
			throw new Exception("TwitchDataRequest instance can not be re-used for another request!");
		}
		_requested = true;
		NameValueCollection parameters = new NameValueCollection
		{
			{ "client_id", "f9lmcos9wdqjyzqz9jy6bs672msxgk6" },
			{
				"limit",
				maxResults.ToString()
			},
			{
				"offset",
				(page * maxResults).ToString()
			},
			{ "query", query }
		};
		HttpWebRequest httpWebRequest = HttpUtils.CreateRequest("https://api.twitch.tv/kraken/search/streams?" + HttpUtils.BuildQueryString(parameters));
		httpWebRequest.Accept = "application/vnd.twitchtv.v5+json";
		HttpUtils.RequestJson(httpWebRequest, null, delegate(JObject data, Exception ex, HttpWebResponse res)
		{
			if (!HandleErrors(data, ex, res))
			{
				data["searchQuery"] = JToken.op_Implicit(query);
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
