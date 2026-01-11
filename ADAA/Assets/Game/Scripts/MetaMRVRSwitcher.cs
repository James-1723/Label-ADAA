using UnityEngine;

public class MetaMRVRSwitcher : MonoBehaviour
{
    public Camera cam;
    public OVRPassthroughLayer passthrough;   // 直接抓元件，別只用 GameObject
    public Material skyboxMat;

    void Start()
    {
        if (!cam) cam = GetComponent<Camera>();
        EnableMRPassthrough(); // 或依需求
    }

    public void EnableVRSkybox()
    {
        cam.clearFlags = CameraClearFlags.Skybox;
        if (skyboxMat) RenderSettings.skybox = skyboxMat;

        // if (passthrough) passthrough.enabled = false;   // 關元件
        // if (OVRManager.instance != null) OVRManager.instance.isInsightPassthroughEnabled = false; // 關全域
    }

    public void EnableMRPassthrough()
    {
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0,0,0,0);       // 透明黑給 Underlay
        RenderSettings.skybox = null;

        if (passthrough)
        {
            passthrough.enabled = true;
        }
        if (OVRManager.instance != null) OVRManager.instance.isInsightPassthroughEnabled = true;
    }
}
