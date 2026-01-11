using UnityEngine;
using System.Collections.Generic;

public class PaperCraneEffectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private float rayDistance = 200f;

    [Header("Spawn limits")]
    [SerializeField] private int maxSimultaneous = 6;  // limit active clones
    [SerializeField] private float clickCooldown = 0.05f;

    private readonly Queue<GameObject> liveInstances = new Queue<GameObject>();
    private float lastSpawnTime = -999f;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time - lastSpawnTime < clickCooldown) return;
        if (effectPrefab == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, rayDistance, clickableLayers))
        {
            var rot = Quaternion.LookRotation(hit.normal); 
            var inst = Instantiate(effectPrefab, hit.point, rot);
            liveInstances.Enqueue(inst);

            while (liveInstances.Count > maxSimultaneous)
            {
                var oldest = liveInstances.Dequeue();
                if (oldest) Destroy(oldest);
            }

            lastSpawnTime = Time.time;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (effectPrefab != null && effectPrefab.scene.IsValid())
        {
            Debug.LogWarning("[IceHitSpawner] 'iceHitPrefab' is a SCENE object. " +
                             "Drag a PREFAB ASSET (blue cube icon) from the Project window.", this);
        }
    }
#endif
}