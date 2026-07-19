using UnityEngine;

public class MeleeCombatStrategy : ICombatStrategy
{
    bool isActed;

    float targetDistance;

    public float findTarget_timer;
    public float findTargetTime = 2f;

    public override void OnUpdate(CharacterObj character)
    {
        targetDistance = character.GetTargetDistance();

        UpdateAnim(character);

        OnAttack(character);

        FindTarget(character);
    }

    private void FindTarget(CharacterObj character)
    {
        if (character.currentTarget != null)
        {
            if (targetDistance <= 10 && !character.MoveTrigger)
            {
                findTarget_timer += Time.deltaTime;

                if (findTarget_timer >= findTargetTime)
                {
                    if (targetDistance <= character.status.AttackRange_1)
                    {
                        character.agent.ResetPath();
                    }
                    else
                    {
                        character.Move(character.currentTarget.transform.position);
                    }
                }
            }
            else
            {
                findTarget_timer = 0;
            }
        }
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
            else if (targetDistance <= character.status.AttackRange_1)
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
                    StartCoroutine(character.Attack(character.firePoint_1));
                }
            }
        }
        else
        {
            isActed = false;
        }
    }
}
