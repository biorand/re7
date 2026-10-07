using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Services;

internal class AreaService(Randomizer randomizer) {
    private readonly Dictionary<Guid, Area> _guidToArea = [];
    private ImmutableArray<Area> _areas = [];
    private ImmutableArray<Area> _enemyAreas = [];
    private bool _isLoaded;
    private bool _areEnemyAreasLoaded;

    public ImmutableArray<Area> Areas {
        get {
            EnsureLoaded();
            return _areas;
        }
        private set => _areas = value;
    }

    public Randomizer Randomizer { get; } = randomizer;

    public void LoadAreas() => EnsureLoaded();

    public void UpdateCachedScene(string path, IntelOrca.Biohazard.REE.Rsz.RszScene scene) {
        foreach (var area in _areas.Concat(_enemyAreas)) {
            if (string.Equals(area.Path, path, StringComparison.OrdinalIgnoreCase))
                area.Scene = scene;
        }
    }

    public ImmutableArray<Area> EnemyAreas {
        get {
            EnsureEnemyAreasLoaded();
            return _enemyAreas;
        }
    }

    private void EnsureLoaded() {
        if (_isLoaded)
            return;

        LoadAreasCore();
        _isLoaded = true;
    }

    private void LoadAreasCore() {
        Areas = AreaDefinitionRepository.Default.All
            .Where(a => a.Dlc == null)
            .AsParallel()
            .Select(d => new Area(Randomizer, d))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ToImmutableArray();

        // Map initial guids
        _guidToArea.Clear();

        foreach (var area in _areas) {
            area.MapGameObjectGuids(_guidToArea);
        }
    }

    private void EnsureEnemyAreasLoaded() {
        if (_areEnemyAreasLoaded)
            return;

        LoadEnemyAreasCore();
        _areEnemyAreasLoaded = true;
    }

    private void LoadEnemyAreasCore() {
        var targetRepository = AreaSceneTargetRepository.Default;
        if (targetRepository.All.Count == 0) {
            EnsureLoaded();
            _enemyAreas = Areas
                .Where(area => area.EnemyGenerators.Length != 0)
                .ToImmutableArray();
            return;
        }

        var definitionsByPath = AreaDefinitionRepository.Default.All
            .Where(area => area.Dlc == null)
            .ToDictionary(area => area.Path, StringComparer.OrdinalIgnoreCase);
        _enemyAreas = targetRepository.All
            .Where(targets => targets.GetEnemyGeneratorGuids().Count != 0)
            .Select(targets => definitionsByPath.GetValueOrDefault(targets.Path))
            .Where(definition => definition != null)
            .AsParallel()
            .Select(definition => new Area(Randomizer, definition!, AreaScanMode.IndexedTargets))
            .Where(area => area.EnemyGenerators.Length != 0)
            .OrderBy(area => area.Path, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public Area? FindAreaContainingGameObject(Guid guid) {
        EnsureLoaded();
        _guidToArea.TryGetValue(guid, out var area);
        return area;
    }

    public void RemoveGuid(Guid guid) {
        _guidToArea.Remove(guid);
    }

    public void AddGuidToArea(Guid guid, Area area) {
        _guidToArea[guid] = area;
    }

    public Area FindBestArea(AreaKind kind, int? chapter = null) {
        EnsureLoaded();

        if (chapter != null) {
            return Areas
                .Where(x => x.Definition.Kind == kind)
                .First(x => x.Definition.Chapter == chapter);
        } else {
            return Areas
                .Where(x => x.Definition.Kind == kind)
                .First();
        }
    }
}
