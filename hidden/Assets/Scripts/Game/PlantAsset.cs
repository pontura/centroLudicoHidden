using System.Collections.Generic;
using UnityEngine;

public class PlantAsset : MonoBehaviour
{
    public List<GameObject> bones;
    float _x = 0;
    [SerializeField] float gotoForce = 0;
    [SerializeField] float force = 0;
    [SerializeField] float curvature = 2;
    [SerializeField] float forceMovement = 0.01f;
    [SerializeField] float firstRotationValue = 2;
    void Start()
    {
        _x = transform.position.x;
        transform.localEulerAngles = new Vector3(0,0,90);
        bones = new List<GameObject>();
        Transform[] all = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform go in all)
        {
            if (go.name.Contains("bone"))
            {
                bones.Add(go.gameObject);
            }
        }
    }
    public void SetForce(float gotoForce)
    {
        if(gotoForce>0) gotoForce = 1-gotoForce;
        else gotoForce = (gotoForce+1)*-1;
        this.gotoForce = gotoForce;
    }
    void Update()
    {
         int id = 0;
         force = Mathf.Lerp(force, gotoForce, Time.deltaTime*4);

         foreach(GameObject go in bones)
         {
            float a = 0;
            if(id==0)
                a = force*(curvature*firstRotationValue);
            else 
                a = id*force*curvature;

             go.transform.localEulerAngles = new Vector3(0,0,a);
             id++;
         }
          Vector3 pos = bones[0].transform.position;
          pos.x = _x +(force * forceMovement);  
          bones[0].transform.position = pos;
    }
}
