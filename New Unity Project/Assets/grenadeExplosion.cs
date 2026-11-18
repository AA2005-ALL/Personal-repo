using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class grenadeExplosion : NetworkBehaviour
{
    [SerializeField] float speed = 100f;

    [SerializeField] public float lifetime = 5f;

    [SerializeField] NetworkObject explosionPrefab;

    private float despawnTime;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if(rb == null)
        {
            Debug.LogError("RB not found");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(waitToExplode());
        }
    }

    public override void OnNetworkDespawn()
    {

    }


    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        rb.MovePosition(rb.position + (Vector2)transform.up * speed * Time.fixedDeltaTime);
    }

    IEnumerator waitToExplode()
    {
        yield return new WaitForSeconds(5f);

        NetworkObject.gameObject.SetActive(false);

        NetworkObject explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.Spawn();

        yield return new WaitForSeconds(0.5f);
        explosion.Despawn();
        NetworkObject.Despawn();
    }
}
