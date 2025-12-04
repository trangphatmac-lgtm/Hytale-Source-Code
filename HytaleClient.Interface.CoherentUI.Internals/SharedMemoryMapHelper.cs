using System;
using System.Runtime.InteropServices;

namespace HytaleClient.Interface.CoherentUI.Internals;

internal static class SharedMemoryMapHelper
{
	[Flags]
	private enum WindowsFileMapAccessType : uint
	{
		Copy = 1u,
		Write = 2u,
		Read = 4u,
		AllAccess = 8u,
		Execute = 0x20u
	}

	private static int SHM_RDONLY = 10000;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, WindowsFileMapAccessType dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool UnmapViewOfFile(IntPtr hFileMappingObject);

	public static IntPtr DoMapSharedMemoryWindows(int handleValue, int sizeInBytes)
	{
		return MapViewOfFile((IntPtr)handleValue, WindowsFileMapAccessType.Read, 0u, 0u, (UIntPtr)(ulong)sizeInBytes);
	}

	public static void FreeMapSharedMemoryWindows(IntPtr addr)
	{
		UnmapViewOfFile(addr);
	}

	[DllImport("/usr/lib/system/libsystem_kernel.dylib")]
	private static extern IntPtr mmap(IntPtr addr, int length, int prot, int flags, int fd, ulong offset);

	[DllImport("/usr/lib/system/libsystem_kernel.dylib")]
	private static extern void munmap(IntPtr addr, int length);

	public static IntPtr DoMapSharedMemoryMacOS(int handleValue, int sizeInBytes)
	{
		IntPtr intPtr = mmap(IntPtr.Zero, sizeInBytes, 3, 1, handleValue, 0uL);
		return ((int)intPtr == -1) ? IntPtr.Zero : intPtr;
	}

	public static void FreeMapSharedMemoryMacOS(IntPtr addr, int length)
	{
		munmap(addr, length);
	}

	[DllImport("libc.so.6")]
	private static extern IntPtr shmat(int shmid, IntPtr shmaddr, int shmflg);

	[DllImport("libc.so.6")]
	private static extern int shmdt(IntPtr pointer);

	public static IntPtr DoMapSharedMemoryLinux(int handleValue)
	{
		return shmat(handleValue, IntPtr.Zero, SHM_RDONLY);
	}

	public static void FreeMapSharedMemoryLinux(IntPtr addr)
	{
		shmdt(addr);
	}
}
