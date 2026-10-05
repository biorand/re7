using Biohazard.BioRand.RE7;
using IntelOrca.Biohazard.BioRand.REE;

internal class UvarDefaultsModifier : Modifier {
    private readonly Randomizer _randomizer;

    public UvarDefaultsModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private void ApplyRecipeUnlocks(Randomizer randomizer) {
        if (randomizer.DebugRecipes.Enabled || !randomizer.GetConfigOption<bool>("recipes-unlock-from-start"))
            return;

        // Immediately unlock the combine menu right from the start
        var patches = new List<(string Guid, bool NewValue)>(){
            // Combine_Flag
            ("38208fea-638c-4d54-ac9c-8d05a31436dd", true), // cmb_releasable_RemedyS
            ("d5c61cc1-5fc3-42bd-a247-a0673c3dc1b8", true), // cmb_enable_RemedyS
            ("d8e59fe1-a257-4a78-8574-d20f5ad35e1d", true), // cmb_enable_Eye
            ("66203bf4-f916-42bc-af44-05990479f5e6", true), // cmb_enable_Fuel
            ("5135aae4-9684-45b3-bd37-9edfbceaf054", true), // cmb_enable_Gunpowder
            ("c4256fad-6e47-44e7-9736-54868dfd4214", true), // cmb_enable_Strength
            ("2d37e99b-b701-4f78-b0a7-1c7c7bc3df68", true), // cmb_enable_Sparekey
            ("79aba106-1ebe-44ca-93f3-9ee34b118137", true), // cmb_enable_Plasticexplosive
            ("eab670f7-4475-4258-b900-cbe91664c9a3", true), // cmb_enable_DybbukMedicine

            // UI_Flag
            ("b92f584f-c686-480c-9ca5-27048348efa5", true), // EnableCombine
            ("0ab3a430-7183-4863-b8e3-5b8f4bdee557", true), // RecipeGetCnt
            ("419a0691-1219-4447-b927-e31ac6e35486", true), // UnlockedDictionaryCombine
        };

        patches.ForEach(p => randomizer.FlagService.SetFlag(new Guid(p.Guid), p.NewValue));
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        ApplyRecipeUnlocks(randomizer);
    }
}
