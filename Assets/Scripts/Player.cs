using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    int health = 100;

    public Slider HpBar;

    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float fireRate = 0.5f;
    [SerializeField] float bulletForce = 20f;

    float nextFireTime;

    void Start()
    {
        if (HpBar != null && isOwned)
        {
            OnHealthChanged(health, health);
        }
    }

    void Update()
    {
        if (isOwned)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float speed = 5f * Time.deltaTime;
            transform.Translate(new Vector2(h * speed, v * speed));

            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;

                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 direction = (mousePos - transform.position).normalized;

                CmdShoot(direction);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isServer)
        {
            health -= damage;
            if (health <= 0)
            {
                health = 0;
                RpcDie();
            }
        }
        else
        {
            CmdTakeDamage(damage);
        }
    }

    [Command]
    void CmdTakeDamage(int damage)
    {
        if (health > 0)
        {
            health -= damage;
            if (health <= 0)
            {
                health = 0;
            }
        }
    }

    [ClientRpc]
    void RpcDie()
    {
        gameObject.SetActive(false);
        Invoke(nameof(Respawn), 3f);
    }

    void Respawn()
    {
        if (isServer)
        {
            RpcRespawn();
        }
    }

    [ClientRpc]
    void RpcRespawn()
    {
        health = 100;
        gameObject.SetActive(true);
        transform.position = Vector2.zero;
        if (HpBar != null)
        {
            OnHealthChanged(health, health);
        }
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        if (HpBar != null)
        {
            HpBar.value = (float)newValue / 100f;
        }
    }

    [Command]
    void CmdShoot(Vector2 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction, bulletForce, gameObject);
        }

        NetworkServer.Spawn(bullet);

        Destroy(bullet, 3f);
    }
}