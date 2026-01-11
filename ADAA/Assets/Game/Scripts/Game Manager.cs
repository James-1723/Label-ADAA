using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GDNoob.MyPackage;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public CraneManager craneManager;
    public ImageGridManager imageGridManager;

    [Header("Crane Generation")]
    public float replaceInterval = 5f;
    public int initialCranes = 5;
    public int firstCountThreshold;
    public int secondCountThreshold;
    public int craneCount = 0;
    public List<string> pictureHistory;
    public PhotoCardSpawner spawner;
    public bool isFinalTransition = false;

    [Header("Utopia World")]
    public GameObject lightUtopiaScene;
    public GameObject darkUtopiaScene;
    
    [Header("Scene Lighting")]
    public Light directionalLight;

    [Header("Testing UI Components")]
    public TextMesh testingDisplayTextUI;

    [Header("Audio Source")]
    public AudioSource firstTransitionAudio;
    public AudioSource finalTransitionAudio;
    public AudioSource dystopianBGM;
    public AudioSource utopianBGM;
    public AudioSource heavenBGM;

    [Header("Audio Settings")]
    public float bgmFadeDuration = 1.5f;
    private Coroutine bgmFadeCoroutine;

    [Header("Dissolve Groups (Parents)")]
    public Transform[] dissolveGroupParents;

    [Header("Camera Settings")]
    public MetaMRVRSwitcher metaMRVRSwitcher;


    // Fortune Text / Emotion
    // public Dictionary<string, string> fortuneEmotionData;
    
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        // metaMRVRSwitcher.EnableMRPassthrough();
        lightUtopiaScene.SetActive(true);
        darkUtopiaScene.SetActive(false);
        pictureHistory = new List<string>();
        StartCoroutine(InitialCranesGeneration());
        BGMController(0);
        // fortuneEmotionData = new Dictionary<string, string>();
    }

    private IEnumerator InitialCranesGeneration()
    {
        for (int i = 0; i < initialCranes; i++)
        {
            // 等待每個紙鶴生成完成後再生成下一隻
            yield return StartCoroutine(craneManager.GenerateNewCrane());
        }
        StartCoroutine(ReplaceCranesRoutine());
    }

    private IEnumerator ReplaceCranesRoutine()
    {
        while (!isFinalTransition)
        {
            yield return new WaitForSeconds(replaceInterval);
            yield return StartCoroutine(craneManager.GenerateNewCrane());
        } 
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Crane"))
            {
                Destroy(obj);
            }
        }
    }

    // Receive Data from Paper Crane
    public void ReceiveFortuneEmotionData(string[] data, Transform craneTransform, string photoRootPath)
    {
        craneCount++;
        spawner.SpawnPhotoCard(craneTransform.position, photoRootPath);
        pictureHistory.Add(photoRootPath);
        StartCoroutine(SceneTransition());
    }

    private IEnumerator SceneTransition()
    {
        if (craneCount >= secondCountThreshold)
        {
            yield return new WaitForSeconds(1.5f);
            var allRawImages = FindObjectsOfType<RawImage>();
            foreach (RawImage image in allRawImages)
            {
                image.enabled = false;
            }
            finalTransitionAudio.Play();
            isFinalTransition = true;
            yield return StartCoroutine(DissolveGroupsInSequence(1f));
            yield return new WaitForSeconds(1f);
            metaMRVRSwitcher.EnableVRSkybox();
            imageGridManager.ShowGrid(pictureHistory);
            BGMController(2);
        }
        else if (craneCount >= firstCountThreshold)
        {
            firstTransitionAudio.Play();
            ChangeSceneLight();
            lightUtopiaScene.SetActive(false);
            darkUtopiaScene.SetActive(true);
            BGMController(1);
        }
    }

    public void ChangeSceneLight()
    {
        if (directionalLight == null)
        {
            Debug.LogWarning("未找到可更改的 Directional Light");
            return;
        }

        // 使用「Filter + Temperature」外觀：Filter 設為 #FFFFFF，溫度設為 6500K
        directionalLight.useColorTemperature = true;     // 啟用色溫模式
        directionalLight.color = Color.white;            // Filter：純白
        directionalLight.colorTemperature = 6500f;       // 中性白 (D65)

        Debug.Log("已套用 Light Appearance：Filter=#FFFFFF、Temperature=6500K");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.LogWarning("Hi");
            StartCoroutine(DissolveGroupsInSequence(1f));
        }
    }

    public void TriggerAllDissolveAnimations()
    {
        startAnimationForOneObject[] allDissolveObjects = FindObjectsOfType<startAnimationForOneObject>();
        
        foreach (startAnimationForOneObject dissolveObj in allDissolveObjects)
        {
            if (dissolveObj != null)
            {
                dissolveObj.StartDissolve();
            }
        }
    }

    private IEnumerator DissolveGroupsInSequence(float intervalSeconds)
    {
        if (dissolveGroupParents == null || dissolveGroupParents.Length == 0)
        {
            // 若未指定群組，退回一次觸發全部
            TriggerAllDissolveAnimations();
            yield break;
        }

        for (int i = 0; i < dissolveGroupParents.Length; i++)
        {
            Transform parent = dissolveGroupParents[i];
            if (parent != null)
            {
                var group = parent.GetComponentsInChildren<GDNoob.MyPackage.startAnimationForOneObject>(false);
                foreach (var s in group)
                {
                    if (s != null)
                        s.StartDissolve();
                }
            }

            yield return new WaitForSeconds(intervalSeconds);
        }
    }

    private void BGMController(int stage)
    {   
        switch (stage)
        {
            case 0:
                StartCrossfade(null, dystopianBGM, bgmFadeDuration);
                break;
            case 1:
                StartCrossfade(dystopianBGM, utopianBGM, bgmFadeDuration);
                break;
            case 2:
                StartCrossfade(utopianBGM, heavenBGM, bgmFadeDuration);
                break;
        }
    }

    private void StartCrossfade(AudioSource fadeOut, AudioSource fadeIn, float duration)
    {
        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }
        bgmFadeCoroutine = StartCoroutine(CrossfadeCoroutine(fadeOut, fadeIn, duration));
    }

    private IEnumerator CrossfadeCoroutine(AudioSource fadeOut, AudioSource fadeIn, float duration)
    {
        if (duration <= 0f)
        {
            if (fadeOut != null)
            {
                fadeOut.volume = 0f;
                fadeOut.Stop();
            }
            if (fadeIn != null)
            {
                fadeIn.volume = 1f;
                if (!fadeIn.isPlaying) fadeIn.Play();
            }
            yield break;
        }

        float startOut = fadeOut != null ? fadeOut.volume : 0f;
        float startIn = fadeIn != null ? fadeIn.volume : 0f;

        if (fadeIn != null && !fadeIn.isPlaying)
        {
            // 先把淡入音量設低再播放
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            if (fadeOut != null)
            {
                fadeOut.volume = Mathf.Lerp(startOut, 0f, k);
            }
            if (fadeIn != null)
            {
                fadeIn.volume = Mathf.Lerp(startIn, 1f, k);
            }
            yield return null;
        }

        if (fadeOut != null)
        {
            fadeOut.volume = 0f;
            fadeOut.Stop();
        }
        if (fadeIn != null)
        {
            fadeIn.volume = 1f;
        }
    }
}