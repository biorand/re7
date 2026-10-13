using Biohazard.BioRand.RE7.REEngine;
using Enums.app;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Enemies.Impl;

internal class JackStalker : IEnemyDefinition {
    public string Id => "JackStalker";

    public EnemyID EnemyId => EnemyID.Em3001;

    public DlcType? Dlc => DlcType.Nightmare;

    public EnemyCategory Category => EnemyCategory.Jack;

    public string Name => "Jack Baker (Stalker)";

    public bool IsBoss => false;

    public int BaseHealth => 10000;

    public List<string> RcolPaths
        =>[
            "collision/collider/enemy/em3000/em3000.rcol".RcolFile(),
            "collision/collider/enemy/em3000/em3000throwattack.rcol".RcolFile()
        ];

    public string DirectivesHolderPath
        => "prefab/character/em3000/parameter/directive/em3000directivesholder.user".UserFile();

    public string ResistParamsHolderPath
        => "prefab/character/em2000/parameter/resist/em3000resistparameter.user6".UserFile();

    public string OriginalPrefabPath
        => $"scenes/enemy/em3000.scn".SceneFile();

    public bool UsesEnemyGenerator => true;

    public bool SupportsSpeedRandomization => true;

    private readonly List<WeaponID> _availableWeapons =[
        /* Vanilla */ WeaponID.Shovel, WeaponID.Roller, WeaponID.FireAxe,
        /* Modded */ WeaponID.ChainSaw
    ];

    public RszGameObject IndividualizeTemplate(Rng rng, RszGameObject template) {
        var equipManager = template.FindComponent<app.EquipManager>()!;
        equipManager.EquipWeaponIdRight = rng.Next(_availableWeapons);
        //equipManager.EquipWeaponIdLeft = rng.Next(_availableWeapons);
        template = template.AddOrUpdateComponent(equipManager);
        return template;
    }
}

internal class JackStalkerDirectiveModifier : IDirectiveModifier {
    public bool Supports(IEnemyDefinition enemy)
        => enemy.EnemyId == EnemyID.Em3001;

    public void Apply(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        var rng = randomizer.GetRng("enemy/em3000");
        var applySpeed = enemy.ShouldRandomizeSpeed(randomizer);
        var speedMultiplier = enemy.GetSpeedMultiplier(randomizer);

        var shouldRandomizeHealth = enemy.ShouldRandomizeHealth(randomizer);
        var health = shouldRandomizeHealth ? enemy.GetHealth(randomizer, rng) : (float?)null;
        if (health.HasValue) {
            logger.LogHealthAssignment("Health", enemy.BaseHealth, health.Value);
        } else {
            logger.LogLine("Health: unchanged (enemy health randomization disabled)");
        }

        if (applySpeed) {
            logger.LogMultiplier("Walk speed multiplier", speedMultiplier);
        } else {
            logger.LogLine("Walk speed multiplier: 1x (enemy speed randomization disabled)");
        }

        var holder =
            randomizer.FileRepository.DeserializeUserFile<app.Em3000DirectivesHolder>(enemy.DirectivesHolderPath);
        var scaleProbability = (int)(randomizer.GetConfigOption<double>("enemy-scale-probability", 0) * 100);
        var scaleMin = Math.Clamp(randomizer.GetConfigOption("enemy-scale-min", 0.25f), 0.1f, 10.0f);
        var scaleMax = Math.Clamp(randomizer.GetConfigOption("enemy-scale-max", 2.00f), 0.1f, 10.0f);
        var scaleMultiplier = rng.NextProbability(scaleProbability) ? rng.NextDouble(scaleMin, scaleMax) : 1.0;

        foreach (var directive in holder.holder.Units) {
            var rank = directive.Rank;
            var userFilePath = directive.Directive.Path.UserFile();

            logger.LogDirectiveFile(rank, userFilePath, () =>
                randomizer.FileRepository.ModifyUserFile<app.Em3000BattleDirective>(
                    userFilePath,
                    d => ModifyDirective(d, logger, health, speedMultiplier, scaleMultiplier)
                ));
        }
    }

    private app.Em3000BattleDirective ModifyDirective(
        app.Em3000BattleDirective directive,
        RandomizerLogger logger,
        float? health,
        float speedMultiplier,
        double scaleMultiplier) {
        // Scale
        directive.common.ModelScale *= (float)scaleMultiplier;

        // Health
        if (health.HasValue) {
            var oldHealth = directive.chapter3Battle1Final.Health;
            directive.chapter3Battle1Final.Health = health.Value;
            logger.LogChange("Chapter 3 final health", oldHealth, directive.chapter3Battle1Final.Health);
        }

        // Speed
        if (speedMultiplier == 1f) {
            logger.LogLine("No walk speed changes.");
        } else {
            //directive.common.MotionSpeedForBack *= speedMultiplier;
            //directive.common.MotionSpeedForStepIn *= speedMultiplier;
            var oldWalkSpeed = directive.common.MotionSpeedForWalk;
            directive.common.MotionSpeedForWalk *= speedMultiplier;
            logger.LogChange("Walk speed", oldWalkSpeed, directive.common.MotionSpeedForWalk);
        }

        // Misc.
        var oldDiscoveryTime = directive.chapter3Battle1.MansionAIForceDiscoveryTime;
        directive.chapter3Battle1.MansionAIForceDiscoveryTime = 0.5f; // ;)
        logger.LogChange("Mansion AI forced discovery time", oldDiscoveryTime,
            directive.chapter3Battle1.MansionAIForceDiscoveryTime);
        return directive;
    }
}
