using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerPixelArtSpawner : MonoBehaviour
{
    public enum PivotPosition
    {
        Center,         // 중앙 정렬
        BottomCenter,   // 발밑(하단 중앙) 정렬
        TopLeft         // 좌상단 정렬
    }

    public enum ColorApplyMode
    {
        SpriteRendererColor,   // SpriteRenderer의 color 속성 직접 변경
        MaterialPropertyBlock  // 드로우콜 최적화를 위한 MaterialPropertyBlock 사용
    }

    [Header("Prefab & Parent Settings")]
    [Tooltip("배치할 도트 오브젝트 프리팹입니다. (SpriteRenderer가 붙어있어야 합니다)")]
    [SerializeField] private GameObject tilePrefab;

    [Tooltip("생성된 도트 타일들이 배치될 부모 Transform입니다. 비어있으면 현재 오브젝트가 부모가 됩니다.")]
    [SerializeField] private Transform tilesParent;

    [Header("Transform & Layout Settings")]
    [Tooltip("각 타일 오브젝트의 기본 크기입니다.")]
    [SerializeField] private Vector2 tileSize = Vector2.one;

    [Tooltip("타일과 타일 사이의 간격입니다.")]
    [SerializeField] private Vector2 tileSpacing = Vector2.zero;

    [Tooltip("전체 픽셀아트의 기준점(Pivot) 위치입니다.")]
    [SerializeField] private PivotPosition pivotPosition = PivotPosition.Center;

    [Tooltip("체크하면 저장된 그림의 위아래가 반전됩니다.")]
    [SerializeField] private bool flipY;

    [Header("Color Settings")]
    [SerializeField] private ColorApplyMode colorMode = ColorApplyMode.SpriteRendererColor;
    [SerializeField] private Color blackColor = Color.black;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color greenColor = Color.green;

    // 생성된 타일 오브젝트 관리 리스트
    private readonly List<GameObject> spawnedTiles = new List<GameObject>();
    private Coroutine buildCoroutine;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (tilesParent == null)
        {
            tilesParent = transform;
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (buildCoroutine != null)
        {
            StopCoroutine(buildCoroutine);
        }

        buildCoroutine = StartCoroutine(BuildAfterSceneInitialization());
    }

    private IEnumerator BuildAfterSceneInitialization()
    {
        yield return null;
        BuildAndApplySprite();
        buildCoroutine = null;
    }

    public bool BuildAndApplySprite()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("[PlayerPixelArtSpawner] Tile Prefab이 지정되지 않았습니다.", this);
            return false;
        }

        PixelArtData loadedData = PixelSaveSystem.LoadPixelArt();

        if (loadedData == null)
        {
            Debug.LogError("[PlayerPixelArtSpawner] PixelArtData를 불러오지 못했습니다.", this);
            return false;
        }

        if (loadedData.tiles == null || loadedData.tiles.Count == 0)
        {
            Debug.LogError("[PlayerPixelArtSpawner] JSON은 존재하지만 저장된 타일이 없습니다.", this);
            return false;
        }

        // 1. 기존에 생성된 타일들 제거
        ClearSpawnedTiles();

        // 2. 바운드(최소/최대 좌표) 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (PixelTileData tile in loadedData.tiles)
        {
            if (tile == null) continue;

            minX = Mathf.Min(minX, tile.x);
            maxX = Mathf.Max(maxX, tile.x);
            minY = Mathf.Min(minY, tile.y);
            maxY = Mathf.Max(maxY, tile.y);
        }

        if (minX == int.MaxValue || maxX == int.MinValue || minY == int.MaxValue || maxY == int.MinValue)
        {
            Debug.LogError("[PlayerPixelArtSpawner] 유효한 타일 좌표가 없습니다.", this);
            return false;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // 3. 간격을 포함한 타일 스텝(Step) 계산
        Vector2 step = new Vector2(tileSize.x + tileSpacing.x, tileSize.y + tileSpacing.y);

        // 4. 피벗(Pivot) 기준점에 따른 오프셋 계산
        Vector2 pivotOffset = CalculatePivotOffset(width, height, step);

        int appliedTileCount = 0;

        // 5. 오브젝트 생성 및 배치
        foreach (PixelTileData tile in loadedData.tiles)
        {
            if (tile == null) continue;

            int localX = tile.x - minX;
            int localY = tile.y - minY;

            if (flipY)
            {
                localY = height - 1 - localY;
            }

            // 로컬 위치 계산
            Vector3 localPosition = new Vector3(
                (localX * step.x) + pivotOffset.x,
                (localY * step.y) + pivotOffset.y,
                0f
            );

            // 타일 오브젝트 생성
            GameObject tileObj = Instantiate(tilePrefab, tilesParent);
            tileObj.transform.localPosition = localPosition;
            tileObj.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);

            // 색상 적용
            ApplyColorToTile(tileObj, GetColorByEnum(tile.color));

            spawnedTiles.Add(tileObj);
            appliedTileCount++;
        }

        Debug.Log(
            $"[PlayerPixelArtSpawner] 오브젝트 생성 완료\n" +
            $"부모 대상: {tilesParent.name}\n" +
            $"생성된 타일 수: {appliedTileCount}개\n" +
            $"전체 크기: {width}x{height}",
            this
        );

        return true;
    }

    private Vector2 CalculatePivotOffset(int width, int height, Vector2 step)
    {
        // 전체 도트 영역의 가로/세로 길이 (마지막 타일의 크기 고려)
        float totalWidth = (width - 1) * step.x;
        float totalHeight = (height - 1) * step.y;

        switch (pivotPosition)
        {
            case PivotPosition.Center:
                return new Vector2(-totalWidth * 0.5f, -totalHeight * 0.5f);

            case PivotPosition.BottomCenter:
                return new Vector2(-totalWidth * 0.5f, 0f);

            case PivotPosition.TopLeft:
                return new Vector2(0f, -totalHeight);

            default:
                return Vector2.zero;
        }
    }

    private void ApplyColorToTile(GameObject tileObj, Color color)
    {
        if (!tileObj.TryGetComponent<SpriteRenderer>(out var sr))
        {
            return;
        }

        if (colorMode == ColorApplyMode.SpriteRendererColor)
        {
            sr.color = color;
        }
        else if (colorMode == ColorApplyMode.MaterialPropertyBlock)
        {
            sr.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            sr.SetPropertyBlock(propertyBlock);
        }
    }

    private Color GetColorByEnum(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red: return redColor;
            case PixelColor.Blue: return blueColor;
            case PixelColor.Yellow: return yellowColor;
            case PixelColor.Green: return greenColor;
            case PixelColor.Black:
            default: return blackColor;
        }
    }

    private void ClearSpawnedTiles()
    {
        for (int i = 0; i < spawnedTiles.Count; i++)
        {
            if (spawnedTiles[i] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(spawnedTiles[i]);
                    continue;
                }
#endif
                Destroy(spawnedTiles[i]);
            }
        }

        spawnedTiles.Clear();
    }

    private void OnDisable()
    {
        if (buildCoroutine != null)
        {
            StopCoroutine(buildCoroutine);
            buildCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        ClearSpawnedTiles();
    }
}