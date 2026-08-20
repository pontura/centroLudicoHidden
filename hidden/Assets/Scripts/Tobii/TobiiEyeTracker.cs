using System;
using System.IO;
using UnityEngine;
using Tobii.StreamEngine;

/// <summary>
/// Singleton que conecta con el Tobii Eye Tracker 5 vía Tobii.StreamEngine.Net.dll y
/// expone el punto de mirada en coordenadas normalizadas de pantalla (0,0 = top-left,
/// igual que la convención del stream engine).
///
/// La calibración NATIVA del dispositivo (tobii_calibration_start/collect/compute_and_apply)
/// devuelve TOBII_ERROR_INSUFFICIENT_LICENSE sin una licencia de partner de Tobii: leer el
/// gaze ya calibrado es gratis, pero escribir una calibración nueva en el hardware no.
/// Por eso la calibración se hace acá por software: se ajusta una corrección lineal
/// (corregido = a*crudo + b, por eje) sobre el gaze crudo y se aplica en <see cref="GazeNormalized"/>.
/// </summary>
public class TobiiEyeTracker : MonoBehaviour
{
    public static TobiiEyeTracker Instance { get; private set; }

    public bool IsConnected { get; private set; }
    public string DeviceUrl { get; private set; }

    /// <summary>Punto de mirada crudo tal cual lo entrega el dispositivo (sin corrección de software).</summary>
    public Vector2 RawGazeNormalized { get; private set; }
    public bool RawGazeValid { get; private set; }

    /// <summary>Punto de mirada con la corrección de calibración por software ya aplicada.</summary>
    public Vector2 GazeNormalized { get; private set; }
    public bool GazeValid { get; private set; }
    public double LastGazeTimestamp { get; private set; }

    public bool HasCalibration { get; private set; }

    IEyeTracker tracker;
    IntPtr deviceContext;

    readonly object gazeLock = new object();
    Vector2 pendingGaze;
    bool pendingValid;
    long pendingTimestampUs;
    bool hasPendingSample;

    float calAx = 1f, calBx = 0f, calAy = 1f, calBy = 0f;

    /// <summary>Crea la instancia automáticamente si todavía no existe ninguna en la escena.</summary>
    public static TobiiEyeTracker EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("TobiiEyeTracker");
        return go.AddComponent<TobiiEyeTracker>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Connect();
    }

    void Connect()
    {
        try
        {
            EyeTrackerFactory.Init(null);
            var urls = EyeTrackerFactory.ListEyeTrackers();
            if (urls == null || urls.Count == 0)
            {
                Debug.LogWarning("[Tobii] No se encontró ningún eye tracker conectado.");
                return;
            }

            DeviceUrl = urls[0];
            tracker = EyeTrackerFactory.Create(DeviceUrl, null, null);
            deviceContext = tracker.Context;

            tracker.GazePoint += OnGazePoint;
            tracker.StartProcessingCallbacks();

            IsConnected = true;
            Debug.Log("[Tobii] Conectado a " + DeviceUrl);
        }
        catch (Exception e)
        {
            Debug.LogError("[Tobii] Error al conectar: " + e);
        }
    }

    void OnGazePoint(ref tobii_gaze_point_t gazePoint, IntPtr userData)
    {
        bool valid = gazePoint.validity == tobii_validity_t.TOBII_VALIDITY_VALID;
        lock (gazeLock)
        {
            pendingValid = valid;
            hasPendingSample = true;
            pendingTimestampUs = gazePoint.timestamp_us;
            if (valid)
                pendingGaze = new Vector2(gazePoint.position.x, gazePoint.position.y);
        }
    }

    void Update()
    {
        lock (gazeLock)
        {
            if (!hasPendingSample) return;
            RawGazeValid = pendingValid;
            GazeValid = pendingValid;
            if (pendingValid)
            {
                RawGazeNormalized = pendingGaze;
                GazeNormalized = new Vector2(
                    calAx * pendingGaze.x + calBx,
                    calAy * pendingGaze.y + calBy);
            }
            LastGazeTimestamp = pendingTimestampUs / 1000000.0;
            hasPendingSample = false;
        }
    }

    // ── Calibración por software (corrección lineal por eje) ──────────────

    public void ApplySoftwareCalibration(float ax, float bx, float ay, float by)
    {
        calAx = ax; calBx = bx; calAy = ay; calBy = by;
        HasCalibration = true;
    }

    public void ClearSoftwareCalibration()
    {
        calAx = 1f; calBx = 0f; calAy = 1f; calBy = 0f;
        HasCalibration = false;
    }

    // ── Persistencia de calibración entre sesiones ────────────────────────

    public static string DefaultCalibrationPath =>
        Path.Combine(Application.persistentDataPath, "tobii_calibration.json");

    public bool HasSavedCalibration => File.Exists(DefaultCalibrationPath);

    [Serializable]
    struct CalibrationData
    {
        public float ax, bx, ay, by;
    }

    public bool SaveCalibrationToDisk()
    {
        if (!HasCalibration) return false;

        var data = new CalibrationData { ax = calAx, bx = calBx, ay = calAy, by = calBy };
        File.WriteAllText(DefaultCalibrationPath, JsonUtility.ToJson(data));
        Debug.Log("[Tobii] Calibración guardada en " + DefaultCalibrationPath);
        return true;
    }

    public bool LoadCalibrationFromDisk()
    {
        if (!File.Exists(DefaultCalibrationPath)) return false;

        try
        {
            var data = JsonUtility.FromJson<CalibrationData>(File.ReadAllText(DefaultCalibrationPath));
            ApplySoftwareCalibration(data.ax, data.bx, data.ay, data.by);
            Debug.Log("[Tobii] Calibración cargada desde " + DefaultCalibrationPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Tobii] No se pudo leer la calibración guardada: " + e);
            return false;
        }
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        if (tracker != null)
        {
            try
            {
                tracker.StopProcessingCallbacks();
                tracker.GazePoint -= OnGazePoint;
                tracker.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Tobii] Error al desconectar: " + e);
            }
        }

        if (IsConnected)
            EyeTrackerFactory.Terminate();

        Instance = null;
    }
}
