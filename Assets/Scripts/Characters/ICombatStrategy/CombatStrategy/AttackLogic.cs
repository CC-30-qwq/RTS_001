using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackLogic : StateMachineBehaviour
{
    CharacterObj character;

    public float windupNormalizedTime = 0.5f;

    public float timer;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        timer = 0;

        character = animator.transform.GetComponent<CharacterObj>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        character.agent.isStopped = true;

        Attacking(stateInfo);

        windupNormalizedTime = Mathf.Clamp(windupNormalizedTime, 0.1f, 1);

        if (character.currentTarget != null)
        {
            character.Rotate(character.currentTarget.transform.position);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        character.agent.isStopped = false;
    }

    private void Attacking(AnimatorStateInfo stateInfo)
    {
        timer += Time.deltaTime;

        if (timer > stateInfo.length)
        {
            timer = 0;
        }
        else if (timer > windupNormalizedTime * stateInfo.length)
        {
            character.StartCoroutine(character.AttackIns());
        }
    }
}
