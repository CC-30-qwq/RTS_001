using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size = 10;
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<Projectile>> poolDict;

    void Awake()
    {
        Instance = this;
        poolDict = new Dictionary<string, Queue<Projectile>>();

        foreach (var pool in pools)
        {
            Queue<Projectile> queue = new Queue<Projectile>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                Projectile proj = obj.GetComponent<Projectile>();
                proj.pool = this;
                obj.SetActive(false);
                queue.Enqueue(proj);
            }
            poolDict.Add(pool.tag, queue);
        }
    }

    public Projectile Get(string tag, Vector3 position, Vector3 midPoint, CharacterObj target, float damage)
    {
        if (!poolDict.ContainsKey(tag))
        {
            Debug.LogError($"没有找到标签为 {tag} 的投射物池");
            return null;
        }

        Projectile proj = poolDict[tag].Dequeue();
        proj.transform.position = position;
        proj.characterAttackDamage = damage;
        proj.target = target;
        proj.gameObject.GetComponent<TrailRenderer>().Clear();
        proj.gameObject.SetActive(true);
        proj.Fire(midPoint, target.targetPoint.transform);

        poolDict[tag].Enqueue(proj);
        return proj;
    }
}