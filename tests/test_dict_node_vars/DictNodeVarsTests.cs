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
            if (IsInstanceValid(_diContainer)) _diContainer.QueueFree();
            if (IsInstanceValid(_testSceneInstance)) _testSceneInstance.QueueFree();
        }

        [Test]
        public void TestSingleDefaultInheritance()
        {
            Describe("When a NodeVarContainer scene instance is readied");

            var container = _testSceneInstance.GetNode<INodeVarContainer>("Prefab");
            Assert.IsEqual(container.GetDictNodeVar("InstancedVar1"), 0, "InstancedVar1 is default");
            Assert.IsEqual(container.GetDictNodeVar("InstancedVar2"), "heyo", "InstancedVar2 is default");
            Assert.IsEqual(container.GetDictNodeVar("InstancedVar3"), "new stuff", "InstancedVar3 is overwritten");
            Assert.IsEqual(container.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyAttributeGetVar)), true, $"{nameof(InheritedNodeVarContainer.MyAttributeGetVar)} is overwritten");
            //Assert.IsEqual(
            //    container.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyAttributeSetVar), true),
            //    true,
            //    $"{nameof(InheritedNodeVarContainer.MyAttributeSetVar)} is default"
            //);
            Assert.IsEqual(container.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyBoolVar)), false, $"{nameof(InheritedNodeVarContainer.MyBoolVar)} is default");
            Assert.IsEqual(container.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyFloatVar)), 0.543f, $"{nameof(InheritedNodeVarContainer.MyFloatVar)} is overwritten");
            //Assert.IsEqual(
            //    container.GetDictNodeVar(nameof(InheritedNodeVarContainer.MyGetVar), true),
            //    "newText",
            //    $"{nameof(InheritedNodeVarContainer.MyGetVar)} is overwritten"
            //);
            Assert.IsEqual(container.GetDictNodeVar(nameof(InheritedNodeVarContainer.MySetVar)), Vector3.Zero, $"{nameof(InheritedNodeVarContainer.MySetVar)} is default");
        }

        [Test]
        public void TestForwarding()
        {
            Describe("When a NodeVarContainer has forwarded variables and is readied");

            var forwardContainer = _testSceneInstance.GetNode<INodeVarContainer>("Forwarded");
            var siblingContainer = _testSceneInstance.GetNode<INodeVarContainer>("AnotherContainer");

            Assert.IsEqual(forwardContainer.GetDictNodeVar("Var1"), 235, "Var1 is forwarded from child");
            Assert.IsEqual(forwardContainer.GetDictNodeVar("Var2"), "This is from another container!", "Var2 is forwarded from sibling");
            Assert.IsEqual(forwardContainer.GetDictNodeVar("Var3"), 0, "Var3 is default");

            var newVec = new Vector2(35, 3.5f);
            Assert.IsEqual(siblingContainer.GetDictNodeVar("SettableVar"), Vector2.Zero, "Sibling SettableVar is initially Vector2.Zero");
            forwardContainer.SetDictNodeVar("Var4", newVec);
            Assert.IsEqual(siblingContainer.GetDictNodeVar("SettableVar"), newVec, "Sibling SettableVar is now Vector2(35, 3.5f) after setting through Var4 forwarding");
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