using UnityEngine;

[System.Serializable]
public class CharacterStatus
{
    public int TeamID;
    [Range(0, 1)] public int CanFly_Lv;
    [Range(0, 1)] public int CanAttackFly_Lv;
    public float Health;
    public bool IsAlive => Health > 0;
    public float MoveSpeed;
    public float AttackRange_0;
    public float AttackRange_1;
    public float AttackSpeed;
    public float AttackDamage;
    public float AbilityPower;
}
