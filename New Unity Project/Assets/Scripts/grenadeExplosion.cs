using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class grenadeExplosion : NetworkBehaviour
{
    [SerializeField] float speed = 100f;

    [SerializeField] NetworkVariable<float> lifetime = new NetworkVariable<float>(5f);

    [SerializeField] NetworkObject explosionPrefab;

    NetworkVariable<float> despawnTime = new NetworkVariable<float>(1f);
    private Rigidbody2D rb;

    private AudioSource explosionSound;

    private float outerExplosionRadius = 80f;

    private int centralExplosionRadius = 60;

    private int innerExplosionRadius = 40;

    private int minimumDamageRadius = 13;

    private int maxExplosionDamage = 80;

    private int mediumExplosionDamage = 60;

    private int smallExplosionDamage = 40; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        explosionSound = GetComponent<AudioSource>();

        if(rb == null)
        {
            Debug.LogError("RB not found");
        }

        if(explosionSound == null)
        {
            Debug.LogError("Explosion sound not found");
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
        yield return new WaitForSeconds(lifetime.Value);

        PlayerNetworkData[] players = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log(players[i].transform.position + " This is the distance from this player to the explosion " + Vector3.Distance(transform.position, players[i].transform.position));

            if (Vector3.Distance(transform.position, players[i].transform.position) <= outerExplosionRadius && Vector3.Distance(transform.position, players[i].transform.position) > centralExplosionRadius)
            {
                Debug.Log("Dealing small explosion damage");
                players[i].TakeDamage(smallExplosionDamage);
            }

            if(Vector3.Distance(transform.position, players[i].transform.position) <= centralExplosionRadius && Vector3.Distance(transform.position, players[i].transform.position) > innerExplosionRadius)
            {
                Debug.Log("Dealing medium explosion damage");
                players[i].TakeDamage(mediumExplosionDamage);
            }

            if(Vector3.Distance(transform.position, players[i].transform.position) >= minimumDamageRadius && Vector3.Distance(transform.position, players[i].transform.position) <= innerExplosionRadius)
            {
                Debug.Log("Dealing maximum explosion damage");
                players[i].TakeDamage(maxExplosionDamage);
            }

            if (Vector3.Distance(transform.position, players[i].transform.position) > outerExplosionRadius)
            {
                Debug.Log("Dealing no explosion damage");
                //continue;
            }
        }

        NetworkObject explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosionSound.Play();
        explosion.Spawn();

        yield return new WaitForSeconds(despawnTime.Value);

        explosion.Despawn();
        NetworkObject.Despawn();
    }
}
