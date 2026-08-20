using UnityEngine;
using YaguarLib.Audio;

public class FlowerAsset : MonoBehaviour
{
    [HideInInspector] public ProgressBar progressBar;
    FlowersGame game;
    public Animator anim;
    public states state;
    public Animator growFX;
    [SerializeField] IngameAudio sfx;
    [SerializeField] IngameAudio uisfx;
    [SerializeField] float totalTime;
    float closeBackdurationPerLevel;
    float closeBackdurationSum;
    float timeToOpenPerLevelSubstract;
    float timeToOpenPerLevelMin;

    public enum states
    {
        idle,
        hit,
        done,
        reverse
    }
    [SerializeField] float _value;
    public float value {
        get
        {
             if(state == states.hit || state == states.reverse)
             {
                AnimatorStateInfo animState = anim.GetCurrentAnimatorStateInfo(0);
                if((state == states.reverse || state == states.hit) && animState.IsName("growght"))
                {         
                    return _value;
                }
                return 0;
             }
             else
              return 0;
        }
    }

    public void SetProgressBar(ProgressBar pb)
    {
        this.progressBar = pb;
    }
    public void Init(FlowersGame game, float totalTime, float timeToOpenPerLevelMin, float timeToOpenPerLevelSubstract)
    {
        closeBackdurationPerLevel = (float)(game.gamesManager.settings.closeBackdurationPerLevel[ game.gamesManager.levelID]);
        closeBackdurationSum = game.gamesManager.settings.closeBackdurationSum;
        this.timeToOpenPerLevelMin = timeToOpenPerLevelMin;
        this.timeToOpenPerLevelSubstract = timeToOpenPerLevelSubstract; 
        this.totalTime = totalTime;
        this.game = game;
    }
    public void OnDisable()
    {
        CancelInvoke();
    }
    public void Restart()
    {
        state = states.idle;
        Idle();
    }
    public void EyesOut()
    {
        if(state == states.done || state == states.reverse) return;
        progressBar.Close();
        Debug.Log("EyesOut");
        Reverse();
    }
    void Reverse()
    {
        if(state != states.hit)
            Trigger("hit");
        state = states.reverse;
        growFX.Play("decrease");
        anim.SetFloat("speed", -1);
        uisfx.Play("decrece");
    }
    public void OnDone()
    {
        state = states.done;
        uisfx.Stop();
        sfx.Stop();
        sfx.Play("flowerDone");
        Trigger("done");   
        growFX.Play("nule");   
        Invoke("OnReOpen", closeBackdurationPerLevel);
        closeBackdurationPerLevel += closeBackdurationSum;
    }
    void OnReOpen()
    {
        CancelInvoke();
        print("OnReOpen");
        _value = 0.95f;
        game.UnDone();
        state = states.reverse;     
        anim.Play("growght", 0,0.95f);
        anim.SetFloat("speed", -1); 
        growFX.Play("decrease");
        uisfx.Play("decrece");
    }
    void Idle()
    {
        state = states.idle;
        sfx.Stop();
        uisfx.Stop();
        anim.SetFloat("speed", 1);   
        Trigger("idle"); 
        growFX.Play("nule");      
    }
    public void OnHit()
    {
        if(state == states.hit || state == states.done) return;
          
        growFX.Play("increase");   
        print("OnHit " + state);

        uisfx.Stop();
        if (state != states.reverse)
            Trigger("hit");

        uisfx.Play("crece");
        sfx.PlayLoop("particulas");

        state = states.hit;
        anim.SetFloat("speed", 1);
    }
    float timer;
    void Update()
    {
        timer += Time.deltaTime;
        if(timer>1)
        {
            totalTime -= timeToOpenPerLevelSubstract;
            if(totalTime<timeToOpenPerLevelMin) totalTime = timeToOpenPerLevelMin;
            timer = 0;
        }
        AnimatorStateInfo animState = anim.GetCurrentAnimatorStateInfo(0);
        if(state == states.reverse && animState.IsName("growght"))
        {         
           // Debug.Log(animState.IsName("growght") + " | normalizedTime: " + animState.normalizedTime + " anim speed: " + anim.speed);
            
            if (animState.normalizedTime <= 0.05f)
            {
                Debug.Log("idle");
                 Idle();
             }
             _value -= Time.deltaTime / totalTime;
             if(_value<0)_value = 0;
             //_value = animState.normalizedTime;
        } else if(state == states.hit)
        {     
             _value += Time.deltaTime / totalTime;
             //_value = animState.normalizedTime;
        }
    }
    void Trigger(string s)
    {
        print("_____________" + s);
        anim.SetTrigger(s);
        
    }
}
