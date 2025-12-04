namespace Epic.OnlineServices.TitleStorage;

public struct ReadFileOptions
{
	public ProductUserId LocalUserId { get; set; }

	public Utf8String Filename { get; set; }

	public uint ReadChunkLengthBytes { get; set; }

	public OnReadFileDataCallback ReadFileDataCallback { get; set; }

	public OnFileTransferProgressCallback FileTransferProgressCallback { get; set; }
}
