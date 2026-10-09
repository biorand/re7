using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Patches;

[ExportMod(Name = "DLC Weapon Lab", FileName = "dlc-weapon-lab", Version = "0.1.0",
    Author = "BioRand", Description = "Experimental campaign weapon adapters. Not certified for a saved playthrough.")]
internal sealed class DlcWeaponLabPatch(IPatchContext context) : IPatch {
    private const string CampaignSettings = "natives/stm/prefab/item/resourceitemsettings.user.2";
    private const string CampaignMessages = "natives/stm/message/ui_item_mes.msg.17";

    public void Apply() {
        // ApplyAll discovers every IPatch. This experiment must never affect seed generation.
        if (!context.ExportingMod) return;

        var candidates = DlcWeaponCatalog.Weapons.Where(w => w.IsLabCandidate).ToArray();
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
            context.SetPfbFile(PrefabPath(weapon.CampaignPrefab), prefab.RebuildResources().Build());

            // These are new serialized objects, not changes to the source DLC settings/prefabs.
            item.ItemPrefab = new via.Prefab { Standby = true, Path = weapon.CampaignPrefab };
            item.CanStoreItembox = true;
            imported.Add(item);
            var sourceMessages = context.GetMsgFile((weapon.Chapter == 8
                ? "ch8/message/ch8_item_mes.msg" : "message/ch9_item_mes.msg").MessageFile());
            CopyMessage(messages, sourceMessages, item.NameMsg);
            CopyMessage(messages, sourceMessages, item.ManualMsg);
        }

        var ids = imported.Select(x => x.ItemDataID).ToHashSet(StringComparer.Ordinal);
        campaign._Settings = [.. campaign._Settings.Where(x => !ids.Contains(x.ItemDataID)), .. imported];
        context.SerializeUserFile(CampaignSettings, campaign);
        context.SetMsgFile(CampaignMessages, messages.Build());
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
        // Keep native weapon identity and parameters; use campaign ammunition for this lab.
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
