---
title: Instantiation
sidebar_position: 2
---

To use the loaded module, you need to instantiate it.  
At this time, you need to import the objects required by the module.

```csharp
var module = await moduleAsset.LoadModuleAsync();

var instance = new Instance(module, new Imports()
{
    {
        "module name", new ModuleExports()
        {
            { "key", new Memory(new Limits(1)) }
        }
    }
});
```

### Importing Functions

:::info
See [Bindings](./bindings.md) for more information.
:::

### Importing Linear Memory

```csharp
var module = await moduleAsset.LoadModuleAsync();

var instance = new Instance(module, new Imports()
{
    {
        "module name", new ModuleExports()
        {
            { "key", new Memory(new Limits(1)) }
        }
    }
});
```

You can also import objects exported by other modules together.

```csharp
var instance0 = new Instance(module0, new Imports());

var instance1 = new Instance(module1, new Imports()
{
    { "module 0", instance0 }
});
```
