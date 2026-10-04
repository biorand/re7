using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using Enums.app;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Rsz;
using System.Numerics;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerBirthdaySkillInventoryBehaviorTests {
    private const string Skl001ItemPrefabPath = "Prefab/Skill/skl001/Skl001.pfb";
    private const string Skl001DropPrefabPath = "Prefab/Skill/skl001/Skl001Get.pfb";
    private const string Skl001PassiveSkillUserPath = "Prefab/Skill/skl001/Skl001PassiveSkill.user";

    [Fact]
    public void BirthdaySkills_AreInjectedIntoCampaignKeyItemSettings_AsEquipItems() {
        using var result = RandomizerTest.RunState();

        var settings = result.ReadAfterUserFile<app.ItemSettings>(RandomizerTestPaths.KeyItemSettingsPath)._Settings;
        var skill = settings.SingleOrDefault(x => x.ItemDataID == "skl001");

        Assert.True(result.WasFileModified(RandomizerTestPaths.KeyItemSettingsPath));
        Assert.NotNull(skill);
        Assert.Equal(Enums.app.Item.ItemCategoryType.KeyItem, skill!.Category);
        Assert.Equal(ItemSortCategory.EquipItem, skill.SortCategory);
        Assert.Equal(Enums.app.Item.ItemSlotSize.Slot2, skill.SlotSize);
        Assert.Equal(Skl001ItemPrefabPath, skill.ItemPrefab.Path.ToString());
        Assert.Equal(Skl001DropPrefabPath, skill.DropItemSetting.DropItemPrefab.Path.ToString());
        Assert.DoesNotContain(settings, x => x.ItemDataID == "skl001no");
        Assert.Equal(23, settings.Count(x => x.ItemDataID.StartsWith("skl", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void BirthdaySkills_AreRegisteredInCampaignItemResources_AndUiMessages() {
        using var result = RandomizerTest.RunState();

        var itemResources = result.ReadAfterScene(RandomizerTestPaths.ItemResourcesScenePath);
        var skillFolder = itemResources.Children
            .OfType<IntelOrca.Biohazard.REE.Rsz.RszFolder>()
            .SingleOrDefault(x => x.Name == "skl001");
        var skillResourceScenePath = "scenes/items/resources/skl001.scn".SceneFile();
        var skillResourceScene = result.ReadAfterScene(skillResourceScenePath);
        var resourceNode = skillResourceScene.FindGameObject("ItemResource")?.FindComponent("app.ItemResource");
        var dropPrefabPath = PrefabPath(Skl001DropPrefabPath);
        var dropPrefab = result.ReadAfterPfb(dropPrefabPath);
        var itemPrefabPath = PrefabPath(Skl001ItemPrefabPath);
        var itemPrefab = result.ReadAfterPfb(itemPrefabPath);
        var passiveSkillUserPath = Skl001PassiveSkillUserPath.UserFile();
        var dropItem = dropPrefab.GetGameObjects()
            .Select(x => x.FindComponent("app.Item"))
            .FirstOrDefault(x => x != null);
        var dropInteract = dropPrefab.GetGameObjects()
            .Select(x => x.FindComponent("app.InteractDetailSearch"))
            .FirstOrDefault(x => x != null);
        var dropPassiveSkill = dropPrefab.GetGameObjects()
            .Select(x => x.FindComponent("app.PassiveSkillItem"))
            .FirstOrDefault(x => x != null);
        var itemPassiveSkill = itemPrefab.GetGameObjects()
            .Select(x => x.FindComponent("app.PassiveSkillItem"))
            .FirstOrDefault(x => x != null);
        var birthdaySettings =
            result.ReadBeforeUserFile<app.ItemSettings>("prefab/item/birthdayskillitemsetting.user".UserFile());
        var skillSetting = birthdaySettings._Settings.Single(x => x.ItemDataID == "skl001");
        var uiItemMessages = result.ReadAfterMsgFile(RandomizerTestPaths.UiItemMessagePath);

        Assert.True(result.WasFileModified(RandomizerTestPaths.ItemResourcesScenePath));
        Assert.NotNull(skillFolder);
        Assert.Equal("Scenes/Items/Resources/skl001.scn", ((RszResourceNode)skillFolder!.Settings["ScenePath"]).Value);
        Assert.NotNull(resourceNode);
        Assert.Equal("skl001", ((RszStringNode)resourceNode!["_ItemDataId"]).Value);
        Assert.Equal(
            Skl001DropPrefabPath,
            ((RszResourceNode)((RszObjectNode)resourceNode["_ResourcePrefab"])["Path"]).Value);
        Assert.True(result.WasFileModified(dropPrefabPath));
        Assert.NotNull(dropItem);
        Assert.Equal("skl001", ((RszStringNode)dropItem!["ItemDataID"]).Value);
        Assert.NotNull(dropInteract);
        Assert.Null(dropPassiveSkill);
        Assert.True(result.WasFileModified(itemPrefabPath));
        Assert.True(result.WasFileModified(passiveSkillUserPath));
        Assert.NotNull(itemPassiveSkill);
        Assert.Equal(
            Skl001PassiveSkillUserPath,
            ((RszUserDataNode)itemPassiveSkill!["PassiveSkill"]).Path);
        AssertSkillVisualResources(
            itemPrefab,
            "skl001",
            "Props/sm9958_skillpatch01/sm9958_skillpatch01.mesh",
            "Props/sm9958_skillpatch01/skl001/skl001.mdf2");
        AssertSkillVisualResources(
            dropPrefab,
            "skl001",
            "Props/sm9958_skillpatch01/sm9958_skillpatch01.mesh",
            "Props/sm9958_skillpatch01/skl001/skl001.mdf2");
        var passiveSkill = ReadAfterPassiveSkillUser(result, Skl001PassiveSkillUserPath);
        Assert.Equal(0.5f, passiveSkill.Get<float>("ReloadSpeedChangeRate"));
        Assert.Equal(0f, passiveSkill.Get<float>("HitTimeBonusChangeRate"));
        Assert.True(passiveSkill.Get<bool>("IsBulletStackNumInfinity"));
        Assert.NotNull(uiItemMessages.FindMessage(skillSetting.NameMsg));
        Assert.NotNull(uiItemMessages.FindMessage(skillSetting.ManualMsg));
        Assert.Equal("Infinite Ammo", uiItemMessages.GetString(skillSetting.NameMsg, LanguageId.English));
        Assert.Equal(
            "Infinite ammo.\r\n50% Increased reload speed.",
            uiItemMessages.GetString(skillSetting.ManualMsg, LanguageId.English));
    }

    [Fact]
    public void BirthdaySkillVisuals_ReferenceOverlayPatchMeshesAndMaterials() {
        using var result = RandomizerTest.RunState();
        var dropTemplate = ReadBeforePfb(
            result,
            PrefabPath("Prefab/Props_Dynamic/sm2479_PowerUpCoin01A/Get/sm2479_PowerUpCoin01A_Get.pfb"));
        var dropTemplateRotation = FindItemTransform(dropTemplate, "PowerUpCoin01A").Rotation;

        foreach (var (itemDataId, meshFolder) in ExpectedSkillVisuals()) {
            var beforeItemPrefab = ReadBeforePfb(result, PrefabPath(GetSkillItemPrefabPath(itemDataId)));
            var itemPrefab = result.ReadAfterPfb(PrefabPath(GetSkillItemPrefabPath(itemDataId)));
            var dropPrefab = result.ReadAfterPfb(PrefabPath(GetSkillDropPrefabPath(itemDataId)));
            var mesh = $"Props/{meshFolder}/{meshFolder}.mesh";
            var material = $"Props/{meshFolder}/{itemDataId}/{itemDataId}.mdf2";

            AssertSkillVisualResources(
                itemPrefab,
                itemDataId,
                mesh,
                material,
                BirthdaySkillVisuals.CorrectRotation(FindItemTransform(beforeItemPrefab, itemDataId).Rotation));
            AssertSkillVisualResources(
                dropPrefab,
                itemDataId,
                mesh,
                material,
                BirthdaySkillVisuals.CorrectRotation(dropTemplateRotation));
        }

        Assert.DoesNotContain(result.ChangedFiles.Keys, IsBirthdaySkillOverlayAsset);
        Assert.DoesNotContain(result.AdditionalAssetFiles.Keys, IsBirthdaySkillOverlayAsset);
    }

    [Fact]
    public void BirthdaySkillVisuals_RotationCorrection_ReducesQuaternionXOnly() {
        var rotation = new Quaternion(-0.4470568f, 0.4819061f, 0.5673797f, 0.495448f);

        var corrected = BirthdaySkillVisuals.CorrectRotation(rotation);

        Assert.Equal(-0.2235284f, corrected.X, 6);
        Assert.Equal(rotation.Y, corrected.Y);
        Assert.Equal(rotation.Z, corrected.Z);
        Assert.Equal(rotation.W, corrected.W);
    }

    [Fact]
    public void BirthdaySkillCsvValues_AreReadFromCsv() {
        const string customNameCsv = "\"Custom \"\"Ammo\"\" \\u03b1\"";
        const string customName = "Custom \"Ammo\" \u03b1";
        const string customDescriptionCsv =
            "\"Custom birthday skill, line 1\\r\\nline 2 with \"\"quotes\"\" and \\u03b1.\"";
        const string customDescription =
            "Custom birthday skill, line 1\r\nline 2 with \"quotes\" and \u03b1.";
        var csv = System.Text.Encoding.UTF8.GetString(EmbeddedData.GetFile("birthday_skills.csv"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = csv.Split('\n');
        var header = lines[0].Split(',');
        var skl001 = lines[1].Split(',');
        SetCsvColumn("Name", customNameCsv);
        SetCsvColumn("InventoryDescription", customDescriptionCsv);
        SetCsvColumn("AttackChangeRate", "1.25");
        SetCsvColumn("ReloadSpeedChangeRate", "0.75");
        SetCsvColumn("IsBulletStackNumInfinity", "FALSE");
        SetCsvColumn("IsPsychostimulantEffectInfinity", "TRUE");
        lines[1] = string.Join(",", skl001);
        csv = string.Join("\r\n", lines);

        using var result = RandomizerTest.RunState(prepareRandomizer: randomizer => {
            randomizer.DynamicData.SetData(DynamicDataName.BirthdaySkills, System.Text.Encoding.UTF8.GetBytes(csv));
        });

        var passiveSkill = ReadAfterPassiveSkillUser(result, Skl001PassiveSkillUserPath);
        var skillSetting = result
            .ReadAfterUserFile<app.ItemSettings>(RandomizerTestPaths.KeyItemSettingsPath)
            ._Settings
            .Single(x => x.ItemDataID == "skl001");
        var uiItemMessages = result.ReadAfterMsgFile(RandomizerTestPaths.UiItemMessagePath);

        Assert.Equal(1.25f, passiveSkill.Get<float>("AttackChangeRate"));
        Assert.Equal(0.75f, passiveSkill.Get<float>("ReloadSpeedChangeRate"));
        Assert.False(passiveSkill.Get<bool>("IsBulletStackNumInfinity"));
        Assert.True(passiveSkill.Get<bool>("IsPsychostimulantEffectInfinity"));
        Assert.Equal(customName, uiItemMessages.GetString(skillSetting.NameMsg, LanguageId.English));
        Assert.Equal(customDescription, uiItemMessages.GetString(skillSetting.ManualMsg, LanguageId.English));

        void SetCsvColumn(string columnName, string value) {
            var index = Array.IndexOf(header, columnName);
            Assert.True(index >= 0, $"Missing CSV column '{columnName}'.");
            skl001[index] = value;
        }
    }

    [Fact]
    public void BirthdaySkillSupport_IncludesREFrameworkPlugin_WhenDlcItemsAreAllowed() {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["allow-dlc-items"] = true;
            config["random-enemy-drops"] = false;
            config["recipes-add-new"] = false;
        });

        var (zip, _) = RandomizerTest.Run(configuration.ToJson(), seed: 0x7B157);
        using var zipDisposable = zip;

        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7.lua"));
        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7/inventory.lua"));
        Assert.Null(zip.GetEntry("reframework/plugins/managed/Biohazard.BioRand.RE7.REFrameworkPlugins.dll"));
        Assert.NotNull(zip.GetEntry("reframework/data/BioRand7/config.json"));
    }

    [Fact]
    public void BirthdaySkillSupport_IncludesREFrameworkPlugin_WhenStartingSkillsAreEnabled() {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["allow-dlc-items"] = false;
            config["random-starting-inventory-skills-mia"] = true;
            config["random-enemy-drops"] = false;
            config["recipes-add-new"] = false;
        });

        var (zip, _) = RandomizerTest.Run(configuration.ToJson(), seed: 0x7B157);
        using var zipDisposable = zip;

        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7.lua"));
        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7/inventory.lua"));
        Assert.Null(zip.GetEntry("reframework/plugins/managed/Biohazard.BioRand.RE7.REFrameworkPlugins.dll"));
        Assert.NotNull(zip.GetEntry("reframework/data/BioRand7/config.json"));
    }

    private static string PrefabPath(string prefabPath)
        => $"{prefabPath.Of()}.{FileVersions.PfbFileVersion}".ToLowerInvariant();

    private static string GetSkillItemPrefabPath(string itemDataId)
        => $"Prefab/Skill/{itemDataId}/{ToSkillPrefabName(itemDataId)}.pfb";

    private static string GetSkillDropPrefabPath(string itemDataId)
        => $"Prefab/Skill/{itemDataId}/{ToSkillPrefabName(itemDataId)}Get.pfb";

    private static string ToSkillPrefabName(string itemDataId)
        => $"{char.ToUpperInvariant(itemDataId[0])}{itemDataId[1..]}";

    private static (string ItemDataId, string MeshFolder)[] ExpectedSkillVisuals()
        =>[
            ("skl001", "sm9958_skillpatch01"),
            ("skl002", "sm9959_skillpatch02"),
            ("skl008", "sm9959_skillpatch02"),
            ("skl010", "sm9959_skillpatch02"),
            ("skl012", "sm9959_skillpatch02"),
            ("skl014", "sm9959_skillpatch02"),
            ("skl016", "sm9959_skillpatch02"),
            ("skl023", "sm9959_skillpatch02"),
            ("skl003", "sm9960_skillpatch03"),
            ("skl009", "sm9960_skillpatch03"),
            ("skl011", "sm9960_skillpatch03"),
            ("skl013", "sm9960_skillpatch03"),
            ("skl015", "sm9960_skillpatch03"),
            ("skl017", "sm9960_skillpatch03"),
            ("skl018", "sm9960_skillpatch03"),
            ("skl019", "sm9960_skillpatch03"),
            ("skl021", "sm9960_skillpatch03"),
            ("skl022", "sm9960_skillpatch03"),
        ];

    private static bool IsBirthdaySkillOverlayAsset(string path)
        => path.StartsWith("natives/stm/props/sm995", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("natives/stm/ui/ui0100/tex/ui0105_iam.tex.35", StringComparison.OrdinalIgnoreCase);

    private static RszScene ReadBeforePfb(RandomizerRunResult result, string path)
        => new PfbFile(FileVersions.PfbFileVersion, result.ReadBeforeBytes(path))
            .ReadScene(result.Randomizer.FileRepository.TypeRepository);

    private static GeneratedViaTransform FindItemTransform(RszScene scene, string itemDataId) {
        var gameObject = scene.GetGameObjects().Single(x =>
            string.Equals(x.FindComponent<app.Item>()?.ItemDataID, itemDataId, StringComparison.OrdinalIgnoreCase));
        var transform = gameObject.FindComponent<GeneratedViaTransform>();

        Assert.NotNull(transform);
        return transform!;
    }

    private static void AssertSkillVisualResources(
        RszScene scene,
        string itemDataId,
        string meshPath,
        string materialPath,
        Quaternion? expectedRotation = null) {
        var gameObject = scene.GetGameObjects().Single(x =>
            string.Equals(x.FindComponent<app.Item>()?.ItemDataID, itemDataId, StringComparison.OrdinalIgnoreCase));
        var mesh = gameObject.FindComponent("via.render.Mesh");

        Assert.NotNull(mesh);
        Assert.Equal(meshPath, ((RszResourceNode)mesh!["Mesh"]).Value);
        Assert.Equal(materialPath, ((RszResourceNode)mesh["Material"]).Value);

        if (expectedRotation != null) {
            var transform = gameObject.FindComponent<GeneratedViaTransform>();
            Assert.NotNull(transform);
            AssertQuaternionEquals(expectedRotation.Value, transform!.Rotation);
        }
    }

    private static void AssertQuaternionEquals(Quaternion expected, Quaternion actual) {
        if (Quaternion.Dot(expected, actual) < 0) {
            actual = new Quaternion(-actual.X, -actual.Y, -actual.Z, -actual.W);
        }

        Assert.InRange(MathF.Abs(actual.X - expected.X), 0, 0.0001f);
        Assert.InRange(MathF.Abs(actual.Y - expected.Y), 0, 0.0001f);
        Assert.InRange(MathF.Abs(actual.Z - expected.Z), 0, 0.0001f);
        Assert.InRange(MathF.Abs(actual.W - expected.W), 0, 0.0001f);
    }

    private static RszObjectNode ReadAfterPassiveSkillUser(RandomizerRunResult result, string userPath) {
        var path = userPath.UserFile();
        return new UserFile(result.ReadAfterBytes(path))
            .GetObjects(result.Randomizer.FileRepository.TypeRepository)[0];
    }
}
