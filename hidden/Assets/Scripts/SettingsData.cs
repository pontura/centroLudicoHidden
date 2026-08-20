using System;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public float gameOver_duration;
    public string calibration;
    public string calibrationDone;
    public string calibration_done;
    public string[] summary_texts;

    public int[] hitRadiusPerLevel;
    public int[] closeBackdurationPerLevel;
    public int closeBackdurationSum;
    public float timeToOpenPerLevelSubstract;
    public float timeToOpenPerLevelMin;
    public int[] timeToOpenPerLevel;
    public float timeToRestartFromEnding;

    public string summary_text()
    {
        return summary_texts[UnityEngine.Random.Range(0, summary_texts.Length)];
    }

}
