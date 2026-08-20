using UnityEngine;
public class EndCutscene : MonoBehaviour
{
    public GamesManager gamesManager;
    RectTransform canvasRect;
    public Canvas canvas;    
    RectTransform gazeCursor;
    TobiiEyeTracker eyeTracker;
    public GameObject eyesGO;

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

        eyesGO.transform.localPosition = worldPos;
    }
    void UpdateEyes()
    {
        
        if (eyeTracker == null || !eyeTracker.IsConnected) 
        {
            eyesGO.SetActive(false);return;
        }

        if (!eyeTracker.GazeValid)
        {
            eyesGO.SetActive(false);
            gazeCursor.gameObject.SetActive(false);
            return;
        }
        eyesGO.SetActive(true);
        gazeCursor.gameObject.SetActive(true);
        gazeCursor.anchoredPosition = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, eyeTracker.GazeNormalized);
        
        Camera cam = Camera.main;
        Vector3 worldPos = cam.ScreenToWorldPoint(gazeCursor.position);
        worldPos.z = 0f;

        eyesGO.transform.localPosition = worldPos;
    }
}
