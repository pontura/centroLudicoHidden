using UnityEngine;

public class Summary : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] TMPro.TMP_Text field;

    void Start()
    {
        anim.gameObject.SetActive(false);
    }
    System.Action OnDone;   
    public void Init(System.Action OnDone)
    {      
        if(GetComponent<GamesManager>().state == GamesManager.states.calibrate)
            field.text = GetComponent<GamesManager>().settings.calibration;
        else  if(GetComponent<GamesManager>().levelID <1)
            field.text = GetComponent<GamesManager>().settings.calibrationDone;
        else
            field.text = GetComponent<GamesManager>().settings.summary_text();   
            
        this.OnDone = OnDone;  
        anim.gameObject.SetActive(true);
        anim.Play("entry");
        Invoke("Idle", 1);
    }
    void Idle()
    {
        OnDone();
        OnDone = null;
    }
    public void Close()
    {        
        anim.gameObject.SetActive(true);
        anim.Play("exit");
        Invoke("Done", 2);
    }
    void Done()
    {
        anim.gameObject.SetActive(false);
    }
}
