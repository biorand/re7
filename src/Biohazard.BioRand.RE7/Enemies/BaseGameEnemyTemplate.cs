using Biohazard.BioRand.RE7.Extensions;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Enemies;

internal static class BaseGameEnemyTemplate {
    internal const string MargeScenePath = "natives/stm/scenes/chapter/chapter3/enemy_em3600.scn.20";

    public static RszGameObject Adapt(IPatchContext context, string alias, RszGameObject template) {
        if (alias is not ("Em3600" or "Em4000" or "Em4100" or "Em4200"))
            return template;

        // Campaign actors also serialize these optional Birthday fields. Remove
        // their asset references from imported copies without replacing the
        // configured controllers, transforms, or native FSM identities.
        // Keep ExtraUnit and its Prefab wrapper: native callbacks dereference
        // them even when Parameter is null, as in the vanilla Em3000 actor.
        template = template.VisitComponents(component => {
            if (component.Type.Name == $"app.{alias}ActionController") {
                component = component
                    .Set("ExtraUnit.TimeScore.Path", new RszResourceNode(""))
                    .Set("ExtraUnit.TimeScore.Standby", false)
                    .Set("ExtraUnit.Parameter", new RszNullNode());
                component = RemoveBirthdayAlternates(component, "OtherResistParameterHolder");
                component = RemoveBirthdayAlternates(component, "OtherSlipParameterHolder");
            }
            if (component.Type.Name == $"app.{alias}Think")
                component = RemoveBirthdayAlternates(component, "OtherDirectivesHolder");
            return component;
        });

        if (alias == "Em3600") {
            // The embedded Marguerite donor uses Birthday damage audio. Copy
            // the native campaign sound array; retain all other template fields.
            var nativeScene = context.GetScnFile(MargeScenePath).ReadScene(context.TypeRepository);
            var nativeActor = nativeScene.FindGameObject(gameObject =>
                gameObject.FindComponent("app.Em3600ActionController") != null)
                ?? throw new InvalidDataException("Campaign Marguerite actor is missing from its native scene.");
            var nativeSound = nativeActor.FindComponent("app.WwiseContainerApp")
                ?? throw new InvalidDataException("Campaign Marguerite sound component is missing.");
            template = template.VisitComponents(component => component.Type.Name == "app.WwiseContainerApp"
                ? component.SetField("ContainableAsset", nativeSound["ContainableAsset"])
                : component);
        }
        return template;
    }

    private static RszObjectNode RemoveBirthdayAlternates(RszObjectNode component, string field) {
        if (component.Type.FindFieldIndex(field) == -1)
            return component;
        var array = (RszArrayNode)component[field];
        return component.SetField(field, new RszArrayNode(array.Type, array.Children
            .Where(node => node is not RszUserDataNode userData ||
                userData.Path?.Contains("birthday", StringComparison.OrdinalIgnoreCase) != true)
            .ToImmutableArray()));
    }
}
