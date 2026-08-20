using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minijuego para validar la calibración: mirá fijo la pelota roja hasta que se pone
/// verde y salta a otra posición. También muestra un cursor celeste con la mirada cruda
/// para que se vea a simple vista qué tan preciso está el tracking.
/// </summary>
public class GazeBallGame : MonoBehaviour
{
    [Header("Pelota")]
    [SerializeField] float ballSize = 100f;
    [SerializeField] float hitRadiusNormalized = 0.09f;
    [SerializeField] float dwellTimeToScore = 0.5f;
    [SerializeField] Vector2 marginMin = new Vector2(0.1f, 0.12f);
    [SerializeField] Vector2 marginMax = new Vector2(0.9f, 0.88f);
    [SerializeField] float minJumpDistance = 0.3f;

    static readonly Color BallIdleColor = new Color(0.95f, 0.25f, 0.25f);
    static readonly Color BallHitColor = new Color(0.3f, 1f, 0.4f);

    RectTransform canvasRect;
    RectTransform ball;
    Image ballImage;
    RectTransform gazeCursor;
    Image gazeCursorImage;
    Text scoreText;

    Vector2 ballNormPos;
    float dwellTimer;
    int score;
    bool busy;

    TobiiEyeTracker eyeTracker;

    void Start()
    {
        eyeTracker = TobiiEyeTracker.EnsureExists();
        BuildUI();
        SpawnBall(new Vector2(0.5f, 0.5f));
    }

    void BuildUI()
    {
        TobiiUIUtil.CreateFullscreenCanvas("GameCanvas", out canvasRect);

        var scoreGO = new GameObject("ScoreText", typeof(Text));
        scoreGO.transform.SetParent(canvasRect, false);
        scoreText = scoreGO.GetComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.alignment = TextAnchor.UpperLeft;
        scoreText.fontSize = 28;
        scoreText.color = Color.white;
        var scoreRect = scoreGO.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(24f, -24f);
        scoreRect.sizeDelta = new Vector2(300f, 50f);
        UpdateScoreText();

        var ballGO = new GameObject("Ball", typeof(Image));
        ballGO.transform.SetParent(canvasRect, false);
        ballImage = ballGO.GetComponent<Image>();
        ballImage.sprite = TobiiUIUtil.MakeCircleSprite();
        ballImage.color = BallIdleColor;
        ball = ballGO.GetComponent<RectTransform>();
        ball.anchorMin = new Vector2(0f, 1f);
        ball.anchorMax = new Vector2(0f, 1f);
        ball.pivot = new Vector2(0.5f, 0.5f);
        ball.sizeDelta = new Vector2(ballSize, ballSize);

        var cursorGO = new GameObject("GazeCursor", typeof(Image));
        cursorGO.transform.SetParent(canvasRect, false);
        gazeCursorImage = cursorGO.GetComponent<Image>();
        gazeCursorImage.sprite = TobiiUIUtil.MakeCircleSprite();
        gazeCursorImage.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        gazeCursor = cursorGO.GetComponent<RectTransform>();
        gazeCursor.anchorMin = new Vector2(0f, 1f);
        gazeCursor.anchorMax = new Vector2(0f, 1f);
        gazeCursor.pivot = new Vector2(0.5f, 0.5f);
        gazeCursor.sizeDelta = new Vector2(18f, 18f);
        gazeCursor.gameObject.SetActive(false);
    }

    void Update()
    {
        if (eyeTracker == null || !eyeTracker.IsConnected) return;

        if (!eyeTracker.GazeValid)
        {
            gazeCursor.gameObject.SetActive(false);
            dwellTimer = 0f;
            return;
        }

        gazeCursor.gameObject.SetActive(true);
        gazeCursor.anchoredPosition = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, eyeTracker.GazeNormalized);

        if (busy) return;

        float dist = Vector2.Distance(eyeTracker.GazeNormalized, ballNormPos);
        if (dist <= hitRadiusNormalized)
        {
            dwellTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(dwellTimer / dwellTimeToScore);
            ballImage.color = Color.Lerp(BallIdleColor, BallHitColor, progress);
            ball.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, progress);

            if (dwellTimer >= dwellTimeToScore)
                StartCoroutine(PopAndRespawn());
        }
        else
        {
            dwellTimer = 0f;
            ballImage.color = BallIdleColor;
            ball.localScale = Vector3.one;
        }
    }

    IEnumerator PopAndRespawn()
    {
        busy = true;
        dwellTimer = 0f;
        score++;
        UpdateScoreText();

        const float popTime = 0.15f;
        float t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            ball.localScale = Vector3.one * Mathf.Lerp(1.25f, 0f, t / popTime);
            yield return null;
        }

        Vector2 next;
        int guard = 0;
        do
        {
            next = new Vector2(Random.Range(marginMin.x, marginMax.x), Random.Range(marginMin.y, marginMax.y));
            guard++;
        } while (Vector2.Distance(next, ballNormPos) < minJumpDistance && guard < 20);

        SpawnBall(next);

        t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            ball.localScale = Vector3.one * Mathf.Lerp(0f, 1f, t / popTime);
            yield return null;
        }
        ball.localScale = Vector3.one;
        busy = false;
    }

    void SpawnBall(Vector2 norm)
    {
        ballNormPos = norm;
        ballImage.color = BallIdleColor;
        ball.anchoredPosition = TobiiUIUtil.NormalizedToAnchoredTopLeft(canvasRect, ballNormPos);
    }

    void UpdateScoreText()
    {
        scoreText.text = $"Puntos: {score}";
    }
}
