namespace HytaleClient.Math;

internal struct AverageCollector
{
	public double Val { get; private set; }

	public long Count { get; private set; }

	public double AddAndGet(double v)
	{
		Add(v);
		return Val;
	}

	public void Add(double v)
	{
		long count = Count + 1;
		Count = count;
		Val = Val - Val / (double)Count + v / (double)Count;
	}

	public void Remove(double v)
	{
		if (Count == 1)
		{
			Count = 0L;
			Val = 0.0;
		}
		else if (Count > 1)
		{
			Val = (Val - v / (double)Count) / (1.0 - 1.0 / (double)Count);
			long count = Count - 1;
			Count = count;
		}
	}

	public void Reset()
	{
		Val = 0.0;
		Count = 0L;
	}

	public static double Add(double val, double v, int n)
	{
		return val - val / (double)n + v / (double)n;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}", "Val", Val, "Count", Count);
	}
}
