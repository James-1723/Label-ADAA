using UnityEngine;
using UnityEngine.UI;
using System.IO;

[DisallowMultipleComponent]
public class PhotoCardSpawner : MonoBehaviour
{
    [Header("Prefab & Parent (��ĳ�� World Space Canvas)")]
    [Tooltip("Prefab �W�ݦ� RawImage�F���� PhotoShapeController �|�۰ʸɤW")]
    public GameObject photoCardPrefab;

    [Header("�d���j�p�]UI/�@�ɳ��^")]
    public Vector2 cardSize = new Vector2(0.4f, 0.4f);
    [Header("�l�Ƽ{�ΡGRectTransform.localScale")]
    public float initialScale = 0.4f;

    [Header("���V�ϥΪ�")]
    public bool faceUserOnSpawn = true;   // �ͦ���O�_�¦V�۾�
    public bool rotateY180ForUI = true;   // UI �`���ݭn Y �b 180�X

    [Header("Amoeba ��t�w�]�]�i�̳ߦn�ա^")]
    public bool crossfadeMask = true;
    public float crossfadeDuration = 0.18f;
    public float amoebaFreq = 12f;
    [Range(0, 0.3f)] public float amoebaStrength = 0.14f;
    [Range(0, 1f)] public float circleInner = 0.90f;
    [Range(0, 0.25f)] public float borderFeather = 0.06f;

    [Header("Runtime Mask�]�H���B�n�Ѽơ^")]
    public Vector2Int maskSize = new Vector2Int(512, 512);
    public float maskScale = 3.5f;
    public float maskThresh = 0.5f;
    public float maskFeather = 0.06f;

    // ========= �A�n�� API�G�u�ǡu�ɦW�ά۹���|�v�Y�i =========
    // �ҡGSpawnPhotoCard(0,1.6f,2, "photo1.png")
    // �]�i�Ǥl��Ƨ��GSpawnPhotoCard(pos, "pics/photo1.jpg")
    public PhotoShapeController SpawnPhotoCard(float x, float y, float z, string fileNameOrRelative)
        => SpawnPhotoCard(new Vector3(x, y, z), fileNameOrRelative);

    public PhotoShapeController SpawnPhotoCard(Vector3 worldPos, string fileNameOrRelative)
    {
        string fullPath = CombineWithPersistent(fileNameOrRelative);  // persistentDataPath + �۹���|
        if (!File.Exists(fullPath))
        {
            Debug.LogError(
                $"[PhotoCardSpawner] �䤣���ɮסG{fullPath}\n" +
                $"persistentDataPath�G{Application.persistentDataPath}\n" +
                $"�ǤJ�ȡG{fileNameOrRelative}"
            );
            return null;
        }

        var photoTex = LoadTexture2D(fullPath);
        if (!photoTex)
        {
            Debug.LogError($"[PhotoCardSpawner] Ū�ϥ��ѡG{fullPath}");
            return null;
        }

        return SpawnPhotoCard(worldPos, photoTex); // �� Texture2D ����
    }

    // ===== �]�䴩������ Texture2D�]�q����ӷ��^=====
    public PhotoShapeController SpawnPhotoCard(Vector3 worldPos, Texture2D photoTex)
    {
        if (photoCardPrefab == null)
        {
            Debug.LogError("[PhotoCardSpawner] �Цb Inspector ���w photoCardPrefab �P uiParent�]World Space Canvas�^�C");
            return null;
        }
        if (!photoTex)
        {
            Debug.LogError("[PhotoCardSpawner] photoTex ���šC");
            return null;
        }

        // 1) �ͦ� Prefab
        var go = Instantiate(photoCardPrefab, worldPos, Quaternion.identity);

        // 2) �]�w�j�p
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSize.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSize.y);
            rt.localScale = Vector3.one * initialScale;
        }
        else
        {
            go.transform.localScale = Vector3.one * initialScale;
        }

        // 3) �¦V�ϥΪ̡]�A�� LookAt + 180�X �g�k�^
        if (faceUserOnSpawn && Camera.main)
        {
            var t = go.transform;
            var cam = Camera.main.transform;
            t.LookAt(cam, Vector3.up);            // ���¬۾�
            if (rotateY180ForUI)                  // UI �`���ݭn½ 180�X
                t.Rotate(0f, 180f, 0f, Space.Self);
        }

        // 4) ���o/�ɤW����A�M�Ѽ�
        var ctrl = go.GetComponent<PhotoShapeController>();
        if (!ctrl) ctrl = go.AddComponent<PhotoShapeController>();
        ctrl.SetAmoebaParams(freq: amoebaFreq, strength: amoebaStrength, feather: borderFeather, inner: circleInner);

        // 5) �����H���B�n + �M�Ρ]�ߨ�i���^
        var maskTex = ctrl.GenerateMaskRuntime(
            width: maskSize.x,
            height: maskSize.y,
            scale: maskScale,
            threshold: maskThresh,
            feather: maskFeather,
            seed: Random.Range(0, 999999));

        ctrl.ApplyPhotoAndMask(photoTex, maskTex);

        // 6) �O����M�z�]���� Destroy ������ʺA�K�ϡ^
        var handle = go.AddComponent<PhotoCardHandle>();
        handle.photoTexture = photoTex;
        handle.maskTexture = maskTex;

        return ctrl;
    }

    // ===== Helper�GpersistentDataPath + �۹���|/�ɦW =====
    static string CombineWithPersistent(string fileNameOrRelative)
    {
        // �Y���F������|�� file://�A�N�����ΡF�_�h���� persistentDataPath �U���۹���|
        if (string.IsNullOrEmpty(fileNameOrRelative)) return null;

        if (fileNameOrRelative.StartsWith("file://"))
        {
            try { return new System.Uri(fileNameOrRelative).LocalPath; }
            catch { /* ignore */ }
        }
        if (Path.IsPathRooted(fileNameOrRelative))
            return fileNameOrRelative;

        // �w�]�GpersistentDataPath/�ɦW�Τl���|
        // 修復路徑分隔符問題：統一使用正斜線
        string combinedPath = Path.Combine(Application.persistentDataPath, fileNameOrRelative);
        return combinedPath.Replace('\\', '/');
    }

    // ===== �u��G�Ϻ��ɮ� �� Texture2D =====
    public static Texture2D LoadTexture2D(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (!tex.LoadImage(bytes, markNonReadable: false))
        {
            Object.Destroy(tex);
            return null;
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}

/// ���d���Q Destroy ������ʺA�K�ϡA�קK�O����~��
public class PhotoCardHandle : MonoBehaviour
{
    public Texture2D photoTexture;
    public Texture2D maskTexture;
    void OnDestroy()
    {
        if (photoTexture) Destroy(photoTexture);
        if (maskTexture) Destroy(maskTexture);
    }
}
