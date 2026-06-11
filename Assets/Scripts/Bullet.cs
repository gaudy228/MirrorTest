using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] int damage = 35;

    public GameObject owner;
    private Player ownerPlayer;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, float force, GameObject ownerObject)
    {
        owner = ownerObject;
        ownerPlayer = owner.GetComponent<Player>();

        if (rb != null)
        {
            rb.velocity = direction * force;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(!isServer)
        {
            return;
        }

        if (other.gameObject == owner)
        {
            return;
        }

        if (other.TryGetComponent(out Player player))
        {
            if(player.GetTeam() != ownerPlayer.GetTeam())
            {
                player.TakeDamage(damage);
            }
        }
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}