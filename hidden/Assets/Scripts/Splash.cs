using UnityEngine;

public class Splash : MonoBehaviour
{
     [SerializeField] Animator anim;
    float timer;
    System.Action OnDone;  
    bool isOn; 

    void Start()
    {
        anim.gameObject.SetActive(false);
    }
    public void Init(System.Action OnDone)
    {      
        timer = 0;
        this.OnDone = OnDone;  
        anim.gameObject.SetActive(true);
        anim.Play("entry");
    }
    public void Clicked()
    {
        OnDone();
        CancelInvoke();
    }   
    public void Close()
    {        
        CancelInvoke();
        anim.gameObject.SetActive(true);
        anim.Play("exit");
        Invoke("Done", 2);
    }
    public void Done()
    {
        anim.gameObject.SetActive(false);
    }
}
