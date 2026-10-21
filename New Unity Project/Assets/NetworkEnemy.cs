using Unity.Netcode;
using UnityEngine;
public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    private Rigidbody2D rb;
    public NetworkVariable<int> Health = new NetworkVariable<int>(30);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        PlayerNetworkData target = FindNearestPlayer();
        if (target == null) return;
        Vector2 direction =
        ((Vector2)target.transform.position - rb.position).normalized;
        rb.MovePosition(rb.position +
        direction * moveSpeed * Time.fixedDeltaTime);
    }

    private PlayerNetworkData FindNearestPlayer()
    {
        PlayerNetworkData nearest = null;

        float nearestDistance = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            PlayerNetworkData player = client.PlayerObject.GetComponent<PlayerNetworkData>();

            if (player == null || !player.IsAlive.Value)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }

    public bool TakeDamage(int damage)
    {
        if (!IsServer || Health.Value <= 0)
        {
            return false;
        }

        Health.Value = Mathf.Max(0, Health.Value - damage);

        if (Health.Value == 0)
        {
            NetworkGameManager gameManager = FindFirstObjectByType<NetworkGameManager>();

            if (gameManager != null)
            {
                gameManager.ScheduleEnemyRespawn();
            }

            NetworkObject.Despawn(true);

            return true;
        }

        return false;
    }
}