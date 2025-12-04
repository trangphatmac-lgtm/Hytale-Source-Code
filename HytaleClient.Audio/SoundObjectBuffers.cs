using HytaleClient.Math;

namespace HytaleClient.Audio;

internal struct SoundObjectBuffers
{
	public const int HasSingleEventBitId = 0;

	public const int HasUniqueEventBitId = 1;

	public const int IsLiveBitId = 2;

	public int Count;

	public uint[] SoundObjectId;

	public Vector3[] Position;

	public Vector3[] FrontOrientation;

	public Vector3[] TopOrientation;

	public int[] LastPlaybackId;

	public byte[] BoolData;

	public SoundObjectBuffers(int maxSoundObjects)
	{
		Count = maxSoundObjects;
		SoundObjectId = new uint[maxSoundObjects];
		Position = new Vector3[maxSoundObjects];
		FrontOrientation = new Vector3[maxSoundObjects];
		TopOrientation = new Vector3[maxSoundObjects];
		LastPlaybackId = new int[maxSoundObjects];
		BoolData = new byte[maxSoundObjects];
	}
}
