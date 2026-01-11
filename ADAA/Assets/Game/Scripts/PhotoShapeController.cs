using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class PhotoShapeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage raw;

    [Header("Mask Transition")]
    [SerializeField] private bool enableCrossfade = true;
    [SerializeField] private float crossfadeDuration = 0.18f;

    [Header("Alpha / Border")]
    [Range(0, 1)][SerializeField] private float cutoff = 0.001f;
    [Range(0, 1)][SerializeField] private float circleInner = 0.88f;    // 中心保留半徑
    [Range(0, 1)][SerializeField] private float borderFeather = 0.04f;  // 邊緣柔化

    [Header("Amoeba Edge Noise")]
    [SerializeField] private float edgeNoiseFreq = 12f;                    // 沿圓周顆粒數
    [Range(0, 0.3f)][SerializeField] private float edgeNoiseStrength = 0.08f;
    [SerializeField] private float edgeNoisePhase = 0f;                    // 可做動畫：phase += t*speed

    [Header("Mask Influence")]
    [Range(0, 1)][SerializeField] private float maskInfluence = 0.5f;
    [Range(0.5f, 4f)][SerializeField] private float maskContrast = 1.5f;
    [SerializeField] private bool invertMask = false;

    // Shader IDs
    static readonly int ID_MainTex = Shader.PropertyToID("_MainTex");
    static readonly int ID_MaskA = Shader.PropertyToID("_MaskA");
    static readonly int ID_MaskB = Shader.PropertyToID("_MaskB");
    static readonly int ID_Blend = Shader.PropertyToID("_Blend");
    static readonly int ID_Cutoff = Shader.PropertyToID("_Cutoff");
    static readonly int ID_RectAspect = Shader.PropertyToID("_RectAspect");
    static readonly int ID_CircleInner = Shader.PropertyToID("_CircleInner");
    static readonly int ID_BorderFeather = Shader.PropertyToID("_BorderFeather");
    static readonly int ID_EdgeNoiseFreq = Shader.PropertyToID("_EdgeNoiseFreq");
    static readonly int ID_EdgeNoiseStrength = Shader.PropertyToID("_EdgeNoiseStrength");
    static readonly int ID_EdgeNoisePhase = Shader.PropertyToID("_EdgeNoisePhase");
    static readonly int ID_MaskInfluence = Shader.PropertyToID("_MaskInfluence");
    static readonly int ID_MaskContrast = Shader.PropertyToID("_MaskContrast");
    static readonly int ID_InvertMask = Shader.PropertyToID("_InvertMask");

    Material mat;
    Coroutine blendCo;

    void Reset() { raw = GetComponent<RawImage>(); }
    void Awake()
    {
        if (!raw) raw = GetComponent<RawImage>();
        var shader = Shader.Find("UI/PhotoWithMaskLerp");
        if (!shader) { Debug.LogWarning("找不到 UI/PhotoWithMaskLerp，暫用 UI/Default。"); shader = Shader.Find("UI/Default"); }
        mat = new Material(shader);
        raw.material = mat;
        SyncAllParams();
        UpdateRectAspect();
    }

    void OnRectTransformDimensionsChange() { UpdateRectAspect(); }

    void LateUpdate()
    {
        // 若你在 Inspector 中調參，這裡即時同步
        SyncAllParams();
        // 想做“會動的變形蟲”可打開下面一行（每幀改 phase）
        // edgeNoisePhase += Time.unscaledDeltaTime * 0.2f; mat.SetFloat(ID_EdgeNoisePhase, edgeNoisePhase);
    }

    void UpdateRectAspect()
    {
        if (raw == null || mat == null) return;
        var size = raw.rectTransform.rect.size;
        float aspect = (size.y > 1e-4f) ? (size.x / size.y) : 1f;
        mat.SetFloat(ID_RectAspect, aspect);
    }

    void SyncAllParams()
    {
        if (mat == null) return;
        mat.SetFloat(ID_Cutoff, cutoff);
        mat.SetFloat(ID_CircleInner, circleInner);
        mat.SetFloat(ID_BorderFeather, borderFeather);
        mat.SetFloat(ID_EdgeNoiseFreq, edgeNoiseFreq);
        mat.SetFloat(ID_EdgeNoiseStrength, edgeNoiseStrength);
        mat.SetFloat(ID_EdgeNoisePhase, edgeNoisePhase);
        mat.SetFloat(ID_MaskInfluence, maskInfluence);
        mat.SetFloat(ID_MaskContrast, maskContrast);
        mat.SetFloat(ID_InvertMask, invertMask ? 1f : 0f);
    }

    // —— 對外 API —— //
    public void SetPhoto(Texture photo) { if (!mat) return; raw.texture = photo; mat.SetTexture(ID_MainTex, photo); }
    public void SetMaskImmediate(Texture mask) { if (!mat) return; mat.SetTexture(ID_MaskA, mask); mat.SetFloat(ID_Blend, 0f); }
    public void CrossfadeToMask(Texture newMask, float? durationOverride = null)
    {
        if (!mat) return;
        var lastB = mat.GetTexture(ID_MaskB);
        if (lastB) mat.SetTexture(ID_MaskA, lastB); else mat.SetTexture(ID_MaskA, newMask);
        mat.SetTexture(ID_MaskB, newMask);
        if (blendCo != null) StopCoroutine(blendCo);
        blendCo = StartCoroutine(CoBlend(durationOverride ?? crossfadeDuration));
    }
    IEnumerator CoBlend(float t)
    {
        mat.SetFloat(ID_Blend, 0f); float e = 0f;
        while (e < t) { e += Time.unscaledDeltaTime; mat.SetFloat(ID_Blend, Mathf.SmoothStep(0, 1, e / t)); yield return null; }
        mat.SetFloat(ID_Blend, 1f);
    }
    public void ApplyPhotoAndMask(Texture photo, Texture mask)
    { SetPhoto(photo); if (enableCrossfade) CrossfadeToMask(mask); else SetMaskImmediate(mask); }

    // 供其他城市/腳本直接控制變形噪聲
    public void SetAmoebaParams(float freq, float strength, float? feather = null, float? inner = null)
    {
        edgeNoiseFreq = freq; edgeNoiseStrength = strength;
        if (feather.HasValue) borderFeather = feather.Value;
        if (inner.HasValue) circleInner = inner.Value;
        SyncAllParams();
    }

    // 快速生成遮罩（與先前相同）
    public Texture2D GenerateMaskRuntime(int width = 512, int height = 512, float scale = 3.5f, float threshold = 0.5f, float feather = 0.06f, int seed = -1)
    {
        if (seed < 0) seed = Random.Range(0, 999999);
        var tex = new Texture2D(width, height, TextureFormat.Alpha8, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[width * height];
        float invW = 1f / (width - 1), invH = 1f / (height - 1), fth = Mathf.Clamp01(feather);
        for (int y = 0; y < height; y++)
        {
            float v = y * invH;
            for (int x = 0; x < width; x++)
            {
                float u = x * invW;
                float n = Mathf.PerlinNoise((u * scale) + seed, (v * scale) - seed * 0.37f);
                n += 0.5f * Mathf.PerlinNoise((u * scale * 2f) - seed * 0.11f, (v * scale * 2f) + seed * 0.19f);
                n *= 0.67f;
                float a = Mathf.InverseLerp(threshold - fth, threshold + fth, n);
                byte A = (byte)Mathf.RoundToInt(a * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, A);
            }
        }
        tex.SetPixels32(pixels); tex.Apply(false, false); return tex;
    }
}
