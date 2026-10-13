using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Enemies.Impl;
using Biohazard.BioRand.RE7.REEngine;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class EnemyDirectiveModifier : Modifier {
    private readonly Randomizer _randomizer;

    public EnemyDirectiveModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private readonly List<IDirectiveModifier> _enemySpecificDirectiveModifiers =[
        new EvelineFinalBossDirectiveModifier(),
        new InsectsDirectiveModifier(),
        new JackShearsKneeDownDirectiveModifier(),
        new JackShearsDirectiveModifier(),
        new JackMutatedDirectiveModifier(),
        new JackStalkerDirectiveModifier(),
        new MargeMutatedDirectiveModifier(),
        new MargeStalkerDirectiveModifier(),
        new MiaDirectiveModifier(),
        new MoldedDirectiveModifier(),
        new MoldedFatDirectiveModifier(),
        new MoldedQuickDirectiveModifier(),
    ];

    private readonly List<IDirectiveModifier> _genericDirectiveModifiers =[
        new EnemyRankParamDirectiveModifier(),
    ];

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        // Nightmare Jack variants share campaign Em3000/Em8000 parameters.
        // Keep those campaign fixes; specialized modifiers gate DLC-only files.
        foreach (var enemy in EnemyDefinitions.Instance.All.OrderBy(em => em.EnemyId)) {
            var matchingModifiers = _enemySpecificDirectiveModifiers
                .Where(modifier => modifier.Supports(enemy))
                .ToArray();

            if (matchingModifiers.Length == 0) {
                continue;
            }

            logger.Push($"{enemy.EnemyId} -- {enemy.Name}");
            if (matchingModifiers.Length == 1) {
                matchingModifiers[0].Apply(enemy, randomizer, logger);
            } else {
                foreach (var enemySpecificModifier in matchingModifiers) {
                    logger.Push(enemySpecificModifier.GetLogLabel());
                    enemySpecificModifier.Apply(enemy, randomizer, logger);
                    logger.Pop();
                }
            }

            logger.Pop();
        }

        foreach (var modifier in _genericDirectiveModifiers) {
            logger.Push($"Generic -- {modifier.GetLogLabel()}");
            modifier.Apply(null!, randomizer, logger);
            logger.Pop();
        }
    }
}

internal sealed class EnemyRankParamDirectiveModifier : IDirectiveModifier {
    private const string EnemyRankParameterHolderPath =
        "prefab/character/misc/parameter/battle/enemyrankparameterholder.user";

    public bool Supports(IEnemyDefinition enemy) => true;

    public void Apply(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        var applyDamage = randomizer.GetConfigOption<bool>("random-enemy-damage");

        if (!applyDamage) {
            logger.LogSkip("Enemy damage randomization is disabled.");
            return;
        }

        var rng = randomizer.GetRng("enemy/enemy-rank-params");
        var damageMultiplier = GetDamageMultiplier(randomizer, rng);
        logger.LogMultiplier("Damage multiplier", damageMultiplier);

        var holderPath = EnemyRankParameterHolderPath.UserFile();
        var holder = randomizer.FileRepository.DeserializeUserFile<app.EnemyRankParameterHolder>(holderPath);

        foreach (var unit in holder.Units) {
            var rank = unit.Rank;
            var userFilePath = unit.RankParameter.Path.UserFile();

            logger.LogDirectiveFile(rank, userFilePath, () =>
                randomizer.FileRepository.ModifyUserFile<app.EnemyRankParameter>(userFilePath, param => {
                    var oldRate = param.DamageRate;
                    param.DamageRate *= damageMultiplier;
                    logger.LogChange("Damage rate", oldRate, param.DamageRate);

                    return param;
                }));
        }
    }

    private static float GetDamageMultiplier(Randomizer randomizer, Rng rng) {
        if (randomizer.GetConfigOption<bool>("enemy-insta-death")) {
            return 9999f;
        }

        return (float)rng.NextDouble(
            randomizer.GetConfigOption<double>("enemy-damage-min"),
            randomizer.GetConfigOption<double>("enemy-damage-max"));
    }
}
