using UnityEngine;
using System.Collections;

public class PaperCraneEffectOneShot : MonoBehaviour
{
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioSource audioSource;

    private void OnEnable()
    {
        // Ensure particles only play once
        if (particles != null)
        {
            var main = particles.main;
            main.loop = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }

        if (audioSource != null)
        {
            audioSource.Play();
        }

        StartCoroutine(DestroyWhenFinished());
    }

    private IEnumerator DestroyWhenFinished()
    {
        while ((particles != null && particles.IsAlive(true)) ||
               (audioSource != null && audioSource.isPlaying))
        {
            yield return null;
        }

        Destroy(gameObject); // destroys this clone only
    }
}