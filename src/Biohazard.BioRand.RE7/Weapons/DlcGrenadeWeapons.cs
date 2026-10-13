using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Weapons;

internal static class DlcGrenadeWeapons {
    internal static ImmutableArray<string> RequiredAssetPaths { get; } = [.. System.Text.Encoding.UTF8
        .GetString(EmbeddedData.GetFile("dlc_grenade_assets.txt"))
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)];

    internal static ImmutableArray<(string ItemId, string Model, string ManagerField)> Sources { get; } = [
        ("Grenadebomb", "wp3000_grenadebomb", "Throwable01Prefab"),
        ("Thermatebomb", "wp3010_thermatebomb", "Throwable02Prefab"),
        ("Stangrenadebomb", "wp3020_stangrenadebomb", "Throwable03Prefab"),
    ];

    internal static string ManagerPrefab(string root) => $"{root}/GrenadeShellManager.pfb";

    internal static void ExportShellManager(IPatchContext context, string root) {
        var source = context.GetScnFile("ch8/scenes/chapter8.scn".SceneFile()).ReadScene(context.TypeRepository);
        var original = source.GetGameObjects().Single(go => go.Components.Any(c => c.Type.Name == "app.CH8ShellManager"));
        var manager = original.WithName("BioRandGrenadeShellManager").WithPrefab("").WithChildren([])
            .WithComponents([.. original.Components.Where(c => c.Type.Name is "via.Transform" or "app.CH8ShellManager")])
            .CloneWithNewGuids(new Rng(789213));
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        PfbFile.Builder? template = null;
        foreach (var (id, model, field) in Sources) {
            var reference = $"{root}/{id}/Shell.pfb";
            var collision = $"CH8/Collision/Collider/Weapon/{model[..6]}/{model}.rcol";
            var shell = context.GetPfbFile($"ch8/prefab/weapon/{model}/shell/{model}_shell.pfb".Of() + ".17")
                .ToBuilder(context.TypeRepository);
            if (!shell.Scene.GetGameObjects().SelectMany(go => go.Components).Any(c => c.Type.Name == "app.CH8Throwable"))
                throw new InvalidDataException($"Missing native throwable component: {id}");
            // Isolate the pool's shell and attack data, never the source DLC resources.
            shell.Scene = shell.Scene.Visit(node => node is RszResourceNode r
                && string.Equals(r.Value, collision, StringComparison.OrdinalIgnoreCase)
                    ? new RszResourceNode($"{root}/{id}/Attack.rcol") : node);
            files.Add(reference.Of() + ".17", shell.RebuildResources().Build().Data.ToArray());
            files.Add($"{root}/{id}/Attack.rcol".RcolFile(), context.GetFile(collision.RcolFile())
                ?? throw new InvalidDataException($"Missing grenade collision: {collision}"));
            manager = manager.WithComponents([.. manager.Components.Select(c => c.Type.Name != "app.CH8ShellManager" ? c
                : c.Set(field + ".Path", new RszResourceNode(reference)).Set(field + ".Standby", true))]);
            template ??= shell;
        }
        // Retain the native default bullet pool and timing; no copied DLC system managers.
        template!.Scene = template.Scene.WithChildren([manager]);
        files.Add(ManagerPrefab(root).Of() + ".17", template.RebuildResources().Build().Data.ToArray());
        foreach (var (path, data) in files) context.SetFile(path, data);
    }
}
