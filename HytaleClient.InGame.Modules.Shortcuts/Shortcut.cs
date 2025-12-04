using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Shortcuts;

internal abstract class Shortcut
{
	public string Name { get; protected set; }

	public string Command { get; private set; }

	public Shortcut(string name, string command)
	{
		Name = name;
		Command = command;
	}

	public JObject ToJsonObject()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		JObject val = new JObject();
		val.Add("name", JToken.FromObject((object)Name));
		val.Add("command", JToken.FromObject((object)Command));
		return val;
	}
}
