using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HytaleClient.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Application.Services.Api;

internal class HytaleServicesApiClient
{
	public enum SortOrder
	{
		Ascending,
		Descending
	}

	public enum ServerSortField
	{
		Ping,
		PlayerCount,
		Name
	}

	public struct FetchPublicServerQuery
	{
		public string Name;

		public int? SlotsMin;

		public int? SlotsMax;

		public string[] Tags;

		public string[] Languages;
	}

	private const string BaseUrl = "https://stable.dal-dev.launcherapi.hytale.dev/internalapi/v1alpha1";

	private readonly HttpClient _client;

	public HytaleServicesApiClient()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		_client = new HttpClient();
		_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Static", "7531cca5-d568-486f-91e9-0d8aad17d249");
	}

	public async Task<Server[]> FetchPublicServers(FetchPublicServerQuery query, int size = 50, SortOrder sortOrder = SortOrder.Ascending, ServerSortField sortField = ServerSortField.Name)
	{
		NameValueCollection parameters = new NameValueCollection
		{
			["size"] = size.ToString(),
			["sort.order"] = ((sortOrder == SortOrder.Ascending) ? "ASC" : "DESC")
		};
		switch (sortField)
		{
		case ServerSortField.Name:
			parameters["sort.field"] = "SERVER_NAME";
			break;
		case ServerSortField.Ping:
			parameters["sort.field"] = "PING";
			break;
		case ServerSortField.PlayerCount:
			parameters["sort.field"] = "PLAYER_COUNT";
			break;
		}
		if (query.Name != null)
		{
			parameters["query.name"] = query.Name;
		}
		if (query.SlotsMin.HasValue)
		{
			parameters["query.slotsMin"] = query.SlotsMin.Value.ToString();
		}
		if (query.SlotsMax.HasValue)
		{
			parameters["query.slotsMax"] = query.SlotsMax.Value.ToString();
		}
		if (query.Tags != null)
		{
			string[] tags = query.Tags;
			foreach (string tag in tags)
			{
				parameters.Add("query.tags", tag);
			}
		}
		if (query.Languages != null)
		{
			string[] languages = query.Languages;
			foreach (string language in languages)
			{
				parameters.Add("query.languages", language);
			}
		}
		Uri uri = new Uri("https://stable.dal-dev.launcherapi.hytale.dev/internalapi/v1alpha1/servers?" + HttpUtils.BuildQueryString(parameters));
		JObject json = DeserializeFromStream(await (await _client.GetAsync(uri)).Content.ReadAsStreamAsync());
		JToken serversJson = json["servers"];
		Server[] servers = new Server[((IEnumerable<JToken>)serversJson).Count()];
		for (int i = 0; i < servers.Length; i++)
		{
			servers[i] = CreateServerObjectFromJson((JObject)serversJson[(object)i]);
		}
		return servers;
	}

	private async Task<Server[]> FetchUserServers(Guid userUuid, string listType)
	{
		string[] obj = new string[5] { "https://stable.dal-dev.launcherapi.hytale.dev/internalapi/v1alpha1/users/", null, null, null, null };
		Guid guid = userUuid;
		obj[1] = guid.ToString();
		obj[2] = "/";
		obj[3] = listType;
		obj[4] = "/servers";
		Uri uri = new Uri(string.Concat(obj));
		HttpResponseMessage res = await _client.GetAsync(uri);
		res.EnsureSuccessStatusCode();
		JObject serverData = DeserializeFromStream(await res.Content.ReadAsStreamAsync());
		JToken serversJson = serverData["servers"];
		Server[] servers = new Server[((IEnumerable<JToken>)serversJson).Count()];
		for (int i = 0; i < servers.Length; i++)
		{
			servers[i] = CreateServerObjectFromJson((JObject)serversJson[(object)i]);
		}
		return servers;
	}

	public async Task<Server[]> FetchFavoriteServers(Guid userUuid)
	{
		return await FetchUserServers(userUuid, "favorites");
	}

	public async Task<Server[]> FetchRecentServers(Guid userUuid)
	{
		return await FetchUserServers(userUuid, "recent");
	}

	public async Task AddServerToRecents(Guid serverUuid, Guid userUuid)
	{
		await UpdateUserListServer(serverUuid, userUuid, "recent", HttpMethod.Put);
	}

	public async Task AddServerToFavorites(Guid serverUuid, Guid userUuid)
	{
		await UpdateUserListServer(serverUuid, userUuid, "favorites", HttpMethod.Put);
	}

	public async Task RemoveServerFromFavorites(Guid serverUuid, Guid userUuid)
	{
		await UpdateUserListServer(serverUuid, userUuid, "favorites", HttpMethod.Delete);
	}

	public async Task RemoveServerFromRecents(Guid serverUuid, Guid userUuid)
	{
		await UpdateUserListServer(serverUuid, userUuid, "recent", HttpMethod.Delete);
	}

	private async Task UpdateUserListServer(Guid serverUuid, Guid userUuid, string list, HttpMethod method)
	{
		Uri uri = new Uri(string.Format("{0}/users/{1}/{2}/servers/{3}", "https://stable.dal-dev.launcherapi.hytale.dev/internalapi/v1alpha1", userUuid, list, serverUuid));
		StringContent httpContent = new StringContent("", Encoding.UTF8, "application/json");
		(await _client.SendAsync(new HttpRequestMessage(method, uri)
		{
			Content = (HttpContent)(object)httpContent
		})).EnsureSuccessStatusCode();
	}

	public async Task<Server> FetchServerDetails(Guid serverUuid)
	{
		Uri uri = new Uri(string.Format("{0}/servers/{1}", "https://stable.dal-dev.launcherapi.hytale.dev/internalapi/v1alpha1", serverUuid));
		JObject json = DeserializeFromStream(await (await _client.GetAsync(uri)).Content.ReadAsStreamAsync());
		return CreateServerObjectFromJson(json);
	}

	private static JObject DeserializeFromStream(Stream stream)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected O, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		using StreamReader streamReader = new StreamReader(stream);
		JsonTextReader val = new JsonTextReader((TextReader)streamReader);
		try
		{
			return (JObject)JToken.ReadFrom((JsonReader)(object)val);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static Server CreateServerObjectFromJson(JObject serverJson)
	{
		Server server = new Server
		{
			UUID = Guid.Parse((string)serverJson["serverUuid"][(object)"hexString"]),
			Name = (string)serverJson["name"],
			Description = (string)serverJson["description"],
			OnlinePlayers = (int)serverJson["playerCount"],
			MaxPlayers = (int)serverJson["slots"],
			Host = (string)serverJson["ip"],
			Version = (string)serverJson["version"],
			ImageUrl = (string)serverJson["imageUrl"]
		};
		server.Tags = new List<string>();
		foreach (JToken item3 in (IEnumerable<JToken>)serverJson["tags"])
		{
			string item = (string)item3;
			server.Tags.Add(item);
		}
		JToken val = default(JToken);
		if (serverJson.TryGetValue("online", ref val))
		{
			server.IsOnline = (bool)val;
		}
		JToken val2 = default(JToken);
		if (serverJson.TryGetValue("languages", ref val2))
		{
			server.Languages = new List<string>();
			foreach (JToken item4 in (IEnumerable<JToken>)val2)
			{
				string item2 = (string)item4;
				server.Languages.Add(item2);
			}
		}
		return server;
	}
}
