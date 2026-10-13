using Biohazard.BioRand.RE7.REEngine;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Enemies.Impl;

internal class MargeMutated : IEnemyDefinition {
    internal static readonly IReadOnlyList<EnemyHealthPart> PartHealth =[
        new("MargeMutated", "Marguerite Baker (Mutated)", 15000),
        new("MargeMutated-escape-resist", "Escape Resist", 1100),
        new("MargeMutated-wall-move-resist", "Wall Move Resist", 900),
        new("MargeMutated-sneak-grapple-resist", "Sneak Grapple Resist", 300),
    ];

    public string Id => "MargeMutated";

    public EnemyID EnemyId => EnemyID.Em3600;

    public EnemyCategory Category => EnemyCategory.Marguerite;

    public string Name => "Marguerite Baker (Mutated)";

    public bool IsBoss => true;

    public int BaseHealth => 15000;

    public IReadOnlyList<EnemyHealthPart> HealthParts => PartHealth;

    public List<string> RcolPaths =>[
        "collision/collider/enemy/em3600/em3600.rcol".RcolFile(),
        "collision/collider/enemy/em3600/em3600shell.rcol".RcolFile()
    ];

    public string DirectivesHolderPath
        => "prefab/character/em3600/em3600directivesholder.user".UserFile();

    public string ResistParamsHolderPath
        => "prefab/character/em3600/em3600resistparameterholder.user".UserFile();

    public string OriginalPrefabPath
        => BaseGameEnemyTemplate.MargeScenePath;

    public bool UsesEnemyGenerator => true;

    public bool SupportsSpeedRandomization => true;
}

internal class MargeMutatedDirectiveModifier : IDirectiveModifier {
    private const string ResistFolder = "prefab/character/em3600/resistparameters";

    private static readonly IReadOnlyDictionary<string, string> ResistHealthPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){
            ["MargeMutated-escape-resist"] = "units[0].parts[0].healthMax",
            ["MargeMutated-wall-move-resist"] = "units[1].parts[0].healthMax",
            ["MargeMutated-sneak-grapple-resist"] = "units[2].parts[0].healthMax",
        };

    public bool Supports(IEnemyDefinition enemy)
        => enemy.EnemyId == EnemyID.Em3600;

    public void Apply(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        var applySpeed = enemy.ShouldRandomizeSpeed(randomizer);
        var applyHealth = enemy.ShouldRandomizeHealth(randomizer);

        if (!applySpeed && !applyHealth) {
            logger.LogSkip("Boss health and enemy speed randomization are disabled.");
            return;
        }

        if (applySpeed) {
            ApplySpeed(enemy, randomizer, logger);
        } else {
            logger.LogSkip("Enemy speed randomization is disabled.");
        }

        if (applyHealth) {
            ApplyResistHealth(enemy, randomizer, logger);
        } else {
            logger.LogSkip("Boss health randomization is disabled.");
        }
    }

    private void ApplySpeed(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        var newSpeed = enemy.GetSpeedMultiplier(randomizer);
        logger.LogMultiplier("Speed multiplier", newSpeed);

        var holder =
            randomizer.FileRepository.DeserializeUserFile<app.Em3600DirectivesHolder>(enemy.DirectivesHolderPath);
        foreach (var directive in holder.holder.Units) {
            var rank = directive.Rank;
            var userFilePath = directive.Directive.Path.UserFile();

            logger.LogDirectiveFile(rank, userFilePath, () =>
                randomizer.FileRepository.ModifyUserFile<app.Em3600Directive>(
                    userFilePath,
                    d => ModifyDirective(d, logger, newSpeed)
                ));
        }
    }

    private static void ApplyResistHealth(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        var rng = randomizer.GetRng("enemy/em3600/health");
        var healthValues = enemy.HealthParts
            .Where(part => ResistHealthPaths.ContainsKey(part.ConfigId))
            .ToDictionary(
                part => part.ConfigId,
                part => enemy.GetHealth(randomizer, rng, part),
                StringComparer.OrdinalIgnoreCase);

        foreach (var part in enemy.HealthParts.Where(part => healthValues.ContainsKey(part.ConfigId))) {
            logger.LogHealthAssignment(part.Label, part.BaseHealth, healthValues[part.ConfigId]);
        }

        foreach (var (label, path) in GetResistFiles()) {
            logger.LogDirectiveFile(label, path, () => randomizer.FileRepository.ModifyUserFile(path,
                resistParameter => {
                    foreach (var part in enemy.HealthParts.Where(part => healthValues.ContainsKey(part.ConfigId))) {
                        var fieldPath = ResistHealthPaths[part.ConfigId];
                        var oldHealth = resistParameter.Get<float>(fieldPath);
                        var newHealth = healthValues[part.ConfigId];
                        resistParameter = resistParameter.Set(fieldPath, newHealth);
                        logger.LogChange(part.Label, oldHealth, newHealth);
                    }

                    return resistParameter;
                }));
        }
    }

    private static IEnumerable<(string Label, string Path)> GetResistFiles() {
        yield return ("Default", "prefab/character/em3600/em3600resistparameter.user".UserFile());
        yield return ("Easy", $"{ResistFolder}/em3600resistparameter_easy.user".UserFile());
        yield return ("Normal", $"{ResistFolder}/em3600resistparameter_normal.user".UserFile());
        yield return ("Hard", $"{ResistFolder}/em3600resistparameter_hard.user".UserFile());
        yield return ("Harder", $"{ResistFolder}/em3600resistparameter_harder.user".UserFile());
        yield return ("Hardest", $"{ResistFolder}/em3600resistparameter_hardest.user".UserFile());
    }

    private app.Em3600Directive ModifyDirective(
        app.Em3600Directive directive,
        RandomizerLogger logger,
        float speed) {
        var oldNormalAttackInterval = directive.MyCommonParam.NormalAttackIntervalTime;
        directive.MyCommonParam.NormalAttackIntervalTime /= speed;
        logger.LogChange("Normal attack interval", oldNormalAttackInterval,
            directive.MyCommonParam.NormalAttackIntervalTime);

        var oldGrappleAttackInterval = directive.MyCommonParam.GrappleAttackIntervalTime;
        directive.MyCommonParam.GrappleAttackIntervalTime /= speed;
        logger.LogChange("Grapple attack interval", oldGrappleAttackInterval,
            directive.MyCommonParam.GrappleAttackIntervalTime);

        var oldGroundAttackInterval = directive.MyCommonParam.GroundAttackIntervalTime;
        directive.MyCommonParam.GroundAttackIntervalTime /= speed;
        logger.LogChange("Ground attack interval", oldGroundAttackInterval,
            directive.MyCommonParam.GroundAttackIntervalTime);

        var oldWallAttackInterval = directive.MyCommonParam.WallAttackIntervalTime;
        directive.MyCommonParam.WallAttackIntervalTime /= speed;
        logger.LogChange("Wall attack interval", oldWallAttackInterval, directive.MyCommonParam.WallAttackIntervalTime);

        var oldTwoLegMoveSpeed = directive.MyCommonParam.ChangeTwoLegMoveSpeed;
        directive.MyCommonParam.ChangeTwoLegMoveSpeed *= speed;
        logger.LogChange("Two-leg move speed", oldTwoLegMoveSpeed, directive.MyCommonParam.ChangeTwoLegMoveSpeed);

        var oldFourLegMoveSpeed = directive.MyCommonParam.ChangeFourLegMoveSpeed;
        directive.MyCommonParam.ChangeFourLegMoveSpeed *= speed;
        logger.LogChange("Four-leg move speed", oldFourLegMoveSpeed, directive.MyCommonParam.ChangeFourLegMoveSpeed);

        var oldNormalMoveSpeedRate = directive.NormalModeParam.MoveSpeedRate;
        directive.NormalModeParam.MoveSpeedRate *= speed;
        logger.LogChange("Normal mode move speed", oldNormalMoveSpeedRate, directive.NormalModeParam.MoveSpeedRate);

        var oldNormalBlendRate = directive.NormalModeParam.MoveSpeedBlendRateUpSpeed;
        directive.NormalModeParam.MoveSpeedBlendRateUpSpeed *= speed;
        logger.LogChange("Normal mode blend-up speed", oldNormalBlendRate,
            directive.NormalModeParam.MoveSpeedBlendRateUpSpeed);

        var oldWallMoveSpeed = directive.WallMoveModeParam.MoveSpeed;
        directive.WallMoveModeParam.MoveSpeed *= speed;
        logger.LogChange("Wall move speed", oldWallMoveSpeed, directive.WallMoveModeParam.MoveSpeed);

        var oldGenerateTime = directive.GenerateModeParam.GenerateTime;
        directive.GenerateModeParam.GenerateTime /= speed;
        logger.LogChange("Generate time", oldGenerateTime, directive.GenerateModeParam.GenerateTime);

        var oldSpawnBugsInterval = directive.GenerateModeParam.SpawnBugsIntervalTime;
        directive.GenerateModeParam.SpawnBugsIntervalTime /= speed;
        logger.LogChange("Spawn bugs interval", oldSpawnBugsInterval,
            directive.GenerateModeParam.SpawnBugsIntervalTime);

        var oldSneakTime = directive.SneakModeParam.SneakTime;
        directive.SneakModeParam.SneakTime /= speed;
        logger.LogChange("Sneak time", oldSneakTime, directive.SneakModeParam.SneakTime);

        var oldEscapeMoveSpeed = directive.EscapeModeParam.MoveSpeed;
        directive.EscapeModeParam.MoveSpeed *= speed;
        logger.LogChange("Escape move speed", oldEscapeMoveSpeed, directive.EscapeModeParam.MoveSpeed);

        var oldLastModeMoveSpeed = directive.LastModeParam.MoveSpeed;
        directive.LastModeParam.MoveSpeed *= speed;
        logger.LogChange("Last-mode move speed", oldLastModeMoveSpeed, directive.LastModeParam.MoveSpeed);

        return directive;
    }
}
