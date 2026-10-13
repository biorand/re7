using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Weapons;

namespace Biohazard.BioRand.RE7;

// The generation server can have more source assets than the target installation.
// Availability is a seed setting, never inferred from the server's baseline.
internal static class DlcContent {
    public const string BaseGameOnlyConfigKey = "base-game-only";

    public static bool IsAllowed(Randomizer randomizer)
        => !randomizer.GetConfigOption(BaseGameOnlyConfigKey, true);

    public static bool IsAllowed(IPatchContext context)
        => context.ExportingMod || !context.GetConfigOption(BaseGameOnlyConfigKey, true);

    public static bool IsDlcItem(string id)
        => ItemDefinitionRepository.Default.FromId(id)?.Dlc != null
           || DlcCampaignWeapons.Contains(id)
           || ItemDrops.IsBirthdaySkill(id)
           || ItemDrops.DlcCoinDrops.Any(coin => coin.Id == id);

    public static bool IsItemAllowed(Randomizer randomizer, string id)
        => IsAllowed(randomizer) || !IsDlcItem(id);
}
