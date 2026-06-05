using UnityEngine;

[DisallowMultipleComponent]
public class SphereShooter : MonoBehaviour
{
    [Header("Prefab i spawn")]
    public Rigidbody spherePrefab;
    public Transform spawnPoint;
    public float spawnDistance = 1.0f;
    public float sphereScale = 1.0f;

    [Header("Strzał")]
    public float shootForce = 25f;
    public float cooldown = 0.2f;
    public float lifeTime = 10f;
    public bool ignorePlayerCollision = true;

    [Header("Opcje kamery")]
    public Camera shooterCamera;

    private float nextShotTime;
    private Collider[] playerColliders;

    private void Awake()
    {
        if (!shooterCamera)
            shooterCamera = Camera.main;

        playerColliders = GetComponentsInChildren<Collider>(includeInactive: true);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + cooldown;
            ShootSphere();
        }
    }

    private void ShootSphere()
    {
        if (!spherePrefab)
        {
            Debug.LogWarning("SphereShooter: brak przypisanego prefabrykatu kulek.", this);
            return;
        }

        if (!shooterCamera)
        {
            Debug.LogWarning("SphereShooter: brak kamery do strzelania.", this);
            return;
        }

        Vector3 position = GetSpawnPosition();
        Quaternion rotation = shooterCamera.transform.rotation;

        Rigidbody instance = Instantiate(spherePrefab, position, rotation);
        if (!instance)
            return;

        instance.transform.localScale = Vector3.one * sphereScale;

        if (ignorePlayerCollision)
            IgnoreCollisionWithPlayer(instance);

        instance.linearVelocity = shooterCamera.transform.forward * shootForce;
        Destroy(instance.gameObject, lifeTime);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint)
            return spawnPoint.position;

        if (shooterCamera)
            return shooterCamera.transform.position + shooterCamera.transform.forward * spawnDistance;

        return transform.position;
    }

    private void IgnoreCollisionWithPlayer(Rigidbody sphere)
    {
        if (!sphere)
            return;

        Collider[] sphereColliders = sphere.GetComponentsInChildren<Collider>();
        if (sphereColliders == null || sphereColliders.Length == 0 || playerColliders == null)
            return;

        foreach (Collider sphereCollider in sphereColliders)
        {
            if (!sphereCollider)
                continue;

            foreach (Collider playerCollider in playerColliders)
            {
                if (!playerCollider || sphereCollider == playerCollider)
                    continue;

                Physics.IgnoreCollision(sphereCollider, playerCollider);
            }
        }
    }
}
