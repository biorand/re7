using Biohazard.BioRand.RE7.Items;
using CsvHelper;

namespace Biohazard.BioRand.RE7.DataGen;

// CsvHelper 33 flattened these computed position/rotation properties after the
// scalar columns. Preserve that exported schema, including its repeated names.
internal sealed class ItemPlacementCsvWriter : CsvRecordWriter<ItemPlacement> {
    private static readonly string[] Columns = [
        "IsExtra", "Comment", "Id", "Enabled", "StackNum", "EasyNum", "HardNum",
        "Difficulty", "Chapter", "PosX", "PosY", "PosZ", "RotX", "RotY", "RotZ", "RotW",
        "Guid", "SaveGuid", "GameObjectName", "SceneFile", "Mesh", "Material", "Dlc",
        "GuidOrAuto", "X", "Y", "Z", "X", "Y", "Z", "W"
    ];

    public override int FieldCount => Columns.Length;

    public override void WriteHeader(CsvWriter writer) {
        foreach (var column in Columns)
            writer.WriteField(column);
    }

    public override void Write(CsvWriter writer, ItemPlacement record) {
        writer.WriteField(record.IsExtra);
        writer.WriteField(record.Comment);
        writer.WriteField(record.Id);
        writer.WriteField(record.Enabled);
        writer.WriteField(record.StackNum);
        writer.WriteField(record.EasyNum);
        writer.WriteField(record.HardNum);
        writer.WriteField(record.Difficulty);
        writer.WriteField(record.Chapter);
        writer.WriteField(record.PosX);
        writer.WriteField(record.PosY);
        writer.WriteField(record.PosZ);
        writer.WriteField(record.RotX);
        writer.WriteField(record.RotY);
        writer.WriteField(record.RotZ);
        writer.WriteField(record.RotW);
        writer.WriteField(record.Guid);
        writer.WriteField(record.SaveGuid);
        writer.WriteField(record.GameObjectName);
        writer.WriteField(record.SceneFile);
        writer.WriteField(record.Mesh);
        writer.WriteField(record.Material);
        writer.WriteField(record.Dlc);
        writer.WriteField(record.GuidOrAuto);
        var position = record.Position;
        writer.WriteField(position.X);
        writer.WriteField(position.Y);
        writer.WriteField(position.Z);
        var rotation = record.Rotation;
        writer.WriteField(rotation.X);
        writer.WriteField(rotation.Y);
        writer.WriteField(rotation.Z);
        writer.WriteField(rotation.W);
    }
}