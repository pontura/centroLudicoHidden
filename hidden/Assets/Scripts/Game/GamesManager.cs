using System.Collections;
using System.IO;
using UnityEngine;

public class GamesManager : MonoBehaviour
{

    TobiiEyeTracker eyeTracker;
    public GameObject outOfView;
    public EndCutscene endCutscene;
    public bool useEyes;
    public SettingsData settings;
    [SerializeField] BaseGame game;
    [SerializeField] FlowersGame[] levels;
    [SerializeField] Splash splash;
    [SerializeField] Summary summary;
    public CalibrationManager calibrationManager;
    public Transform gameCanvasContainer;    
    public Canvas canvas;    
    public RectTransform gazeCursor;
    public states state;
    public int levelID;
    public enum states
    {
        splash,
        game,
        summary,
        calibrate,
        endCutscene
    }

    void Start()
    {
        eyeTracker = TobiiEyeTracker.EnsureExists();  
        gameCanvasContainer.gameObject.SetActive(false);
        StartCoroutine(LoadSettings());
    }
    IEnumerator LoadSettings()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "settings.json");

        string json = "";
        if (path.Contains("://"))
        {
            using (WWW www = new WWW(path))
            {
                yield return www;
                json = www.text;
            }
        }
        else if (File.Exists(path))
        {
            json = File.ReadAllText(path);
        }
        else
        {
            Debug.LogWarning("Settings file not found at " + path);
        }

        if (json != "")
        {
            settings = JsonUtility.FromJson<SettingsData>(json);
            Debug.Log("Settings loaded: " + json);
            Invoke("StartDelayed", 0.5f);
        }
    }
    void StartDelayed()
    {
        OnSplash();
    }
    [SerializeField] bool transitioning;
    public void OnSplash()
    {
        StopAllCoroutines();
        state = states.splash;
        splash.Init(OnInitGame);
        StartCoroutine(OnSplashC());
        AudioManager.Instance.ambienceManager.PlayLoop("amb1");
        AudioManager.Instance.sfxManager.Play("intro",0.5f, ()=> {
            AudioManager.Instance.sfxManager.PlayLoop("abejas", 1f);
            AudioManager.Instance.sfx.audioMixer.SetFloat("sfxVol", 12f);
        });
    }
    IEnumerator OnSplashC()
    {
        yield return new WaitForSeconds(1);
        DestroyAllGames();
        yield return new WaitForSeconds(1);
        transitioning = false;
    }
    void DestroyAllGames()
    {        
        if(game != null)
        {
            Destroy(game.gameObject);
            Utils.RemoveAllChildsIn(gameCanvasContainer);
        }
        if(_endCutscene != null)
        {
            Destroy(_endCutscene.gameObject);            
        }
    }
    void OnInitGame()
    {        
        CancelInvoke();
        //InitEndCutscene();
        InitNewGame();
        Invoke("OnInitGameDelayed", 0.5f);
    }
    void InitNewGame()
    {
        AudioManager.Instance.sfxManager.CancelAllOnClipDone();
        AudioManager.Instance.sfxManager.StopLoop();
        AudioManager.Instance.sfx.audioMixer.SetFloat("sfxVol", 0f);
        AudioManager.Instance.sfxManager.Play("outro", 0.5f);
        AudioManager.Instance.uiSfxManager.Play("click_1");
        game = Instantiate(levels[levelID]);
        game.OnStart(this);
        state = states.game;
    }
    EndCutscene _endCutscene;
    void InitEndCutscene()
    {
        AudioManager.Instance.sfxManager.CancelAllOnClipDone();
        AudioManager.Instance.sfxManager.StopLoop();
        AudioManager.Instance.sfx.audioMixer.SetFloat("sfxVol", 0f);
        AudioManager.Instance.sfxManager.Play("outro", 0.5f);
        AudioManager.Instance.uiSfxManager.Play("click_1");
        _endCutscene = Instantiate(endCutscene);
        _endCutscene.OnStart(this);
        _endCutscene.transform.localPosition = Vector3.zero;
        state = states.endCutscene;
    }
    void OnInitGameDelayed()
    {        
        CancelInvoke();
        splash.Close();
        Invoke("GameStarted", 1.5f);
    }
    void GameStarted()
    {
        transitioning = false;
        gameCanvasContainer.gameObject.SetActive(true);
    }

    public void OnSummary()
    {
        if(state == states.game)
        {
            state = states.summary;
            summary.Init(OnSummaryInit);
        }
    }
    void OnSummaryInit()
    {
        StartCoroutine(OnSummaryInitC());
    }
    IEnumerator OnSummaryInitC()
    {
        yield return new WaitForSeconds(1);
        DestroyAllGames();
        yield return new WaitForSeconds(1);        
        levelID++;

        if(levelID>levels.Length-1)
        {
            levelID = -1;
            InitEndCutscene();
        }
        else
            InitNewGame();

        gameCanvasContainer.gameObject.SetActive(false);
        yield return new WaitForSeconds(1);
        summary.Close();
        yield return new WaitForSeconds(1);
        if(levelID != -1)
            GameStarted();
    }
    float lastKeyDownTime;
    float delayToInteract = 3;
    void Update()
    {
        lastKeyDownTime += Time.deltaTime;
        if(lastKeyDownTime>delayToInteract)
        {
            if(Input.GetKeyDown(KeyCode.Space) && !transitioning)
            {   
                print("Space pressed");
                lastKeyDownTime = 0;
                switch(state)
                {
                    case states.splash:
                        transitioning = true;
                        splash.Clicked();
                        break;
                    case states.game:
                    case states.endCutscene:
                        levelID = 0;
                        transitioning = true;
                        OnSplash();
                        break;
                }
            }
            if(Input.GetKeyDown(KeyCode.C) && !transitioning)
            {
                print("C pressed");
                lastKeyDownTime = 0;
                if(state == states.splash)
                    Calibrate();
            }
        }
        UpdateEyes();
    }


    void UpdateEyes()
    {
        if (eyeTracker == null || !eyeTracker.IsConnected) return;

         outOfView.SetActive(!eyeTracker.GazeValid);
    }

    public void Calibrate()
    {
        if(state == states.calibrate) return;
        state = states.calibrate;
        print("Calibrate");
        summary.Init(OnCalibrateInit);
    }
    void OnCalibrateInit()
    {
        StartCoroutine(CalibrateInitC());        
    }
    IEnumerator CalibrateInitC()
    {
        yield return new WaitForSeconds(1);
        DestroyAllGames();
        yield return new WaitForSeconds(1);
        calibrationManager.Init();
        splash.Done();
        summary.Close();
    }
    public void CalibrationDone()
    {
        levelID = -1; // el summary le suma 1: 
        print("Calibration Done");
        state = states.summary;
        summary.Init(OnSummaryInit);
    }
}
