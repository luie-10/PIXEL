using UnityEngine;

public class PlayerPixelArtSpawner : MonoBehaviour
{
    [Header("Sprite Display")]
    public SpriteRenderer spriteRenderer;

    [Header("Tile Colors")]
    public Color blackColor = Color.black;
    public Color redColor = Color.red;
    public Color blueColor = Color.blue;
    public Color yellowColor = Color.yellow;
    public Color greenColor = Color.green;

    private void Start()
    {
        BuildAndApplySprite();
    }

    public void BuildAndApplySprite()
    {
        // 1. 저장된 JSON 데이터 불러오기
        PixelArtData loadedData = PixelSaveSystem.LoadPixelArt();
        if (loadedData == null || loadedData.tiles.Count == 0) return;

        // 2. 픽셀 범위(크기) 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var tile in loadedData.tiles)
        {
            if (tile.x < minX) minX = tile.x;
            if (tile.x > maxX) maxX = tile.x;
            if (tile.y < minY) minY = tile.y;
            if (tile.y > maxY) maxY = tile.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // 3. 텍스처(이미지) 생성 (도트가 안 깨지게 FilterMode.Point 설정)
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        // 투명 배경 초기화
        Color[] clearColors = new Color[width * height];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
        texture.SetPixels(clearColors);

        // 4. 좌표에 맞춰 색상 픽셀 찍기
        foreach (var tile in loadedData.tiles)
        {
            int texX = tile.x - minX;
            int texY = tile.y - minY;
            texture.SetPixel(texX, texY, GetColorByEnum(tile.color));
        }

        texture.Apply();

        // 5. 하나의 Sprite로 완성해서 SpriteRenderer에 연결
        Rect rect = new Rect(0, 0, width, height);
        Vector2 pivot = new Vector2(0.5f, 0.5f); // 중심점(피벗) 설정
        Sprite generatedSprite = Sprite.Create(texture, rect, pivot, 16f); // 16 = 픽셀 크기

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sprite = generatedSprite;
    }

    private Color GetColorByEnum(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red: return redColor;
            case PixelColor.Blue: return blueColor;
            case PixelColor.Yellow: return yellowColor;
            case PixelColor.Green: return greenColor;
            default: return blackColor;
        }
    }
}