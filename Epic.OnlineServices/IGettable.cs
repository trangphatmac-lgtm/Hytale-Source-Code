namespace Epic.OnlineServices;

internal interface IGettable<T> where T : struct
{
	void Get(out T other);
}
