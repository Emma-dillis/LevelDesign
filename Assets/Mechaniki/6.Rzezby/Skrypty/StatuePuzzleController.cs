using System.Collections;
using UnityEngine;

public class StatuePuzzleController : MonoBehaviour
{
    [Header("Rzezby")]
    public StatueStation[] statues;

    [Header("Kule do zrzucenia")]
    public Rigidbody[] fallingSpheres;
    public bool lockSpheresOnStart = true;
    public float sphereReleaseDelay = 0.35f;

    private bool finaleStarted;

    private void Awake()
    {
        if (statues == null || statues.Length == 0)
            statues = FindObjectsOfType<StatueStation>();

        foreach (StatueStation statue in statues)
        {
            if (statue)
                statue.SetController(this);
        }

        if (lockSpheresOnStart)
            LockSpheres();
    }

    private void LockSpheres()
    {
        foreach (Rigidbody sphere in fallingSpheres)
        {
            if (!sphere) continue;

            sphere.useGravity = false;
            sphere.isKinematic = true;
            sphere.linearVelocity = Vector3.zero;
            sphere.angularVelocity = Vector3.zero;
        }
    }

    public void StatueTurnedToCenter(StatueStation turnedStatue)
    {
        if (finaleStarted || !AllStatuesTurned())
            return;

        StartCoroutine(FinaleRoutine());
    }

    private bool AllStatuesTurned()
    {
        if (statues == null || statues.Length == 0)
            return false;

        foreach (StatueStation statue in statues)
        {
            if (!statue || !statue.IsTurnedToCenter)
                return false;
        }

        return true;
    }

    private IEnumerator FinaleRoutine()
    {
        finaleStarted = true;

        foreach (StatueStation statue in statues)
        {
            if (statue)
                statue.RotateHandsForFinale();
        }

        if (sphereReleaseDelay > 0f)
            yield return new WaitForSeconds(sphereReleaseDelay);

        foreach (Rigidbody sphere in fallingSpheres)
        {
            if (!sphere) continue;

            sphere.isKinematic = false;
            sphere.useGravity = true;
            sphere.WakeUp();
        }
    }
}
