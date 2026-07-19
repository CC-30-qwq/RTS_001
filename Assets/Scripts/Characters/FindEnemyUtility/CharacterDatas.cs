using Unity.Mathematics;

public struct UnitData
{
    public float3 position;
    public int teamID;
    public bool isAlive;
    public int CanFly_Lv;
    public int CanAttackFly_Lv;
}

public struct EnemyData
{
    public float3 position;
    public int teamID;
    public bool isAlive;
    public int CanFly_Lv;
    public int CanAttackFly_Lv;
    public int originalIndex;
}

public struct TargetResult
{
    public int targetEnemyIndex;
}