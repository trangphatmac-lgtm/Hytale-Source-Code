using System;

namespace Epic.OnlineServices.Auth;

[Flags]
public enum LoginFlags : ulong
{
	None = 0uL,
	NoUserInterface = 1uL
}
