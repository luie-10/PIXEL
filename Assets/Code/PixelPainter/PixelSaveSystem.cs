using System;
using System.IO;
using UnityEngine;

public static class PixelSaveSystem
{
    private const string SaveFileName = "player_pixel_art.json";

    private static string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );

    /// <summary>
    /// 저장 파일 존재 여부 확인
    /// </summary>
    public static bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    /// <summary>
    /// ✨ 캐릭터 데이터(JSON 파일) 완전히 초기화/삭제
    /// </summary>
    public static void DeleteSaveData()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log($"[PixelSaveSystem] 기존 도트 JSON 파일 삭제 완료: {SavePath}");
            }
            else
            {
                Debug.Log("[PixelSaveSystem] 삭제할 기존 도트 JSON 파일이 없습니다.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[PixelSaveSystem] 파일 삭제 실패\n" +
                $"경로: {SavePath}\n" +
                $"{exception}"
            );
        }
    }

    public static void SavePixelArt(PixelArtData data)
    {
        if (data == null)
        {
            Debug.LogError(
                "[PixelSaveSystem] 저장할 PixelArtData가 null입니다."
            );

            return;
        }

        if (data.tiles == null)
        {
            Debug.LogError(
                "[PixelSaveSystem] 저장할 tiles 목록이 null입니다."
            );

            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(
                SavePath,
                json
            );

            Debug.Log(
                $"[PixelSaveSystem] 저장 완료\n" +
                $"경로: {SavePath}\n" +
                $"타일 개수: {data.tiles.Count}\n" +
                $"JSON 길이: {json.Length}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[PixelSaveSystem] 저장 실패\n" +
                $"경로: {SavePath}\n" +
                $"{exception}"
            );
        }
    }

    public static PixelArtData LoadPixelArt()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogError(
                $"[PixelSaveSystem] 저장 파일이 없습니다.\n" +
                $"확인 경로: {SavePath}"
            );

            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError(
                    $"[PixelSaveSystem] 저장 파일이 비어 있습니다.\n" +
                    $"경로: {SavePath}"
                );

                return null;
            }

            PixelArtData data =
                JsonUtility.FromJson<PixelArtData>(json);

            if (data == null)
            {
                Debug.LogError(
                    "[PixelSaveSystem] JSON 역직렬화 결과가 null입니다."
                );

                return null;
            }

            if (data.tiles == null)
            {
                data.tiles = new System.Collections.Generic.List<PixelTileData>();
            }

            Debug.Log(
                $"[PixelSaveSystem] 불러오기 완료\n" +
                $"경로: {SavePath}\n" +
                $"타일 개수: {data.tiles.Count}"
            );

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[PixelSaveSystem] 불러오기 실패\n" +
                $"경로: {SavePath}\n" +
                $"{exception}"
            );

            return null;
        }
    }
}