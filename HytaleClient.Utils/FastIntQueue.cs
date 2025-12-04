namespace HytaleClient.Utils;

public class FastIntQueue
{
	public int Count;

	private int _maxCount;

	private int[] _values;

	private int _startIndex;

	private int _endIndex;

	public FastIntQueue(int maxCount)
	{
		_maxCount = maxCount;
		_values = new int[_maxCount];
		_startIndex = 0;
		_endIndex = 0;
	}

	public void Push(int value)
	{
		_values[_endIndex] = value;
		Count++;
		_endIndex++;
		if (_endIndex >= _maxCount)
		{
			_endIndex = 0;
		}
	}

	public int Pop()
	{
		int result = _values[_startIndex];
		Count--;
		_startIndex++;
		if (_startIndex >= _maxCount)
		{
			_startIndex = 0;
		}
		return result;
	}

	public void Clear()
	{
		Count = 0;
	}
}
