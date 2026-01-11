using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Text;
using UnityEngine.Networking;

public class LLMGenerator : MonoBehaviour
{
    public string[] fortune;
    public string[] emotion = new string[] { "anger", "fear", "disgust", "sadness", "happiness", "shy", "envy", "anxiety", "passive" };
    
    // 新增用於儲存單次生成結果的變數
    public string generatedEmotion;
    public string generatedFortune;
    
    private string apiKey = "";

    /// <summary>
    /// 隨機生成一個籤詩，直接將結果存入 generatedEmotion 和 generatedFortune 變數
    /// </summary>
    public IEnumerator GenerateSingleFortune()
    {
        // 隨機選擇一個情緒
        string randomEmotion = emotion[UnityEngine.Random.Range(0, emotion.Length)];
        
        string url = "https://api.openai.com/v1/chat/completions";
        string prompt = $"Create a fortune poem having future predictions or guiding advice of life problems in a {emotion} emotion tone, within 30-50 words.";

        string jsonData = @"{
            ""model"": ""gpt-3.5-turbo"",
            ""messages"": [
                { ""role"": ""user"", ""content"": """ + prompt + @""" }
            ]
        }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                
                try
                {
                    OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(responseText);
                    
                    if (response.choices != null && response.choices.Length > 0)
                    {
                        string generatedText = response.choices[0].message.content;
                        // 過濾掉不需要的字符
                        generatedText = FilterUnwantedCharacters(generatedText);
                        
                        // 直接賦值給類別變數
                        generatedEmotion = randomEmotion;
                        generatedFortune = generatedText;
                    }
                    else
                    {
                        Debug.LogError("API 回應中沒有找到生成的文字");
                        generatedEmotion = randomEmotion;
                        generatedFortune = "無法生成籤詩，請稍後再試";
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"解析 JSON 時發生錯誤: {e.Message}");
                    generatedEmotion = randomEmotion;
                    generatedFortune = "無法解析回應，請稍後再試";
                }
            }
            else
            {
                Debug.LogError("錯誤：" + request.error);
                generatedEmotion = randomEmotion;
                generatedFortune = "無法生成籤詩，請稍後再試";
            }
        }
    }

    // 過濾不需要的字符
    private string FilterUnwantedCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        // 移除反斜線、換行符、回車符，並清理前後空白
        return text.Replace("\\", "")
                  .Replace("\n", " ")
                  .Replace("\r", " ")
                  .Replace("  ", " ") // 移除多餘的雙空格
                  .Trim();
    }
}