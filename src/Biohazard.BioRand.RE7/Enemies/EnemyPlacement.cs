using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using System.Diagnostics;

namespace Biohazard.BioRand.RE7.Enemies;

[DebuggerDisplay("{Guid}")]
public class EnemyPlacement {
    public required EnemyID EnemyID { get; set; }
    public string Name { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Include { get; set; } = "";
    public string Exclude { get; set; } = "";
    public string SpawnGroup { get; set; } = "";
    public string? Comment { get; set; } = "";

    public bool Enabled { get; set; }
    public DlcType? Dlc { get; set; }
    public bool IsSpawnInfo { get; set; }
    public int Chapter { get; set; }

    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float RotW { get; set; }

    public Guid Guid { get; set; }

    public string SceneFile { get; set; } = "";

    internal SerializablePosition Position => new(PosX, PosY, PosZ);
    internal SerializableRotation Rotation => new(RotX, RotY, RotZ, RotW);
    internal EulerAngles Euler => new(Rotation);
}
