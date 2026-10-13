using Biohazard.BioRand.RE7.Items;

namespace Biohazard.BioRand.RE7.Services;

internal class StaticItemRandomizationService {
    private const string RandomizerKey = "modifier/static-items";

    public Rng Rng { get; }
    public RandomItemSettings RandomItemSettings { get; }

    public StaticItemRandomizationService(Randomizer randomizer) {
        Rng = randomizer.GetRng(RandomizerKey);
        RandomItemSettings = new RandomItemSettings(){
            MinAmmoQuantity = randomizer.GetConfigOption("item-drop-ammo-min", 0.1),
            MaxAmmoQuantity = randomizer.GetConfigOption("item-drop-ammo-max", 1.0),
            ItemRatioKeyFunc = id => randomizer.GetConfigOption<double>($"item-drop-ratio-{ItemDrops.GetConfigId(id)}")
        };
    }
}
