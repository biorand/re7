using System.Text;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class BaseGameOnlyEnemyTests {
    private const string ScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20";
    private const string NightmareScenePath = "natives/stm/scenes/chapter/chapter7/chapter7_3/base_only_test.scn.20";

    [Theory, Trait("Category", "RequiresPak")]
    [InlineData("Em3600")]
    [InlineData("Em4000")]
    [InlineData("Em4100")]
    [InlineData("Em4200")]
    public void CampaignTemplateAdaptationRetainsStateAndUsesCampaignSound(string alias) {
        using var randomizer = new Randomizer(new RandomizerInput { Configuration = RandomizerTest.CreateFeatureTestConfiguration() }, "", new EmptyReporter());
        using var repository = new FileRepository(randomizer, RandomizerTest.InputPakPath, randomizer.DynamicData);
        var original = randomizer.TemplateService.GetEnemyTemplate(alias);
        var adapted = BaseGameEnemyTemplate.Adapt(repository, alias, original);
        Assert.Equal(original.Guid, adapted.Guid);
        Assert.Equal(GetStateIds(original), GetStateIds(adapted));
        Assert.Equal(GetActionUids(original), GetActionUids(adapted));
        var resources = new List<string>();
        adapted.Visit(node => {
            if (node is RszResourceNode { Value: { } path }) resources.Add(path);
            if (node is RszUserDataNode { Path: { } userPath }) resources.Add(userPath);
            return node;
        });
        Assert.DoesNotContain(resources, path => path.Contains("birthday", StringComparison.OrdinalIgnoreCase));
        if (alias == "Em3600") {
            var native = repository.GetScnFile(BaseGameEnemyTemplate.MargeScenePath).ReadScene(repository.TypeRepository)
                .FindGameObject(gameObject => gameObject.FindComponent("app.Em3600ActionController") != null)!;
            Assert.Equal(GetSoundPaths(native), GetSoundPaths(adapted));
            Assert.NotEqual(GetSoundPaths(original), GetSoundPaths(adapted));
        }

        // DLC-enabled callers keep the embedded variant, including its existing sound.
        Assert.Same(original, randomizer.TemplateService.GetEnemyTemplate(alias));

        static string[] GetSoundPaths(RszGameObject gameObject) =>
            ((RszArrayNode)gameObject.FindComponent("app.WwiseContainerApp")!["ContainableAsset"])
                .Children.OfType<RszResourceNode>().Select(resource => resource.Value).OfType<string>().ToArray();
        static Guid[] GetStateIds(RszGameObject gameObject) {
            var ids = new List<Guid>();
            gameObject.VisitComponents(component => {
                foreach (var field in new[] { "SaveGUID", "InstanceGuid" }) {
                    if (component.Type.FindFieldIndex(field) >= 0)
                        ids.Add(component.Get<Guid>(field));
                }
                return component;
            });
            return ids.ToArray();
        }
        static uint[] GetActionUids(RszGameObject gameObject) {
            var uids = new List<uint>();
            gameObject.Visit(node => {
                if (node is RszObjectNode obj && obj.Type.FindFieldIndex("v2_UID") >= 0)
                    uids.Add(obj.Get<uint>("v2_UID"));
                return node;
            });
            return uids.ToArray();
        }
    }

    [Theory]
    [InlineData("Em3001")]
    [InlineData("Em8001")]
    public void NightmareTemplatesRequireDlcContent(string alias) {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
        });
        using var randomizer = new Randomizer(new RandomizerInput { Configuration = configuration }, "", new EmptyReporter());
        var enemy = EnemyDefinitions.Instance.FromId(alias)!;
        Assert.Equal(DlcType.Nightmare, enemy.Dlc);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new EnemyTemplateFactory(randomizer).GetOrCreateSpawnInfoTemplate(alias, new Rng(7)));
        Assert.Contains("base-game-only", exception.Message);

        configuration[DlcContent.BaseGameOnlyConfigKey] = false;
        Assert.True(enemy.IsAllowed(randomizer));
        Assert.NotNull(new EnemyTemplateFactory(randomizer).GetOrCreateSpawnInfoTemplate(alias, new Rng(7)));
    }

    [Fact, Trait("Category", "RequiresPak")]
    public void BaseGameOnlyFiltersWeightedExplicitAndGroupedEnemyImports() {
        using var result = RandomizerTest.RunState(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            config["extra-enemy-amount"] = 1.0;
            config["enemy-multiplier"] = 2.0;
            foreach (var enemy in EnemyDefinitions.Instance.Randomizable) {
                config[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enemy.Id switch {
                    "JackStalker" or "JackShears" => 1000000.0,
                    "MoldedQuick" => 1.0,
                    _ => 0.0,
                };
            }
        }, prepareRandomizer: randomizer => {
            randomizer.DynamicData.SetData(DynamicDataName.Enemies,
                Encoding.UTF8.GetBytes("Guid,SceneFile,IsSpawnInfo,Tags,Include,Exclude,SpawnGroup,Dlc\n" +
                    $"a4aeb8b2-8223-4ab6-af71-818c76b33b0f,{NightmareScenePath},TRUE,aggro,,,encounter,Nightmare\n"));
            randomizer.DynamicData.SetData(DynamicDataName.SpawnGroups,
                Encoding.UTF8.GetBytes("Name,Parameter,State,X,Y,Z,Radius,Time\nencounter,spawn,,,,,,0\n"));
            randomizer.DynamicData.SetData(DynamicDataName.ExtraEnemies, Encoding.UTF8.GetBytes(
                "Enabled,Id,Include,Exclude,SceneFile,Chapter,PosX,RotW,SpawnGroup\n" +
                $"TRUE,Em3001,,,{ScenePath},4,10,1,encounter\n" +
                $"TRUE,Em8001,,,{ScenePath},4,11,1,encounter\n" +
                $"TRUE,,JackStalker JackShears,,{ScenePath},4,12,1,encounter\n" +
                $"TRUE,Em3001|Em8001|Em4100,,,{ScenePath},4,13,1,encounter\n" +
                $"TRUE,,JackStalker JackShears MoldedQuick,,{ScenePath},4,14,1,encounter\n" +
                $"TRUE,random,,,{ScenePath},4,15,1,encounter\n" +
                $"TRUE,Em4100,,,{NightmareScenePath},7,16,1,encounter\n"));
        });

        var replacements = result.ChangedFiles.Keys.Where(path => path.EndsWith(".scn.20", StringComparison.Ordinal))
            .SelectMany(path => result.ReadAfterScene(path).GetGameObjects())
            .Where(gameObject => gameObject.Name.Contains("_Now_", StringComparison.Ordinal))
            .Select(gameObject => gameObject.FindComponent<app.EnemySpawnInfo>()!)
            .ToArray();
        Assert.NotEmpty(replacements);
        Assert.All(replacements, spawn => Assert.Equal("Em4100", spawn.UnitAlias));

        var scene = result.ReadAfterScene(ScenePath);
        var extraSpawns = scene.GetGameObjects().Where(EnemySpawnInfoRules.IsExtraEnemySpawnInfo).ToArray();
        Assert.Equal(6, extraSpawns.Length);
        Assert.All(extraSpawns, gameObject => {
            Assert.Equal("Em4100", gameObject.FindComponent<app.EnemySpawnInfo>()!.UnitAlias);
            Assert.InRange(gameObject.FindComponent<GeneratedViaTransform>()!.Position.X, 13, 15);
        });
        var group = Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups);
        Assert.Equal(extraSpawns.Select(gameObject => gameObject.Guid).Order(), group.Members.Select(member => member.Guid).Order());
        Assert.DoesNotContain(NightmareScenePath, result.Randomizer.EnemyPlacementService.AggroScenePaths);
        Assert.False(result.WasFileModified(NightmareScenePath));
    }

    [Fact, Trait("Category", "RequiresPak")]
    public void BaseGameOnlySkipsNightmareHealthDataAndKeepsCampaignJackFixes() {
        using var result = RandomizerTest.RunState(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
            config["boss-random-health"] = true;
        });

        Assert.DoesNotContain(result.ChangedFiles.Keys, path => path.Contains("/em8001/", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.WasFileModified("natives/stm/prefab/character/em8000/parameter/resist/em8000resistparameter.user.2"));
        var jack = EnemyDefinitions.Instance.FromId("Em3001")!;
        var holder = result.ReadAfterUserFile<app.Em3000DirectivesHolder>(jack.DirectivesHolderPath);
        Assert.All(holder.holder.Units, unit => Assert.True(result.WasFileModified(unit.Directive.Path.UserFile())));
    }
}
