using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class NetwrokProjectile : NetworkBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 2f;
    [SerializeField] SpriteRenderer projectileVisual;
    private NetworkVariable<ulong> syncedShooterClientId = new NetworkVariable<ulong>();
    private NetworkVariable<int> syncedShotId = new NetworkVariable<int>();
    private float despawnTime;
    private ulong shooterNetworkObjectId;
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

        if(other.TryGetComponent<NetworkObject>(out var hit))
        {
            if(hit.NetworkObjectId == shooterNetworkObjectId)
            {
                //Debug.Log("This bullet is mine");
                return;
            }

            PlayerNetworkData playerShooter = FindShooter();
            NetworkEnemy enemyShooter = FindEnemyShooter();

            if (hit.GetComponent<NetworkEnemy>())
            {
                NetworkEnemy enemy = hit.GetComponent<NetworkEnemy>();

                bool killedEnemy = enemy.TakeDamage(playerShooter.GetComponent<PlayerWeapon>().damageDealt.Value);

                if (killedEnemy)
                {
                    if (playerShooter != null)
                    {
                        playerShooter.AddScore(1);
                    }
                }

                NetworkObject.Despawn();

                return;
            }

            if(hit.GetComponent<PlayerNetworkData>())
            {
                //Debug.Log("I know I'm hitting a player");
                PlayerNetworkData target = hit.GetComponent<PlayerNetworkData>();

                if (target == null)
                {
                    return;
                }

                bool killedPlayer;

                if (enemyShooter != null)
                {
                    if (enemyShooter.NetworkObjectId == shooterNetworkObjectId)
                    {
                        killedPlayer = target.TakeDamage(10);
                    }
                }

                if(playerShooter != null) 
                {
                    if (playerShooter.NetworkObjectId == shooterNetworkObjectId)
                    {
                        killedPlayer = target.TakeDamage(playerShooter.GetComponent<PlayerWeapon>().damageDealt.Value);

                        if (killedPlayer)
                        {
                            if (playerShooter.GetComponent<NetworkEnemy>() == null)
                            {
                                Debug.Log("Whoever killed you is not an enemy");
                                playerShooter.AddScore(1);
                            }
                        }
                    }
                }

                NetworkObject.Despawn();
            }
        }


        //NetworkEnemy enemy = other.GetComponent<NetworkEnemy>();

        //if (enemy != null)
        //{
        //    bool killedEnemy = enemy.TakeDamage(10);

        //    if (killedEnemy)
        //    {
        //        if (shooter != null)
        //        {
        //            shooter.AddScore(1);
        //        }
        //    }

        //    NetworkObject.Despawn();

        //    return;
        //}

    }

    public void Initialise(ulong objectId, int shotId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        shooterNetworkObjectId = objectId;
        //syncedShooterObjectId.Value = clientId;
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
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterNetworkObjectId, out NetworkObject shooter))
        {
            return null;
        }

        return shooter.GetComponent<PlayerNetworkData>();
    }

    private NetworkEnemy FindEnemyShooter()
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(shooterNetworkObjectId, out NetworkObject shooter))
        {
            return null;
        }

        return shooter.GetComponent<NetworkEnemy>();
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
