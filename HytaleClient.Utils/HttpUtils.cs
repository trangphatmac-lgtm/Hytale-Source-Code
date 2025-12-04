using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using NLog;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Utils;

public static class HttpUtils
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static void RequestJson(HttpWebRequest req, JObject postData, Action<JObject, Exception, HttpWebResponse> callback)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				RequestJsonSync(req, postData, out var responseData, out var exception, out var res);
				callback(responseData, exception, res);
			}
			catch (Exception ex)
			{
				Logger.Error<Exception>(ex);
			}
		});
	}

	public static void RequestJson(string url, string method, JObject postData, Action<JObject, Exception, HttpWebResponse> callback)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				RequestJsonSync(url, method, postData, out var responseData, out var exception, out var res);
				callback(responseData, exception, res);
			}
			catch (Exception ex)
			{
				Logger.Error<Exception>(ex);
			}
		});
	}

	public static void RequestJson(string url, Action<JObject, Exception, HttpWebResponse> callback)
	{
		RequestJson(url, "GET", null, callback);
	}

	public static bool IsHttpStatusCode(Exception ex, HttpStatusCode statusCode)
	{
		if (ex is WebException)
		{
			HttpWebResponse obj = (HttpWebResponse)((WebException)ex).Response;
			return obj != null && obj.StatusCode == statusCode;
		}
		return false;
	}

	private static void RequestJsonSync(HttpWebRequest req, JObject postData, out JObject responseData, out Exception exception, out HttpWebResponse res)
	{
		if (req.Method != "GET" && postData != null)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(((object)postData).ToString());
			req.ContentLength = bytes.Length;
			try
			{
				using Stream stream = req.GetRequestStream();
				stream.Write(bytes, 0, bytes.Length);
			}
			catch (Exception ex)
			{
				responseData = null;
				exception = ex;
				res = null;
				return;
			}
		}
		HttpWebResponse httpWebResponse;
		JObject val;
		try
		{
			httpWebResponse = (HttpWebResponse)req.GetResponse();
			val = JObject.Parse(Encoding.UTF8.GetString(httpWebResponse.GetResponseStream().ReadAllBytes()));
		}
		catch (WebException ex2)
		{
			responseData = null;
			exception = ex2;
			res = (HttpWebResponse)ex2.Response;
			return;
		}
		catch (Exception ex3)
		{
			responseData = null;
			exception = ex3;
			res = null;
			return;
		}
		responseData = val;
		exception = null;
		res = httpWebResponse;
	}

	private static void RequestJsonSync(string url, string method, JObject postData, out JObject responseData, out Exception exception, out HttpWebResponse res)
	{
		RequestJsonSync(CreateRequest(url, method), postData, out responseData, out exception, out res);
	}

	public static HttpWebRequest CreateRequest(string url, string method = "GET")
	{
		HttpWebRequest httpWebRequest = WebRequest.CreateHttp(url);
		httpWebRequest.Method = method;
		httpWebRequest.ContentType = "application/json";
		httpWebRequest.Timeout = 15000;
		return httpWebRequest;
	}

	public static string BuildQueryString(NameValueCollection parameters)
	{
		StringBuilder stringBuilder = new StringBuilder(parameters.Count * 4 - 1);
		string[] allKeys = parameters.AllKeys;
		foreach (string text in allKeys)
		{
			string[] values = parameters.GetValues(text);
			foreach (string value in values)
			{
				if (stringBuilder.Length != 0)
				{
					stringBuilder.Append('&');
				}
				stringBuilder.Append(WebUtility.UrlEncode(text));
				stringBuilder.Append("=");
				stringBuilder.Append(WebUtility.UrlEncode(value));
			}
		}
		return stringBuilder.ToString();
	}
}
