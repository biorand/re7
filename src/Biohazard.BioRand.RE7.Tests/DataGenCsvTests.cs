using Biohazard.BioRand.RE7.DataGen.Commands;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Weapons;
using System.Collections.Immutable;
using System.Globalization;

namespace Biohazard.BioRand.RE7.Tests;

public class DataGenCsvTests {
    private const string ItemPlacementHeader =
        "IsExtra,Comment,Id,Enabled,StackNum,EasyNum,HardNum,Difficulty,Chapter," +
        "PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,Guid,SaveGuid,GameObjectName,SceneFile," +
        "Mesh,Material,Dlc,GuidOrAuto,X,Y,Z,X,Y,Z,W\r\n";

    private const string ItemDefinitionHeader =
        "Id,Tags,Name,CategoryType,Size,IsUnlockable,Dlc,MaxStack,WeaponId,CanStoreInItemBox," +
        "DeveloperComment,SourceUserFile,IsWeapon,IsStackable,IsStackLimitConfigurable," +
        "StackLimitConfigId,IsDlcItem,ItemId,IsStoryProgressionItem\r\n";

    [Fact]
    public void GetCsv_ItemPlacements_PreservesFlattenedColumnsAndDynamicDispatch() {
        var placement = new ItemPlacement {
            Id = "Handgun",
            PosX = 1.25f,
            PosY = 2.5f,
            PosZ = 3.75f,
            RotW = 1,
            Tags = ImmutableArray.Create("foo", "bar"),
            Comment = " padded \"quoted\",\r\n日本語 ",
            Guid = Guid.Parse("12345678-1234-1234-1234-123456789abc")
        };
        object data = new[] { placement }.OrderBy(x => x.Id);

        var csv = (string)GenerateCommand.GetCsv((dynamic)data);

        Assert.Equal(ItemPlacementHeader +
            "False,\" padded \"\"quoted\"\",\r\n日本語 \",Handgun,False,0,-1,-1,,0," +
            "1.25,2.5,3.75,0,0,0,1,12345678-1234-1234-1234-123456789abc," +
            "00000000-0000-0000-0000-000000000000,,,,,," +
            "12345678-1234-1234-1234-123456789abc,1.25,2.5,3.75,0,0,0,1\r\n", csv);
    }

    [Fact]
    public void GetCsv_ItemPlacements_PreservesComputedFallbackValues() {
        var placement = new ItemPlacement { Id = "Handgun" };

        var rows = Serialization.Csv.Read(GenerateCommand.GetCsv(new[] { placement }));

        Assert.Equal(placement.GuidOrAuto.ToString(), rows[23, 1]);
        Assert.Equal("0", rows[27, 1]);
        Assert.Equal("0", rows[28, 1]);
        Assert.Equal("0", rows[29, 1]);
        Assert.Equal("1", rows[30, 1]);
    }

    [Fact]
    public void GetCsv_ItemDefinitions_PreservesComputedScalarColumns() {
        var definition = new ItemDefinition {
            Id = "Handgun",
            Name = "Test",
            MaxStack = 42,
            SourceUserFile = "source.user"
        };

        var csv = GenerateCommand.GetCsv(new[] { definition });

        Assert.Equal(ItemDefinitionHeader +
            "Handgun,,Test,OtherItem,Slot1,False,,42,,False,,source.user,False,True,True," +
            "inventory-stack-limit-handgun,False,Handgun,False\r\n", csv);
    }

    [Fact]
    public void GetCsv_WeaponDefinitions_PreservesScalarSchema() {
        var definition = new WeaponDefinition {
            WeaponId = default,
            Id = "test",
            Damage = new() { ["test"] = new WeaponDamageStats { Damage = 10, Stun = 2 } },
            BulletItemIDs = new() { Enums.app.ItemID.Handgun },
            RcolPaths = new() { "one", "two" }
        };

        var csv = GenerateCommand.GetCsv(new[] { definition });

        Assert.Equal(
            "WeaponId,Id,Name,IsGun,IsInventoryWeapon,MaxLoadNum,IsLoadNumInfinity," +
            "IsBulletStackNumInfinity,Range,UserType,Mesh,Material,PrefabPath,UserParamsPath\r\n" +
            "Hand,test,,False,False,,,,,Player,,,,\r\n", csv);
    }

    [Fact]
    public void GetCsv_AnonymousRecords_PreservesEscapingAndInvariantFormatting() {
        var previousCulture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var data = new[] {
                new { Name = " padded ", Description = "\"quoted\",\r\n日本語", Price = 1.25m,
                    Enabled = true, Empty = (int?)null }
            };

            var csv = GenerateCommand.GetCsv(data);

            Assert.Equal("Name,Description,Price,Enabled,Empty\r\n" +
                "\" padded \",\"\"\"quoted\"\",\r\n日本語\",1.25,True,\r\n", csv);
        }
        finally {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void GetCsv_EmptyTypedSequences_PreservesHeaders() {
        Assert.Equal(ItemDefinitionHeader, GenerateCommand.GetCsv(Array.Empty<ItemDefinition>()));
        Assert.Equal(ItemPlacementHeader, GenerateCommand.GetCsv(Array.Empty<ItemPlacement>()));
    }

    [Fact]
    public void GetCsv_Bytes_PreservesPrimitiveRecordsWithoutHeader() {
        Assert.Equal("1\r\n2\r\n255\r\n", GenerateCommand.GetCsv(new byte[] { 1, 2, 255 }));
    }

    [Fact]
    public void GetCsv_EmptyBytes_DoesNotWriteHeader() {
        Assert.Equal("", GenerateCommand.GetCsv(Array.Empty<byte>()));
    }

    [Fact]
    public void GetCsv_Strings_RejectsUnnamedRecords() {
        Assert.Throws<ArgumentException>(() => GenerateCommand.GetCsv(new[] { "via.Transform" }));
    }
}