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

    private MyNet MyNet;
    private MatchManager MatchManager;
    float nextFireTime;

    [SyncVar(hook = nameof(OnTeamChanged))]
    TeamType teamType = TeamType.None;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

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

    public void Init(MyNet myNet, MatchManager matchManager)
    {
        MyNet = myNet;
        MatchManager = matchManager;
    }

    void OnTeamChanged(TeamType oldValue, TeamType newValue)
    {
        UpdateTeam(newValue);
    }

    [Server]
    public void SetTeam(TeamType team)
    {
        teamType = team;
    }

    public TeamType GetTeam()
    {
        return teamType;
    }

    public int GetHealth()
    {
        return health;
    }

    private void UpdateTeam(TeamType team)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                return;
            }
        }

        if (team == TeamType.Red)
        {
            spriteRenderer.color = Color.red;
        }
        else if (team == TeamType.Green)
        {
            spriteRenderer.color = Color.green;
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
                gameObject.SetActive(false);
                RpcDie();
                if (MatchManager != null)
                {
                    MatchManager.CheckRoundEnd();
                }
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
                gameObject.SetActive(false);

                if (MatchManager != null)
                {
                    MatchManager.CheckRoundEnd();
                }
            }
        }
    }

    [ClientRpc]
    void RpcDie()
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        gameObject.SetActive(false);

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    [Server]
    public void Respawn()
    {
        if (!isServer)
        {
            return;
        }

        health = 100;

        Transform spawnPoint = null;
        if (MyNet != null)
        {
            spawnPoint = MyNet.GetSpawnPoint(teamType);
        }

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }

        gameObject.SetActive(true);

        RpcRespawn(spawnPoint != null ? spawnPoint.position : Vector3.zero);
    }

    [ClientRpc]
    void RpcRespawn(Vector3 position)
    {
        if (this == null)
        {
            return;
        }

        if (gameObject == null)
        {
            return;
        }

        transform.position = position;

        health = 100;

        gameObject.SetActive(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            UpdateTeam(teamType);
        }

        if (HpBar != null)
        {
            HpBar.value = 1f;
            OnHealthChanged(100, 100);
        }
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        if (HpBar != null && this != null && gameObject != null)
        {
            HpBar.value = (float)newValue / 100f;
        }
    }

    [Command]
    void CmdShoot(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            return;
        }

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

public enum TeamType
{
    Red,
    Green,
    None
}