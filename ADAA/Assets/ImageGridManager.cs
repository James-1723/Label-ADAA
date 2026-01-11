using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ImageGridManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject imagePrefab;
    public Transform gridParent;
    public float imageSize = 1f;
    public float spacing = 0.1f;
    public KeyCode showGridKey = KeyCode.G; // 按G鍵顯示/隱藏

    [Header("Individual Scales")]
    public float[] imageScales = new float[]
    {
        0.8f, 1.32f, 1.38f, 1.21f, 2.16f, 1.4f, 0.96f, 1.3f, 1.48f
    };

    private List<Texture2D> images = new List<Texture2D>();
    private List<GameObject> imageObjects = new List<GameObject>();
    private bool isGridVisible = false;
    private bool imagesLoaded = false;

    void Awake()
    {
        if (gridParent == null)
        {
            Transform canvas = GetComponentInChildren<Canvas>()?.transform;
            if (canvas != null)
                gridParent = canvas.Find("GridParent");
        }

        if (imagePrefab == null)
            CreateImagePrefab();
    }

    void Start()
    {
        StartCoroutine(LoadImages());
    }

    void Update()
    {
        // 按G鍵顯示/隱藏九宮格
        if (Input.GetKeyDown(showGridKey))
        {
            if (isGridVisible)
                HideGrid();
            else
                ShowGrid();
        }
    }

    IEnumerator LoadImages()
    {
        string root = Application.persistentDataPath;
        string imagePath = Path.Combine(root, "SavedImages");

        if (!Directory.Exists(imagePath))
        {
            Debug.LogError("Images folder not found: " + imagePath);
            yield break;
        }

        List<string> allFiles = new List<string>();
        allFiles.AddRange(Directory.GetFiles(imagePath, "*.jpg"));
        allFiles.AddRange(Directory.GetFiles(imagePath, "*.png"));

        // 依最後修改時間由新到舊排序，只取前 9 張
        var topNine = allFiles
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .Take(9)
            .ToList();

        foreach (string file in topNine)
        {
            yield return StartCoroutine(LoadSingleImage(file));
        }

        Debug.Log($"Loaded {images.Count} images from {imagePath}");
        imagesLoaded = true;

        // 載入完成後先隱藏
        HideGrid();
    }

    IEnumerator LoadSingleImage(string filePath)
    {
        // 若給的是相對路徑（例如 SavedImages/xxx.png），自動補上 persistentDataPath
        if (!Path.IsPathRooted(filePath))
        {
            string root = Application.persistentDataPath;
            filePath = Path.Combine(root, filePath);
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            yield break;
        }

        byte[] fileData = null;
        try
        {
            fileData = File.ReadAllBytes(filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to read file: " + filePath + "\n" + ex.Message);
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(fileData))
        {
            Debug.LogError("Failed to decode image: " + filePath);
            yield break;
        }

        images.Add(texture);
        yield return null;
    }

    void ShowImages()
    {
        // 清除現有圖片
        foreach (GameObject obj in imageObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        imageObjects.Clear();

        // 顯示前9張圖片
        int imagesToShow = Mathf.Min(9, images.Count);

        Vector2[] scatteredPositions = new Vector2[]
        {
            new Vector2(-150f, 120f),   // 左上
            new Vector2(20f, 180f),     // 中上
            new Vector2(170f, 90f),     // 右上
            new Vector2(-120f, -20f),   // 左中
            new Vector2(30f, 15f),      // 中心
            new Vector2(190f, -50f),    // 右中
            new Vector2(-180f, -150f),  // 左下
            new Vector2(-30f, -120f),   // 中下
            new Vector2(150f, -170f)    // 右下
        };

        float[] scatteredRotations = new float[]
        {
            -12f,   // 左上：逆時針傾斜
            8f,     // 中上：順時針傾斜
            -5f,    // 右上：輕微逆時針
            15f,    // 左中：順時針傾斜
            0f,     // 中心：不旋轉
            -18f,   // 右中：逆時針傾斜
            10f,    // 左下：順時針傾斜
            -8f,    // 中下：逆時針傾斜
            12f     // 右下：順時針傾斜
        };

        for (int i = 0; i < imagesToShow; i++)
        {
            // 建立圖片物件
            GameObject imageObj = Instantiate(imagePrefab, gridParent);
            Image img = imageObj.GetComponent<Image>();

            // 建立圓角圖片
            Texture2D roundedTexture = CreateRoundedTexture(images[i], 25);
            Sprite sprite = Sprite.Create(roundedTexture,
                new Rect(0, 0, roundedTexture.width, roundedTexture.height),
                new Vector2(0.5f, 0.5f));
            img.sprite = sprite;

            // 設定位置和旋轉 (散落佈局)
            RectTransform rect = imageObj.GetComponent<RectTransform>();

            // 移除正方形約束 - 保持原始圖片比例
            float aspectRatio = (float)roundedTexture.width / roundedTexture.height;
            if (aspectRatio > 1) // 寬圖
            {
                rect.sizeDelta = new Vector2(imageSize, imageSize / aspectRatio);
            }
            else // 高圖或正方形
            {
                rect.sizeDelta = new Vector2(imageSize * aspectRatio, imageSize);
            }

            // 使用預設散落座標
            rect.anchoredPosition = scatteredPositions[i];

            // 設定旋轉角度
            rect.localRotation = Quaternion.Euler(0, 0, scatteredRotations[i]);

            // 設定個別縮放
            float scale = (i < imageScales.Length) ? imageScales[i] : 1f;
            rect.localScale = Vector3.one * scale;

            imageObjects.Add(imageObj);
        }

        Debug.Log($"Showing {imagesToShow} images in scattered layout");
    }

    Texture2D CreateRoundedTexture(Texture2D original, int cornerRadius)
    {
        Texture2D rounded = new Texture2D(original.width, original.height);

        for (int x = 0; x < original.width; x++)
        {
            for (int y = 0; y < original.height; y++)
            {
                float distToEdge = GetDistanceToRoundedRect(x, y, original.width, original.height, cornerRadius);

                if (distToEdge > 0)
                {
                    rounded.SetPixel(x, y, Color.clear); // 圓角外透明
                }
                else if (distToEdge > -2)
                {
                    // 邊緣抗鋸齒
                    Color originalColor = original.GetPixel(x, y);
                    float alpha = Mathf.Clamp01(-distToEdge / 2f);
                    originalColor.a *= alpha;
                    rounded.SetPixel(x, y, originalColor);
                }
                else
                {
                    rounded.SetPixel(x, y, original.GetPixel(x, y)); // 原始像素
                }
            }
        }

        rounded.Apply();
        return rounded;
    }

    float GetDistanceToRoundedRect(int x, int y, int width, int height, int cornerRadius)
    {
        float centerX = width / 2f;
        float centerY = height / 2f;

        float dx = Mathf.Max(0, Mathf.Abs(x - centerX) - (centerX - cornerRadius));
        float dy = Mathf.Max(0, Mathf.Abs(y - centerY) - (centerY - cornerRadius));

        return Mathf.Sqrt(dx * dx + dy * dy) - cornerRadius;
    }

    void CreateImagePrefab()
    {
        // 自動建立ImageItem預製件
        GameObject imageItem = new GameObject("ImageItem");
        imageItem.AddComponent<RectTransform>();
        imageItem.AddComponent<Image>();

        // 這裡可以設定為臨時預製件或者要求使用者提供
        imagePrefab = imageItem;
    }

    // 公開方法：顯示九宮格（使用預載的圖片）
    public void ShowGrid()
    {
        if (!imagesLoaded)
        {
            Debug.LogWarning("Images not loaded yet!");
            return;
        }

        ShowImages();
        isGridVisible = true;
        Debug.Log("Grid shown");
    }

    // 公開方法：顯示九宮格（使用指定的圖片路徑列表）
    public void ShowGrid(List<string> imagePaths)
    {
        if (imagePaths == null || imagePaths.Count == 0)
        {
            Debug.LogWarning("No image paths provided!");
            return;
        }

        StartCoroutine(LoadAndShowCustomImages(imagePaths));
    }

    // 載入並顯示自定義圖片路徑的圖片
    IEnumerator LoadAndShowCustomImages(List<string> imagePaths)
    {
        // 清除現有圖片
        images.Clear();
        
        // 載入指定路徑的圖片
        int loadedCount = 0;
        foreach (string imagePath in imagePaths)
        {
            if (loadedCount >= 9) break; // 最多載入9張圖片
            
            yield return StartCoroutine(LoadSingleImage(imagePath));
            loadedCount++;
        }
        
        Debug.Log($"Loaded {images.Count} custom images");
        
        // 顯示圖片
        ShowImages();
        isGridVisible = true;
        Debug.Log("Custom grid shown");
    }

    // 公開方法：隱藏九宮格
    public void HideGrid()
    {
        // 清除所有圖片
        foreach (GameObject obj in imageObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        imageObjects.Clear();

        isGridVisible = false;
        Debug.Log("Grid hidden");
    }
}