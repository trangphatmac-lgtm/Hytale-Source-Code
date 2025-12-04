using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HytaleClient.Utils;

public class SpiralIterator : IEnumerable<long>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__11 : IEnumerator<long>, IDisposable, IEnumerator
	{
		private int _003C_003E1__state;

		private long _003C_003E2__current;

		public SpiralIterator _003C_003E4__this;

		private long _003CchunkCoordinates_003E5__1;

		private int _003Ctemp_003E5__2;

		long IEnumerator<long>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetEnumerator_003Ed__11(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				if (!_003C_003E4__this._isSetup)
				{
					throw new Exception("SpiralIterator is not setup!");
				}
				break;
			case 1:
				_003C_003E1__state = -1;
				break;
			}
			if (_003C_003E4__this._i < _003C_003E4__this._maxI)
			{
				if (!_003C_003E4__this._isSetup)
				{
					throw new Exception("SpiralIterator is not setup!");
				}
				_003CchunkCoordinates_003E5__1 = ChunkHelper.IndexOfChunkColumn(_003C_003E4__this._chunkX + _003C_003E4__this._x, _003C_003E4__this._chunkZ + _003C_003E4__this._z);
				if (_003C_003E4__this._x == _003C_003E4__this._z || (_003C_003E4__this._x < 0 && _003C_003E4__this._x == -_003C_003E4__this._z) || (_003C_003E4__this._x > 0 && _003C_003E4__this._x == 1 - _003C_003E4__this._z))
				{
					_003Ctemp_003E5__2 = _003C_003E4__this._dx;
					_003C_003E4__this._dx = -_003C_003E4__this._dz;
					_003C_003E4__this._dz = _003Ctemp_003E5__2;
				}
				_003C_003E4__this._x += _003C_003E4__this._dx;
				_003C_003E4__this._z += _003C_003E4__this._dz;
				_003C_003E4__this._i++;
				_003C_003E2__current = _003CchunkCoordinates_003E5__1;
				_003C_003E1__state = 1;
				return true;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private bool _isSetup;

	private int _chunkX;

	private int _chunkZ;

	private int _maxI;

	private int _i;

	private int _x;

	private int _z;

	private int _dx;

	private int _dz;

	public void Initialize(int chunkX, int chunkZ, int radius)
	{
		_chunkX = chunkX;
		_chunkZ = chunkZ;
		int num = 1 + radius * 2;
		_maxI = num * num;
		_i = 0;
		_x = (_z = 0);
		_dx = 0;
		_dz = -1;
		_isSetup = true;
	}

	public void Reset()
	{
		_isSetup = false;
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__11))]
	public IEnumerator<long> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__11(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
