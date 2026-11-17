using System.Reflection;
using System.Text.RegularExpressions;
using TianziGe.Sim;

namespace TianziGe.Sim.Tests;

/// <summary>
/// 架构守卫（铁律 1 / ADR-001）：
/// Sim 是纯 .NET 类库，不能把引擎依赖、静态可变状态或浮点规则字段带进来。
/// </summary>
[Category("Architecture")]
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

    [Test]
    public void SimSource_DoesNotUseGodotNamespace()
    {
        string simDirectory = Path.Combine(RepositoryPaths.FindRoot(), "Sim");
        string[] offenders = Directory
            .EnumerateFiles(simDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildArtifact(file))
            .Where(file => Regex.IsMatch(File.ReadAllText(file), @"\busing\s+(static\s+)?Godot\b"))
            .Select(file => Path.GetRelativePath(simDirectory, file))
            .ToArray();

        Assert.That(offenders, Is.Empty,
            "Sim 源码不得出现 using Godot；发现：" + string.Join(", ", offenders));
    }

    private static bool IsBuildArtifact(string file)
    {
        string separator = Path.DirectorySeparatorChar.ToString();
        return file.Contains(separator + "bin" + separator, StringComparison.Ordinal)
               || file.Contains(separator + "obj" + separator, StringComparison.Ordinal);
    }

    [Test]
    public void SimTypes_HaveNoStaticMutableFields()
    {
        string[] offenders = typeof(FarmState).Assembly.GetTypes()
            .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
            .SelectMany(type => type
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false))
                .Where(field => IsStaticMutableField(field))
                .Select(field => $"{type.FullName}.{field.Name}"))
            .ToArray();

        Assert.That(offenders, Is.Empty,
            "Sim 不得有静态可变字段（F3 A1）；发现：" + string.Join(", ", offenders));
    }

    [Test]
    public void SimTypes_HaveNoFloatingPointFields()
    {
        string[] offenders = typeof(FarmState).Assembly.GetTypes()
            .SelectMany(type => type
                .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => ContainsFloatingPoint(field.FieldType))
                .Select(field => $"{type.FullName}.{field.Name}"))
            .ToArray();

        Assert.That(offenders, Is.Empty,
            "规则层禁止 float/double 字段（F2 §4.1 / ADR-001）；发现：" + string.Join(", ", offenders));
    }

    private static bool ContainsFloatingPoint(Type type)
    {
        if (type == typeof(float) || type == typeof(double))
        {
            return true;
        }

        if (type.IsArray)
        {
            return type.GetElementType() is { } element && ContainsFloatingPoint(element);
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ContainsFloatingPoint);
    }

    private static bool IsStaticMutableField(FieldInfo field)
    {
        if (field.IsLiteral)
        {
            return false;
        }

        // 静态只读的字符串 / 基本类型 / 枚举不可变，可视为常量。
        if (field.IsInitOnly)
        {
            Type type = field.FieldType;
            return !(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal));
        }

        return true;
    }
}
