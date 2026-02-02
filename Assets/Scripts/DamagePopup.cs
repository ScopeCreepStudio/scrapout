using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float fadeOutSpeed = 3f;
    [SerializeField] private float critScale = 1.25f;

    private float timer;
    private CanvasGroup canvasGroup;
    private bool isCrit;

    void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetText(float value)
    {
        if (text != null)
            text.text = value.ToString("F0");
    }

    public void SetGradient(VertexGradient gradient)
    {
        if (text == null) return;
        text.enableVertexGradient = true;
        text.colorGradient = gradient;
    }

    public void SetColor(Color color)
    {
        if (text == null) return;
        text.enableVertexGradient = false;
        text.color = color;
    }

    public void SetCrit(bool crit)
    {
        isCrit = crit;
        transform.localScale = Vector3.one * (isCrit ? critScale : 1f);
    }

    void OnEnable()
    {
        timer = 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (!isCrit)
            transform.localScale = Vector3.one;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Move up in world space
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        // Face the camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        if (canvasGroup != null && timer > lifetime)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, fadeOutSpeed * Time.deltaTime);
        }

        if (timer > lifetime + 0.6f)
            Destroy(gameObject);
    }
}
