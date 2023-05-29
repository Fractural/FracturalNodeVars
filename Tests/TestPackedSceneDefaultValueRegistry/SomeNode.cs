using Godot;

namespace Tests
{
    public class SomeNode : Node
    {
        [Export]
        public float SomeFloat { get; set; } = 0.34f;
        [Export]
        public string SomeString { get; set; } = "234";
        public int SomeNonExportedInt { get; set; } = 34;
    }
}