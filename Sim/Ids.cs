namespace TianziGe.Sim;

/// <summary>
/// 强类型 ID（F3 R4）。规则层一律用这些类型，禁止裸 int / string 互相传错。
/// </summary>
public readonly record struct GlyphId(int Value);

public readonly record struct CharId(string Value);

public readonly record struct EntityId(int Value);

public readonly record struct SeasonId(int Value);
