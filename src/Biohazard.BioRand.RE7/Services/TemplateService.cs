using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;
using Biohazard.BioRand.RE7.Items;

namespace Biohazard.BioRand.RE7.Services;

internal class TemplateService {
    private const string TemplateSceneFileName = "template.scn";
    private const string EnemyFsmGeneratorTemplateName = "FsmGenerator";
    private readonly ScnFile _templateScnFile;
    private readonly RszScene _scene;
    private readonly Randomizer _randomizer;
    private readonly Dictionary<string, RszGameObject> _itemTemplates = new();

    public TemplateService(Randomizer randomizer) {
        _randomizer = randomizer;
        _templateScnFile = new(
            FileVersions.SceneFileVersion,
            EmbeddedData.GetFile($"{TemplateSceneFileName}.{FileVersions.SceneFileVersion}")
        );

        _scene = _templateScnFile.ReadScene(randomizer.FileRepository.TypeRepository);
        _scene.VisitGameObjects(go => {
            if (go.Name.StartsWith("ItemTemplate")) {
                _itemTemplates.Add(go.Name.SubstringAfter("_"), go);
            }
        });
    }

    public RszGameObject GetObject(string name)
        => _scene.FindGameObject(name) ?? throw new Exception($"Object with name {name} not found in template scene!");

    public RszGameObject? TryGetObject(string name)
        => _scene.FindGameObject(name);

    public RszGameObject GetEnemyTemplate(string enemyID)
        => GetObject($"EnemyTemplate_{enemyID}");

    public RszGameObject GetEnemySpawnInfo(string enemyID)
        => GetObject($"EnemySpawnInfo_{enemyID}");

    public RszGameObject? TryGetEnemySpawnInfo(string enemyID)
        => TryGetObject($"EnemySpawnInfo_{enemyID}");

    public RszGameObject GetEnemyGenerator()
        => GetObject("EnemyGenerator");

    public RszGameObject GetEnemyFsmGenerator()
        => GetObject(EnemyFsmGeneratorTemplateName);

    public RszGameObject GetItemTemplate(string id) {
        if (!_itemTemplates.ContainsKey(id) && ItemDrops.DlcCoinDrops.Any(coin => coin.Id == id)) {
            // Purchased DLC coins have no scene templates. Reuse the base-game coin pickup
            // while retaining the purchased reward ID, including its _Buy suffix.
            var template = GetItemTemplate("Coin");
            var item = template.FindComponent("app.Item")!.Set("ItemDataID", id);
            _itemTemplates.Add(id, template.AddOrUpdateComponent(item));
        }
        if (!_itemTemplates.ContainsKey(id) && BirthdaySkillVisuals.TryGetResources(id, out var resources)) {
            BirthdaySkillVisuals.CopyRequiredFiles(_randomizer.FileRepository, id);
            var template = GetItemTemplate("Coin");
            var mesh = template.FindComponent("via.render.Mesh")!
                .Set("Mesh", new RszResourceNode(resources.Mesh))
                .Set("Material", new RszResourceNode(resources.Material));
            var item = template.FindComponent("app.Item")!.Set("ItemDataID", id);
            _itemTemplates.Add(id, template.AddOrUpdateComponent(mesh).AddOrUpdateComponent(item));
        }
        _itemTemplates.TryGetValue(id, out RszGameObject? result);
        return result ?? throw new Exception($"Item template {id} not found in template scene!");
    }
}
