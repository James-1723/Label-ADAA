using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ComfyWebsocket : MonoBehaviour
{
    public ComfyImageCtr comfyImageCtr;
    public ComfyPromptCtr comfyPromptCtr;

    private string serverAddress = "140.119.110.218:8188";
    private string clientId = Guid.NewGuid().ToString();
    private ClientWebSocket ws = new ClientWebSocket();
    private string promptID;

    public async void ConnectAndListen(string newPromptID)
    {
        promptID = newPromptID;
        if (ws.State == WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket 已連線，不重複連線");
            return;
        }
        await ws.ConnectAsync(new Uri($"ws://{serverAddress}/ws?clientId={clientId}"), CancellationToken.None);
        StartListening();
    }

    private async void StartListening()
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = null;

        while (ws.State == WebSocketState.Open)
        {
            var sb = new StringBuilder();
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
            }
            while (!result.EndOfMessage);

            string response = sb.ToString();
            Debug.Log("Received: " + response);

            if (response.Contains("\"queue_remaining\": 0"))
            {
                // 下載並存檔，最後把檔名傳回 PromptCtr
                comfyImageCtr.RequestAndSave(promptID, (fileName, fullPath) =>
                {
                    comfyPromptCtr?.NotifyImageSaved(fileName, fullPath);
                });
            }
        }
    }

    async void OnDestroy()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
    }
}