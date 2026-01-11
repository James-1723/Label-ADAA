using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PaperCrane : MonoBehaviour
{
    [Header("Crane's Emotion, Fortune Text and Material")]
    public string emotion;
    
    public string fortuneText;

    public Material craneMaterial;
    public string photoRootPath;
    public GameObject particleEffect;

    [Header("Testing UI Components")]
    public TextMesh displayText;
    public Text fortuneTextUI;

    [Header("Flight Settings")]
    public float flySpeed = 2f;
    public float rotationSpeed = 180f;

    [Header("Flying Bounds")]
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsSize = new Vector3(10f, 5f, 10f);
    
    [Header("Flying Physics")]
    private Vector3 moveDirection;
    private Quaternion targetRotation;
    private bool isRotating = false;
    // public PhotoCardSpawner spawner;

    private void Start()
    {
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<SphereCollider>();
        }
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Personal UI Panel Component
        GameObject uiObject = GameObject.Find("FortuneText");

        if (uiObject != null)
        {
            fortuneTextUI = uiObject.GetComponent<Text>();
            if (fortuneTextUI == null)
            {
                // Debug.LogError("在 FortuneTextUI 物件上找不到 Text 組件");
            }
        }
        else
        {
            // Debug.LogError("找不到名為 FortuneTextUI 的物件");
        }

        GameObject uiObject3D = GameObject.Find("Fortune");
        if (uiObject3D != null)
        {
            displayText = uiObject3D.GetComponent<TextMesh>();
            if (displayText == null)
            {
                // Debug.LogError("在 FortuneTextUI3D 物件上找不到 TextMesh 組件");
            }
        }
        else
        {
            // Debug.LogError("找不到名為 Fortune 的物件");
        }
        
        moveDirection = Random.onUnitSphere;
        // moveDirection.y *= 0.5f;
        moveDirection = moveDirection.normalized;
        
        targetRotation = GetRotationForDirection(moveDirection);
        transform.rotation = targetRotation;
    }

    private Vector3 GetRandomPositionInBounds()
    {
        return new Vector3(
            Random.Range(boundsCenter.x - boundsSize.x / 2, boundsCenter.x + boundsSize.x / 2),
            Random.Range(boundsCenter.y - boundsSize.y / 2, boundsCenter.y + boundsSize.y / 2),
            Random.Range(boundsCenter.z - boundsSize.z / 2, boundsCenter.z + boundsSize.z / 2)
        );
    }

    private void Update()
    {
        transform.position += moveDirection * flySpeed * Time.deltaTime;

        if (isRotating)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                isRotating = false;
            }
        }

        CheckBoundsAndReflect();
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        Vector3 reflection = Vector3.Reflect(moveDirection, contact.normal);
        
        reflection = (reflection + Random.insideUnitSphere * 0.1f).normalized;
        reflection.y *= 0.5f; // 保持較小的垂直移動
        moveDirection = reflection.normalized;

        targetRotation = GetRotationForDirection(moveDirection);
        isRotating = true;

        transform.position += contact.normal * 0.1f;
    }

    private Quaternion GetRotationForDirection(Vector3 direction)
    {
        return Quaternion.LookRotation(-direction);
    }

    private void CheckBoundsAndReflect()
    {
        Vector3 position = transform.position;
        bool needsReflection = false;
        Vector3 reflection = moveDirection;

        if (position.x < boundsCenter.x - boundsSize.x / 2 || position.x > boundsCenter.x + boundsSize.x / 2)
        {
            reflection.x = -moveDirection.x;
            needsReflection = true;

            position.x = Mathf.Clamp(position.x, 
                boundsCenter.x - boundsSize.x / 2, 
                boundsCenter.x + boundsSize.x / 2);
        }

        if (position.y < boundsCenter.y - boundsSize.y / 2 || position.y > boundsCenter.y + boundsSize.y / 2)
        {
            reflection.y = -moveDirection.y;
            needsReflection = true;
            position.y = Mathf.Clamp(position.y, 
                boundsCenter.y - boundsSize.y / 2, 
                boundsCenter.y + boundsSize.y / 2);
        }

        if (position.z < boundsCenter.z - boundsSize.z / 2 || position.z > boundsCenter.z + boundsSize.z / 2)
        {
            reflection.z = -moveDirection.z;
            needsReflection = true;
            position.z = Mathf.Clamp(position.z, 
                boundsCenter.z - boundsSize.z / 2, 
                boundsCenter.z + boundsSize.z / 2);
        }

        if (needsReflection)
        {
            transform.position = position;
            moveDirection = reflection.normalized;
            moveDirection = (moveDirection + Random.insideUnitSphere * 0.05f).normalized;
            moveDirection.y *= 0.5f;
            moveDirection = moveDirection.normalized;

            targetRotation = GetRotationForDirection(moveDirection);
            isRotating = true;
        }
    }

    public void Initialize(string emotion, string fortuneText, string filePath)
    {
        this.emotion = emotion;
        this.fortuneText = fortuneText;
        this.photoRootPath = filePath;
        
        craneMaterial = Resources.Load<Material>($"Crane Material/{emotion}");
        if (craneMaterial != null)
        {
            GetComponent<Renderer>().material = craneMaterial;
        }
        else
        {
            Debug.LogError($"找不到情緒 {emotion} 對應的材質");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }

    public void SendFortuneEmotionData()
    {
        GameObject effect = Instantiate(particleEffect, transform.position, transform.rotation);
        Destroy(gameObject);
        Debug.Log($"Sending Data to Game Manager");
        // Displaying on UI Components
        // displayText.text = $"Emotion: {emotion}\nFortune: {fortuneText}";
        // fortuneTextUI.text = $"Emotion: {emotion}\nFortune: {fortuneText}";

        // Sending Data to Game Manager
        GameManager.Instance.ReceiveFortuneEmotionData(new string[] { emotion, fortuneText }, this.transform, photoRootPath);

        // spawner.SpawnPhotoCard(this.transform.position, photoRootPath);

    }

    // 測試用
    private void OnMouseDown()
    {
        GameObject effect = Instantiate(particleEffect, transform.position, transform.rotation);
        Destroy(gameObject);
        GameManager.Instance.ReceiveFortuneEmotionData(new string[] { emotion, fortuneText }, this.transform, photoRootPath);
    }
}