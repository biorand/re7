using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Weapons;
using Biohazard.BioRand.RE7.REEngine;

namespace Biohazard.BioRand.RE7.Services;

internal class TemplateService {
    private const string TemplateSceneFileName = "template.scn";
    private const string EnemyFsmGeneratorTemplateName = "FsmGenerator";
    private readonly ScnFile _templateScnFile;
    private readonly RszScene _scene;
    private readonly Randomizer _randomizer;
    private readonly Dictionary<string, RszGameObject> _itemTemplates = new();
    private readonly HashSet<string> _dlcTemplates = new(StringComparer.Ordinal);

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
        if (!_dlcTemplates.Contains(id) && DlcCampaignWeapons.Contains(id)) {
            if (!DlcCampaignWeapons.IsEnabled(_randomizer.FileRepository))
                throw new InvalidOperationException($"DLC campaign weapons are disabled: {id}");
            var baseId = id switch {
                "CKnife" => "MiaKnife",
                "Handgun_Albert_C" => "Handgun_M19",
                "Shotgun_Albert" => "MachineGun",
                _ => "Shotgun_DB",
            };
            var definition = WeaponDefinitionRepository.Default.FromWeaponId(
                ItemDefinitionRepository.Default.FromId(id)!.WeaponId!.Value);
            var nativeWeapon = ReadDlcWeaponComponent(id);
            _itemTemplates[id] = CreateDlcWeaponTemplate(GetItemTemplate(baseId), id, definition, nativeWeapon);
            _dlcTemplates.Add(id);
        }
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

    internal static RszGameObject CreateDlcWeaponTemplate(RszGameObject template, string id, WeaponDefinition definition, RszObjectNode nativeWeapon) {
        return template.WithName($"ItemTemplate_{id}").WithPrefab("").Visit(node => {
            if (node is not RszObjectNode obj) return node;
            return obj.Type.Name switch {
                "app.Item" => obj.Set("ItemDataID", id),
                "app.fsm.ItemAddTest" => obj.Set("_ItemDataID", id),
                "app.WeaponGun" or "app.Weapon" => nativeWeapon,
                "via.render.Mesh" => obj.Set("Mesh", new RszResourceNode(definition.Mesh))
                    .Set("Material", new RszResourceNode(definition.Material)),
                _ => obj,
            };
        });
    }

    public RszGameObject RebindDlcPickup(RszGameObject pickup, string originalId, string id) {
        if (!DlcCampaignWeapons.Contains(id)) return pickup;
        var weapon = ReadDlcWeaponComponent(id);
        return pickup.Visit(node => node is RszObjectNode obj ? obj.Type.Name switch {
            "app.Weapon" or "app.WeaponGun" => weapon,
            "app.fsm.ItemAddTest" when obj.Get<string>("_ItemDataID") == originalId => obj.Set("_ItemDataID", id),
            _ => obj,
        } : node);
    }

    private RszObjectNode ReadDlcWeaponComponent(string id) {
        var source = DlcCampaignWeapons.Sources.Single(w => w.ItemId == id);
        var inventory = _randomizer.FileRepository.GetPfbFile(source.CampaignPrefab.Of() + ".17")
            .ReadScene(_randomizer.FileRepository.TypeRepository);
        return inventory.GetGameObjects().SelectMany(g => g.Components)
            .Single(c => c.Type.Name is "app.Weapon" or "app.WeaponGun");
    }
}
