using Biohazard.BioRand.RE7.REEngine;

namespace Biohazard.BioRand.RE7.Enemies.Impl;

internal class FlyingBug : InsectBase {
    public FlyingBug() : base("FlyingBug", EnemyID.Em5400, "Flying Bug", 150) { }
}

internal class InsectHive : InsectBase {
    public InsectHive() : base("InsectHive", EnemyID.Em5510, "Insect Hive", 2400) { }
    // Also has variants Em5511 and Em5512, but they only differ in their appearance.
    // Hives generate Em5520 swarms internally, so they must share the swarm placement ban.
    public override bool SupportsRandomEnemyPlacement => false;
}

internal class InsectSwarm : InsectBase {
    public InsectSwarm() : base("InsectSwarm", EnemyID.Em5520, "Insect Swarm", 800) { }
    // Off-location swarms can get stuck and have been associated with native crashes.
    // Retain the definition for vanilla encounters and old profiles, but never add new ones.
    public override bool SupportsRandomEnemyPlacement => false;
}

// ?
//internal class InsectSwarm2 : InsectBase
//{
//    public InsectSwarm2() : base("InsectSwarm2", EnemyID.Em5540, "Insect Swarm 2", 999999) { }
//}

internal abstract class InsectBase(string id, EnemyID enemyId, string name, int health) : IEnemyDefinition {
    public string Id => id;

    public EnemyID EnemyId => enemyId;

    public EnemyCategory Category => EnemyCategory.Insect;

    public string Name => name;

    public bool IsBoss => false;

    public int BaseHealth => health;

    private string SanitizedId => EnemyId.ToString().ToLower();

    public List<string> RcolPaths =>[
        $"collision/collider/enemy/{SanitizedId}/{SanitizedId}.rcol".RcolFile(),
    ];

    public string DirectivesHolderPath
        => $"prefab/character/{SanitizedId}/{SanitizedId}directivesholder.user".UserFile();

    public string ResistParamsHolderPath
        => $"prefab/character/{SanitizedId}/{SanitizedId}resistparameterholder.user".UserFile();

    public string OriginalPrefabPath
        => $"scenes/enemy/{EnemyId.ToString().ToLowerInvariant()}.scn".SceneFile();

    public bool UsesEnemyGenerator => true;

    public bool SupportsSpeedRandomization => true;
    public virtual bool SupportsRandomEnemyPlacement => true;
}

internal class InsectsDirectiveModifier : IDirectiveModifier {
    public bool Supports(IEnemyDefinition enemy)
        => enemy.EnemyId is EnemyID.Em5400 or EnemyID.Em5510 or EnemyID.Em5520;

    public void Apply(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        if (!enemy.ShouldRandomizeSpeed(randomizer)) {
            logger.LogSkip("Enemy speed randomization is disabled.");
            return;
        }

        var speedMultiplier = enemy.GetSpeedMultiplier(randomizer);

        logger.LogMultiplier("Speed multiplier", speedMultiplier);

        if (enemy is FlyingBug) {
            var holder =
                randomizer.FileRepository.DeserializeUserFile<app.Em5400DirectivesHolder>(enemy.DirectivesHolderPath);
            foreach (var directive in holder.holder.Units) {
                var rank = directive.Rank;
                var userFilePath = directive.Directive.Path.UserFile();

                logger.LogDirectiveFile(rank, userFilePath, () =>
                    randomizer.FileRepository.ModifyUserFile<app.Em5400Directive>(
                        userFilePath,
                        directive => ModifyDirective(directive, logger, speedMultiplier)));
            }
        } else if (enemy is InsectHive) {
            var holder =
                randomizer.FileRepository.DeserializeUserFile<app.Em5510DirectivesHolder>(enemy.DirectivesHolderPath);
            foreach (var directive in holder.holder.Units) {
                var rank = directive.Rank;
                var userFilePath = directive.Directive.Path.UserFile();

                logger.LogDirectiveFile(rank, userFilePath, () =>
                    randomizer.FileRepository.ModifyUserFile<app.Em5510UserData>(
                        userFilePath,
                        directive => ModifyDirective(directive, logger, speedMultiplier)));
            }
        } else if (enemy is InsectSwarm) {
            var holder =
                randomizer.FileRepository.DeserializeUserFile<app.Em5520DirectivesHolder>(enemy.DirectivesHolderPath);
            foreach (var directive in holder.holder.Units) {
                var rank = directive.Rank;
                var userFilePath = directive.Directive.Path.UserFile();

                logger.LogDirectiveFile(rank, userFilePath, () =>
                    randomizer.FileRepository.ModifyUserFile<app.Em5520Directive>(
                        userFilePath,
                        directive => ModifyDirective(directive, logger, speedMultiplier)));
            }
        }
    }

    private static app.Em5400Directive ModifyDirective(
        app.Em5400Directive directive,
        RandomizerLogger logger,
        float speedMultiplier) {
        var oldDefaultSpeed = directive.MyCommonParam.DefaultSpeed;
        directive.MyCommonParam.DefaultSpeed *= speedMultiplier;
        logger.LogChange("Default speed", oldDefaultSpeed, directive.MyCommonParam.DefaultSpeed);

        var oldAttackSpeed = directive.MyCommonParam.AttackSpeed;
        directive.MyCommonParam.AttackSpeed *= speedMultiplier;
        logger.LogChange("Attack speed", oldAttackSpeed, directive.MyCommonParam.AttackSpeed);

        var oldAttackIntervalMin = directive.MyCommonParam.AttackIntervalSecMin;
        directive.MyCommonParam.AttackIntervalSecMin /= speedMultiplier;
        logger.LogChange("Attack interval min", oldAttackIntervalMin, directive.MyCommonParam.AttackIntervalSecMin);

        var oldAttackIntervalMax = directive.MyCommonParam.AttackIntervalSecMax;
        directive.MyCommonParam.AttackIntervalSecMax /= speedMultiplier;
        logger.LogChange("Attack interval max", oldAttackIntervalMax, directive.MyCommonParam.AttackIntervalSecMax);

        return directive;
    }

    private static app.Em5510UserData ModifyDirective(
        app.Em5510UserData directive,
        RandomizerLogger logger,
        float speedMultiplier) {
        var oldIntervalTime = directive.MyGenerateParam.IntervalTime;
        directive.MyGenerateParam.IntervalTime /= speedMultiplier;
        logger.LogChange("Generate interval", oldIntervalTime, directive.MyGenerateParam.IntervalTime);

        var oldWaitTime = directive.MyGenerateParam.WaitTime;
        directive.MyGenerateParam.WaitTime /= speedMultiplier;
        logger.LogChange("Generate wait time", oldWaitTime, directive.MyGenerateParam.WaitTime);

        return directive;
    }

    private static app.Em5520Directive ModifyDirective(
        app.Em5520Directive directive,
        RandomizerLogger logger,
        float speedMultiplier) {
        var oldDefaultSpeed = directive.MyMoveParam.DefaultSpeed;
        directive.MyMoveParam.DefaultSpeed *= speedMultiplier;
        logger.LogChange("Default speed", oldDefaultSpeed, directive.MyMoveParam.DefaultSpeed);

        var oldNearPlayerSpeed = directive.MyMoveParam.NearPlayerSpeed;
        directive.MyMoveParam.NearPlayerSpeed *= speedMultiplier;
        logger.LogChange("Near-player speed", oldNearPlayerSpeed, directive.MyMoveParam.NearPlayerSpeed);

        var oldAttackTime = directive.MyAttackParam.AttackTime;
        directive.MyAttackParam.AttackTime /= speedMultiplier;
        logger.LogChange("Attack time", oldAttackTime, directive.MyAttackParam.AttackTime);

        var oldAttackIntervalTime = directive.MyAttackParam.AttackIntervalTime;
        directive.MyAttackParam.AttackIntervalTime /= speedMultiplier;
        logger.LogChange("Attack interval", oldAttackIntervalTime, directive.MyAttackParam.AttackIntervalTime);

        return directive;
    }
}
