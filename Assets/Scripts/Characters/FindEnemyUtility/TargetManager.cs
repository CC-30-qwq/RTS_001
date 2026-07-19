using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    [Header("设置")]
    public List<CharacterObj> allcharacters;
    public float updateInterval = 0.2f;

    private NativeArray<UnitData> unitDataArray;
    private NativeArray<EnemyData> enemyDataArray;
    private NativeArray<TargetResult> resultArray;
    private JobHandle jobHandle;
    private float lastUpdateTime;

    void Start()
    {
        unitDataArray = new NativeArray<UnitData>(0, Allocator.Persistent);
        enemyDataArray = new NativeArray<EnemyData>(0, Allocator.Persistent);
        resultArray = new NativeArray<TargetResult>(0, Allocator.Persistent);
        lastUpdateTime = -updateInterval;

        if (allcharacters == null || allcharacters.Count == 0)
        {
            RefreshCharacterList();
        }
    }

    public void RefreshCharacterList()
    {
        CharacterObj[] foundCharacters = FindObjectsOfType<CharacterObj>();
        allcharacters = new List<CharacterObj>(foundCharacters);
    }

    void Update()
    {
        if (Time.time < lastUpdateTime + updateInterval) return;

        jobHandle.Complete();
        DisposeArrays();
        PrepareData();

        var job = new FindEnemyJob
        {
            units = unitDataArray,
            enemies = enemyDataArray,
            results = resultArray
        };

        lastUpdateTime = Time.time;
        jobHandle = job.Schedule(resultArray.Length, 64);
    }

    void LateUpdate()
    {
        jobHandle.Complete();
        ApplyResults();
    }

    void PrepareData()
    {
        if (allcharacters == null || allcharacters.Count == 0)
        {
            unitDataArray = new NativeArray<UnitData>(0, Allocator.Persistent);
            enemyDataArray = new NativeArray<EnemyData>(0, Allocator.Persistent);
            resultArray = new NativeArray<TargetResult>(0, Allocator.Persistent);
            return;
        }

        List<int> validIndices = new List<int>();

        for (int i = 0; i < allcharacters.Count; i++)
        {
            CharacterObj character = allcharacters[i];
            if (character == null || character.status == null) continue;

            validIndices.Add(i);
        }

        unitDataArray = new NativeArray<UnitData>(validIndices.Count, Allocator.Persistent);
        resultArray = new NativeArray<TargetResult>(validIndices.Count, Allocator.Persistent);

        List<EnemyData> enemyList = new List<EnemyData>();

        for (int i = 0; i < validIndices.Count; i++)
        {
            int originalIndex = validIndices[i];
            CharacterObj character = allcharacters[originalIndex];

            unitDataArray[i] = new UnitData
            {
                position = character.transform.position,
                teamID = character.status.TeamID,
                isAlive = character.status.IsAlive,
                CanAttackFly_Lv = character.status.CanAttackFly_Lv,
                CanFly_Lv = character.status.CanFly_Lv
            };

            enemyList.Add(new EnemyData
            {
                position = character.transform.position,
                teamID = character.status.TeamID,
                isAlive = character.status.IsAlive,
                CanAttackFly_Lv = character.status.CanAttackFly_Lv,
                CanFly_Lv = character.status.CanFly_Lv,
                originalIndex = i
            });
        }

        enemyDataArray = new NativeArray<EnemyData>(enemyList.ToArray(), Allocator.Persistent);
    }

    void ApplyResults()
    {
        if (allcharacters == null || !resultArray.IsCreated || resultArray.Length == 0) return;
        if (!enemyDataArray.IsCreated) return;

        for (int i = 0; i < resultArray.Length && i < allcharacters.Count; i++)
        {
            CharacterObj character = allcharacters[i];
            if (character == null) continue;

            int targetIdx = resultArray[i].targetEnemyIndex;

            if (targetIdx >= 0 && targetIdx < enemyDataArray.Length)
            {
                EnemyData enemyData = enemyDataArray[targetIdx];
                if (enemyData.originalIndex >= 0 && enemyData.originalIndex < allcharacters.Count)
                {
                    character.SetTarget(allcharacters[enemyData.originalIndex]);
                }
                else
                {
                    character.SetTarget(null);
                }
            }
            else
            {
                character.SetTarget(null);
            }
        }
    }

    void DisposeArrays()
    {
        if (unitDataArray.IsCreated) unitDataArray.Dispose();
        if (enemyDataArray.IsCreated) enemyDataArray.Dispose();
        if (resultArray.IsCreated) resultArray.Dispose();
    }

    void OnDestroy()
    {
        jobHandle.Complete();
        DisposeArrays();
    }
}
