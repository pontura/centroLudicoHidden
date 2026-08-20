using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    Camera worldCamera;       // la cámara que ve el objeto 3D (ej. Camera.main)
    RectTransform canvasRect; // RectTransform del Canvas (o panel padre)
    Canvas canvas;
    Animation anim;

    [SerializeField] public Image sprite;
    Transform target;
    
    private float fillMin = 0.16f;
    private float fillMax = 0.84f;
    bool isOn;
    public void Init(Transform target, Canvas canvas)
    {
        anim = GetComponent<Animation>();
        this.canvas = canvas;
        worldCamera = Camera.main;
        canvasRect = canvas.GetComponent<RectTransform>();

        this.target = target;
        
        transform.localScale = Vector2.one;
        SetBarValue(0);

        gameObject.SetActive(false);
    }
    public void SetOn()
    {
        isOn = true;
        anim.Play("on");
        gameObject.SetActive(true);
    }
    public bool OnUpdate(float value)
    {        
        SetBarValue(value);

        if (value >= 1)
            Close();            
        else
            SetPos(target.transform.position);

        return value>=1;
    }
    public void Close()
    {        
        anim.Play("off");
        Invoke("Reset", 0.25f);
    }   
    void Reset()
    {
        if(isOn)
            gameObject.SetActive(false);
        isOn = false;
    }  
    void SetPos(Vector2 pos)
    {
        // 1. World space -> Screen space
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(pos);
        // 2. Screen space -> Canvas local space
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCam, out Vector2 localPoint))
        {
            GetComponent<RectTransform>().anchoredPosition = localPoint;
        }
    }
    
    void SetBarValue(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        sprite.fillAmount = Mathf.Lerp(fillMin, fillMax, normalizedValue);
    }
}
