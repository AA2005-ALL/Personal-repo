using UnityEngine;

public class PredictedProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    private float despawnTime;

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
    }
}
