using UnityEngine;

public class PredictedProjectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 2f;
    private float despawnTime;

    [SerializeField] float correctionSpeed = 5f;
    [SerializeField] float compensationTime = 0.5f;
    private Transform authoritativeTarget;

    private void Start()
    {
        despawnTime = Time.time + lifetime;
    }

    private void Update()
    {
        transform.position += transform.up * speed * Time.deltaTime;

        if (Time.time >= despawnTime)
        {
            Destroy(gameObject);
        }

        if (authoritativeTarget != null)
        {
            Vector3 compensatedPosition = authoritativeTarget.position + authoritativeTarget.up * speed * compensationTime;
            float alpha = Mathf.Clamp01(correctionSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, compensatedPosition, alpha);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerNetworkData player = other.GetComponent<PlayerNetworkData>();

        if (player == null)
        {
            return;
        }

        if (player.IsOwner)
        {
            return;
        }

        Destroy(gameObject);
    }

    public void SetAuthoritativeTarget(Transform target)
    {
        authoritativeTarget = target;
    }

    public void ConfirmServerDespawn()
    {
        Destroy(gameObject);
    }
}
