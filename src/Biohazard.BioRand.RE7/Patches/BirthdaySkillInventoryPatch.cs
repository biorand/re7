using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Items;
using Enums.app;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Patches;

internal class BirthdaySkillInventoryPatch(IPatchContext context) : IPatch {
    private readonly string _birthdaySkillSettingsPath = "prefab/item/birthdayskillitemsetting.user".UserFile();
    private readonly string _keyItemSettingsPath = "prefab/item/keyitemsettings.user".UserFile();
    private readonly string _itemResourcesScenePath = "scenes/items/itemresources.scn".SceneFile();

    private readonly string _itemResourceTemplateScenePath =
        "scenes/items/resources/powerupcoin01a.scn".SceneFile();

    private readonly string _skillDropPrefabTemplatePath =
        GetPrefabPakPath("Prefab/Props_Dynamic/sm2479_PowerUpCoin01A/Get/sm2479_PowerUpCoin01A_Get.pfb");

    private readonly string _uiItemMessagePath = "message/ui_item_mes.msg".MessageFile();
    private readonly string _uiBirthdayMessagePath = "message/ui_birthday_mes.msg".MessageFile();

    public void Apply() {
        var birthdaySkills = context
            .DeserializeUserFile<app.ItemSettings>(_birthdaySkillSettingsPath)
            ._Settings
            .Where(IsBirthdaySkill)
            .OrderBy(x => x.ItemDataID, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var birthdaySkillValues = LoadBirthdaySkillValues(birthdaySkills);

        CopySkillInventoryAssets(birthdaySkills, birthdaySkillValues);
        ApplyKeyItemSettings(birthdaySkills);
        ApplyDropPrefabs(birthdaySkills);
        ApplyItemResources(birthdaySkills);
        ApplyBirthdayMessages(birthdaySkills, birthdaySkillValues);
    }

    private IReadOnlyDictionary<string, BirthdaySkillValueRow> LoadBirthdaySkillValues(
        IReadOnlyList<app.ItemData> birthdaySkills) {
        var csv = context.DynamicData.GetData(DynamicDataName.BirthdaySkills)
                  ?? throw new RandomizerUserException("Unable to load Birthday skill CSV data.");
        var rows = Serialization.Csv.Deserialize<BirthdaySkillValueRow>(csv)
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemDataID))
            .ToArray();

        var duplicateIds = rows
            .GroupBy(x => x.ItemDataID.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (duplicateIds.Length != 0) {
            throw new RandomizerUserException($"Duplicate Birthday skill CSV rows: {string.Join(", ", duplicateIds)}.");
        }

        var result = rows.ToDictionary(x => x.ItemDataID.Trim(), StringComparer.OrdinalIgnoreCase);
        var expectedIds = birthdaySkills
            .Select(x => x.ItemDataID)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingIds = expectedIds
            .Where(x => !result.ContainsKey(x))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingIds.Length != 0) {
            throw new RandomizerUserException($"Missing Birthday skill CSV rows: {string.Join(", ", missingIds)}.");
        }

        var unknownIds = result.Keys
            .Where(x => !expectedIds.Contains(x))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownIds.Length != 0) {
            throw new RandomizerUserException($"Unknown Birthday skill CSV rows: {string.Join(", ", unknownIds)}.");
        }

        return result;
    }

    private void CopySkillInventoryAssets(
        IReadOnlyList<app.ItemData> birthdaySkills,
        IReadOnlyDictionary<string, BirthdaySkillValueRow> birthdaySkillValues) {
        var visualTemplateMesh = GetSkillVisualTemplateMesh();
        foreach (var skill in birthdaySkills) {
            var itemPrefabPath = GetSkillItemPrefabPath(skill);
            CopyRequiredFile(itemPrefabPath, $"Birthday skill '{skill.ItemDataID}' item prefab");
            ApplySkillVisuals(itemPrefabPath, skill, visualTemplateMesh);
            CopyConfiguredPassiveSkillFile(skill, GetPassiveSkillUserPath(skill, birthdaySkillValues[skill.ItemDataID]),
                birthdaySkillValues[skill.ItemDataID]);
        }
    }

    private void CopyRequiredFile(string path, string description) {
        var data = context.GetFile(path) ??
                   throw new RandomizerUserException($"Unable to read {description} at '{path}'.");
        context.SetFile(path, data);
    }

    private void CopyConfiguredPassiveSkillFile(app.ItemData skill, string path, BirthdaySkillValueRow values) {
        ValidatePassiveSkillPath(skill, path, values.PassiveSkillUserPath);
        context.ModifyUserFile(path, root => {
            if (!string.Equals(root.Type.Name, "app.PlayerPassiveSkill", StringComparison.Ordinal)) {
                throw new RandomizerUserException(
                    $"Birthday skill '{skill.ItemDataID}' passive userdata is '{root.Type.Name}', expected 'app.PlayerPassiveSkill'.");
            }

            return ApplyBirthdaySkillValues(root, values);
        });
    }

    private static void ValidatePassiveSkillPath(app.ItemData skill, string path, string csvPath) {
        if (string.IsNullOrWhiteSpace(csvPath)) {
            return;
        }

        if (!string.Equals(
                NormalizeUserFilePath(path),
                NormalizeUserFilePath(csvPath),
                StringComparison.OrdinalIgnoreCase)) {
            throw new RandomizerUserException(
                $"Birthday skill '{skill.ItemDataID}' CSV row points to '{csvPath}', expected '{path}'.");
        }
    }

    private static string NormalizeUserFilePath(string path) {
        var result = path.Trim().Replace('\\', '/');
        const string prefix = "natives/stm/";
        if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            result = result[prefix.Length..];
        }

        var versionSuffix = $".{FileVersions.UserFileVersion}";
        if (result.EndsWith(versionSuffix, StringComparison.OrdinalIgnoreCase)) {
            result = result[..^versionSuffix.Length];
        }

        return result;
    }

    private static RszObjectNode ApplyBirthdaySkillValues(RszObjectNode root, BirthdaySkillValueRow values) {
        return root
            .Set("AttackChangeRate", values.AttackChangeRate)
            .Set("MeleeAttackChangeRate", values.MeleeAttackChangeRate)
            .Set("DyingAttackChangeRate", values.DyingAttackChangeRate)
            .Set("StunChangeRate", values.StunChangeRate)
            .Set("MaxHealthChangeValue", values.MaxHealthChangeValue)
            .Set("DamageChangeRate", values.DamageChangeRate)
            .Set("IdleAutoRecoverySpeedChangeValue", values.IdleAutoRecoverySpeedChangeValue)
            .Set("GuardDamageCutRateChangeValue", values.GuardDamageCutRateChangeValue)
            .Set("WalkSpeedChangeRate", values.WalkSpeedChangeRate)
            .Set("MoveSpeedChangeRate", values.MoveSpeedChangeRate)
            .Set("DyingMoveSpeedChangeRate", values.DyingMoveSpeedChangeRate)
            .Set("ReloadSpeedChangeRate", values.ReloadSpeedChangeRate)
            .Set("HitTimeBonusChangeRate", values.HitTimeBonusChangeRate)
            .Set("KillTimeBonusChangeRate", values.KillTimeBonusChangeRate)
            .Set("DamageTimeBonusChangeRate", values.DamageTimeBonusChangeRate)
            .Set("IsBulletStackNumInfinity", values.IsBulletStackNumInfinity)
            .Set("IsPsychostimulantEffectInfinity", values.IsPsychostimulantEffectInfinity);
    }

    private void ApplyKeyItemSettings(IReadOnlyList<app.ItemData> birthdaySkills) {
        var transformedSkillSettings = birthdaySkills
            .Select(CreateCampaignInventorySkill)
            .ToArray();

        context.ModifyUserFile<app.ItemSettings>(_keyItemSettingsPath, root => {
            root._Settings =[
                .. root._Settings.Where(x => !IsBirthdaySkill(x)),
                .. transformedSkillSettings
            ];
            return root;
        });
    }

    private void ApplyItemResources(IReadOnlyList<app.ItemData> birthdaySkills) {
        context.ModifyScnFile(_itemResourcesScenePath, scene => {
            var templateFolder = scene.Children
                .OfType<RszFolder>()
                .First(x => x.Name == "PowerUpCoin01A");
            var resourceIds = birthdaySkills
                .Select(x => x.ItemDataID)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var children = scene.Children
                .Where(child => child is not RszFolder folder || !resourceIds.Contains(folder.Name))
                .ToList();
            foreach (var skill in birthdaySkills) {
                children.Add(CreateResourceFolder(templateFolder, skill.ItemDataID));
            }

            return scene.WithChildren(children.ToImmutableArray());
        });

        var itemResourceTemplate = context.GetScnFile(_itemResourceTemplateScenePath);
        foreach (var skill in birthdaySkills) {
            var template = itemResourceTemplate.ToBuilder(context.TypeRepository);
            template.Scene = template.Scene.VisitComponents((_, component) =>
                component.Type.Name != "app.ItemResource"
                    ? component
                    : component
                        .Set("_ItemDataId", skill.ItemDataID)
                        .Set("_ResourcePrefab.Standby", true)
                        .Set("_ResourcePrefab.Path", GetSkillDropPrefabReference(skill)));
            context.SetScnFile(GetItemResourceScenePath(skill.ItemDataID), template.AddMissingResources().Build());
        }
    }

    private void ApplyDropPrefabs(IReadOnlyList<app.ItemData> birthdaySkills) {
        var dropPrefabTemplate = context.GetPfbFile(_skillDropPrefabTemplatePath);
        var visualTemplateMesh = GetSkillVisualTemplateMesh();
        foreach (var skill in birthdaySkills) {
            var template = dropPrefabTemplate.ToBuilder(context.TypeRepository);

            template.Scene = template.Scene
                .VisitGameObjects(gameObject => gameObject.Name switch{
                    "sm2479_PowerUpCoin01A_Get" => gameObject
                        .WithName($"{skill.ItemDataID}_Get"),
                    _ => gameObject
                })
                .VisitComponents((_, component) => {
                    return component.Type.Name switch{
                        "app.Item" => component
                            .Set("SaveGUID", $"BirthdaySkillDrop:{skill.ItemDataID}:Item".GetGuidHash())
                            .Set("ItemDataID", skill.ItemDataID)
                            .Set("ItemStackNum", 1),
                        "app.InteractDetailSearch" => component
                            .Set("SaveGUID", $"BirthdaySkillDrop:{skill.ItemDataID}:Interact".GetGuidHash())
                            .Set("IsGetItemCountEnabled", false),
                        _ => component,
                    };
                })
                .VisitGameObjects(gameObject => ApplySkillVisuals(gameObject, skill, visualTemplateMesh));

            context.SetPfbFile(GetSkillDropPrefabPath(skill), template.RebuildResources().Build());
        }
    }

    private RszObjectNode GetSkillVisualTemplateMesh() {
        return context.GetPfbFile(_skillDropPrefabTemplatePath)
                   .ReadScene(context.TypeRepository)
                   .GetGameObjects()
                   .Select(gameObject => gameObject.FindComponent("via.render.Mesh"))
                   .FirstOrDefault(mesh => mesh != null)
               ?? throw new RandomizerUserException(
                   $"Birthday skill visual template '{_skillDropPrefabTemplatePath}' has no via.render.Mesh component.");
    }

    private void ApplySkillVisuals(string prefabPath, app.ItemData skill, RszObjectNode visualTemplateMesh) {
        var prefab = context.GetPfbFile(prefabPath).ToBuilder(context.TypeRepository);
        prefab.Scene =
            prefab.Scene.VisitGameObjects(gameObject => ApplySkillVisuals(gameObject, skill, visualTemplateMesh));
        context.SetPfbFile(prefabPath, prefab.RebuildResources().Build());
    }

    private static RszGameObject ApplySkillVisuals(
        RszGameObject gameObject,
        app.ItemData skill,
        RszObjectNode visualTemplateMesh) {
        if (!IsItemGameObject(gameObject, skill.ItemDataID) ||
            !BirthdaySkillVisuals.TryGetResources(skill.ItemDataID, out var visualResources)) {
            return gameObject;
        }

        var mesh = gameObject.FindComponent("via.render.Mesh") ?? visualTemplateMesh;
        mesh = mesh
            .Set("Mesh", new RszResourceNode(visualResources.Mesh))
            .Set("Material", new RszResourceNode(visualResources.Material));
        return BirthdaySkillVisuals.ApplyRotationCorrection(gameObject.AddOrUpdateComponent(mesh));
    }

    private static bool IsItemGameObject(RszGameObject gameObject, string itemDataId) {
        var item = gameObject.FindComponent("app.Item");
        return item != null &&
               string.Equals(item.Get<string>("ItemDataID"), itemDataId, StringComparison.OrdinalIgnoreCase);
    }

    private string GetPassiveSkillUserPath(app.ItemData skill, BirthdaySkillValueRow values) {
        if (!string.IsNullOrWhiteSpace(values.PassiveSkillUserPath)) {
            return NormalizeUserFilePath(values.PassiveSkillUserPath).UserFile();
        }

        var itemPrefab = context.GetPfbFile(GetSkillItemPrefabPath(skill))
            .ReadScene(context.TypeRepository);
        var passiveSkillComponent = itemPrefab.GetGameObjects()
                                        .Select(x => x.FindComponent("app.PassiveSkillItem"))
                                        .FirstOrDefault(x => x != null)
                                    ?? throw new RandomizerUserException(
                                        $"Birthday skill '{skill.ItemDataID}' item prefab has no app.PassiveSkillItem component.");

        if (passiveSkillComponent["PassiveSkill"] is not RszUserDataNode passiveSkill) {
            throw new RandomizerUserException(
                $"Birthday skill '{skill.ItemDataID}' passive component has no PlayerPassiveSkill userdata.");
        }

        return passiveSkill.Path.UserFile();
    }

    private void ApplyBirthdayMessages(
        IReadOnlyList<app.ItemData> birthdaySkills,
        IReadOnlyDictionary<string, BirthdaySkillValueRow> birthdaySkillValues) {
        if (!context.Exists(_uiBirthdayMessagePath)) {
            return;
        }

        var birthdayMessages = context.GetMsgFile(_uiBirthdayMessagePath);
        context.ModifyMsgFile(_uiItemMessagePath, itemMessages => {
            foreach (var skill in birthdaySkills) {
                CopyMessageIfMissing(itemMessages, birthdayMessages, skill.NameMsg);
                ApplyInventoryNameOverride(
                    itemMessages,
                    birthdayMessages,
                    skill.NameMsg,
                    birthdaySkillValues[skill.ItemDataID].Name);
                CopyMessageIfMissing(itemMessages, birthdayMessages, skill.ManualMsg);
                ApplyInventoryDescriptionOverride(
                    itemMessages,
                    birthdayMessages,
                    skill.ManualMsg,
                    birthdaySkillValues[skill.ItemDataID].InventoryDescription);
            }
        });
    }

    private static bool IsBirthdaySkill(app.ItemData item)
        => item.ItemDataID.StartsWith("skl", StringComparison.OrdinalIgnoreCase)
           && !item.ItemDataID.EndsWith("no", StringComparison.OrdinalIgnoreCase); // Exclude dummy skills

    private static app.ItemData CreateCampaignInventorySkill(app.ItemData source) {
        var cloned = Clone(source);

        cloned.Category = Enums.app.Item.ItemCategoryType.KeyItem;
        cloned.SortCategory = ItemSortCategory.EquipItem;
        cloned.SortPriority = 100 + ParseSkillNumber(source.ItemDataID);
        cloned.MaxStackNum = 1;
        cloned.CanStoreItembox = true;
        cloned.DropItemSetting = new app.ItemData.DropItemData{
            DropItemPrefab = new via.Prefab{
                Standby = true,
                Path = GetSkillDropPrefabReference(source),
            }
        };

        return cloned;
    }

    private static int ParseSkillNumber(string itemDataId)
        => int.TryParse(itemDataId.AsSpan(3), out var value) ? value : 0;

    private static RszFolder CreateResourceFolder(RszFolder template, string itemDataId) {
        return new RszFolder(
            template.Settings
                .Set("Name", itemDataId)
                .Set("ScenePath", GetItemResourceSceneReference(itemDataId)),
            []);
    }

    internal static void CopyMessageIfMissing(MsgFile.Builder destination, MsgFile source, Guid guid) {
        if (guid == Guid.Empty || destination.FindMessage(guid) != null) {
            return;
        }

        var sourceMessage = source.FindMessage(guid);
        if (sourceMessage == null) {
            return;
        }

        destination.Messages.Add(CreateDestinationMessage(destination, sourceMessage));
    }

    private static Msg CreateDestinationMessage(MsgFile.Builder destination, Msg source) {
        var sourceValues = source.Values.ToDictionary(x => x.Language, x => x.Text);
        var englishFallback = sourceValues.TryGetValue(LanguageId.English, out var english)
            ? english
            : sourceValues.Values.FirstOrDefault() ?? string.Empty;

        return new Msg{
            Guid = source.Guid,
            Crc = source.Crc,
            Name = source.Name,
            Values =[
                .. destination.Languages.Select(language => new MsgValue(
                    language,
                    sourceValues.TryGetValue(language, out var text) ? text : englishFallback))
            ],
            Attributes =[
                .. destination.Attributes.Select(CreateDefaultAttributeValue)
            ]
        };
    }

    private static void ApplyInventoryNameOverride(
        MsgFile.Builder destination,
        MsgFile source,
        Guid guid,
        string? inventoryName) {
        if (string.IsNullOrWhiteSpace(inventoryName)) {
            return;
        }

        ApplyMessageTextOverride(destination, source, guid, inventoryName);
    }

    private static void ApplyInventoryDescriptionOverride(
        MsgFile.Builder destination,
        MsgFile source,
        Guid guid,
        string? inventoryDescription) {
        if (guid == Guid.Empty || inventoryDescription == null) {
            return;
        }

        ApplyMessageTextOverride(destination, source, guid, inventoryDescription);
    }

    private static void ApplyMessageTextOverride(
        MsgFile.Builder destination,
        MsgFile source,
        Guid guid,
        string text) {
        if (guid == Guid.Empty) {
            return;
        }

        if (destination.FindMessage(guid) == null) {
            return;
        }

        var normalizedText = NormalizeMessageText(DecodeCsvMessageText(text));
        var sourceText = GetMessageText(source, guid, LanguageId.English);
        if (sourceText != null &&
            NormalizeMessageText(sourceText) == normalizedText) {
            return;
        }

        destination.SetStringAll(guid, ToMsgLineEndings(normalizedText));
    }

    private static string? GetMessageText(MsgFile source, Guid guid, LanguageId language) {
        var message = source.FindMessage(guid);
        return message == null
            ? null
            : (from value in message.Values where value.Language == language select value.Text).FirstOrDefault();
    }

    private static string DecodeCsvMessageText(string text) {
        if (!text.Contains('\\', StringComparison.Ordinal)) {
            return text;
        }

        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (c != '\\' || i + 1 >= text.Length) {
                result.Append(c);
                continue;
            }

            var escape = text[++i];
            switch (escape) {
                case 'r':
                    result.Append('\r');
                    break;
                case 'n':
                    result.Append('\n');
                    break;
                case 't':
                    result.Append('\t');
                    break;
                case '"':
                    result.Append('"');
                    break;
                case '\\':
                    result.Append('\\');
                    break;
                case 'u' when i + 4 < text.Length && TryParseHex(text.AsSpan(i + 1, 4), out var value):
                    result.Append((char)value);
                    i += 4;
                    break;
                default:
                    result.Append('\\');
                    result.Append(escape);
                    break;
            }
        }

        return result.ToString();
    }

    private static bool TryParseHex(ReadOnlySpan<char> text, out int value) {
        value = 0;
        foreach (var c in text) {
            var digit = HexValue(c);
            if (digit < 0) {
                value = 0;
                return false;
            }

            value = (value << 4) | digit;
        }

        return true;
    }

    private static int HexValue(char c) {
        return c switch{
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };
    }

    private static string NormalizeMessageText(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string ToMsgLineEndings(string normalizedText)
        => normalizedText.Replace("\n", "\r\n", StringComparison.Ordinal);

    private static MsgAttributeValue CreateDefaultAttributeValue(MsgAttributeDefinition definition) {
        return definition.Type switch{
            MsgAttributeType.Wstring => new MsgAttributeValue(definition, string.Empty),
            MsgAttributeType.Int64 => new MsgAttributeValue(definition, 0L),
            MsgAttributeType.Double => new MsgAttributeValue(definition, 0d),
            _ => new MsgAttributeValue(definition, 0UL),
        };
    }

    private static string GetItemResourceScenePath(string itemDataId)
        => $"scenes/items/resources/{itemDataId}.scn".SceneFile();

    private static string GetItemResourceSceneReference(string itemDataId)
        => $"Scenes/Items/Resources/{itemDataId}.scn";

    private static string GetSkillDropPrefabReference(app.ItemData skill) {
        var inventoryPrefabPath = skill.ItemPrefab.Path.ToString();
        if (string.IsNullOrWhiteSpace(inventoryPrefabPath) ||
            !inventoryPrefabPath.EndsWith(".pfb", StringComparison.OrdinalIgnoreCase)) {
            return $"Prefab/Skill/{skill.ItemDataID}/{skill.ItemDataID}Get.pfb";
        }

        return $"{inventoryPrefabPath[..^".pfb".Length]}Get.pfb";
    }

    private static string GetSkillDropPrefabPath(app.ItemData skill)
        => GetPrefabPakPath(GetSkillDropPrefabReference(skill));

    private static string GetSkillItemPrefabPath(app.ItemData skill) {
        var itemPrefabPath = skill.ItemPrefab.Path.ToString();
        if (string.IsNullOrWhiteSpace(itemPrefabPath)) {
            throw new RandomizerUserException($"Birthday skill '{skill.ItemDataID}' has no item prefab.");
        }

        return GetPrefabPakPath(itemPrefabPath);
    }

    private static string GetPrefabPakPath(string prefabPath)
        => $"{prefabPath.Of()}.{FileVersions.PfbFileVersion}".ToLowerInvariant();

    private static via.Prefab CreatePrefabReference(object path)
        => new(){
            Standby = true,
            Path = path,
        };

    private static app.ItemData.DropItemData CreateDropItemData(object path)
        => new(){
            DropItemPrefab = CreatePrefabReference(path),
        };

    private static via.Prefab NormalizePrefab(via.Prefab source)
        => CreatePrefabReference(source.Path);

    private static app.ItemData.DropItemData NormalizeDropItemData(via.Prefab source)
        => CreateDropItemData(source.Path);

    private static app.ItemData Clone(app.ItemData source)
        => new(){
            _Comment = source._Comment,
            ItemDataID = source.ItemDataID,
            NameMsg = source.NameMsg,
            ManualMsg = source.ManualMsg,
            Category = source.Category,
            SortCategory = source.SortCategory,
            SortPriority = source.SortPriority,
            SlotSize = source.SlotSize,
            MaxStackNum = source.MaxStackNum,
            CanStoreItembox = source.CanStoreItembox,
            ItemPrefab = NormalizePrefab(source.ItemPrefab),
            WeaponSetting = Clone(source.WeaponSetting),
            UISetting = Clone(source.UISetting),
            DropItemSetting = NormalizeDropItemData(source.ItemPrefab),
        };

    private static app.ItemData.WeaponData Clone(app.ItemData.WeaponData source)
        => new(){
            ReticleType = source.ReticleType,
            WeaponInfoType = source.WeaponInfoType,
        };

    private static app.ItemData.UIData Clone(app.ItemData.UIData source)
        => new(){
            IconFrameNo = source.IconFrameNo,
            RoomID = source.RoomID,
            MapIconFrameNo = source.MapIconFrameNo,
        };

    private sealed class BirthdaySkillValueRow {
        public string ItemDataID { get; set; } = "";
        public string Name { get; set; } = "";
        public string PassiveSkillUserPath { get; set; } = "";
        public string? InventoryDescription { get; set; }
        public float AttackChangeRate { get; set; }
        public float MeleeAttackChangeRate { get; set; }
        public float DyingAttackChangeRate { get; set; }
        public float StunChangeRate { get; set; }
        public float MaxHealthChangeValue { get; set; }
        public float DamageChangeRate { get; set; }
        public float IdleAutoRecoverySpeedChangeValue { get; set; }
        public float GuardDamageCutRateChangeValue { get; set; }
        public float WalkSpeedChangeRate { get; set; }
        public float MoveSpeedChangeRate { get; set; }
        public float DyingMoveSpeedChangeRate { get; set; }
        public float ReloadSpeedChangeRate { get; set; }
        public float HitTimeBonusChangeRate { get; set; }
        public float KillTimeBonusChangeRate { get; set; }
        public float DamageTimeBonusChangeRate { get; set; }
        public bool IsBulletStackNumInfinity { get; set; }
        public bool IsPsychostimulantEffectInfinity { get; set; }
    }
}
