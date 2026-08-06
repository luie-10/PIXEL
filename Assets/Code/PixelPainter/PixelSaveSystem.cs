using System.IO;
using UnityEngine;

public static class PixelSaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "player_pixel_art.json");

    public static void SavePixelArt(PixelArtData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[픽셀 저장 완료] 경로: {SavePath}");
    }

    public static PixelArtData LoadPixelArt()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<PixelArtData>(json);
        }
        Debug.LogWarning("저장된 픽셀 데이터 파일이 없습니다.");
        return null;
    }
}