# Fractural Node Vars 📦

![Deploy](https://github.com/Fractural/FracturalNodeVars/actions/workflows/deploy.yml/badge.svg) ![Unit Tests](https://github.com/Fractural/FracturalNodeVars/actions/workflows/tests.yml/badge.svg)

Inspector editable variables for nodes in Godot.

## Dependencies

- FracturalCommons

## NodeVars

Node variables or `NodeVar` for short are improved inspector variables. These variables are stored as Godot dictionary which, with some custom inspector plugins, become editable varaiables in the inspector.

NodeVars features:

- Can fetch it's value from a NodeVar on another node, allowing painless dependency injection directly within the inspector. This is possible only through publically settable NodeVars.
  - `ContainerPath` - Path to NodeVarContainer
  - `ContainerVarName` - Name of the source NodeVar in the NodeVarContainer
- Visibilitiy modifiers to specify what operations are allowing on the NodeVar
  - `Get` - The NodeVar is readable from outside and writable inside of the node.
  - `Set` - The NodeVar is writable from outside and readable inside of the node.
  - `Get/Set` - The NodeVar is writable and readable from inside/outside of the node.
  - `Private` - The NodeVar is readable and writable from inside of the node
- Can be exported as a dictionary of `NodeVars`, that is editable within the Inspector.

## NodeVarContainer

`NodeVarContainer` is a collection of NodeVars. This node can be inherited.

Variables

- Node Vars - An editable dictionary of Node Vars. Inherited Node Vars cannot be deleted.
- Mode - The behaviour of the container
  - `Local` - User can add or remove NodeVars from the container.
  - `Attributes` - Container only displays properties on the container with the `NodeVar` attribute. Containers that inherit from NodeVarContainer can declare properties that use the `NodeVar` attribute.
  - `LocalAttributes` - User can add or remove NodeVars, and properties on the container with `NodeVar` attribute are also added to the container.

You can also implement your own version of a container by creating a Node that implements the `INodeVarContainer` interface.

> **NOTE:**
>
> `NodeVarContainer` does not work with inherited scenes. This is due to a Godot limitation of not exposing the inhertance of a scene at all to tool scripts.
