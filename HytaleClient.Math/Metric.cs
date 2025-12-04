namespace HytaleClient.Math;

internal struct Metric
{
	public AverageCollector Average;

	public long Min { get; private set; }

	public long Max { get; private set; }

	public Metric(AverageCollector averageCollector)
	{
		Min = long.MaxValue;
		Average = averageCollector;
		Max = long.MinValue;
	}

	public void Add(long value)
	{
		if (value < Min)
		{
			Min = value;
		}
		Average.Add(value);
		if (value > Max)
		{
			Max = value;
		}
	}

	public void Remove(long value)
	{
		Average.Remove(value);
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}, {4}: {5}", "Average", Average, "Min", Min, "Max", Max);
	}
}
