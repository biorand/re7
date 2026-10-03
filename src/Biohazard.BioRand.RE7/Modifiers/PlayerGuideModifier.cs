using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;

namespace Biohazard.BioRand.RE7.Modifiers;

internal sealed class PlayerGuideModifier(Randomizer randomizer) : Modifier {
    internal const string IconPath = "natives/stm/ui/ui0100/tex/ui0101_01_iam.tex.35";
    internal const string Title = "! BioRand Survival Guide";
    internal const string Description = "Read this before playing.\r\nImportant tips for progression,\r\ninventory space and saving.";
    internal static readonly string Guide = """
        Do not skip the deputy's window cutscene before
        the garage fight with Jack.

        Do not skip the morgue puzzle before the
        Scissor Jack boss fight.

        Visit the room beneath the Old House fireplace
        for the Mia/Lucas cutscene before progressing
        further through the Old House.
        <PAGE>Lucas no longer takes your items before the puzzle
        room. Leave enough inventory space for its items.

        By default, Madhouse checkpoints are enabled
        and saving does not require cassette tapes.

        Still stuck? Report your seed, room and objective,
        and keep the affected save for investigation.
        """.ReplaceLineEndings("\r\n");

    public override void Apply(RandomizerLogger logger) {
        // Run after randomized dialogue so this information is always available,
        // including when message randomization is disabled. English is the fallback
        // for languages without a translated BioRand guide.
        randomizer.FileRepository.ModifyMsgFile("message/ui_item_mes.msg".MessageFile(), messages => {
            messages.SetStringAll("Item_002", Title);
            messages.SetStringAll("Item_002_Desc", Description);
        });
        randomizer.FileRepository.ModifyMsgFile("message/ui_file_mes.msg".MessageFile(), messages => {
            messages.SetStringAll("File_Title_063", Title);
            messages.SetStringAll("File_Title_063_Desc", Guide);
        });
        randomizer.FileRepository.SetFile(IconPath, EmbeddedData.GetFile("player-guide-icon.tex.35"));
        logger.LogLine("Replaced Mia's email with the BioRand survival guide.");
    }
}
