using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using Enums.app;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Rsz;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class GoldenCrowbarBehaviorTests {
    private const string InventoryPrefab = "natives/stm/prefab/weapon/wp1370_goldenbar/item/wp1370_goldenbar_item.pfb.17";
    private const string ResourceSettings = "natives/stm/prefab/item/resourceitemsettings.user.2";

    [Fact]
    public void GoldenCrowbar_HasCampaignInventoryDefinitionMessagesAndDlcDependencies() {
        using var result = RandomizerTest.RunState();
        var before = result.ReadBeforeUserFile<app.ItemSettings>(ResourceSettings)._Settings;
        var after = result.ReadAfterUserFile<app.ItemSettings>(ResourceSettings)._Settings;
        Assert.DoesNotContain(before, item => item.ItemDataID == "GoldenBar");
        var gold = Assert.Single(after, item => item.ItemDataID == "GoldenBar");
        Assert.DoesNotContain(after, item => item.ItemDataID == "GoldenBarNo");
        Assert.Equal(before.Count + 1, after.Count);
        Assert.Equal(Enums.app.Item.ItemCategoryType.Weapon, gold.Category);
        Assert.Equal("Prefab/Weapon/wp1370_GoldenBar/Item/wp1370_GoldenBar_Item.pfb", gold.ItemPrefab.Path.ToString());
        Assert.True(gold.CanStoreItembox);

        foreach (var path in new[] {
            InventoryPrefab,
            "natives/stm/collision/collider/weapon/wp1370/wp1370_goldenbar.rcol.20",
            "natives/stm/vfx/vfx_provider/vfx_epv_weapon_id/epv_wp1370/epvc_wp1370.pfb.17",
        }) {
            Assert.True(result.WasFileModified(path), $"DLC dependency must be shipped: {path}");
            Assert.Equal(result.ReadBeforeBytes(path), result.ReadAfterBytes(path));
        }

        var prefab = result.ReadAfterPfb(InventoryPrefab);
        var item = prefab.GetGameObjects().Single(go => go.FindComponent<app.Item>() != null);
        Assert.Equal("GoldenBar", item.FindComponent<app.Item>()!.ItemDataID);
        Assert.Equal(WeaponID.GoldenBar, item.FindComponent<app.Weapon>()!.WeaponID);
        var messages = result.ReadAfterMsgFile(RandomizerTestPaths.UiItemMessagePath);
        Assert.Equal("Golden Crowbar", messages.GetString(gold.NameMsg, LanguageId.English));
        Assert.Contains("burst into flames", messages.GetString(gold.ManualMsg, LanguageId.English));
        Assert.Equal(messages.ToBuilder().Languages, messages.FindMessage(gold.NameMsg)!.Values.Select(value => value.Language));
    }

    [Theory]
    [InlineData("Normal")]
    [InlineData("Main House")]
    public void GoldenCrowbar_DebugStartingInventoryResolvesToRegisteredWeapon(string chapter) {
        using var result = RandomizerTest.RunState(config => {
            config["start-chapter"] = chapter;
            config["tags"] = "re7:debugstartitems";
            config["random-starting-inventory-ethan"] = true;
        }, prepareRandomizer: randomizer => randomizer.DynamicData.SetData(
            DynamicDataName.DebugStartItems, Encoding.UTF8.GetBytes("ItemId,Quantity\nGoldenBar,1\n")));
        var inventory = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;
        Assert.Single(inventory, item => item.ItemDataID == "GoldenBar" && item.Num == 1);
        Assert.Single(result.ReadAfterUserFile<app.ItemSettings>(ResourceSettings)._Settings,
            item => item.ItemDataID == "GoldenBar");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GoldenCrowbar_BirdCageRewardHasRegisteredItemAndRequestedModel(bool preserveModels) {
        const string scenePath = "natives/stm/leveldesign/itemset/chapter3/mainhouse_hall/hard.scn.20";
        using var result = RandomizerTest.RunState(config => {
            config["random-bird-cage-drugs-coins"] = true;
            config["preserve-item-models"] = preserveModels;
        }, prepareRandomizer: randomizer => randomizer.DynamicData.SetData(DynamicDataName.BirdCages,
            Encoding.UTF8.GetBytes("Enabled,ItemId,MinAmount,MaxAmount,CoinsMin,CoinsMax,InputItemIds\ntrue,GoldenBar,1,1,3,3,Coin CoinOld\n")));
        var before = result.ReadBeforeScene(scenePath);
        var after = result.ReadAfterScene(scenePath);
        var rewards = RandomizerTestHelpers.GetBirdCageStates(after).Where(cage => cage.ItemId == "GoldenBar").ToArray();
        Assert.NotEmpty(rewards);
        Assert.Single(result.ReadAfterUserFile<app.ItemSettings>(ResourceSettings)._Settings,
            item => item.ItemDataID == "GoldenBar");
        foreach (var reward in rewards) {
            Assert.Equal(1, reward.ItemCount);
            Assert.Equal(3, reward.CoinCount);
            var mesh = GetRewardMesh(after, reward.ContainerGuid);
            var beforeMesh = GetRewardMesh(before, reward.ContainerGuid);
            Assert.Equal(preserveModels ? ((RszResourceNode)beforeMesh["Mesh"]).Value : "Weapon/wp1370_GoldenBar/wp1370_GoldenBar.mesh",
                ((RszResourceNode)mesh["Mesh"]).Value);
            Assert.Equal(preserveModels ? ((RszResourceNode)beforeMesh["Material"]).Value : "Weapon/wp1370_GoldenBar/wp1370.mdf2",
                ((RszResourceNode)mesh["Material"]).Value);
        }
    }

    private static RszObjectNode GetRewardMesh(RszScene scene, Guid containerGuid)
        => scene.FindGameObject(containerGuid)!.Children.Single(go => go.FindComponent<app.Item>() != null)
            .FindComponent("via.render.Mesh")!;
}
