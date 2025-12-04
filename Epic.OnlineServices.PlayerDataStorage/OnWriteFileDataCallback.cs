using System;

namespace Epic.OnlineServices.PlayerDataStorage;

public delegate WriteResult OnWriteFileDataCallback(ref WriteFileDataCallbackInfo data, out ArraySegment<byte> outDataBuffer);
