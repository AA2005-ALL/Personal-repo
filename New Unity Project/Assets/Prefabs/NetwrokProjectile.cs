using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class NetwrokProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private SpriteRenderer projectileVisual;
    private NetworkVariable<ulong> syncedShooterClientId = new NetworkVariable<ulong>();
    private NetworkVariable<int> syncedShotId = new NetworkVariable<int>();
    private float despawnTime;
    private ulong shooterClientId;
    private Rigidbody2D rb;
    private PredictedProjectile matchedPredictedProjectile;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            despawnTime = Time.time + lifetime;
        }

        if (!IsServer && syncedShooterClientId.Value == NetworkManager.LocalClientId)
        {
            projectileVisual.enabled = false;
        }

        if (matchedPredictedProjectile != null)
        {
            matchedPredictedProjectile.ConfirmServerDespawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (matchedPredictedProjectile != null)
        {
            matchedPredictedProjectile.ConfirmServerDespawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer)
        {
            return;
        }

        PlayerNetworkData shooter = FindShooter();

        NetworkEnemy enemy = other.GetComponent<NetworkEnemy>();

        if (enemy != null)
        {
            bool killedEnemy = enemy.TakeDamage(10);

            if (killedEnemy)
            {
                if (shooter != null)
                {
                    shooter.AddScore(1);
                }
            }

            NetworkObject.Despawn();

            return;
        }


        PlayerNetworkData target = other.GetComponent<PlayerNetworkData>();

        if (target == null)
        {
            return;
        }

        if(target == shooter)
        {
            print("is this bullet mine? " + shooter.name);
            //return;
        }

        else
        {
            Debug.Log("I know I'm hitting a player");
            bool killedPlayer = target.TakeDamage(10);

            if (killedPlayer)
            {
                if (shooter != null)
                {
                    shooter.AddScore(1);
                }
            }

            NetworkObject.Despawn();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("I know I'm hitting a player");
        PlayerNetworkData target = collision.gameObject.GetComponent<PlayerNetworkData>();
        bool killedPlayer = target.TakeDamage(10);
    }

    public void Initialise(ulong clientId, int shotId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        shooterClientId = clientId;
        syncedShooterClientId.Value = clientId;
        syncedShotId.Value = shotId;
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (Time.time >= despawnTime)
        {
            NetworkObject.Despawn();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        rb.MovePosition(rb.position + (Vector2)transform.up * speed * Time.fixedDeltaTime);
    }

    private PlayerNetworkData FindShooter()
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(shooterClientId, out NetworkClient client))
        {
            return null;
        }

        if (client.PlayerObject == null)
        {
            return null;
        }

        return client.PlayerObject.GetComponent<PlayerNetworkData>();
    }

    private void MatchPredictedProjectile()
    {
        if (IsServer)
        {
            return;
        }

        if (syncedShooterClientId.Value != NetworkManager.LocalClientId)
        {
            return;
        }

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

        if (localPlayer == null)
        {
            return;
        }

        PlayerWeapon weapon = localPlayer.GetComponent<PlayerWeapon>();

        if (weapon == null)
        {
            return;
        }

        PredictedProjectile predicted = weapon.FindPredictedShot(syncedShotId.Value);

        if (predicted != null)
        {
            matchedPredictedProjectile = predicted;
            predicted.SetAuthoritativeTarget(transform);
        }
    }
}
