using System.Collections.Generic;
using UnityEngine;

public class GameOverMoment : MonoBehaviour
{
    public Bee abeja1;
    public Bee abeja2;
    public Transform container;
    List<Bee> all; 
    bool isOn;
    [SerializeField] float hitRadiusNormalized;

    void Start()
    {
    }
    public void Init(int hitRadiusNormalized)
    {
        this.hitRadiusNormalized = (float)hitRadiusNormalized/3;
        isOn = true;
        all = new List<Bee>();
        Utils.RemoveAllChildsIn(container);

        for(int a = 0; a<20; a++)
        {
            Bee bee;
            if(Random.Range(0,10)<5)
                bee = Instantiate(abeja1, container);
            else
                bee = Instantiate(abeja2, container);

            int direction = 0;
            if(Random.Range(0,10)<5)
                direction = 1;
            else
                direction = -1;
            bee.Init(direction);
            all.Add(bee);
        }
    }
    public void OnUpdate(Vector2 eyesPos)
    {
        if (!isOn) return;
        int id =0;
        foreach (Bee bee in all)
        {
            Vector2 fPos = bee.transform.position;
            float dist = Vector2.Distance(eyesPos, fPos);
            if (dist <= hitRadiusNormalized)
            {
                bee.Spotted(true);
            }
            else
            {
                bee.Spotted(false);        
            }
            id++;
        }
    }
}
