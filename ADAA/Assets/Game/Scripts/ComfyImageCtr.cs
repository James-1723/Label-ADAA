using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class ImageData
{
    public string filename;
    public string subfolder;
    public string type;
}

[System.Serializable]
public class OutputData
{
    public ImageData[] images;
}

[System.Serializable]
public class PromptData
{
    public OutputData outputs;
}

public class ComfyImageCtr : MonoBehaviour
{
    public TextMeshPro comfyText3D;
    public ComfyPromptCtr comfyPromptCtr;

    // �s�G�~���]WebSocket�^�I�s�A������^�� (�ɦW, ������|)
    public void RequestAndSave(string promptID, Action<string, string> onDone)
    {
        StartCoroutine(RequestAndSaveRoutine(promptID, onDone));
    }

    IEnumerator RequestAndSaveRoutine(string promptID, Action<string, string> onDone)
    {
        string url = "http://140.119.110.218:8188/history/" + promptID;

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(": Error: " + webRequest.error);
                    SafeSetText("History request failed");
                    comfyPromptCtr.generating = false;
                    onDone?.Invoke(null, null);
                    yield break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(": HTTP Error: " + webRequest.error);
                    SafeSetText("History HTTP error");
                    comfyPromptCtr.generating = false;
                    onDone?.Invoke(null, null);
                    yield break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(":\nReceived: " + webRequest.downloadHandler.text);
                    string fileFromHistory = ExtractFilename(webRequest.downloadHandler.text); // �� �O�d���k
                    string imageURL = "http://140.119.110.218:8188/view?filename=" + UnityWebRequest.EscapeURL(fileFromHistory);
                    yield return DownloadAndSave(imageURL, fileFromHistory, onDone);
                    break;
            }
        }
    }

    // �O�d�G�A����l JSON �ѪR�覡�]���n�ʥ��^
    string ExtractFilename(string jsonString)
    {
        string keyToLookFor = "\"filename\":";
        int startIndex = jsonString.IndexOf(keyToLookFor);

        if (startIndex == -1)
        {
            return "filename key not found";
        }

        startIndex += keyToLookFor.Length;
        string fromFileName = jsonString.Substring(startIndex);
        int endIndex = fromFileName.IndexOf(',');
        string filenameWithQuotes = fromFileName.Substring(0, endIndex).Trim();
        string filename = filenameWithQuotes.Trim('"');
        Debug.Log(filename);
        return filename;
    }

    IEnumerator DownloadAndSave(string imageUrl, string fileFromHistory, Action<string, string> onDone)
    {
        // ���L���ݡA�T�O�ɮפw�N��
        yield return new WaitForSeconds(0.3f);

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                byte[] pngData = texture.EncodeToPNG();

                string root = Application.persistentDataPath; // �� persistentDataPath
                string folderPath = System.IO.Path.Combine(root, "SavedImages").Replace('\\', '/');
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                // �H history �����ɦW���D�F�Y�S .png ���ɦW�N�ɤW .png
                string finalFileName = fileFromHistory.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    ? fileFromHistory
                    : (fileFromHistory + ".png");
                string fullPath = System.IO.Path.Combine(folderPath, finalFileName).Replace('\\', '/');

                // �Y�ɦW�w�s�b �� �[�ɶ��W�קK�л\
                if (System.IO.File.Exists(fullPath))
                {
                    string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string nameNoExt = System.IO.Path.GetFileNameWithoutExtension(finalFileName);
                    finalFileName = $"{nameNoExt}_{stamp}.png";
                    fullPath = System.IO.Path.Combine(folderPath, finalFileName).Replace('\\', '/');
                }

                System.IO.File.WriteAllBytes(fullPath, pngData);
                Debug.Log("Saved image to: " + fullPath);
                SafeSetText("Saved image to: " + fullPath);

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
                comfyPromptCtr.generating = false;
                // 回傳包含 SavedImages 路徑的相對路徑，而不是只有檔案名
                string relativePath = "SavedImages/" + finalFileName;
                onDone?.Invoke(relativePath, fullPath);
            }
            else
            {
                Debug.LogError("Image download failed: " + webRequest.error);
                SafeSetText("Image download failed: " + webRequest.error);
                comfyPromptCtr.generating = false;
                onDone?.Invoke(null, null);
            }
        }
    }

    private void SafeSetText(string msg)
    {
        if (comfyText3D != null) comfyText3D.text = msg;
    }
}