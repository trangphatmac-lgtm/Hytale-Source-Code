namespace Epic.OnlineServices.TitleStorage;

public struct FileMetadata
{
	public uint FileSizeBytes { get; set; }

	public Utf8String MD5Hash { get; set; }

	public Utf8String Filename { get; set; }

	public uint UnencryptedDataSizeBytes { get; set; }

	internal void Set(ref FileMetadataInternal other)
	{
		FileSizeBytes = other.FileSizeBytes;
		MD5Hash = other.MD5Hash;
		Filename = other.Filename;
		UnencryptedDataSizeBytes = other.UnencryptedDataSizeBytes;
	}
}
