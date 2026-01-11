using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraneManager : MonoBehaviour
{
    public GameObject paperCranePrefab;
    public LLMGenerator llmGenerator;
    public float spawnRadius = 5f;
    public EmotionFortunePromptComposer composer;
    private List<PaperCrane> activeCranes = new List<PaperCrane>();  // 追蹤所有生成的紙鶴    

    private void Start()
    {
        activeCranes.Clear();
    }

    public IEnumerator GenerateNewCrane()
    {
        yield return StartCoroutine(GenerateSingleCrane());
    }

    private IEnumerator GenerateSingleCrane()
    {
        yield return StartCoroutine(llmGenerator.GenerateSingleFortune());
        if (llmGenerator.generatedEmotion != null && llmGenerator.generatedFortune != null)
        {
            string currentEmotion = llmGenerator.generatedEmotion;
            string currentFortune = llmGenerator.generatedFortune;

            Vector3 randomPosition = transform.position + Random.insideUnitSphere * spawnRadius;
            randomPosition.y = Mathf.Abs(randomPosition.y);

            var task = composer.ComposeAndSpawn(currentEmotion, currentFortune);


            // Debug.LogWarning($"currentFileName: {task.Result}");

            
            // 等待 Task 完成
            yield return new WaitUntil(() => task.IsCompleted);
            
            GameObject craneObject = Instantiate(paperCranePrefab, randomPosition, Random.rotation);
            PaperCrane crane = craneObject.GetComponent<PaperCrane>();
            
            if (task.IsCompletedSuccessfully)
            {
                string fileName = task.Result;
                if (fileName == null || fileName == "")
                {
                    craneObject.SetActive(false);
                    Debug.LogError("ComposeAndSpawn 執行失敗");
                    yield break;
                }
                crane.Initialize(currentEmotion, currentFortune, fileName);
                // GameManager.Instance.craneGeneratedCount++;
                // GameManager.Instance.testingDisplayTextUI.text = $"Crane Generated Count: {GameManager.Instance.craneGeneratedCount}";
                activeCranes.Add(crane);
                Debug.Log($"生成新紙鶴：{currentEmotion} - {currentFortune}");
            }
        }
        else
        {
            Debug.LogError("生成紙鶴失敗：無法獲取籤詩內容");
        }
    }
    
    // private void ProcessGenerationQueue()
    // {
    //     if (generationQueue.Count > 0 && !isGenerating)
    //     {
    //         isGenerating = true;
    //         var nextGeneration = generationQueue.Dequeue();
    //         nextGeneration.Invoke();
    //     }
    // }

    // private IEnumerator GenerateCraneCoroutine()
    // {
    //     yield return StartCoroutine(llmGenerator.GenerateFortune());

    //     if (llmGenerator.fortune != null && llmGenerator.fortune.Length == 2)
    //     {
    //         // 先保存當前的 fortune 數據，避免被下次生成覆蓋
    //         string currentEmotion = llmGenerator.fortune[0];
    //         string currentFortune = llmGenerator.fortune[1];
            
    //         Vector3 randomPosition = transform.position + Random.insideUnitSphere * spawnRadius;
    //         randomPosition.y = Mathf.Abs(randomPosition.y);

    //         GameObject craneObject = Instantiate(paperCranePrefab, randomPosition, Random.rotation);
    //         // PaperCrane crane = craneObject.GetComponent<PaperCrane>();
            
    //         // if (crane != null)
    //         // {   
    //         //     // 使用協程來處理異步操作
    //         //     yield return StartCoroutine(ComposeAndInitializeCrane(crane, currentEmotion, currentFortune));
    //         //     activeCranes.Add(crane);
    //         //     Debug.Log($"生成新紙鶴：{currentEmotion} - {currentFortune}");
    //         // }
    //         PaperCrane crane = this.paperCranePrefab.GetComponent<PaperCrane>();
    //         yield return StartCoroutine(ComposeAndInitializeCrane(crane, currentEmotion, currentFortune));
    //         activeCranes.Add(crane);
    //         Debug.Log($"生成新紙鶴：{currentEmotion} - {currentFortune}");
    //     }
    //     else
    //     {
    //         Debug.LogError("生成紙鶴失敗：無法獲取籤詩內容");
    //     }
        
    //     // 完成生成，標記為非生成狀態並處理下一個佇列項目
    //     isGenerating = false;
    //     ProcessGenerationQueue();
    // }

    // private IEnumerator ComposeAndInitializeCrane(PaperCrane crane, string emotion, string fortune)
    // {
    //     var task = composer.ComposeAndSpawn(emotion, fortune);
        
    //     // 等待 Task 完成
    //     yield return new WaitUntil(() => task.IsCompleted);
        
    //     if (task.IsCompletedSuccessfully)
    //     {
    //         string fileName = task.Result;
    //         crane.Initialize(emotion, fortune, fileName);
    //         GameManager.Instance.craneGeneratedCount++;
    //         GameManager.Instance.testingDisplayTextUI.text = $"Crane Generated Count: {GameManager.Instance.craneGeneratedCount}";
    //     }
    //     else
    //     {
    //         Debug.LogError("ComposeAndSpawn 執行失敗");
    //         // 即使失敗也要初始化紙鶴，只是沒有檔案名稱
    //         crane.Initialize(emotion, fortune, "");
    //     }
    // }

    public List<PaperCrane> GetActiveCranes()
    {
        return activeCranes;
    }

    public void CleanupDestroyedCranes()
    {
        activeCranes.RemoveAll(crane => crane == null);
    }
}