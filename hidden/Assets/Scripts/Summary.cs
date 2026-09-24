using UnityEngine;
using UnityEngine.UI;

public class Summary : MonoBehaviour
{
    [SerializeField] Image honeyBar;
    [SerializeField] Animator anim;
    [SerializeField] TMPro.TMP_Text field;
    bool honeyOn;
    float from = 0;
    float to = 0;
    float value;
    float animDuration = 0.2f;

    void Start()
    {
        anim.gameObject.SetActive(false);
    }
    System.Action OnDone;   
    int totalLevels = 0;
    int levelID;
    GamesManager gamesManager;
    public void Init(System.Action OnDone)
    {    
        print("Summary init");

        gamesManager =   GetComponent<GamesManager>();
        totalLevels = gamesManager.levels.Count;
        levelID =gamesManager.levelID;

        if(levelID>0)
            from = (float)(levelID) / totalLevels;
        else 
            from = 0;

        value = from;

        to =  (float)(levelID+1) / totalLevels;

        if(gamesManager.state == GamesManager.states.calibrate)
            field.text = gamesManager.settings.calibration;
        else  if(gamesManager.levelID <1)
            field.text = gamesManager.settings.calibrationDone;
        else
            field.text = gamesManager.settings.summary_text();   
            
        this.OnDone = OnDone;  
        anim.gameObject.SetActive(true);
        anim.Play("entry");
        Invoke("Idle", 3.5f);
    }
    void Idle()
    {
        honeyOn = true;
        OnDone();
        OnDone = null;
    }
    public void Close()
    {        
        print("Summary Close");
        anim.gameObject.SetActive(true);

        if(levelID >= totalLevels-1)
        {
            anim.Play("exit_final");
            Invoke("Done", 4.5f);            
        }
        else
        {            
            Invoke("Done", 2);
            anim.Play("exit");
        }
    }
    void Done()
    {
        anim.gameObject.SetActive(false);
    }
    void Update()
    {
        if(!honeyOn) return;
        value +=  Time.deltaTime*animDuration;
        
        honeyBar.fillAmount = value;
        
        if(value>= to)
        {
            honeyOn = false;
            CancelInvoke();
            Invoke("SetClose", 1);
        }
    }
    void SetClose()
    {
        gamesManager.SummaryDone();
        Close();        
    }
}
