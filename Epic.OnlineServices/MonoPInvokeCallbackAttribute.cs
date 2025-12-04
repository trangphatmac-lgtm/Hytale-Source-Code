using System;

namespace Epic.OnlineServices;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class MonoPInvokeCallbackAttribute : Attribute
{
	public MonoPInvokeCallbackAttribute(Type type)
	{
	}
}
