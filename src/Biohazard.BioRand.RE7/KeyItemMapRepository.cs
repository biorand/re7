namespace Biohazard.BioRand.RE7;

internal sealed record KeyItemMapDefinition(
    string Id,
    string Title,
    string FileName,
    int Width,
    int Height,
    int Order,
    string SourceUrl);

internal sealed record KeyItemMapLocation(
    string MapId,
    string RoomName,
    float X,
    float Y,
    bool IsApproximate = false);

internal static class KeyItemMapRepository {
    private static readonly IReadOnlyDictionary<string, KeyItemMapDefinition> _maps =
        new[]{
                new KeyItemMapDefinition("main-house-1f", "Main House 1F", "main-house-1f.gif", 723, 412, 10,
                    "https://www.evilresource.com/maps/re7_mainhouse1f/map.gif?00ddec4b"),
                new KeyItemMapDefinition("main-house-2f", "Main House 2F", "main-house-2f.gif", 508, 242, 11,
                    "https://www.evilresource.com/maps/re7_mainhouse2f/map.gif?2f562fd1"),
                new KeyItemMapDefinition("main-house-attic", "Main House Attic", "main-house-attic.gif", 61,
                    154, 12, "https://www.evilresource.com/maps/re7_mainhouseattic/map.gif?4709bc40"),
                new KeyItemMapDefinition("processing-area", "Processing Area", "processing-area.gif", 498, 310,
                    13, "https://www.evilresource.com/maps/re7_processingarea/map.gif?b86da613"),
                new KeyItemMapDefinition("yard", "Yard", "yard.gif", 624, 406, 20,
                    "https://www.evilresource.com/maps/re7_yard/map.gif?f39eb0a5"),
                new KeyItemMapDefinition("old-house-1f", "Old House 1F", "old-house-1f.gif", 728, 615, 30,
                    "https://www.evilresource.com/maps/re7_oldhouse1f/map.gif?bf9ff326"),
                new KeyItemMapDefinition("old-house-2f", "Old House 2F", "old-house-2f.gif", 589, 202, 31,
                    "https://www.evilresource.com/maps/re7_oldhouse2f/map.gif?8041e41d"),
                new KeyItemMapDefinition("old-house-west-1f", "Old House (West) 1F", "old-house-west-1f.gif",
                    538, 425, 32, "https://www.evilresource.com/maps/re7_oldhousewest1f/map.gif?131d7b65"),
                new KeyItemMapDefinition("old-house-west-2f", "Old House (West) 2F", "old-house-west-2f.gif",
                    357, 175, 33, "https://www.evilresource.com/maps/re7_oldhousewest2f/map.gif?e50da9b3"),
                new KeyItemMapDefinition("testing-area-1f", "Testing Area 1F", "testing-area-1f.gif", 683, 627,
                    40, "https://www.evilresource.com/maps/re7_testingarea1f/map.gif?93ff8e7e"),
                new KeyItemMapDefinition("ship-2f", "Ship 2F (VHS)", "ship-2f.gif", 421, 209, 50,
                    "https://www.evilresource.com/maps/re7_ship2f/map.gif?2d1fa381"),
                new KeyItemMapDefinition("ship-s2", "Ship S2 (VHS)", "ship-s2.gif", 505, 432, 51,
                    "https://www.evilresource.com/maps/re7_ships2/map.gif?4c949e96"),
                new KeyItemMapDefinition("wrecked-ship-2f", "Wrecked Ship 2F", "wrecked-ship-2f.gif", 421,
                    209, 60, "https://www.evilresource.com/maps/re7_wreckedship2f/map.gif?9c1cc358"),
                new KeyItemMapDefinition("wrecked-ship-3f", "Wrecked Ship 3F", "wrecked-ship-3f.gif", 412,
                    208, 61, "https://www.evilresource.com/maps/re7_wreckedship3f/map.gif?53fe460d"),
                new KeyItemMapDefinition("wrecked-ship-4f", "Wrecked Ship 4F", "wrecked-ship-4f.gif", 323,
                    224, 62, "https://www.evilresource.com/maps/re7_wreckedship4f/map.gif?aac74d22"),
                new KeyItemMapDefinition("swamp", "Swamp", "swamp.gif", 432, 1080, 70,
                    "https://www.evilresource.com/maps/re7_swamp/map.gif?606e3071"),
                new KeyItemMapDefinition("salt-mine-b1", "Salt Mine B1", "salt-mine-b1.gif", 371, 820, 71,
                    "https://www.evilresource.com/maps/re7_saltmineb1/map.gif?bda45d2b"),
                new KeyItemMapDefinition("salt-mine-b3", "Salt Mine B3", "salt-mine-b3.gif", 913, 1524, 72,
                    "https://www.evilresource.com/maps/re7_saltmineb3/map.gif?1c459aa5"),
            }
            .ToDictionary(map => map.Id, StringComparer.OrdinalIgnoreCase);

    // Pickup GUIDs are stable placement identities. Coordinates are room-label centroids on the corresponding
    // Evil Resource floor plan, not a world-space projection. This keeps the spoiler useful even when scene origins
    // and floor coordinate systems differ.
    private static readonly IReadOnlyDictionary<Guid, KeyItemMapLocation> _locations =
        new Dictionary<Guid, KeyItemMapLocation>{
            [new("54c7a657-2d7e-448d-b767-c04fdf4ec3bc")] = At("main-house-1f", "Pantry - box above hatch", 324f, 209.33f),
            [new("323c9f2e-4660-4650-8f21-4fdecdecace0")] = At("main-house-1f", "Pantry - shelves above hatch", 324f, 209.33f),
            [new("ee1ddf5f-6a3e-4a53-b27d-ca2ac20dbbaa")] = At("main-house-1f", "Kitchen", 275f, 237f),
            [new("077f9206-19e7-4937-994b-cd13a80dabd4")] = At("processing-area", "Workshop", 434.67f, 150.67f),
            [new("0944c68d-50a1-4207-b645-796a353aab95")] = At("old-house-west-1f", "Old Yard", 372.8f, 284.12f),
            [new("0da28012-ad6a-0da5-1f0a-cacd2c677ed3")] = At("main-house-1f", "Living Room", 311f, 283f),
            [new("15114d15-56af-468e-ab53-154e305e0ad1")] = At("wrecked-ship-4f", "Captain's Cabin", 298.8f, 158f),
            [new("1ca1024c-bc24-0e14-4d97-693fd5d03651")] = At("main-house-1f", "Main Hall", 482.9f, 264.95f),
            [new("1c01c49f-81fa-0d1c-2312-fd50eafe79a3")] = At("old-house-2f", "Altar", 99.67f, 110.06f),
            [new("24512acb-965b-462c-941e-375f9d62bd5e")] = At("processing-area", "Dissection Room", 168.14f, 193f),
            [new("25295cda-b1c6-428b-47c7-fa9b4bcdaf61")] = At("main-house-1f", "Dining Area", 244.4f, 283.1f),
            [new("284aa600-8e59-475a-82b4-d7cd353f70e9")] = At("wrecked-ship-2f", "Lounge", 138.5f, 140.5f),
            [new("36300031-8048-27f5-47c5-73cb8b7d0cd6")] = At("ship-2f", "Kitchen", 269f, 140.5f),
            [new("3e3778e9-b321-4141-8c90-9cb7b4c0e6c8")] = At("testing-area-1f", "Barn", 163.71f, 486.29f),
            [new("400c0d7e-f8cd-43d0-9c3e-c1ccfa2c0704")] = At("old-house-1f", "Living Room", 394.5f, 351.5f),
            [new("401dbfaa-3469-0702-1c9a-d74a7d185216")] = At("main-house-2f", "Recreation Room", 147.4f, 80.2f),
            [new("41a59cb8-7613-4d4b-a530-58aebfe0e1c8")] = At("old-house-1f", "Gallery", 251f, 176.33f),
            [new("4415c5c8-4096-9536-4f5e-ddc2cdf657be")] = At("old-house-1f", "Cellar", 606.33f, 365.33f),
            [new("49d012b8-66cf-7c47-440e-6d41c40dd75c")] = At("main-house-1f", "Main Hall", 482.9f, 264.95f),
            [new("5561df8c-2d47-791e-465b-e304f57ebeb1")] = At("salt-mine-b3", "North Cavern", 314.31f, 1048.24f),
            [new("5ed27b77-dc51-d228-4875-66abf42ecfb5")] = At("processing-area", "Morgue", 43.5f, 168f),
            [new("5fcf24e4-526b-4a7e-ab3d-b7357b2ac243")] = At("wrecked-ship-2f", "Lounge", 138.5f, 140.5f),
            [new("62f86528-44f6-0adf-0a70-bdf505111bb7")] = At("processing-area", "Incinerator Room", 343.75f, 85.5f),
            [new("665a86ed-7e9c-4b56-a889-4377fa1d3f47")] = At("main-house-1f", "Pantry", 324f, 209.33f),
            [new("6f2e3d07-d773-54ba-4fde-2a285ad336d5")] = At("salt-mine-b3", "Cultivation Room", 388f, 1060.4f),
            [new("71417782-8c02-4be1-88d2-735fc79e7940")] = At("main-house-2f", "Bathroom", 39.33f, 157.67f),
            [new("751cff95-a933-48ad-8ffa-6f96e25f8959")] = At("processing-area", "Dissection Room", 168.14f, 193f),
            [new("75cb5c16-a0d7-51da-465a-92a35fd66efc")] = At("swamp", "Mine Office", 146.25f, 533.12f),
            [new("7a0710fd-6939-02b3-1a5b-229ce8cf7e77")] = At("main-house-1f", "Main Hall", 482.9f, 264.95f),
            [new("887e0b13-658a-441b-9210-726205b76601")] = At("old-house-1f", "Living Room", 394.5f, 351.5f),
            [new("896dd0bb-f3ee-41bf-b4a0-0b28e99da94c")] = At("main-house-attic", "Attic", 30.5f, 41.5f),
            [new("89e0718a-9f23-0ba7-2ec6-0affca6c028b")] = At("testing-area-1f", "Monitoring Room", 594f, 126.38f),
            [new("8b233067-d1dc-44f8-a7d9-1f2319c55746")] = At("old-house-west-1f", "Old Bedroom", 288f, 131f),
            [new("8dbcca92-78c1-4143-917e-cdf7d0673897")] = At("salt-mine-b1", "Abandoned Mine", 118.48f, 338f),
            [new("96da0bd0-1a8b-4c35-bc02-695da693e8d4")] = At("processing-area", "Dissection Room", 168.14f, 193f),
            [new("986468f2-3801-cd66-44fc-80b99d145e43")] = At("testing-area-1f", "Monitoring Room", 594f, 126.38f),
            [new("a0151085-587f-ac99-474c-30c12a8ce080")] = At("old-house-1f", "Entrance", 283.5f, 351.5f),
            [new("a3f59645-063b-41a5-a86a-6e5b8c507a88")] = At("old-house-1f", "Cellar", 606.33f, 365.33f),
            [new("a5b09aa7-83dc-2764-4347-1ed98d4297d2")] = At("old-house-2f", "Detention Room", 474.5f, 125.5f),
            [new("abb03a3a-a10b-4dca-9385-6274ca2e004a")] = At("yard", "Trailer", 158.6f, 155.2f),
            [new("acb20802-02e5-4901-a6a8-70a3a39a6b72")] = At("old-house-west-1f", "Old Bedroom", 288f, 131f),
            [new("aeb30396-40c7-4272-90b0-161f2440fe08")] = At("wrecked-ship-2f", "Lounge", 138.5f, 140.5f),
            [new("af78cd5c-b090-4557-bd9c-2f6a0d74b0c0")] = At("old-house-west-2f", "Landing", 166.33f, 34f),
            [new("b1548f47-609a-0190-3976-50b2aeafd6b6")] = At("main-house-1f", "Drawing Room", 651.36f, 354.09f),
            [new("ba174dbf-64e9-0e76-187a-801520648246")] = At("wrecked-ship-3f", "Sick Bay", 202.33f, 90.67f),
            [new("c3af1930-0d59-a26c-458b-04d7a8ee0f0f")] = At("yard", "Courtyard", 334.47f, 156.84f),
            [new("c55742d9-eb59-48fb-8dab-05361bc455b6")] = At("old-house-west-2f", "Landing", 166.33f, 34f),
            [new("ccf47d14-a937-43c4-9b87-f35b07d14034")] = At("main-house-attic", "Attic", 30.5f, 41.5f),
            [new("d02c77ce-2be3-41bb-a367-9b42479bed19")] = At("wrecked-ship-3f", "Control Room", 294.6f, 160f),
            [new("d5c20bd7-969c-43e0-b506-bb954421eb42")] = At("old-house-west-1f", "Old Yard", 372.8f, 284.12f),
            [new("d87bf384-39f3-d2ee-41e9-2f2124140a37")] = At("main-house-1f", "Pantry", 324f, 209.33f),
            [new("e0c1712e-c7a5-481f-a593-8dae6beed197")] = At("old-house-1f", "Dining Room", 448f, 275.5f),
            [new("f29dd369-30b8-52c6-4d65-3f8ebf01190e")] = At("old-house-west-1f", "Old Yard", 372.8f, 284.12f),
            [new("f2a6c628-621b-40c9-aba7-4ff07ddaddff")] = At("wrecked-ship-2f", "Lounge", 138.5f, 140.5f),
            [new("f8cb1cae-ef77-2370-44b2-4d6da9affc26")] = At("wrecked-ship-4f", "Captain's Cabin", 298.8f, 158f),
            [new("fc898db5-7468-4db8-b8cf-ceaf08bf48c2")] = At("old-house-1f", "Dining Room", 448f, 275.5f),

            // These chapter-four itemset placements span the Ship S2 floor. The exact room is not encoded in the
            // itemset metadata, so the spoiler deliberately marks the floor as an approximation.
            [new("043f94c1-8b75-4871-97b4-cb3f58b52bf0")] = ShipS2Approximation(),
            [new("246ed21e-33ea-4da4-a3bd-ab77caac2fe5")] = ShipS2Approximation(),
            [new("288c220b-0804-44ca-b202-fe41d6d16e88")] = ShipS2Approximation(),
            [new("2f2d2867-24f4-44c9-b06b-cf31722b3a2e")] = ShipS2Approximation(),
            [new("431d5466-ed8e-47b3-be3b-0a38c8c1a2fe")] = ShipS2Approximation(),
            [new("6e1c8de3-5401-43a4-b17a-beeb0fd43e4d")] = ShipS2Approximation(),
            [new("8a0dceb5-e2bc-465c-8753-1707063af479")] = ShipS2Approximation(),
            [new("c462a268-af3a-42ea-9370-75c7d423aa47")] = ShipS2Approximation(),
            [new("e3b64592-382a-4446-8753-ab6bf1eefeb8")] = ShipS2Approximation(),
            [new("f175d9a5-e1db-4691-bd4e-b6e95159fa65")] = ShipS2Approximation(),
        };

    public static IEnumerable<KeyItemMapDefinition> Maps => _maps.Values;
    public static IReadOnlyDictionary<Guid, KeyItemMapLocation> Locations => _locations;

    public static bool TryGetMap(string id, out KeyItemMapDefinition definition)
        => _maps.TryGetValue(id, out definition!);

    public static bool TryGetLocation(Guid targetGuid, out KeyItemMapLocation location)
        => _locations.TryGetValue(targetGuid, out location!);

    private static KeyItemMapLocation At(string mapId, string roomName, float x, float y)
        => new(mapId, roomName, x, y);

    private static KeyItemMapLocation ShipS2Approximation()
        => new("ship-s2", "Ship S2 itemset", 252.5f, 216f, true);
}
