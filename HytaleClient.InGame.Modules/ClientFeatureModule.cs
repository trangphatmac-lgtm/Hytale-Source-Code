using System;
using System.Collections.Generic;
using System.Linq;
using HytaleClient.Protocol;

namespace HytaleClient.InGame.Modules;

internal class ClientFeatureModule : Module
{
	private Dictionary<ClientFeature, bool> features = new Dictionary<ClientFeature, bool>();

	public ClientFeatureModule(GameInstance gameInstance)
		: base(gameInstance)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		foreach (ClientFeature item in Enum.GetValues(typeof(ClientFeature)).Cast<ClientFeature>())
		{
			features.Add(item, value: false);
		}
	}

	public bool IsFeatureEnabled(ClientFeature feature)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		features.TryGetValue(feature, out var value);
		return value;
	}

	public void SetFeatureEnabled(ClientFeature feature, bool enabled)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		features[feature] = enabled;
		_gameInstance.App.Interface.SettingsComponent.Build();
	}
}
