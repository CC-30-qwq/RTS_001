using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class CharacterObj : MonoBehaviour
{
    [HideInInspector] public Animator animator;
    [HideInInspector] public NavMeshAgent agent;

    public GameObject footEffect;
    public Transform firePoint_0;
    public Transform firePoint_1;
    public Transform targetPoint;

    public CharacterStatus status;
    public CharacterObj currentTarget;

    public bool AttackTrigger;
    public bool MoveTrigger;

    ICombatStrategy combatStrategy;

    void OnValidate()
    {
        status.AttackRange_0 = Mathf.Clamp(status.AttackRange_0, 1.5f, status.AttackRange_1);
        status.AttackRange_1 = Mathf.Clamp(status.AttackRange_1, 1.5f, 100);
        status.AttackSpeed = Mathf.Clamp(status.AttackSpeed, 0.1f, 10f);

        GetPoint();

        InstantHitCollider();
        InstantAllPoint();

        SetCharacterCollider();
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        combatStrategy = transform.GetComponent<ICombatStrategy>();
    }

    void Start()
    {
        IsSelected(false);

        agent.acceleration = 50;
        agent.angularSpeed = 300;

        AttackTrigger = false;
    }

    void Update()
    {
        animator.SetBool("isMove", agent.velocity.magnitude > 0.1f);
        animator.SetFloat("MoveSpeed", agent.velocity.magnitude / status.MoveSpeed);
        animator.SetFloat("AttackSpeed", status.AttackSpeed);

        agent.speed = status.MoveSpeed;

        combatStrategy?.OnUpdate(this);
    }

    #region Spawn
    private void GetPoint()
    {
        if (transform.Find("firePoint_0") != null)
        {
            firePoint_0 = transform.Find("firePoint_0");
        }
        if (transform.Find("firePoint_1") != null)
        {
            firePoint_1 = transform.Find("firePoint_1");
        }
        if (transform.Find("targetPoint") != null)
        {
            targetPoint = transform.Find("targetPoint");
        }
    }

    private void SetCharacterCollider()
    {
        if (transform.GetComponent<CapsuleCollider>() == null)
        {
            transform.gameObject.AddComponent<CapsuleCollider>();
        }
        else
        {
            transform.GetComponent<CapsuleCollider>().center = new Vector3(0, transform.GetComponent<CapsuleCollider>().height / 2, 0);
            transform.GetComponent<CapsuleCollider>().radius = 0.5f;
        }
    }

    private void InstantAllPoint()
    {
        CreatPoint("targetPoint");
        CreatPoint("firePoint_0");
        CreatPoint("firePoint_1");
    }

    private void InstantHitCollider()
    {
        CreateCollider(firePoint_0);
        CreateCollider(firePoint_1);
    }

    private void CreatPoint(string name)
    {
        foreach (Transform point in transform)
        {
            if (transform.Find(name) == null)
            {
                GameObject Point = new GameObject(name);
                Point.transform.SetParent(transform, false);
                Debug.Log("Has created the" + name);
            }
        }
    }

    private void CreateCollider(Transform firePoint)
    {
        if (firePoint != null)
        {
            HitCollider Virturl = firePoint.GetComponent<HitCollider>();
            if (Virturl == null)
            {
                Virturl = firePoint.gameObject.AddComponent<HitCollider>();
            }
            else
            {
                Virturl.character = this;
            }
        }
    }
    #endregion

    #region Locomotion
    public void Move(Vector3 targetPos)
    {
        agent.SetDestination(targetPos);
    }

    public void Rotate(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        Quaternion targetRot = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, agent.angularSpeed / 10 * Time.deltaTime);
    }
    #endregion

    #region Combat
    public float GetTargetDistance()
    {
        if (currentTarget != null)
        {
            return Vector3.Distance(transform.position, currentTarget.transform.position);
        }
        else
        {
            return status.AttackRange_1 + 1;
        }
    }

    #region Ranged
    public void Shot(Transform firePoint, string projectileType, float heightModifily)
    {
        if (currentTarget != null && currentTarget.targetPoint != null)
        {
            ProjectilePool.Instance.Get(projectileType, firePoint.position, GetMidPoint(heightModifily), currentTarget, status.AttackDamage);
        }
    }

    public void Fire(Transform firePoint, string projectileType, float radius, float height, int count)
    {
        if (currentTarget != null)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 midPoint = firePoint.position + GetRandomPoint(radius, height);
                ProjectilePool.Instance.Get(projectileType, firePoint.position, midPoint, currentTarget, status.AbilityPower);
            }
        }
    }

    Vector3 GetMidPoint(float heightModifily)
    {
        if (currentTarget != null)
        {
            return new Vector3((transform.position.x + currentTarget.transform.position.x) / 2, heightModifily * Vector3.Distance(transform.position, currentTarget.transform.position), (transform.position.z + currentTarget.transform.position.z) / 2);
        }
        else
        {
            return Vector3.zero;
        }
    }

    Vector3 GetRandomPoint(float radius, float height)
    {
        if (currentTarget != null)
        {
            return new Vector3(Random.Range(-radius, radius), Random.Range(-height, height), Random.Range(-radius, radius));
        }
        else
        {
            return Vector3.zero;
        }
    }
    #endregion

    #region Melee
    public IEnumerator Attack(Transform firePoint)
    {
        firePoint.GetComponent<HitCollider>().attackDamage = status.AttackDamage;

        if (currentTarget != null)
        {
            firePoint.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(0.2f);
        firePoint.gameObject.SetActive(false);
    }
    #endregion
    #endregion

    #region OutputIns
    public IEnumerator AttackIns()
    {
        AttackTrigger = true;
        yield return null;
        AttackTrigger = false;
    }

    public IEnumerator MoveIns()
    {
        MoveTrigger = true;
        yield return null;
        MoveTrigger = false;
    }
    #endregion

    #region Control
    public void IsSelected(bool isSelected)
    {
        footEffect.SetActive(isSelected);
    }

    public void SetTarget(CharacterObj target)
    {
        currentTarget = target;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, status.AttackRange_0);
        Gizmos.DrawWireSphere(transform.position, status.AttackRange_1);
    }
}
