using System.Collections.Concurrent;
using System.Threading;

namespace HytaleClient.Data.BlockyModels;

internal class NodeNameManager
{
	private int _nextIndex;

	private ConcurrentDictionary<int, string> _nodeNamesByIds = new ConcurrentDictionary<int, string>();

	private ConcurrentDictionary<string, int> _nodeIdsByName = new ConcurrentDictionary<string, int>();

	public static NodeNameManager Copy(NodeNameManager other)
	{
		return new NodeNameManager
		{
			_nextIndex = other._nextIndex,
			_nodeIdsByName = new ConcurrentDictionary<string, int>(other._nodeIdsByName),
			_nodeNamesByIds = new ConcurrentDictionary<int, string>(other._nodeNamesByIds)
		};
	}

	public bool TryGetNameFromId(int nodeNameId, out string nodeName)
	{
		return _nodeNamesByIds.TryGetValue(nodeNameId, out nodeName);
	}

	public int GetOrAddNameId(string nodeName)
	{
		if (!_nodeIdsByName.TryGetValue(nodeName, out var value))
		{
			int nextIndex;
			do
			{
				nextIndex = _nextIndex;
				value = nextIndex + 1;
			}
			while (nextIndex != Interlocked.CompareExchange(ref _nextIndex, value, nextIndex));
			_nodeIdsByName[nodeName] = value;
			_nodeNamesByIds[value] = nodeName;
		}
		return value;
	}
}
