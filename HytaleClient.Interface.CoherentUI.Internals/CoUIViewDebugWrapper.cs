using System;
using System.Threading;
using Coherent.UI;
using Coherent.UI.Binding;

namespace HytaleClient.Interface.CoherentUI.Internals;

internal class CoUIViewDebugWrapper
{
	private readonly View _innerView;

	public CoUIViewDebugWrapper(View view)
	{
		_innerView = view;
	}

	private void AssertCoherentThread()
	{
		if (Thread.CurrentThread.Name != "CoherentUIManager")
		{
			throw new Exception("Should be called from CoherentUIManager thread!");
		}
	}

	public BoundEventHandle BindCall(string name, Delegate handler)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		AssertCoherentThread();
		return _innerView.BindCall(name, handler);
	}

	public BoundEventHandle RegisterForEvent(string name, Delegate handler)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		AssertCoherentThread();
		return _innerView.RegisterForEvent(name, handler);
	}

	public void KeyEvent(KeyEventData arg0)
	{
		AssertCoherentThread();
		_innerView.KeyEvent(arg0);
	}

	public void MouseEvent(MouseEventData arg0)
	{
		AssertCoherentThread();
		_innerView.MouseEvent(arg0);
	}

	public void TriggerEvent(string name, object data1, object data2 = null, object data3 = null, object data4 = null, object data5 = null)
	{
		AssertCoherentThread();
		ViewExtensions.TriggerEvent<object, object, object, object, object>(_innerView, name, data1, data2, data3, data4, data5);
	}

	public void UnbindCall(BoundEventHandle handle)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		AssertCoherentThread();
		_innerView.UnbindCall(handle);
	}

	public void UnregisterFromEvent(BoundEventHandle handle)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		AssertCoherentThread();
		_innerView.UnregisterFromEvent(handle);
	}

	public ImageData CreateImageData(string name, int width, int height, IntPtr data, bool flipY)
	{
		AssertCoherentThread();
		return _innerView.CreateImageData(name, width, height, data, flipY);
	}

	public void SetMasterVolume(double volume)
	{
		AssertCoherentThread();
		_innerView.SetMasterVolume(volume);
	}

	public void KillFocus()
	{
		AssertCoherentThread();
		_innerView.KillFocus();
	}

	public void SetFocus()
	{
		AssertCoherentThread();
		_innerView.SetFocus();
	}

	public void Load(string path)
	{
		AssertCoherentThread();
		_innerView.Load(path);
	}

	public void Resize(uint width, uint height)
	{
		AssertCoherentThread();
		_innerView.Resize(width, height);
	}

	public void SetZoomLevel(double zoomLevel)
	{
		AssertCoherentThread();
		_innerView.SetZoomLevel(zoomLevel);
	}

	public void Redraw()
	{
		AssertCoherentThread();
		_innerView.Redraw();
	}

	public void Destroy()
	{
		AssertCoherentThread();
		_innerView.Destroy();
	}

	public void Reload(bool ignoreCache)
	{
		AssertCoherentThread();
		_innerView.Reload(ignoreCache);
	}
}
