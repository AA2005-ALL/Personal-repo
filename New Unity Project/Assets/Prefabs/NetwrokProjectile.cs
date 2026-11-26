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
            //Ensures the shooter can't hit themself
            if(hit.NetworkObjectId == shooterNetworkObjectId)
            {
                return;
            }

            //Finds player and enemy shooters
            PlayerNetworkData playerShooter = FindShooter();
            NetworkEnemy enemyShooter = FindEnemyShooter();

            //Carries out enemy damage logic
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

            //Carries out player damage logic
            if(hit.GetComponent<PlayerNetworkData>())
            {
                PlayerNetworkData target = hit.GetComponent<PlayerNetworkData>();

                if (target == null)
                {
                    return;
                }

                bool killedPlayer;

                //If the shooter was an enemy no score is added and damage is dealt
                if (enemyShooter != null)
                {
                    if (enemyShooter.NetworkObjectId == shooterNetworkObjectId)
                    {
                        killedPlayer = target.TakeDamage(10);
                    }
                }

                //If the shooter is a player the bullet deals damage and if they get a kill score is added
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

    //Uses network object ID instead of client ID to allow enemy shooting logic to be handled
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
