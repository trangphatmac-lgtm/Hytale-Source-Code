namespace HytaleClient.Audio.Commands;

public enum CommandType : byte
{
	RefreshBanks,
	RegisterSoundObject,
	UnregisterSoundObject,
	SetPosition,
	SetListenerPosition,
	PostEvent,
	ActionOnEvent,
	SetRTPC
}
