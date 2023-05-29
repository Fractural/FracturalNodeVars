# Fractural Node Vars 📦

![Deploy](https://github.com/Fractural/FracturalNodeVars/actions/workflows/deploy.yml/badge.svg) ![Unit Tests](https://github.com/Fractural/FracturalNodeVars/actions/workflows/tests.yml/badge.svg)

Inspector editable variables for nodes in Godot.

## Dependencies

- FracturalCommons

## NodeVars

Node variables or `NodeVar` for short are improved inspector variables. These variables are stored as Godot dictionary which, with some custom inspector plugins, become editable varaiables in the inspector.

NodeVars features: 
- Can fetch it's value from a NodeVar on another node, allowing painless dependency injection directly within the inspector
- Visibilitiy modifiers to specify what operations are allowing on the NodeVar
    - Get - The NodeVar is readable from outside of the node.
    - Set - The NodeVar is writable from outside of the node.
    - Get/Set - The NodeVar is writable and readable from outside of the node. 
- Can be exported as a dictionary of `NodeVars`, that is editable within the Inspector.

## DictNodeVars

Node Var Dictionaries or `DictNodeVars` are dictionaries of NodeVars. They can be exported using the `HintString.AddDictNodevarsProp` extension method for `PropertyListBuilder`. A functioning node vars container is provided by `DictNodeVarsContainer`.

> **NOTE:**
> 
> `DictNodeVars` does not work with inherited scenes. This is due to a Godot limitation of not exposing the inhertance of a scene at all to tool scripts.
