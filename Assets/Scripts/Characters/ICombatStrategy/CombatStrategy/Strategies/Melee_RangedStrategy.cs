using UnityEngine;

public class Melee_RangedStrategy : ICombatStrategy
{
    bool isActed;

    public float targetDistance;

    public float height;
    public string projectileType;

    public override void OnUpdate(CharacterObj character)
    {
        targetDistance = character.GetTargetDistance();

        UpdateAnim(character);

        OnAttack(character);
    }

    private void UpdateAnim(CharacterObj character)
    {
        if (character.currentTarget != null && Vector3.Distance(character.agent.destination, character.transform.position) < 0.1f)
        {
            if (targetDistance <= character.status.AttackRange_0)
            {
                character.animator.SetBool("isAttack", true);
                character.animator.SetInteger("AttackType", 0);
            }
            else if (targetDistance <= character.status.AttackRange_1 && character.currentTarget.status.CanFly_Lv == 0)
            {
                character.animator.SetBool("isAttack", true);
                character.animator.SetInteger("AttackType", 1);
            }
            else
            {
                character.animator.SetBool("isAttack", false);
            }
        }
        else
        {
            character.animator.SetBool("isAttack", false);
        }
    }

    public void OnAttack(CharacterObj character)
    {
        if (character.AttackTrigger)
        {
            if (!isActed)
            {
                isActed = true;
                if (character.animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack_0"))
                {
                    StartCoroutine(character.Attack(character.firePoint_0));
                }
                else if (character.animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack_1"))
                {
                    character.Shot(character.firePoint_1, projectileType, height);
                }
            }
        }
        else
        {
            isActed = false;
        }
    }
}
