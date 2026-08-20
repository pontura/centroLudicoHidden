using UnityEngine;

public class BaseGame : MonoBehaviour
{
    
    public GamesManager gamesManager;
    public RectTransform canvasRect;
    
    public Canvas canvas;    
    RectTransform gazeCursor;
    
    TobiiEyeTracker eyeTracker;

    states state;
    public enum states
    {
        playing,
        done
    }
    public virtual void OnStart(GamesManager gamesManager)
    {
        canvas = gamesManager.canvas;
        gazeCursor =gamesManager.gazeCursor;

         eyeTracker = TobiiEyeTracker.EnsureExists();  
        canvasRect = canvas.GetComponent<RectTransform>();
         if(gamesManager.useEyes)
         {
            gazeCursor.anchorMin = new Vector2(0f, 1f);
            gazeCursor.anchorMax = new Vector2(0f, 1f);
            gazeCursor.pivot = new Vector2(0.5f, 0.5f);
            gazeCursor.gameObject.SetActive(false);
         }
        this.gamesManager = gamesManager;
    }

    void Update()
    {
        if(gamesManager.useEyes)
            UpdateEyes();
        else
            UpdateMouse();      
    }
    void UpdateMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;

        mouseScreenPos.z = 0; // distancia desde la cámara al plano

        Vector2 localPoint;
        Camera cam2 = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, cam2, out localPoint))
        {
            gazeCursor.anchoredPosition = localPoint;
        }

        Camera cam = Camera.main;

        mouseScreenPos.z = Mathf.Abs(cam.transform.position.z); 

        Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        worldPos.z = 0f;

        OnUpdate(worldPos);

    }
    void UpdateEyes()
    {
        if (eyeTracker == null || !eyeTracker.IsConnected) return;

        if (!eyeTracker.GazeValid)
        {
            gazeCursor.gameObject.SetActive(false);
            return;
        }
        gazeCursor.gameObject.SetActive(true);
        gazeCursor.anchoredPosition = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, eyeTracker.GazeNormalized);
        
        Camera cam = Camera.main;
        Vector3 worldPos = cam.ScreenToWorldPoint(gazeCursor.position);
        worldPos.z = 0f;

        OnUpdate(worldPos);
    }
    
    public virtual void OnUpdate(Vector2 eyesPos) {}

    public void OnGameOver()
    {
        AudioManager.Instance.sfxManager.PlayLoop("abejas", 1f);
        AudioManager.Instance.sfx.audioMixer.SetFloat("sfxVol", 12f);
        state = states.done;
        Invoke("OnGameOverDone", gamesManager.settings.gameOver_duration);
    }
    void OnGameOverDone()
    {
        AudioManager.Instance.sfxManager.StopLoop();
        AudioManager.Instance.sfx.audioMixer.SetFloat("sfxVol", 0f);
        AudioManager.Instance.uiSfxManager.Play("summary",0.5f);
        gamesManager.OnSummary();
    }

}
