using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class ScnFileTests {
    [Fact]
    public void BaselineScenes_RoundTripHierarchyFieldsAndResourceTables() {
        var repository = FileRepository.RszRepository;
        var pakList = new PakList(Encoding.UTF8.GetString(EmbeddedData.GetFile("pakcontentsrt.txt.gz").Ungzip()));
        using var pak = new PakFile(RandomizerTest.InputPakPath);
        var tested = 0;
        foreach (var hash in pak.FileHashes) {
            var path = pakList.GetPath(hash);
            if (path?.EndsWith($".scn.{FileVersions.SceneFileVersion}", StringComparison.Ordinal) != true)
                continue;

            var original = new ScnFile(FileVersions.SceneFileVersion, pak.GetEntryData(hash));
            AssertRoundTrip(original, repository, path);
            tested++;
        }

        Assert.True(tested > 0, "The baseline must contain scenes for the serialization audit.");
    }

    [Fact]
    public void TemplateScene_RoundTripsHierarchyFieldsAndResourceTables() {
        var path = $"template.scn.{FileVersions.SceneFileVersion}";
        AssertRoundTrip(new ScnFile(FileVersions.SceneFileVersion, EmbeddedData.GetFile(path)),
            FileRepository.RszRepository, path);
    }

    private static void AssertRoundTrip(ScnFile original, RszTypeRepository repository, string path) {
        var builder = original.ToBuilder(repository);
        var rebuilt = builder.Build();
        Assert.True(original.Resources.SequenceEqual(rebuilt.Resources), $"Resource table changed: {path}");
        Assert.True(original.Prefabs.SequenceEqual(rebuilt.Prefabs), $"Prefab table changed: {path}");

        // Instance indices and padding can normalize when rebuilding. Compare the
        // actual hierarchy and fields, including nested FSM actions and references.
        AssertNode(builder.Scene, rebuilt.ReadScene(repository), path);
    }

    private static void AssertNode(IRszNode expected, IRszNode actual, string path) {
        Assert.True(expected.GetType() == actual.GetType(), $"Node kind changed: {path}");
        switch (expected) {
            case RszScene scene:
                AssertChildren(scene.Children, ((RszScene)actual).Children, path);
                break;
            case RszFolder folder:
                var actualFolder = (RszFolder)actual;
                AssertNode(folder.Settings, actualFolder.Settings, path + "/folder-settings");
                AssertChildren(folder.Children, actualFolder.Children, path + "/" + folder.Name);
                break;
            case RszGameObject gameObject:
                var actualGameObject = (RszGameObject)actual;
                path += "/" + gameObject.Name;
                Assert.True(gameObject.Guid == actualGameObject.Guid, $"GameObject GUID changed: {path}");
                Assert.True(gameObject.Prefab == actualGameObject.Prefab, $"Prefab reference changed: {path}");
                Assert.True(gameObject.Padding == actualGameObject.Padding, $"GameObject metadata changed: {path}");
                AssertNode(gameObject.Settings, actualGameObject.Settings, path + "/settings");
                AssertChildren(gameObject.Components, actualGameObject.Components, path + "/components");
                AssertChildren(gameObject.Children, actualGameObject.Children, path);
                break;
            case RszObjectNode obj:
                var actualObject = (RszObjectNode)actual;
                Assert.True(obj.Type.Id == actualObject.Type.Id && obj.Type.Crc == actualObject.Type.Crc,
                    $"Object type changed: {path}");
                Assert.True(obj.Children.Length == actualObject.Children.Length, $"Field count changed: {path}");
                for (var i = 0; i < obj.Children.Length; i++) {
                    AssertNode(obj.Children[i], actualObject.Children[i], path + "/" + obj.Type.Fields[i].Name);
                }
                break;
            case RszArrayNode array:
                var actualArray = (RszArrayNode)actual;
                Assert.True(array.Type == actualArray.Type, $"Array type changed: {path}");
                AssertChildren(array.Children, actualArray.Children, path);
                break;
            case RszValueNode value:
                var actualValue = (RszValueNode)actual;
                Assert.True(value.Type == actualValue.Type && value.Data.Span.SequenceEqual(actualValue.Data.Span),
                    $"Field value changed: {path}");
                break;
            case RszResourceNode resource:
                Assert.True(resource.Value == ((RszResourceNode)actual).Value, $"Resource reference changed: {path}");
                break;
            case RszStringNode text:
                Assert.True(text.Value == ((RszStringNode)actual).Value, $"String changed: {path}");
                break;
            case RszUserDataNode userData:
                var actualUserData = (RszUserDataNode)actual;
                Assert.True(userData.Path == actualUserData.Path && userData.Type?.Id == actualUserData.Type?.Id,
                    $"Userdata reference changed: {path}");
                break;
            case RszNullNode:
                break;
            default:
                Assert.Fail($"Unhandled node type {expected.GetType().Name}: {path}");
                break;
        }
    }

    private static void AssertChildren<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string path)
        where T : IRszNode {
        Assert.True(expected.Count == actual.Count, $"Child count changed: {path}");
        for (var i = 0; i < expected.Count; i++) {
            AssertNode(expected[i], actual[i], path + $"[{i}]");
        }
    }
}
