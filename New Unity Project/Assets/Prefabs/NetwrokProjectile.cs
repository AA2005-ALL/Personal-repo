using Unity.Netcode;
using UnityEngine;

public class NetwrokProjectile : NetworkBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    private float despawnTime;
    private ulong shooterClientId;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;
        despawnTime = Time.time + lifetime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer)
            return;
        PlayerNetworkData target = other.GetComponent<PlayerNetworkData>();

        if (target == null)
            return;

        if (target.OwnerClientId == shooterClientId)
            return;

        target.TakeDamage(10);
        PlayerNetworkData shooter = FindShooter();

        if (shooter != null)
        {
            shooter.AddScore(1);
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

        transform.position += transform.up * speed * Time.deltaTime;
        
        if (Time.time >= despawnTime)
        {
            NetworkObject.Despawn();
        }
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
