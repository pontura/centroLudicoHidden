using System.Collections.Generic;
using UnityEngine;

public class FlowersGame : BaseGame
{
    [SerializeField] List<FlowerAsset> flowers;
    [SerializeField] List<PlantAsset> plants;
    public int hitRadiusNormalized;
    [SerializeField] ProgressBar progressBar;
    public List<ProgressBar> progressBars;
    float totalTime;
    float timeToOpenPerLevelMin;
    float timeToOpenPerLevelSubstract;
    GameOverMoment gameOverMoment;
    int totalDone;

    public override void OnStart(GamesManager gamesManager)
    { 
        base.OnStart(gamesManager);
        Invoke("Delayed", 2);
    }
    void Delayed()
    {
        if(gamesManager.state != GamesManager.states.game)   return;

        totalTime = gamesManager.settings.timeToOpenPerLevel[gamesManager.levelID];

        timeToOpenPerLevelMin = gamesManager.settings.timeToOpenPerLevelMin;
        timeToOpenPerLevelSubstract = gamesManager.settings.timeToOpenPerLevelSubstract;

        hitRadiusNormalized = gamesManager.settings.hitRadiusPerLevel[gamesManager.levelID];

        progressBars = new List<ProgressBar>();
        transform.GetComponentsInChildren<FlowerAsset>(true, flowers); 
        foreach (FlowerAsset f in flowers)
        {
            ProgressBar pb = Instantiate(progressBar, gamesManager.gameCanvasContainer);
            print(pb);
            progressBars.Add(pb);

            pb.Init(f.transform, canvas);
            f.Init(this, totalTime, timeToOpenPerLevelMin, timeToOpenPerLevelSubstract);
            f.SetProgressBar(pb);
        }
        gameOverMoment = GetComponent<GameOverMoment>();
    }
    // void Update()
    // {
    //     Vector3 mouseScreenPos = Input.mousePosition;
    //     mouseScreenPos.z = 0;
    //     Camera cam = Camera.main;
    //     mouseScreenPos.z = Mathf.Abs(cam.transform.position.z); 

    //     Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreenPos);
    //     worldPos.z = 0f;

    //     OnUpdate(worldPos);

    // }
    FlowerAsset lastaActive;
    float distance = 6;
    public override void OnUpdate(Vector2 eyesPos)
    {
        if(gameOverMoment != null)
            gameOverMoment.OnUpdate(eyesPos);
        foreach (PlantAsset p in plants)
        {
            float diff = eyesPos.x - p.transform.position.x;
            if(Mathf.Abs(diff)<distance)
            {
                p.SetForce(diff/distance);
            }
        }
        int id = 0;
        foreach (FlowerAsset f in flowers)
        {
            if(f.state != FlowerAsset.states.done)
            {
                Vector2 fPos = f.transform.position;
                float dist = Vector2.Distance(eyesPos, fPos);
                if (dist <= hitRadiusNormalized)
                {
                    lastaActive = f;
                    if(f.state == FlowerAsset.states.idle || f.state == FlowerAsset.states.reverse)
                    {                    
                        f.progressBar.SetOn();
                        f.OnHit();
                    }
                    else 
                    {
                        bool done = f.progressBar.OnUpdate(f.value);
                        if(done)
                        {
                            f.OnDone();
                            Done();
                        }
                    }
                }
                else
                {
                    if(f.state == FlowerAsset.states.hit)
                    {
                         f.EyesOut();  
                    }            
                }
                id++;
            }
        }
    }
    public void Done()
    {
        totalDone++;
        if(totalDone >= flowers.Count)
        {
             foreach (FlowerAsset f in flowers)
                f.OnDisable();
            gameOverMoment.Init(hitRadiusNormalized);
            OnGameOver();
        }
    }
    public void UnDone()
    {
        totalDone--;
        if(totalDone<0) totalDone = 0;
    }
}
