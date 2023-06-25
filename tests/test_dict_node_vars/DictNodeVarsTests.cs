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

            var container = _testSceneInstance.GetNode<CustomNodeVarContainer>("CustomNodeVarContainer");
            Assert.IsEqual(container.GetNodeVar("InstancedVar1"), 0, "InstancedVar1 is default");
            Assert.IsEqual(container.GetNodeVar("InstancedVar2"), "heyo", "InstancedVar2 is default");
            Assert.IsEqual(container.GetNodeVar("InstancedVar3"), "new stuff", "InstancedVar3 is overwritten");
            Assert.IsEqual(container.PrivateGetNodeVar(nameof(CustomNodeVarContainer.MyAttributeSetVar)), true, $"{nameof(CustomNodeVarContainer.MyAttributeSetVar)} is overwritten");
            Assert.IsEqual(container.GetNodeVar(nameof(CustomNodeVarContainer.MyBoolVar)), false, $"{nameof(CustomNodeVarContainer.MyBoolVar)} is default");
            Assert.IsEqual(container.GetNodeVar(nameof(CustomNodeVarContainer.MyFloatVar)), 0.543f, $"{nameof(CustomNodeVarContainer.MyFloatVar)} is overwritten");
            Assert.IsEqual(container.GetNodeVar(nameof(CustomNodeVarContainer.MyGetVar)), Vector3.Zero, $"{nameof(CustomNodeVarContainer.MyGetVar)} is default");
            Assert.IsEqual(container.GetNodeVar(nameof(CustomNodeVarContainer.MyAttributeGetVar)), false, $"{nameof(CustomNodeVarContainer.MyAttributeGetVar)} is default");
        }

        [Test]
        public void TestForwarding()
        {
            Describe("When a NodeVarContainer has forwarded variables and is readied");

            var forwardContainer = _testSceneInstance.GetNode<INodeVarContainer>("ForwardedNodeVarContainer");

            Assert.IsEqual(forwardContainer.GetNodeVar("Var1"), 235, "Var1 is forwarded from child");
            Assert.IsEqual(forwardContainer.GetNodeVar("Var2"), "This is from another container!", "Var2 is forwarded from sibling");
            Assert.IsEqual(forwardContainer.GetNodeVar("Var3"), 0, "Var3 is default");
        }

        [Test]
        public void TestExpressionNodeVar()
        {
            Describe("When a NodeVarContainer has an expression and is readied");

            var functionCallContainer = _testSceneInstance.GetNode<INodeVarContainer>("FunctionCallNodeVarContainer");

            Assert.IsEqual(functionCallContainer.GetNodeVar("ExpressionVar"), 235 + 43, "ExpressionVar should evaluate to the correct value");
            Assert.IsEqual(functionCallContainer.GetNodeVar("ExpressionVarForwarded"), (43 + 34) * 2, "ExpressionVarForwarded should evaluate to the correct value");
            Assert.IsEqual(functionCallContainer.GetNodeVar("FuncExpressionVar"), 100000, "FuncExpression should evaluate to correct value");
        }
    }
}