using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Extensions;
using IntelOrca.Biohazard.REE.Rsz;
using System.Numerics;
using System.Collections.Immutable;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Services;

internal class ChestService(Randomizer randomizer) {
    private readonly RszGameObject _chestTemplate = PrepareUnlockedTemplate(randomizer.TemplateService.GetObject("Chest"));
    private readonly Dictionary<string, RszGameObject> _weaponCache = new();
    private readonly Rng _rng = randomizer.GetRng("drops/weapon-chests");
    private readonly Rng _templateRng = randomizer.GetRng("drops/weapon-chests/template-instances");

    private static RszGameObject PrepareUnlockedTemplate(RszGameObject chest) {
        var drawer = chest.Children.Single(child => child.Name == "InteractDrawer");
        // The locked template relies on Fsm_LockCheck to enable this child. An
        // unlocked chest must register its drawer interaction immediately.
        drawer = drawer.WithSettings(drawer.Settings.Set("Update", true).Set("Draw", true));
        // The imported root uses LockLv0, but still carries a LockLv1 child FSM and references
        // to the source scene's drawer. That child waits on locks absent from the placed chest.
        chest = chest.WithChildren([drawer]);
        return chest.WithComponents(chest.Components.Select(component => component.Visit(node => {
            if (node is not RszObjectNode obj) return node;
            return obj.Type.Name switch {
                "app.fsm.InteractTest" => obj.SetField("InteractObj", drawer.Guid),
                "app.fsm.ActivateObject" => obj.SetField("GameObj", drawer.Guid),
                // Motion/collider actions use IsOwnerObjSet. Keep their fallback self-reference local too.
                "app.ObjectSet" => obj.SetField("GameObj", chest.Guid),
                _ => node,
            };
        })).ToImmutableArray());
    }

    private RszGameObject GetCachedWeaponOrCreate(string weaponId) {
        if (!_weaponCache.ContainsKey(weaponId))
            _weaponCache[weaponId] = randomizer.TemplateService.GetItemTemplate(weaponId);
        return _weaponCache[weaponId];
    }

    public RszScene PlaceWeaponChest(RandomizerLogger logger, RszScene scene, ItemPlacement placement) {
        var weaponDrop = randomizer.ItemRandomizer.GetRandomGun(_rng, allowReoccurance: false);
        if (weaponDrop == null) {
            // Should never happen given correct weapon definitions
            logger.LogLine("Failed to get random weapon! Empty weapon pool.");
            return scene;
        }

        var transform = new GeneratedViaTransform(){
            Position = new Vector3(placement.PosX, placement.PosY, placement.PosZ),
            Rotation = placement.Rotation,
            Scale = Vector3.One,
            ParentJoint = "",
            SameJointsContraint = false,
            AbsoluteScaling = false,
            JointFastLockScene = false,
            JointSegmentScale = false
        };

        // Create weapon from template
        var weaponGuid = _rng.NextGuid();
        var weapon = GetCachedWeaponOrCreate(weaponDrop.Id)
            .CloneWithNewGuids(_templateRng, weaponGuid)
            .PreparePickupInteractionsForPlacement()
            .PrepareWeaponPickupInteractionGameObjects();

        // Match vanilla drawer-direct item structure: keep the item hidden until the
        // drawer opens, but leave weapon pickup children updatable so they can register.
        weapon = weapon.WithSettings(
            weapon.Settings
                .Set("Update", false)
                .Set("Draw", false)
        );

        // This object is a child of the drawer. A world placement here applies the
        // chest offset twice and puts the gun outside the player's reach.
        weapon = weapon.AddOrUpdateComponent(new GeneratedViaTransform() {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One,
            ParentJoint = ""
        });

        // Prepare chest
        var chest = _chestTemplate.CloneWithNewGuids(_templateRng);
        chest = chest.AddOrUpdateComponent(transform);
        var interactDrawer = chest.Children.Single(c => c.Name == "InteractDrawer");
        var interactDrawerComponent = interactDrawer.FindComponent<app.InteractDrawer>()!;
        interactDrawerComponent.SaveGUID = _templateRng.NextGuid();
        interactDrawerComponent.SetItemID = "";
        interactDrawerComponent.ChangeStackNum = -1;
        interactDrawerComponent.UseDrawerPos = false;
        interactDrawerComponent.IsDirectGameObjectSet = true;
        interactDrawerComponent.DirectSetGameObject = weaponGuid;
        interactDrawer = interactDrawer.AddOrUpdateComponent(interactDrawerComponent);
        interactDrawer = interactDrawer.AddOrUpdateChild(weapon);
        chest = chest.AddOrUpdateChild(interactDrawer);

        scene = scene.Add(chest);

        logger.LogLine($"[EXTRA] Chest at {transform.Position} in {placement.SceneFile} containing {weapon.Name}");
        return scene;
    }
}
