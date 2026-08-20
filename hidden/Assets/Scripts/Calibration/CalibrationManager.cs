using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class CalibrationManager : MonoBehaviour
{
    [SerializeField] Bee beeActor;
    [Header("Puntos de calibración (0..1, 0,0 = arriba-izquierda)")]
    [SerializeField]
    Vector2[] points =
    {
        new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.1f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.9f),
    };

    [Header("Timing")]
    [SerializeField] float travelTime = 0.5f;

    [Header("Fijación (el punto avanza cuando la mirada queda quieta)")]
    [SerializeField] float fixationRadius = 0.035f;
    [SerializeField] float fixationDuration = 2f;
    [SerializeField] float fixationTimeout = 8f;

    [SerializeField] GameObject bee_for_calibration;
    [SerializeField] RectTransform bee;
    [SerializeField] Canvas canvas;
    [SerializeField] TMPro.TMP_Text statusText;
    [SerializeField] RectTransform canvasRect;
    RectTransform dot;

    TobiiEyeTracker eyeTracker;

    void Start()
    {
        bee_for_calibration.SetActive(false);
        StopAllCoroutines();
        eyeTracker = TobiiEyeTracker.EnsureExists();
        BuildUI();
        StartCoroutine(RunFlow());
        canvasRect.gameObject.SetActive(false);
    }

    public void Init()
    {
        StopAllCoroutines();
        canvasRect.gameObject.SetActive(true);
       // dot.gameObject.SetActive(false);
        StartCoroutine(RunCalibration());
    }

    void BuildUI()
    {
        // var bgGO = new GameObject("Background", typeof(Image));
        // bgGO.transform.SetParent(canvasRect, false);
        // var bgRect = bgGO.GetComponent<RectTransform>();
        // bgRect.anchorMin = Vector2.zero;
        // bgRect.anchorMax = Vector2.one;
        // bgRect.offsetMin = Vector2.zero;
        // bgRect.offsetMax = Vector2.zero;
        // bgGO.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 1f);

        // var textGO = new GameObject("StatusText", typeof(Text));
        // textGO.transform.SetParent(canvasRect, false);
        // statusText = textGO.GetComponent<Text>();
        // statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // statusText.alignment = TextAnchor.UpperCenter;
        // statusText.fontSize = 32;
        // statusText.color = Color.white;
        // var textRect = textGO.GetComponent<RectTransform>();
        // textRect.anchorMin = new Vector2(0f, 1f);
        // textRect.anchorMax = new Vector2(1f, 1f);
        // textRect.pivot = new Vector2(0.5f, 1f);
        // textRect.anchoredPosition = new Vector2(0f, -40f);
        // textRect.sizeDelta = new Vector2(0f, 80f);

        var dotGO = new GameObject("CalibrationDot", typeof(Image));
        Color c = new Color(0,0,0,0);
        dotGO.GetComponent<Image>().color = c;
        dotGO.transform.SetParent(canvasRect, false);
     
        dot = dotGO.GetComponent<RectTransform>();
        dot.anchorMin = new Vector2(0f, 1f);
        dot.anchorMax = new Vector2(0f, 1f);
        dot.pivot = new Vector2(0.5f, 0.5f);
        dot.sizeDelta = new Vector2(40f, 40f);
        dotGO.SetActive(false);

        bee.anchoredPosition = dot.anchoredPosition;
    }

    IEnumerator RunFlow()
    {
        statusText.text = "Conectando con el Tobii...";
        float waitT = 0f;
        while (!eyeTracker.IsConnected && waitT < 5f)
        {
            waitT += Time.deltaTime;
            yield return null;
        }

        if (!eyeTracker.IsConnected)
        {
            statusText.text = "No se encontró el Tobii Eye Tracker 5. Conectalo y presioná R.";
            yield break;
        }

        if (eyeTracker.HasSavedCalibration && eyeTracker.LoadCalibrationFromDisk())
        {
            statusText.text = "Calibración cargada de una sesión anterior (presioná R para recalibrar)";
            yield return new WaitForSeconds(2f);
           // FinishAndStartGame();
            yield break;
        }

        yield return StartCoroutine(RunCalibration());
    }

    IEnumerator RunCalibration()
    {         
        dot.gameObject.SetActive(false);
        bee_for_calibration.SetActive(false);
        statusText.text = GetComponent<GamesManager>().settings.calibration;
        yield return new WaitForSeconds(1.5f);
        
        bee_for_calibration.SetActive(true);

        dot.gameObject.SetActive(true);

        var rawXs = new List<float>();
        var targetXs = new List<float>();
        var rawYs = new List<float>();
        var targetYs = new List<float>();

        Vector2 currentPos = points[0];
        dot.anchoredPosition = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, currentPos);
        bee.anchoredPosition = dot.anchoredPosition;

        foreach (var target in points)
        {
            yield return StartCoroutine(MoveDot(currentPos, target));
            currentPos = target;

            var samples = new List<Vector2>();
            yield return StartCoroutine(WaitForFixation(samples));

            if (samples.Count > 0)
            {
                Vector2 avg = Average(samples);
                rawXs.Add(avg.x); targetXs.Add(target.x);
                rawYs.Add(avg.y); targetYs.Add(target.y);
            }
        }
print("Raw Xs: " + string.Join(", ", rawXs));
print("Target Xs: " + string.Join(", ", targetXs));
print("Raw Ys: " + string.Join(", ", rawYs));       
        dot.gameObject.SetActive(false);
        statusText.text = "Calculando calibración...";

        bool okX = FitLinear(rawXs, targetXs, out float ax, out float bx);
        bool okY = FitLinear(rawYs, targetYs, out float ay, out float by);

        if (okX && okY)
        {
            eyeTracker.ApplySoftwareCalibration(ax, bx, ay, by);
            eyeTracker.SaveCalibrationToDisk();
            statusText.text = "¡Calibración lista!";
            yield return new WaitForSeconds(1.2f);
            FinishAndStartGame();
        }
        else
        {       
            statusText.text = "No se juntaron suficientes datos (¿parpadeos o mirada fuera de pantalla?). Presioná R para reintentar.";
        }
    }

    IEnumerator MoveDot(Vector2 fromNorm, Vector2 toNorm)
    {
        beeActor.Idle();
        float t = 0f;
        Vector2 fromPos = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, fromNorm);
        Vector2 toPos = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, toNorm);
        while (t < travelTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / travelTime));
            dot.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            bee.anchoredPosition =  dot.anchoredPosition ;
            yield return null;
        }
        dot.anchoredPosition = toPos;
        bee.anchoredPosition =  dot.anchoredPosition ;
    }

    /// <summary>
    /// Espera a que la mirada quede quieta (dentro de fixationRadius) durante fixationDuration
    /// antes de dejar avanzar al siguiente punto. Si la mirada se sale del área, el temporizador
    /// se reinicia. Si se supera fixationTimeout sin lograr una fijación estable, avanza igual
    /// con las muestras que haya podido juntar (para no bloquear la calibración).
    /// </summary>
    IEnumerator WaitForFixation(List<Vector2> samplesOut)
    {
        var cluster = new List<Vector2>();
        float stableTimer = 0f;
        float totalTimer = 0f;
        bool fixating = false;

        while (stableTimer < fixationDuration && totalTimer < fixationTimeout)
        {
            totalTimer += Time.deltaTime;

            bool movedAway = false;

            if (eyeTracker.RawGazeValid)
            {
                Vector2 gaze = eyeTracker.RawGazeNormalized;

                if (cluster.Count > 0 && Vector2.Distance(gaze, Average(cluster)) > fixationRadius)
                {
                    cluster.Clear();
                    stableTimer = 0f;
                    movedAway = true;
                }

                cluster.Add(gaze);
                stableTimer += Time.deltaTime;

            }
            else
            {
                if (cluster.Count > 0)
                {
                    cluster.Clear();
                    stableTimer = 0f;
                }
                movedAway = true;
            }

            if (movedAway && fixating)
            {
                beeActor.Idle();
                fixating = false;
            }
            else if (!movedAway && !fixating)
            {
                beeActor.Loading();
                fixating = true;
            }

            float progress = Mathf.Clamp01(stableTimer / fixationDuration);
            float shrink = Mathf.Lerp(40f, 14f, progress);
            dot.sizeDelta = new Vector2(shrink, shrink);

            yield return null;
        }

        dot.sizeDelta = new Vector2(40f, 40f);
        samplesOut.AddRange(cluster);
    }

    static Vector2 Average(List<Vector2> samples)
    {
        Vector2 sum = Vector2.zero;
        foreach (var s in samples) sum += s;
        return sum / samples.Count;
    }

    /// <summary>Ajuste lineal simple por mínimos cuadrados: target ≈ a*raw + b.</summary>
    static bool FitLinear(List<float> raw, List<float> target, out float a, out float b)
    {
        a = 1f; b = 0f;
        int n = raw.Count;
        if (n < 2) return false;

        double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += raw[i];
            sumY += target[i];
            sumXY += raw[i] * target[i];
            sumXX += raw[i] * raw[i];
        }

        double denom = n * sumXX - sumX * sumX;
        if (System.Math.Abs(denom) < 1e-6) return false;

        a = (float)((n * sumXY - sumX * sumY) / denom);
        b = (float)((sumY - a * sumX) / n);
        return true;
    }

    void FinishAndStartGame()
    {
        GetComponent<GamesManager>().CalibrationDone();        
        bee_for_calibration.SetActive(false);
        canvasRect.gameObject.SetActive(false);
    }
}
