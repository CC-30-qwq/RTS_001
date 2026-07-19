using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float maxFollowrange = 10f;
    public float rotateLerp = 1f;
    public float damage;
    public CharacterObj target;

    [HideInInspector] public float characterAttackDamage;
    [HideInInspector] public ProjectilePool pool;

    void Update()
    {
        if (Vector3.Distance(transform.position, target.targetPoint.transform.position) < 0.1f)
        {
            target.transform.GetComponent<CharacterObj>().status.Health -= damage + characterAttackDamage;
            transform.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Respawn"))
        {
            StopAllCoroutines();
        }
    }

    public void Fire(Vector3 midPoint, Transform target)
    {
        transform.up = target.position - transform.position;
        StartCoroutine(Move(transform.position, midPoint, target));
    }

    IEnumerator Move(Vector3 start, Vector3 midPoint, Transform target)
    {
        float distance = Vector3.Distance(start, target.position);
        float totalTime = distance / speed;
        float elapsedTime = 0f;

        while (elapsedTime < totalTime)
        {
            float i = elapsedTime / totalTime;
            Vector3 p = (1 - i) * (1 - i) * start + 2 * (1 - i) * i * midPoint + i * i * target.position;

            // 如果下一个点太远，忽略这个点
            if (Vector3.Distance(transform.position, p) <= maxFollowrange)
            {
                yield return StartCoroutine(MoveToPoint(p));
            }

            elapsedTime += Time.deltaTime;
        }

        if (Vector3.Distance(transform.position, target.position) <= maxFollowrange)
        {
            yield return StartCoroutine(MoveToPoint(target.position));
        }
        else
        {
            yield return StartCoroutine(Hide());
        }
    }

    IEnumerator MoveToPoint(Vector3 p)
    {
        while (Vector3.Distance(transform.position, p) > 0.01f)
        {
            Vector3 dir = p - transform.position;
            transform.up = Vector3.Lerp(transform.up, dir, rotateLerp);
            transform.position = Vector3.MoveTowards(transform.position, p, Time.deltaTime * speed);
            yield return null;
        }
        transform.position = p;
    }

    IEnumerator Hide()
    {
        float timer = 0;

        while (timer < 1f)
        {
            transform.up = Vector3.Lerp(transform.up, transform.up + Vector3.down * Time.deltaTime, rotateLerp);
            transform.position += transform.up * speed * Time.deltaTime;

            timer += Time.deltaTime;
            yield return null;
        }
        transform.gameObject.SetActive(false);
    }
}
