using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HytaleClient.Utils;

internal static class HostnameHelper
{
	public static bool TryParseHostname(string uri, int defaultPort, out string host, out int port, out string error)
	{
		uri = uri.Trim();
		if (uri.Length <= 1)
		{
			host = null;
			port = -1;
			error = "The hostname is too short.";
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = string.Empty;
		bool flag = false;
		string text2 = uri;
		foreach (char c in text2)
		{
			if (string.IsNullOrEmpty(text) && c == ':')
			{
				text = stringBuilder.ToString();
				flag = true;
			}
			stringBuilder.Append(c);
			if (flag && c == ']')
			{
				break;
			}
		}
		if (flag)
		{
			if (IPAddress.TryParse(stringBuilder.ToString(), out IPAddress address) && address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				host = "[" + address.ToString().Split(new char[1] { '%' })[0] + "]";
				uri = uri.Substring(stringBuilder.Length);
			}
			else
			{
				host = text.ToLowerInvariant();
				uri = uri.Substring(host.Length);
			}
		}
		else
		{
			host = stringBuilder.ToString().ToLowerInvariant();
			uri = uri.Substring(host.Length);
		}
		if (string.IsNullOrEmpty(host) || Uri.CheckHostName(host) == UriHostNameType.Unknown)
		{
			port = -1;
			error = "The hostname could not be parsed.";
			return false;
		}
		if (uri.Length != 0 && uri[0] == ':')
		{
			uri = uri.Substring(1);
			stringBuilder = new StringBuilder();
			string text3 = uri;
			foreach (char c2 in text3)
			{
				if (!char.IsDigit(c2))
				{
					port = -1;
					error = "Invalid port specified.";
					return false;
				}
				stringBuilder.Append(c2);
			}
			if (stringBuilder.Length != 0)
			{
				if (!uint.TryParse(stringBuilder.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result > 65535)
				{
					port = -1;
					error = "Invalid port number.";
					return false;
				}
				port = (int)result;
			}
			else
			{
				port = defaultPort;
			}
		}
		else
		{
			port = defaultPort;
		}
		error = null;
		return true;
	}
}
