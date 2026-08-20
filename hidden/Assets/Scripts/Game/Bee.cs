using System.Collections;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class Bee : MonoBehaviour
{
    float _y = 2.5f;
    int direction;
    float initialSpeed = 2; 
    float speed; 
    float duration = 4f;

    public float y_1;
    public float y_2;
    public GameObject[] bodies;
    [SerializeField] bool dontMove;
    public Animator anim;

    float timer;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void Init(int direction)
    {
        foreach(GameObject go in bodies)
        {
            go.SetActive(false);
        }
        bodies[Random.Range(0,bodies.Length)].SetActive(true);
        y_1 = Random.Range(-_y,_y);
        y_2 = Random.Range(-_y,_y);
        this.direction = direction;
        Restart();
        if(direction==1)
            transform.localScale = new Vector3(-1,1,1);

        float _x = Random.Range(0,10 * direction);
        transform.position = new Vector3((10*direction ) + _x,y_1,Random.Range(-5,5));        
        timer = 0f;
    }
    void Restart()
    {         
        speed = Random.Range((float)initialSpeed-((float)initialSpeed/2),(float)initialSpeed+((float)initialSpeed/2));
    }
    void Update()
    {   
        if(dontMove) return;

        UpdateSeen();
        Vector3 pos = transform.position;
      
        if(direction == -1 && pos.x>10)
        {
            pos.x = -10 - Random.Range(0,2);
            Restart();
        }
        else   if(direction == 1 && pos.x<-10)
        {
            pos.x = 10 + Random.Range(0,2);            
            Restart();
        }

        pos.x += speed * Time.deltaTime * (float)-direction;


         timer += Time.deltaTime;

        float t = Mathf.PingPong(timer / duration, 1f);
        float curveValue = curve.Evaluate(t);

        pos.y = Mathf.LerpUnclamped(y_1, y_2, curveValue);


        transform.position = pos; // asegura que termine exacto

    }
    bool loading;
    public void Idle()
    {
        print("Idle");
        loading = false;
        anim.SetTrigger("idle");
    }
    public void Loading()
    {
        if(loading) return;
        loading = true;
        anim.SetTrigger("loading");
    }
    bool canBeeSeen;
    bool spotted;
    public void Spotted(bool isSeen)
    {
        if(isSeen && !spotted)
        {
            canBeeSeen = true;
            timerMove = 0;
            spotted = true;
            anim.SetTrigger("spotted");
        }
        else if(!isSeen && spotted && timerMove>2)
        {
            spotted = false;
        }
    }
    float timerMove;
    void UpdateSeen()
    {
        if(!canBeeSeen) return;
        timerMove += Time.deltaTime;
        if(timerMove>2 && !spotted)
        {
            canBeeSeen = false;
            anim.SetTrigger("idle");
        }
    }
}
