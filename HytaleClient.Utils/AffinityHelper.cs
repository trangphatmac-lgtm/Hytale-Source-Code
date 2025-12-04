using System;
using System.Diagnostics;
using System.Threading;
using HytaleClient.Math;
using NLog;

namespace HytaleClient.Utils;

internal static class AffinityHelper
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private const float SingleplayerClientAffinityRatio = 0.43f;

	private static readonly IntPtr DefaultClientAffinity = (IntPtr)(1L << Environment.ProcessorCount) - 1;

	private static readonly int SingleplayerClientProcessorCount = MathHelper.Round((float)Environment.ProcessorCount * 0.43f);

	private static readonly int SingleplayerServerProcessorCount = MathHelper.Round((float)Environment.ProcessorCount * 0.57f);

	private static readonly IntPtr SingleplayerClientAffinity = (IntPtr)((1L << SingleplayerClientProcessorCount) - 1);

	private static readonly IntPtr SingleplayerServerAffinity = (IntPtr)((1L << SingleplayerServerProcessorCount) - 1 << SingleplayerClientProcessorCount);

	private static readonly bool Enabled = Environment.ProcessorCount <= 8;

	public static void Setup()
	{
		Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
		if (!OptionsHelper.DisableAffinity && Enabled)
		{
			SetupDefaultAffinity();
		}
	}

	public static void SetupDefaultAffinity()
	{
		if (!OptionsHelper.DisableAffinity && Enabled)
		{
			Logger.Info("Setup Default Affinity");
			Process.GetCurrentProcess().ProcessorAffinity = DefaultClientAffinity;
		}
	}

	public static void SetupSingleplayerAffinity(Process serverProcess)
	{
		if (!OptionsHelper.DisableAffinity && Enabled)
		{
			Logger.Info("Setup Singleplayer Affinity");
			Process.GetCurrentProcess().ProcessorAffinity = SingleplayerClientAffinity;
			serverProcess.ProcessorAffinity = SingleplayerServerAffinity;
		}
	}
}
