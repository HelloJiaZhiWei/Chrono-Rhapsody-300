using UnityEngine;
public class AttackSystem : MonoBehaviour
{
    private LayerMask enemyLayer;
    void Start()
    {
        enemyLayer = GetComponentInParent<PlayerCharacter>().EnemyLayer;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if ((enemyLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Monster enemy = other.GetComponent<Monster>();
            if (enemy != null)
            {
                Vector2 hitDirection = (other.transform.position - transform.position).normalized;

                enemy.TakeDamage(1, hitDirection);
            }
        }
    }
}