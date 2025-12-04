using HytaleClient.InGame;

namespace HytaleClient.Data.ClientInteraction.Selector;

internal delegate bool LineOfSightProvider(GameInstance gameInstance, float fromX, float fromY, float fromZ, float toX, float toY, float toZ);
