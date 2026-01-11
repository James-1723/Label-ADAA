using UnityEngine;
using System.Collections;

public class AutoDestroyInstance : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioSource audioSource;

    private IEnumerator Start()
    {
        if (particles != null) particles.Play(true);
        if (audioSource != null) audioSource.Play();

        while ((particles != null && particles.IsAlive(true)) ||
               (audioSource != null && audioSource.isPlaying))
        {
            yield return null;
        }

        Destroy(gameObject); // destroys this instance only
    }
}