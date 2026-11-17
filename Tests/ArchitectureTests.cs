using System.Reflection;
using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

/// <summary>
/// 架构守卫：Sim 是纯 .NET 类库，不能把引擎依赖带进来（铁律 1）。
/// Godot 的托管程序集以 "Godot" 开头；Unity 的以 "UnityEngine" 开头。
/// </summary>
public sealed class ArchitectureTests
{
    [Test]
    public void SimAssembly_DoesNotReferenceAnyEngineAssembly()
    {
        AssemblyName[] references = typeof(FarmState).Assembly.GetReferencedAssemblies();
        string[] engineReferences = references
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Godot", StringComparison.Ordinal)
                           || name.StartsWith("UnityEngine", StringComparison.Ordinal))
            .ToArray();

        Assert.That(engineReferences, Is.Empty,
            "TianziGe.Sim 必须保持零引擎依赖；发现：" + string.Join(", ", engineReferences));
    }
}
