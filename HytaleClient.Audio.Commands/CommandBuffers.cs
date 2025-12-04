using System.Runtime.InteropServices;
using HytaleClient.Math;

namespace HytaleClient.Audio.Commands;

internal struct CommandBuffers
{
	[StructLayout(LayoutKind.Explicit)]
	public struct CommandData
	{
		[FieldOffset(0)]
		public CommandType Type;

		[FieldOffset(1)]
		public AudioDevice.SoundObjectReference SoundObjectReference;

		[FieldOffset(9)]
		public uint EventId;

		[FieldOffset(13)]
		public int PlaybackId;

		[FieldOffset(9)]
		public Vector3 WorldPosition;

		[FieldOffset(21)]
		public Vector3 WorldOrientation;

		[FieldOffset(33)]
		public byte BoolData;

		[FieldOffset(1)]
		public int TransitionDuration;

		[FieldOffset(5)]
		public byte FadeCurveType;

		[FieldOffset(6)]
		public byte ActionType;

		[FieldOffset(1)]
		public float Volume;

		[FieldOffset(5)]
		public uint RTPCId;
	}

	public const int HasSingleEventBitId = 0;

	public const int HasUniqueEventBitId = 1;

	public int Count;

	public CommandData[] Data;

	public CommandBuffers(int maxCommands)
	{
		Count = maxCommands;
		Data = new CommandData[maxCommands];
	}
}
