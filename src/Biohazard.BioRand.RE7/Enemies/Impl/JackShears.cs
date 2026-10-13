using Biohazard.BioRand.RE7.REEngine;
using EnemyResistType = Enums.app.EnemyResistParameter.EnemyResistType;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Enemies.Impl;

internal class JackShears : IEnemyDefinition {
    public string Id => "JackShears";

    public EnemyID EnemyId => EnemyID.Em8001;

    public DlcType? Dlc => DlcType.Nightmare;

    public EnemyCategory Category => EnemyCategory.Jack;

    public string Name => "Jack Baker (Scissor Chainsaw)";

    public bool IsBoss => true;

    public int BaseHealth => 4500;

    public List<string> RcolPaths =>[
        "collision/collider/enemy/em8000/em8000.rcol".RcolFile(),
        "collision/collider/enemy/em8000/em8000chainsawsensor.rcol".RcolFile(),
        "collision/collider/enemy/em8000/em8100deadbody.rcol.20".RcolFile(),
    ];

    public string DirectivesHolderPath
        => "prefab/character/em8000/parameter/directive/em8000directiveholder.user".UserFile();

    public string ResistParamsHolderPath
        => "prefab/character/em8000/parameter/resist/em8000resistparameterholder.user".UserFile();

    public string OriginalPrefabPath
        => $"scenes/chapter/chapter7/chapter7_3/wave5.scn".SceneFile();

    public bool UsesEnemyGenerator => true;
}

internal class JackShearsKneeDownDirectiveModifier : IDirectiveModifier {
    private const string ResistParameterPath = "prefab/character/em8000/parameter/resist/em8000resistparameter.user";
    private const uint AllLowWeaponIds = uint.MaxValue;
    private const uint AllHighWeaponIds = uint.MaxValue;

    public bool Supports(IEnemyDefinition enemy)
        => enemy.EnemyId == EnemyID.Em8001;

    public void Apply(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        var userFilePath = ResistParameterPath.UserFile();
        logger.LogDirectiveFile("Em8000 knee-down resist", userFilePath, () =>
            randomizer.FileRepository.ModifyUserFile(userFilePath, resistParameter => {
                var patchedParts = 0;
                var units = (RszArrayNode)resistParameter["units"];
                for (var unitIndex = 0; unitIndex < units.Children.Length; unitIndex++) {
                    if (units.Children[unitIndex] is not RszObjectNode unit ||
                        (EnemyResistType)unit.Get<int>("resistType") != EnemyResistType.Large) {
                        continue;
                    }

                    var parts = (RszArrayNode)unit["parts"];
                    for (var partIndex = 0; partIndex < parts.Children.Length; partIndex++) {
                        if (parts.Children[partIndex] is not RszObjectNode part) {
                            continue;
                        }

                        var weaponRates = (RszArrayNode)part["weaponRate"];
                        if (weaponRates.Children.Length == 0) {
                            continue;
                        }

                        var existingLowWeaponIds = 0u;
                        var existingHighWeaponIds = 0u;
                        foreach (var weaponRate in weaponRates.Children.OfType<RszObjectNode>()) {
                            existingLowWeaponIds |= weaponRate.Get<uint>("weaponIDs");
                            existingHighWeaponIds |= weaponRate.Get<uint>("weaponIDs2");
                        }

                        var missingLowWeaponIds = AllLowWeaponIds & ~existingLowWeaponIds;
                        var missingHighWeaponIds = AllHighWeaponIds & ~existingHighWeaponIds;
                        if (missingLowWeaponIds == 0 && missingHighWeaponIds == 0) {
                            continue;
                        }

                        var catchAllRatePath = $"units[{unitIndex}].parts[{partIndex}].weaponRate[0]";
                        resistParameter = resistParameter
                            .Set($"{catchAllRatePath}.weaponIDs",
                                resistParameter.Get<uint>($"{catchAllRatePath}.weaponIDs") | missingLowWeaponIds)
                            .Set($"{catchAllRatePath}.weaponIDs2",
                                resistParameter.Get<uint>($"{catchAllRatePath}.weaponIDs2") | missingHighWeaponIds);

                        patchedParts++;
                        logger.LogLine(
                            $"Added missing WeaponID masks to Large resist part '{part.Get<string>("alias")}'.");
                    }
                }

                if (patchedParts == 0) {
                    logger.LogLine("All Large resist parts already accept every maskable WeaponID.");
                }

                return resistParameter;
            }));
    }
}

internal class JackShearsDirectiveModifier : IDirectiveModifier {
    public bool Supports(IEnemyDefinition enemy)
        => enemy.EnemyId == EnemyID.Em8001;

    public void Apply(IEnemyDefinition enemy, Randomizer randomizer, RandomizerLogger logger) {
        if (!enemy.IsAllowed(randomizer)) {
            logger.LogSkip("Nightmare Jack health settings are disabled in base-game-only mode.");
            return;
        }

        if (!enemy.ShouldRandomizeHealth(randomizer)) {
            logger.LogSkip("Boss health randomization is disabled.");
            return;
        }

        var rng = randomizer.GetRng("enemy/em8001");
        var newInitHp = enemy.GetHealth(randomizer, rng);
        logger.LogHealthAssignment("Common.InitHP", enemy.BaseHealth, newInitHp);
        var userFilePath =
            "prefab/character/em8001/parameter/directive/em8001battledirective_default.user".UserFile();
        logger.LogDirectiveFile("Default", userFilePath, () => randomizer.FileRepository.ModifyUserFile(userFilePath,
            directive => {
                var oldInitHp = directive.Get<float>("Common.InitHP");
                logger.LogChange("Common.InitHP", oldInitHp, newInitHp);
                directive = directive.Set("Common.InitHP", newInitHp);
                return directive;
            }));
    }
}
