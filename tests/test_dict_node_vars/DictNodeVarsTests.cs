using Fractural.DependencyInjection;
using Fractural.IO;
using Fractural.NodeVars;
using Godot;

namespace Tests
{
    [Start(nameof(Start))]
    [Pre(nameof(Pre))]
    [Post(nameof(Post))]
    public class DictNodeVarsTests : WAT.Test
    {
        private PackedScene _testScene;
        private Node _testSceneInstance;
        private DIContainer _diContainer;

        public void Start()
        {
            _testScene = IO.LoadResourceOrNull<PackedScene>("./TestDictNodeVars.tscn");
        }

        public void Pre()
        {
            GD.Print("Pre");
            _diContainer = new DIContainer();
            var registry = new PackedSceneDefaultValuesRegistry();
            registry.UseFilesystemScan = true;
            registry.ReloadOnReady = true;
            AddChild(_diContainer);
            _diContainer.AddChild(registry);
            _diContainer.Bind<PackedSceneDefaultValuesRegistry>().ToSingle(registry);

            _testSceneInstance = _diContainer.InstantiatePrefab<Node>(_testScene, -1);
            AddChild(_testSceneInstance);
        }

        public void Post()
        {
            GD.Print("Post");
            _diContainer.QueueFree();
            _testSceneInstance.QueueFree();
        }

        [Test]
        public void TestSingleDefaultInheritance()
        {
            GD.Print("TestSIngle");
            Describe("When a NodeVarContainer scene instance is readied");

            var prefab = _testSceneInstance.GetNode<INodeVarContainer>("Prefab");
            Assert.IsEqual(prefab.GetDictNodeVar("InstancedVar1"), 0, "InstancedVar1 is default");
            Assert.IsEqual(prefab.GetDictNodeVar("InstancedVar2"), "heyo", "InstancedVar2 is default");
            Assert.IsEqual(prefab.GetDictNodeVar("InstancedVar3"), "new stuff", "InstancedVar3 is overwritten");
            Assert.IsEqual(prefab.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyAttributeGetVar)), true, $"{nameof(InheritedNodeVarContainer.MyAttributeGetVar)} is overwritten");
            Assert.IsEqual(
                prefab.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyAttributeSetVar), true),
                true,
                $"{nameof(InheritedNodeVarContainer.MyAttributeSetVar)} is default"
            );
            Assert.IsEqual(prefab.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyBoolVar)), false, $"{nameof(InheritedNodeVarContainer.MyBoolVar)} is default");
            Assert.IsEqual(prefab.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyFloatVar)), 0.543f, $"{nameof(InheritedNodeVarContainer.MyFloatVar)} is overwritten");
            Assert.IsEqual(
                prefab.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyGetVar), true),
                "newText",
                $"{nameof(InheritedNodeVarContainer.MyGetVar)} is overwritten"
            );
            Assert.IsEqual(prefab.GetDictNodeVar(nameof(InheritedNodeVarContainer.MySetVar)), Vector3.Zero, $"{nameof(InheritedNodeVarContainer.MySetVar)} is default");
        }

        //[Test]
        //public void TestDoubleDefaultInheritance()
        //{
        //    var doubleInheritPrefab = _testSceneInstance.Get("DoubleInherit");
        //    Assert.AutoPass("TODO LATER: Make default values work with inherited scenes");
        //}

        //[Test]
        //public void TestTripleInheritance()
        //{
        //    var tripleInheritPrefab = _testSceneInstance.Get("TripleInherit");
        //    Assert.AutoPass("TODO LATER: Make default values work with inherited scenes");
        //}
    }
}