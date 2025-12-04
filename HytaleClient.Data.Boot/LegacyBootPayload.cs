using System;
using System.Runtime.Serialization;
using Utf8Json;
using Utf8Json.Resolvers;

namespace HytaleClient.Data.Boot;

[Obsolete]
public class LegacyBootPayload
{
	[DataMember(Name = "javaExecutableLocation")]
	public string JavaExecutable { get; set; }

	[DataMember(Name = "serverPath")]
	public string ServerJar { get; set; }

	[DataMember(Name = "assetsPath")]
	public string AssetsDirectory { get; set; }

	[DataMember(Name = "customServerArguments")]
	public string CustomServerArguments { get; set; } = string.Empty;


	public static LegacyBootPayload Parse(string json)
	{
		return JsonSerializer.Deserialize<LegacyBootPayload>(json, StandardResolver.CamelCase);
	}
}
