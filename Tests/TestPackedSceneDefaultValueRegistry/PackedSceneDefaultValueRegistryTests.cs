using Fractural.NodeVars;
using Fractural.IO;
using Godot;

namespace Tests
{
    [Start(nameof(Start))]
    public class PackedSceneDefaultValueRegistryTests : WAT.Test
    {
        private PackedScene _someNodePrefab;

        public void Start()
        {
            _someNodePrefab = IO.LoadResourceOrNull<PackedScene>("./SomeNode.tscn");
        }

        [Test]
        public void TestScanFileSystem()
        {
            Describe("When registry performs filesystem scan");
            var registry = new PackedSceneDefaultValuesRegistry();

            Assert.IsNull(registry.GetDefaultValues(_someNodePrefab), "Then default values for scene initially should not exist.");

            registry.UseFilesystemScan = true;
            registry.ReloadOnReady = true;
            AddChild(registry);

            Assert.IsNotNull(registry.GetDefaultValues(_someNodePrefab), "Then after ready default values for scene should exist.");
            Assert.IsEqual(registry.GetDefaultValues(_someNodePrefab)[nameof(SomeNode.SomeFloat)], 0.34f, "Then SomeFloat should be a default value.");
            Assert.IsEqual(registry.GetDefaultValues(_someNodePrefab)[nameof(SomeNode.SomeString)], "SomeString", "Then SomeFloat should be a default value.");
            Assert.IsFalse(registry.GetDefaultValues(_someNodePrefab).ContainsKey(nameof(SomeNode.SomeNonExportedInt)), "Then SomeNonExportedInt should not be a default value.");

            registry.QueueFree();
        }

        [Test]
        public void TestUncachedFetch()
        {
            Describe("When a default value is looked up without cache");
            var registry = new PackedSceneDefaultValuesRegistry();

            registry.UseCache = false;
            registry.UseFilesystemScan = false;
            registry.ReloadOnReady = false;
            AddChild(registry);

            Assert.IsNotNull(registry.GetDefaultValues(_someNodePrefab), "Then the default values for scene should exist.");
            Assert.IsEqual(registry.GetDefaultValues(_someNodePrefab)[nameof(SomeNode.SomeFloat)], 0.34f, "Then SomeFloat should be a default value.");
            Assert.IsEqual(registry.GetDefaultValues(_someNodePrefab)[nameof(SomeNode.SomeString)], "SomeString", "Then SomeFloat should be a default value.");
            Assert.IsFalse(registry.GetDefaultValues(_someNodePrefab).ContainsKey(nameof(SomeNode.SomeNonExportedInt)), "Then SomeNonExportedInt should not be a default value.");

            registry.QueueFree();
        }
    }
}