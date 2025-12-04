using System;
using System.Collections.Generic;
using System.Diagnostics;
using HytaleClient.Utils;

namespace HytaleClient.Application.Services.Api;

internal class Server
{
	private static readonly Comparison<Server> NameComparison = (Server s1, Server s2) => string.Compare(s1.Name, s2.Name, StringComparison.OrdinalIgnoreCase);

	private static readonly Comparison<Server> RatingComparison = (Server s1, Server s2) => string.Compare("", "", StringComparison.OrdinalIgnoreCase);

	private static readonly Comparison<Server> OnlinePlayersComparison = (Server s1, Server s2) => s2.OnlinePlayers.CompareTo(s1.OnlinePlayers);

	public static readonly Comparison<Server> NameSort = ComparisonUtils.Compose<Server>(NameComparison, OnlinePlayersComparison, RatingComparison);

	public static readonly Comparison<Server> RatingSort = ComparisonUtils.Compose<Server>(RatingComparison, OnlinePlayersComparison, NameComparison);

	public static readonly Comparison<Server> OnlinePlayersSort = ComparisonUtils.Compose<Server>(OnlinePlayersComparison, NameComparison, RatingComparison);

	public bool IsLan;

	public Guid UUID;

	public string Name;

	public string Description;

	public string ImageUrl;

	public string Version;

	public List<string> Tags;

	public List<string> Languages;

	public string Host;

	public int OnlinePlayers;

	public int MaxPlayers;

	public int Ping;

	public bool IsOnline;

	public bool IsFavorite;

	public Stopwatch Updated = Stopwatch.StartNew();

	protected bool Equals(Server other)
	{
		return IsLan == other.IsLan && string.Equals(Name, other.Name) && string.Equals(Host, other.Host);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((Server)obj);
	}

	public override int GetHashCode()
	{
		int hashCode = IsLan.GetHashCode();
		hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
		return (hashCode * 397) ^ (Host?.GetHashCode() ?? 0);
	}
}
