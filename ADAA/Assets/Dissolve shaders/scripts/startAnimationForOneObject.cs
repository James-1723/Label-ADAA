using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GDNoob.MyPackage
{
    public class startAnimationForOneObject : MonoBehaviour
    {
        public static startAnimationForOneObject Instance;
        public KeyCode KeyToStartDissolve = KeyCode.L;
        public KeyCode KeyToStartAppear = KeyCode.W;

        private Renderer targetRenderer;
        private Material[] targetMaterial;

        public float dissolveRate = 0.0125f;
        public float refreshRate = 0.025f;

        // Audio
        public AudioSource audioSource; // Assign in Inspector
        public AudioClip appearSound;   // Assign in Inspector

        void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            targetMaterial = targetRenderer.materials;

            // Optional: Auto-get AudioSource if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyToStartDissolve))
            {
                StartCoroutine(dissolveCo1());
            }

            if (Input.GetKeyDown(KeyToStartAppear))
            {
                StartCoroutine(dissolveCo11());
            }
        }

        public void StartDissolve()
        {
            StartCoroutine(dissolveCo1());
        }

        public IEnumerator dissolveCo1()
        {
            if (targetMaterial[0].GetFloat("_visble_amount") < 1)
            {
                float counter = 0;
                while (targetMaterial[0].GetFloat("_visble_amount") <= 1)
                {
                    counter += dissolveRate;
                    for (int i = 0; i < targetMaterial.Length; i++)
                    {
                        targetMaterial[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
            }
        }

        IEnumerator dissolveCo11()
        {
            // Play sound at the start of appear animation
            if (audioSource != null && appearSound != null)
            {
                audioSource.PlayOneShot(appearSound);
            }

            if (targetMaterial[0].GetFloat("_visble_amount") > 0)
            {
                float counter = 1;
                while (counter >= 0)
                {
                    counter -= dissolveRate;
                    for (int i = 0; i < targetMaterial.Length; i++)
                    {
                        targetMaterial[i].SetFloat("_visble_amount", counter);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
            }
        }
    }
}