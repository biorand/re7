using app;
using Biohazard.BioRand.RE7.REEngine;
using Enums.app.GameManager;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class ChapterJumpDataModifier : Modifier {
    private readonly Randomizer _randomizer;

    public ChapterJumpDataModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    public const string StartChapterConfigKey = "start-chapter";
    public const string NormalStartChapter = "Normal";

    private readonly string _path = "scenes/chapterjumpdata/chapterjumpdata.scn".SceneFile();
    private readonly Guid ChapterJumpData_c01 = new("88045366-0683-481a-8b9a-1d8c59aa048a");

    public static IReadOnlyList<StartChapterOption> StartChapterOptions { get; } =[
        new(NormalStartChapter, null),
        new("Main House", ChapterNo.Chapter3),
        new("Wrecked Ship", ChapterNo.Chapter4)
    ];

    public override void LogState(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var transitions = randomizer.FileRepository.GetScnFile(_path)
            .ReadScene(randomizer.FileRepository.TypeRepository);
        transitions.GetGameObjects().ForEach(go => {
            ChapterJumpData? chapterJumpData;
            if ((chapterJumpData = go.FindComponent<ChapterJumpData>()) != null) {
                logger.LogLine(
                    $"Chapter jump data '{chapterJumpData.JumpPositionName}': {chapterJumpData.JumpChapter.ToReadableString()} " +
                    $" (Enabled: {chapterJumpData.Enabled}, get player pos: {chapterJumpData.IsGetPlayerPos})");
            }
        });
    }

    private void ApplyStartChapter(Randomizer randomizer, RandomizerLogger logger, StartChapterOption startChapter) {
        if (startChapter.Chapter == null) {
            return;
        }

        randomizer.FileRepository.ModifyScnFile(_path, scene => {
            var go = scene.FindGameObject(ChapterJumpData_c01)!;
            var jumpData = go.FindComponent<ChapterJumpData>()!;
            jumpData.JumpChapter = startChapter.Chapter.Value;
            go = go.AddOrUpdateComponent(jumpData);
            scene = scene.UpdateGameObject(go);
            return scene;
        });

        logger.LogLine($"Start chapter: {startChapter.Name} ({startChapter.Chapter.Value.ToReadableString()})");
    }

    private static StartChapterOption GetStartChapterOption(string? name)
        => StartChapterOptions.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.Ordinal)) ??
           StartChapterOptions[0];

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        if (randomizer.GetConfigOption<bool>("shuffle-chapters")) {
            throw new IntelOrca.Biohazard.BioRand.RandomizerUserException(
                "Chapter shuffling is unavailable because it can break progression and VHS returns. Disable Shuffle Chapters in this profile.");
        }
        var startChapter = GetStartChapterOption(randomizer.GetConfigOption<string>(StartChapterConfigKey));
        var hasConfiguredStart = startChapter.Chapter != null;

        if (hasConfiguredStart) {
            ApplyStartChapter(randomizer, logger, startChapter);
        }
    }

    public sealed record StartChapterOption(string Name, ChapterNo? Chapter);
}
