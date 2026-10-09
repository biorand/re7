using Biohazard.BioRand.RE7.REEngine;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Weapons;

internal sealed class DlcWeaponImporter(IPatchContext context) {
    private const string CampaignSettings = "natives/stm/prefab/item/resourceitemsettings.user.2";
    private const string CampaignMessages = "natives/stm/message/ui_item_mes.msg.17";

    public void Apply(IReadOnlyList<DlcWeaponSource> candidates, bool isolateParameters = false) {
        var settings = candidates.Select(w => w.Chapter).Distinct().ToDictionary(
            chapter => chapter, chapter => context.DeserializeUserFile<app.ItemSettings>(DlcWeaponCatalog.SettingsPath(chapter)));
        var campaign = context.DeserializeUserFile<app.ItemSettings>(CampaignSettings);
        var imported = new List<app.ItemData>();
        var messages = context.GetMsgFile(CampaignMessages).ToBuilder();
        foreach (var weapon in candidates) {
            var item = settings[weapon.Chapter]._Settings.Single(x => x.ItemDataID == weapon.ItemId);
            var sourcePath = PrefabPath(item.ItemPrefab.Path.ToString()!);
            var prefab = context.GetPfbFile(sourcePath).ToBuilder(context.TypeRepository);
            prefab.Scene = AdaptPrefab(prefab.Scene, context.TypeRepository, weapon);
            if (isolateParameters) {
                prefab.Scene = IsolateParameters(prefab.Scene, weapon);
                prefab.Scene = prefab.Scene.VisitGameObjects(go => go.Components.Any(c => c.Type.Name is "app.Weapon" or "app.WeaponGun")
                    && !go.Components.Any(c => c.Type.Name == "app.WeaponMotionController")
                    ? go.AddOrUpdateComponent(context.TypeRepository.Create("app.WeaponMotionController").Set("Enabled", true)) : go);
            }
            context.SetPfbFile(PrefabPath(weapon.CampaignPrefab), prefab.RebuildResources().Build());

            // These are new serialized objects, not changes to the source DLC settings/prefabs.
            item.ItemPrefab = new via.Prefab { Standby = true, Path = weapon.CampaignPrefab };
            item.CanStoreItembox = true;
            imported.Add(item);
            var sourceMessages = context.GetMsgFile((weapon.Chapter == 8
                ? "ch8/message/ch8_item_mes.msg" : "message/ch9_item_mes.msg").MessageFile());
            CopyMessage(messages, sourceMessages, item.NameMsg);
            CopyMessage(messages, sourceMessages, item.ManualMsg);
            ImportItemResource(weapon);
        }

        var ids = imported.Select(x => x.ItemDataID).ToHashSet(StringComparer.Ordinal);
        campaign._Settings = [.. campaign._Settings.Where(x => !ids.Contains(x.ItemDataID)), .. imported];
        context.SerializeUserFile(CampaignSettings, campaign);
        context.SetMsgFile(CampaignMessages, messages.Build());
        RegisterItemResources(candidates);
    }

    private RszScene IsolateParameters(RszScene scene, DlcWeaponSource weapon) {
        // Joe's M21 intentionally shares campaign M21 parameters and stat controls.
        if (weapon.Chapter != 8) return scene;
        var definition = DlcCampaignWeapons.CreateWeaponDefinitions().Single(w => (int)w.WeaponId == weapon.WeaponId);
        if (weapon.Adapter == DlcWeaponAdapter.Gun) {
            var gun = scene.GetGameObjects().SelectMany(go => go.Components).Single(c => c.Type.Name == "app.WeaponGun");
            var parameter = gun.Get<RszUserDataNode>("WeaponGunParameter");
            context.SetFile(definition.UserParamsPath!, context.GetFile(parameter.Path.UserFile())
                ?? throw new InvalidDataException($"Missing weapon parameters: {parameter.Path}"));
            return scene.Visit(node => node is RszUserDataNode u && u.Path == parameter.Path
                ? new RszUserDataNode(u.Type, weapon.ParameterPath) : node);
        }
        var source = "CH8/Collision/Collider/Weapon/wp1390/wp1390_ChrisKnife.rcol";
        context.SetFile(definition.RcolPaths.Single(), context.GetFile(source.RcolFile())
            ?? throw new InvalidDataException($"Missing knife collision: {source}"));
        return scene.Visit(node => node is RszResourceNode r && string.Equals(r.Value, source, StringComparison.OrdinalIgnoreCase)
            ? new RszResourceNode(weapon.CollisionPath) : node);
    }

    private void ImportItemResource(DlcWeaponSource weapon) {
        var resource = context.GetScnFile(weapon.SourceResourceScene.SceneFile()).ToBuilder(context.TypeRepository);
        var component = resource.Scene.GetGameObjects().SelectMany(go => go.Components)
            .Single(c => c.Type.Name == "app.ItemResource");
        if (component.Get<string>("_ItemDataId") != weapon.ItemId)
            throw new InvalidOperationException($"Unexpected item resource for {weapon.ItemId}.");
        var detail = component.Get<RszResourceNode>("_ResourcePrefab.Path").Value
            ?? throw new InvalidOperationException($"Missing detail prefab for {weapon.ItemId}.");
        context.SetPfbFile(PrefabPath(weapon.DetailPrefab), context.GetPfbFile(PrefabPath(detail)));
        resource.Scene = resource.Scene.VisitComponents((_, c) => c != component ? c : c
            .Set("_ResourcePrefab.Path", new RszResourceNode(weapon.DetailPrefab))
            .Set("_ResourcePrefab.Standby", true));
        context.SetScnFile(weapon.ResourceScene.SceneFile(), resource.RebuildResources().Build());
    }

    private void RegisterItemResources(IReadOnlyList<DlcWeaponSource> weapons) {
        context.ModifyScnFile("scenes/items/itemresources.scn".SceneFile(), scene => {
            var template = scene.Children.OfType<RszFolder>().First(f => f.Name == "PowerUpCoin01A");
            var ids = weapons.Select(w => w.ItemId).ToHashSet(StringComparer.Ordinal);
            return scene.WithChildren([
                .. scene.Children.Where(c => c is not RszFolder f || !ids.Contains(f.Name)),
                .. weapons.Select(w => new RszFolder(template.Settings
                    .Set("Name", w.ItemId).Set("ScenePath", w.ResourceScene), [])),
            ]);
        });
    }

    internal static RszScene AdaptPrefab(RszScene scene, RszTypeRepository types, DlcWeaponSource weapon) {
        if (!weapon.IsLabCandidate)
            throw new InvalidOperationException($"{weapon.ItemId} requires a player-system adapter; no campaign prefab is available.");

        var source = scene.GetGameObjects().SelectMany(go => go.Components)
            .SingleOrDefault(c => c.Type.Name == weapon.ComponentType);
        if (source == null || source.Get<int>("WeaponID") != weapon.WeaponId)
            throw new InvalidOperationException($"Unexpected component or WeaponID for {weapon.ItemId}.");

        return scene.VisitGameObjects(go => go.WithComponents(go.Components
            .Where(c => c.Type.Name is not ("app.DisableSave" or "app.CH8HandgunBulletSound"
                or "app.CH8ReticleChanger" or "app.CH8ReticleMaterialChanger"))
            .Select(c => {
                if (c != source || weapon.Adapter != DlcWeaponAdapter.Gun) return c;

                var gun = types.Create("app.WeaponGun");
                foreach (var field in gun.Type.Fields) gun = gun.Set(field.Name, c[field.Name]);
                return AdaptAmmunition(gun);
            }).ToImmutableArray()));
    }

    private static RszObjectNode AdaptAmmunition(RszObjectNode gun) {
        // The base gun/bullet code supports WeaponID 49/50, but not CH8 RAMROD semantics.
        // Keep native weapon identity and parameters; use campaign ammunition.
        var bullets = gun.Get<RszArrayNode>("BulletInfoList");
        var adapted = bullets.Cast<RszObjectNode>()
            .Where(obj => obj.Get<int>("BulletItemID") != (int)Enums.app.ItemID.AlbertHandgunBulletL)
            .Select(obj => {
                var id = obj.Get<int>("BulletItemID");
                if (id == (int)Enums.app.ItemID.AlbertHandgunBullet)
                    return obj.Set("BulletItemID", (int)Enums.app.ItemID.HandgunBullet);
                if (id == (int)Enums.app.ItemID.AlbertShotgunBullet)
                    return obj.Set("BulletItemID", (int)Enums.app.ItemID.ShotgunBullet);
                return obj;
            }).Cast<IRszNode>().ToImmutableArray();
        return gun.Set("BulletInfoList", new RszArrayNode(bullets.Type, adapted));
    }

    private static string PrefabPath(string reference) => $"{reference.Of()}.{FileVersions.PfbFileVersion}".ToLowerInvariant();

    private static void CopyMessage(MsgFile.Builder destination, MsgFile source, Guid id) {
        if (id == Guid.Empty || destination.FindMessage(id) != null) return;
        var message = source.FindMessage(id) ?? throw new InvalidOperationException($"Missing DLC item message {id}.");
        var values = message.Values.ToDictionary(x => x.Language, x => x.Text);
        var fallback = values.GetValueOrDefault(LanguageId.English) ?? "";
        destination.Messages.Add(new Msg {
            Guid = message.Guid, Crc = message.Crc, Name = message.Name,
            Values = [.. destination.Languages.Select(language => new MsgValue(language, values.GetValueOrDefault(language) ?? fallback))],
            Attributes = [.. destination.Attributes.Select(attribute => attribute.Type switch {
                MsgAttributeType.Wstring => new MsgAttributeValue(attribute, string.Empty),
                MsgAttributeType.Int64 => new MsgAttributeValue(attribute, 0L),
                MsgAttributeType.Double => new MsgAttributeValue(attribute, 0d),
                _ => new MsgAttributeValue(attribute, 0UL),
            })],
        });
    }
}
