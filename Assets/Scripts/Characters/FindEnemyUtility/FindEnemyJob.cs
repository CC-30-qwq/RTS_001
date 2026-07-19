using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FindEnemyJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<UnitData> units;
    [ReadOnly] public NativeArray<EnemyData> enemies;
    public NativeArray<TargetResult> results;

    public void Execute(int index)
    {
        UnitData unit = units[index];

        if (!unit.isAlive)
        {
            results[index] = new TargetResult { targetEnemyIndex = -1 };
            return;
        }

        float nearestDisSq = float.MaxValue;
        int bestEnemyIndex = -1;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyData enemy = enemies[i];

            if (!enemy.isAlive || enemy.teamID == unit.teamID || enemy.CanFly_Lv > unit.CanAttackFly_Lv)
                continue;

            float distSq = math.distancesq(unit.position, enemy.position);

            if (distSq < nearestDisSq)
            {
                nearestDisSq = distSq;
                bestEnemyIndex = i;
            }
        }

        results[index] = new TargetResult { targetEnemyIndex = bestEnemyIndex };
    }
}
