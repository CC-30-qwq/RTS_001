using UnityEngine;

public class HitCollider : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    [HideInInspector] public CharacterObj character;

    public float damageAddiction;
    public bool isMelee;

    void OnValidate()
    {
        if (isMelee)
        {
            if (transform.name == "firePoint_0")
            {
                CreateCollider(character.status.AttackRange_0);
            }
            else if (transform.name == "firePoint_1")
            {
                CreateCollider(character.status.AttackRange_1);
            }
        }
    }

    void Awake()
    {
        if (!isMelee)
        {
            BoxCollider collider = transform.GetComponent<BoxCollider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }

    void Start()
    {
        if (isMelee)
        {
            transform.gameObject.SetActive(false);
        }
    }

    private void CreateCollider(float attackRange)
    {
        BoxCollider collider = transform.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = transform.gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            collider.center = new Vector3(0, 0.5f, attackRange / 2);
            collider.size = new Vector3(collider.size.x, 1, attackRange);
            collider.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Character"))
        {
            other.transform.GetComponent<CharacterObj>().status.Health -= attackDamage + damageAddiction;
        }
    }
}
