using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class EnemyCloneIsolationTests {
    [Theory]
    [InlineData("MiaChainsaw")]
    [InlineData("JackShears")]
    public void EnemyTemplates_CopiesHaveIndependentSaveAndFsmState(string enemyId) {
        using var randomizer = new Randomizer(new RandomizerInput {
            Seed = 42,
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(),
        }, "", new EmptyReporter());
        var factory = new EnemyTemplateFactory(randomizer);
        var enemy = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == enemyId);
        var rng = new Rng(42);
        var copies = Enumerable.Range(0, 3).Select(_ => factory.GetOrCreateEnemyTemplate(
            enemy.EnemyId.ToString(), new GeneratedViaTransform(), false, false,
            new ScaleOptions(0, 1, 1), rng, enemy)).ToArray();

        var sceneFile = new ScnFile(FileVersions.SceneFileVersion,
            EmbeddedData.GetFile($"template.scn.{FileVersions.SceneFileVersion}"));
        var builder = sceneFile.ToBuilder(FileRepository.RszRepository);
        builder.Scene = builder.Scene.WithChildren([.. copies]);
        var scene = builder.Build().ReadScene(FileRepository.RszRepository);
        var reloadedCopies = copies.Select(copy => scene.FindGameObject(copy.Guid)!).ToArray();

        AssertIndependentState(reloadedCopies);
        var template = randomizer.TemplateService.GetEnemyTemplate(enemy.EnemyId.ToString());
        Assert.DoesNotContain(template.FindComponent<app.EnemySave>()!.SaveGUID,
            reloadedCopies.Select(copy => copy.FindComponent<app.EnemySave>()!.SaveGUID));

        // Instance IDs may change; the UIDs inside an FSM are fixed state/action IDs.
        Assert.All(reloadedCopies, copy => Assert.Equal(GetActionUids(template), GetActionUids(copy)));
        var replayRng = new Rng(42);
        foreach (var copy in reloadedCopies) {
            var replay = factory.GetOrCreateEnemyTemplate(enemy.EnemyId.ToString(), new GeneratedViaTransform(),
                false, false, new ScaleOptions(0, 1, 1), replayRng, enemy);
            Assert.Equal(copy.Guid, replay.Guid);
            Assert.Equal(GetStateIds(copy), GetStateIds(replay));
        }
    }

    internal static void AssertIndependentState(IEnumerable<RszGameObject> copies) {
        var ids = copies.SelectMany(GetStateIds).ToArray();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    internal static IReadOnlyList<Guid> GetStateIds(RszGameObject copy) {
        var ids = new List<Guid>();
        Assert.NotNull(copy.FindComponent<app.EnemySave>());
        copy.VisitComponents(component => {
            foreach (var field in new[] { "SaveGUID", "InstanceGuid" }) {
                if (component.Type.FindFieldIndex(field) >= 0) {
                    var guid = component.Get<Guid>(field);
                    if (guid != Guid.Empty) ids.Add(guid);
                }
            }
            return component;
        });
        return ids;
    }

    private static uint[] GetActionUids(RszGameObject root) {
        var result = new List<uint>();
        root.Visit(node => {
            if (node is RszObjectNode obj && obj.Type.FindFieldIndex("v2_UID") >= 0)
                result.Add(obj.Get<uint>("v2_UID"));
            return node;
        });
        return result.ToArray();
    }
}
