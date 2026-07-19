using UnityEngine;

public abstract class ICombatStrategy : MonoBehaviour
{
    public abstract void OnUpdate(CharacterObj character);
}
