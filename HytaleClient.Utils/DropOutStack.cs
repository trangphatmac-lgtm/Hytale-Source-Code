using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HytaleClient.Utils;

public class DropOutStack<T> : IEnumerable<T>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__11 : IEnumerator<T>, IDisposable, IEnumerator
	{
		private int _003C_003E1__state;

		private T _003C_003E2__current;

		public DropOutStack<T> _003C_003E4__this;

		private int _003Ci_003E5__1;

		T IEnumerator<T>.Current
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
				_003Ci_003E5__1 = 0;
				break;
			case 1:
				_003C_003E1__state = -1;
				_003Ci_003E5__1++;
				break;
			}
			if (_003Ci_003E5__1 < _003C_003E4__this._count)
			{
				_003C_003E2__current = _003C_003E4__this._array[_003Ci_003E5__1];
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

	private readonly T[] _array;

	private int _top = 0;

	private int _count = 0;

	public int Count => _count;

	public DropOutStack(int capacity)
	{
		_array = new T[capacity];
	}

	public void Push(T item)
	{
		if (_count < _array.Length)
		{
			_count++;
		}
		_array[_top] = item;
		_top = (_top + 1) % _array.Length;
	}

	public T Pop()
	{
		if (_count == 0)
		{
			return default(T);
		}
		_count--;
		_top = (_array.Length + _top - 1) % _array.Length;
		T result = _array[_top];
		_array[_top] = default(T);
		return result;
	}

	public T Peek()
	{
		return _array[(_array.Length + _top - 1) % _array.Length];
	}

	public T PeekAt(int index)
	{
		if (_count == 0)
		{
			return default(T);
		}
		if (_count < _array.Length)
		{
			return _array[index];
		}
		return _array[(_array.Length + _top + index) % _array.Length];
	}

	public void Clear()
	{
		for (int i = 0; i < _array.Length; i++)
		{
			_array[i] = default(T);
		}
		_top = 0;
		_count = 0;
	}

	[IteratorStateMachine(typeof(DropOutStack<>._003CGetEnumerator_003Ed__11))]
	public IEnumerator<T> GetEnumerator()
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
