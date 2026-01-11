using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class ResponseData
{
    public string prompt_id;
}

public class ComfyPromptCtr : MonoBehaviour
{
    public bool generating = false;
    public TextMeshPro comfyText3D;
    public ComfyWebsocket comfyWebsocket;

    [TextArea(6, 20)]
    public string promptJson; // 需包含 "Pprompt" 與 "Nprompt"

    private TaskCompletionSource<string> _tcsFileName; // 回傳檔名.png
    private Action<string> _onCompletedCallback;

    // 推薦：await 取得檔名
    public async Task<string> QueuePromptAsync(string positivePrompt, string negativePrompt)
    {
        var tcs = new TaskCompletionSource<string>();
        _tcsFileName = tcs;
        _onCompletedCallback = null;
        InternalQueuePrompt(positivePrompt, negativePrompt);
        return await tcs.Task;
    }

    // Callback 版本
    public void QueuePrompt(string positivePrompt, string negativePrompt, Action<string> onCompleted)
    {
        _onCompletedCallback = onCompleted;
        _tcsFileName = null;
        InternalQueuePrompt(positivePrompt, negativePrompt);
    }

    // 舊版（不回檔名）
    public void QueuePrompt(string positivePrompt, string negativePrompt)
    {
        _onCompletedCallback = null;
        _tcsFileName = null;
        InternalQueuePrompt(positivePrompt, negativePrompt);
    }

    private void InternalQueuePrompt(string positivePrompt, string negativePrompt)
    {
        if (!generating)
        {
            generating = true;
            StartCoroutine(QueuePromptCoroutine(positivePrompt, negativePrompt));
        }
        else
        {
            Debug.Log("Generating!");
        }
    }

    private IEnumerator QueuePromptCoroutine(string positivePrompt, string negativePrompt)
    {
        string url = "http://140.119.110.218:8188/prompt";
        Debug.Log("Request URL: " + url);

        int randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
        string promptText = GeneratePromptJson();
        promptText = promptText.Replace("Pprompt", positivePrompt);
        promptText = promptText.Replace("Nprompt", negativePrompt);
        promptText = promptText.Replace("\"seed\": 115018773819333", $"\"seed\": {randomSeed}");
        Debug.Log(promptText);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(promptText);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Request failed!");
            Debug.LogError("Error Type: " + request.result);
            Debug.LogError("HTTP Code: " + request.responseCode);
            Debug.LogError("Error Message: " + request.error);
            Debug.LogError("Server Response: " + request.downloadHandler.text);
            SafeSetText("request failed");
            generating = false;

            _tcsFileName?.TrySetResult(null);
            _onCompletedCallback?.Invoke(null);
        }
        else
        {
            Debug.Log("Prompt queued successfully." + request.downloadHandler.text);
            SafeSetText("Prompt queued successfully.");
            ResponseData data = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
            Debug.Log("Prompt ID: " + data.prompt_id);

            // 讓 WebSocket 監聽，完成後會呼叫 NotifyImageSaved → 回傳檔名
            comfyWebsocket?.ConnectAndListen(data.prompt_id);
        }
    }

    public string GeneratePromptJson()
    {
        string guid = Guid.NewGuid().ToString();
        string promptJsonWithGuid = $@"
{{  ""id"": ""{guid}"",
    ""prompt"": {promptJson}
}}";
        return promptJsonWithGuid;
    }

    // 由 WebSocket → ImageCtr 存檔成功後回呼
    public void NotifyImageSaved(string fileName, string fullPath)
    {
        // fileName 會是 "xxx.png"；fullPath 是 persistentDataPath 下實際路徑
        SafeSetText(string.IsNullOrEmpty(fullPath) ? "Save failed" : $"Saved: {fullPath}");

        // 把檔名回給呼叫端
        _tcsFileName?.TrySetResult(fileName);
        _onCompletedCallback?.Invoke(fileName);
    }

    private void SafeSetText(string msg)
    {
        if (comfyText3D != null) comfyText3D.text = msg;
    }
}