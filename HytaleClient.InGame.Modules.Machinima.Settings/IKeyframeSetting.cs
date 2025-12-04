using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.InGame.Modules.Machinima.Settings;

internal interface IKeyframeSetting
{
	string Name { get; }

	Type ValueType { get; }

	JObject ToJsonObject(JsonSerializer serializer);

	IKeyframeSetting Clone();
}
