using System.Collections.Generic;

/// <summary>
/// Lightweight static registry so gameplay code can iterate enemies
/// without calling FindObjectsByType every frame.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<EnemyHealth> healthList = new List<EnemyHealth>();
    private static readonly List<EnemyAI> aiList = new List<EnemyAI>();

    public static IReadOnlyList<EnemyHealth> AllHealth => healthList;
    public static IReadOnlyList<EnemyAI> AllAI => aiList;
    public static int Count => healthList.Count;

    public static void Register(EnemyHealth h) { healthList.Add(h); }
    public static void Unregister(EnemyHealth h) { healthList.Remove(h); }

    public static void Register(EnemyAI ai) { aiList.Add(ai); }
    public static void Unregister(EnemyAI ai) { aiList.Remove(ai); }
}
