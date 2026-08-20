using UnityEngine;
using UnityEngine.UI;

/// <summary>Helpers de UI compartidos por las pantallas de calibración y juego basadas en gaze.</summary>
public static class TobiiUIUtil
{
    public static Canvas CreateFullscreenCanvas(string name, out RectTransform canvasRect)
    {
        var canvasGO = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasRect = canvasGO.GetComponent<RectTransform>();
        return canvas;
    }

    public static Sprite MakeCircleSprite(int size = 64)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(size / 2f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha > 0.5f ? 1f : alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Convierte una posición normalizada (0..1, origen arriba-izquierda) a anchoredPosition
    /// de un RectTransform hijo con anchor/pivot (0,1) del canvas dado.</summary>
    public static Vector2 NormalizedToAnchoredTopLeft(RectTransform canvasRect, Vector2 norm)
    {
        var size = canvasRect.rect.size;
        return new Vector2(norm.x * size.x, -norm.y * size.y);
    }
}
