using BaseLib;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Baselib.Patches.Content;

[HarmonyPatch(typeof(Reward))]
internal static class CustomRewardPatches
{
    internal static readonly Dictionary<RewardType, CreateRewardFromSave<CustomReward>> _RewardTypeDeserializers = [];

    public static void RegisterCustomReward(RewardType type, CreateRewardFromSave<CustomReward> deserializer)
    {
        if (_RewardTypeDeserializers.ContainsKey(type))
        {
            throw new NotSupportedException($"Registering multiple rewards of the same type ({type}) is not supported");
        }

        BaseLibMain.Logger.Info($"Registering RewardType {CustomEnums.EnumName<RewardType>((int)type)}");
        _RewardTypeDeserializers.Add(type, deserializer);
    }

    [HarmonyPatch(nameof(Reward.FromSerializable))]
    [HarmonyPrefix]
    public static bool FromSerializablePrefix(SerializableReward save, Player player, ref Reward __result)
    {
        if (_RewardTypeDeserializers.ContainsKey(save.RewardType))
        {
            BaseLibMain.Logger.Info($"Found RewardType {CustomEnums.EnumName<RewardType>((int)save.RewardType)} in registry from mod {_RewardTypeDeserializers[save.RewardType].GetType().Assembly}");

            var method = _RewardTypeDeserializers[save.RewardType];
            __result = method.Invoke(save, player);
            return false;
        }

        BaseLibMain.Logger.Warn($"No CustomReward found for RewardType {save.RewardType}, proceeding to basegame method");
        return true;
    }
}
