using Unity.Netcode;
using UnityEngine;

public class NetwrokProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    private float despawnTime;
    private ulong shooterClientId;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;
        despawnTime = Time.time + lifetime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer)
        {
            return;
        }

        NetworkEnemy enemy = other.GetComponent<NetworkEnemy>();

        if (enemy != null)
        {
            bool killedEnemy = enemy.TakeDamage(10);

            if (killedEnemy)
            {
                PlayerNetworkData shooter = FindShooter();

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

        if (target.OwnerClientId == shooterClientId)
        {
            return;
        }

        bool killedPlayer = target.TakeDamage(10);

        if(killedPlayer)
        {
            PlayerNetworkData shooter = FindShooter();

            if (shooter != null)
            {
                shooter.AddScore(1);
            }
        }

        NetworkObject.Despawn();
    }

    public void Initialise(ulong clientId)
    {
        if (!IsServer)
            return;
        shooterClientId = clientId;
    }

    private void Update()
    {
        if (!IsServer)
            return;



        if (Time.time >= despawnTime)
        {
            NetworkObject.Despawn();
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) 
            return;

        rb.MovePosition(rb.position + (Vector2)transform.up * speed * Time.fixedDeltaTime);
    }

    private PlayerNetworkData FindShooter()
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(shooterClientId, out NetworkClient client))
            return null;

        if (client.PlayerObject == null)
        return null;

        return client.PlayerObject.GetComponent<PlayerNetworkData>();
    }
}
