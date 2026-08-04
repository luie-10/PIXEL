using UnityEngine;
using UnityEngine.Tilemaps;

public class CheckerboardGrid : MonoBehaviour
{
    [Header("Tilemap Reference")]
    [SerializeField] private Tilemap backgroundTilemap; // 배경용 타일맵

    [Header("Tile Assets")]
    [SerializeField] private TileBase tileA; // 체커보드 색상 A (예: 밝은 회색)
    [SerializeField] private TileBase tileB; // 체커보드 색상 B (예: 어두운 회색)

    [Header("Grid Size")]
    [SerializeField] private int width = 16;  // 가로 칸 수
    [SerializeField] private int height = 16; // 세로 칸 수

    private void Start()
    {
        GenerateCheckerboard();
    }

    /// <summary>
    /// 설정된 너비와 높이에 따라 체커보드 배경을 생성합니다.
    /// </summary>
    public void GenerateCheckerboard()
    {
        if (backgroundTilemap == null || tileA == null || tileB == null)
        {
            Debug.LogWarning("[CheckerboardGrid] Tilemap 또는 Tile 에셋이 연결되지 않았습니다!");
            return;
        }

        // 기존에 그려진 타일 초기화
        backgroundTilemap.ClearAllTiles();

        // 중앙 정렬을 위해 시작 좌표 계산 (-Width/2 ~ Width/2)
        int startX = -width / 2;
        int startY = -height / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // (x + y)가 짝수냐 홀수냐에 따라 타일 교차 배치
                TileBase targetTile = ((x + y) % 2 == 0) ? tileA : tileB;

                Vector3Int tilePosition = new Vector3Int(startX + x, startY + y, 0);
                backgroundTilemap.SetTile(tilePosition, targetTile);
            }
        }
    }

    // 인스펙터에서 크기를 바꿨을 때 에디터 상에서 바로 확인하고 싶다면 호출 가능
    public void ClearGrid()
    {
        if (backgroundTilemap != null)
            backgroundTilemap.ClearAllTiles();
    }
}