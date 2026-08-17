using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerPixelArtSpawner : MonoBehaviour
{
    [Header("Sprite Display")]
    [Tooltip("플레이어 픽셀을 표시하는 SpriteRenderer입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprite Settings")]
    [SerializeField, Min(0.01f)] private float pixelsPerUnit = 16f;

    [Tooltip("체크하면 위아래가 뒤집혀서 그려집니다.")]
    [SerializeField] private bool flipY;

    [Header("Tile Sprites")]
    [Tooltip("각 타일 색상(픽셀)을 대표하는 스프라이트를 지정하세요.")]
    [SerializeField] private Sprite blackSprite;
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite greenSprite;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;
    private Coroutine buildCoroutine;

    // 저장된 원본 타일 데이터의 x,y 좌표 -> 텍스처 좌표 변환을 위해 보관합니다.
    private int textureMinX;
    private int textureMinY;
    private int textureWidth;
    private int textureHeight;

    /// <summary>
    /// 마지막으로 성공적으로 불러온 픽셀 아트 원본 데이터입니다.
    /// PlayerPixelBody 등 다른 시스템이 색상/좌표 정보를 다시 읽을 때 사용합니다.
    /// </summary>
    public PixelArtData LoadedData { get; private set; }

    /// <summary>
    /// 스프라이트 생성이 완료될 때마다(=본게임에 캐릭터가 실제로 등장하는 시점) 호출됩니다.
    /// </summary>
    public event Action<PlayerPixelArtSpawner> SpriteBuilt;

    private void Awake()
    {
        FindTargetRenderer();
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
        // 씬의 다른 초기화 코드가 먼저 실행된 뒤 대기합니다.
        yield return null;

        BuildAndApplySprite();

        buildCoroutine = null;
    }

    public bool BuildAndApplySprite()
    {
        if (!FindTargetRenderer())
        {
            Debug.LogError(
                "[PlayerPixelArtSpawner] 플레이어 렌더러의 SpriteRenderer를 찾지 못했습니다.",
                this
            );

            return false;
        }

        PixelArtData loadedData = PixelSaveSystem.LoadPixelArt();

        if (loadedData == null)
        {
            Debug.LogError(
                "[PlayerPixelArtSpawner] PixelArtData를 불러오지 못했습니다.",
                this
            );

            return false;
        }

        if (loadedData.tiles == null || loadedData.tiles.Count == 0)
        {
            Debug.LogError(
                "[PlayerPixelArtSpawner] JSON에 저장되어있는 타일이 없습니다.",
                this
            );

            return false;
        }

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (PixelTileData tile in loadedData.tiles)
        {
            if (tile == null)
            {
                continue;
            }

            minX = Mathf.Min(minX, tile.x);
            maxX = Mathf.Max(maxX, tile.x);
            minY = Mathf.Min(minY, tile.y);
            maxY = Mathf.Max(maxY, tile.y);
        }

        if (
            minX == int.MaxValue ||
            maxX == int.MinValue ||
            minY == int.MaxValue ||
            maxY == int.MinValue
        )
        {
            Debug.LogError(
                "[PlayerPixelArtSpawner] 유효한 타일 좌표가 없습니다.",
                this
            );

            return false;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        if (width <= 0 || height <= 0)
        {
            Debug.LogError(
                $"[PlayerPixelArtSpawner] 잘못된 텍스처 크기입니다: {width}x{height}",
                this
            );

            return false;
        }

        ReleaseGeneratedResources();

        generatedTexture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        )
        {
            name = "GeneratedPlayerPixelTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] clearPixels = new Color[width * height];

        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }

        generatedTexture.SetPixels(clearPixels);

        int appliedTileCount = 0;

        foreach (PixelTileData tile in loadedData.tiles)
        {
            if (tile == null)
            {
                continue;
            }

            int textureX = tile.x - minX;
            int textureY = tile.y - minY;

            if (flipY)
            {
                textureY = height - 1 - textureY;
            }

            if (
                textureX < 0 ||
                textureX >= width ||
                textureY < 0 ||
                textureY >= height
            )
            {
                Debug.LogWarning(
                    $"[PlayerPixelArtSpawner] 범위를 벗어난 타일을 건너뜁니다: " +
                    $"원본 ({tile.x}, {tile.y}), 변환 ({textureX}, {textureY})",
                    this
                );

                continue;
            }

            // 저장된 PixelColor 타입을 Sprite로부터 실제로 색상을 추출하여 적용
            Color pixelColor = GetColorFromSprite(tile.color);

            generatedTexture.SetPixel(
                textureX,
                textureY,
                pixelColor
            );

            appliedTileCount++;
        }

        generatedTexture.Apply(false, false);

        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );

        generatedSprite.name = "GeneratedPlayerPixelSprite";

        spriteRenderer.sprite = generatedSprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.enabled = true;

        Debug.Assert(
            spriteRenderer.sprite == generatedSprite,
            "[PlayerPixelArtSpawner] 생성된 Sprite가 Renderer에 할당되지 않았습니다.",
            this
        );

        Debug.Log(
            $"[PlayerPixelArtSpawner] 생성 완료\n" +
            $"경로: {GetHierarchyPath(spriteRenderer.transform)}\n" +
            $"원본 타일: {loadedData.tiles.Count}개\n" +
            $"적용 타일: {appliedTileCount}개\n" +
            $"텍스처 크기: {width}x{height}\n" +
            $"Pixels Per Unit: {pixelsPerUnit}",
            this
        );

        WarnIfAnimatorCanOverwriteSprite();

        // 내구도/능력 시스템이 좌표를 텍스처 좌표로 재계산할 수 있도록 저장해둡니다.
        LoadedData = loadedData;
        textureMinX = minX;
        textureMinY = minY;
        textureWidth = width;
        textureHeight = height;

        // 본게임에 캐릭터가 실제로 등장한 이 시점에 다른 시스템(내구도, 능력치)을 초기화하도록 알립니다.
        SpriteBuilt?.Invoke(this);

        return true;
    }

    /// <summary>
    /// 원본 데이터의 (x, y) 좌표를 현재 생성된 텍스처 좌표로 변환합니다.
    /// 아직 스프라이트가 생성되지 않았거나 좌표가 범위를 벗어나면 false를 반환합니다.
    /// </summary>
    public bool TryGetTextureCoord(int worldTileX, int worldTileY, out int textureX, out int textureY)
    {
        textureX = worldTileX - textureMinX;
        textureY = worldTileY - textureMinY;

        if (flipY)
        {
            textureY = textureHeight - 1 - textureY;
        }

        if (generatedTexture == null ||
            textureX < 0 || textureX >= textureWidth ||
            textureY < 0 || textureY >= textureHeight)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 지정한 원본 타일 좌표 한 칸을 투명하게 지워 텍스처에 반영합니다.
    /// 픽셀 하나가 내구도 0으로 파괴되었을 때(PlayerPixelBody) 호출됩니다.
    /// </summary>
    public void ClearPixelVisual(int worldTileX, int worldTileY)
    {
        if (generatedTexture == null) return;

        if (!TryGetTextureCoord(worldTileX, worldTileY, out int textureX, out int textureY))
        {
            Debug.LogWarning(
                $"[PlayerPixelArtSpawner] 파괴할 픽셀의 좌표가 텍스처 범위 밖입니다: ({worldTileX}, {worldTileY})",
                this
            );
            return;
        }

        generatedTexture.SetPixel(textureX, textureY, Color.clear);
        generatedTexture.Apply(false, false);
    }

    private bool FindTargetRenderer()
    {
        if (spriteRenderer != null)
        {
            return true;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return true;
        }

        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers.Length == 0)
        {
            return false;
        }

        // 가능하면 현재 활성화된 Renderer를 우선 선택합니다.
        foreach (SpriteRenderer rendererCandidate in renderers)
        {
            if (
                rendererCandidate != null &&
                rendererCandidate.gameObject.activeInHierarchy
            )
            {
                spriteRenderer = rendererCandidate;
                break;
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = renderers[0];
        }

        if (renderers.Length > 1)
        {
            Debug.LogWarning(
                $"[PlayerPixelArtSpawner] SpriteRenderer가 {renderers.Length}개입니다. " +
                $"현재 선택된 대상: {GetHierarchyPath(spriteRenderer.transform)}\n" +
                "잘못된 Renderer가 선택되면 인스펙터에서 Sprite Renderer를 직접 지정하세요.",
                this
            );
        }

        return spriteRenderer != null;
    }

    private void WarnIfAnimatorCanOverwriteSprite()
    {
        Animator animator =
            spriteRenderer.GetComponent<Animator>();

        if (animator == null)
        {
            animator =
                spriteRenderer.GetComponentInParent<Animator>();
        }

        if (animator == null || !animator.enabled)
        {
            return;
        }

        Debug.LogWarning(
            "[PlayerPixelArtSpawner] 같은 대상에 활성화된 Animator가 있습니다. " +
            "애니메이션 클립이 SpriteRenderer.sprite를 변경하면 생성된 스프라이트가 다시 덮어써질 수 있습니다.",
            spriteRenderer
        );
    }

    /// <summary>
    /// Enum 타입에 맞는 Sprite를 탐색하고, 해당 Sprite의 대표 Pixel 색상을 추출합니다.
    /// </summary>
    private Color GetColorFromSprite(PixelColor color)
    {
        Sprite targetSprite = GetSpriteByEnum(color);

        if (targetSprite == null)
        {
            // 등록된 스프라이트가 없는 경우 기본 색상 반환
            return GetFallbackColor(color);
        }

        Texture2D texture = targetSprite.texture;

        if (texture == null)
        {
            return GetFallbackColor(color);
        }

        // Texture2D에서 Read/Write가 꺼져있으면 예외가 발생할 수 있는 경우 대비
        try
        {
            // Sprite의 Rect 중 중앙 픽셀 좌표를 계산함
            Rect rect = targetSprite.rect;
            int x = Mathf.FloorToInt(rect.x + rect.width * 0.5f);
            int y = Mathf.FloorToInt(rect.y + rect.height * 0.5f);

            return texture.GetPixel(x, y);
        }
        catch
        {
            // Read/Write Enabled 옵션이 꺼져 있는 경우 기본 색상 처리
            Debug.LogWarning(
                $"[PlayerPixelArtSpawner] {targetSprite.name} 텍스처에 Read/Write 권한이 없습니다. Inspector에서 'Read/Write'를 체크해주세요.",
                this
            );
            return GetFallbackColor(color);
        }
    }

    private Sprite GetSpriteByEnum(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red:
                return redSprite;

            case PixelColor.Blue:
                return blueSprite;

            case PixelColor.Yellow:
                return yellowSprite;

            case PixelColor.Green:
                return greenSprite;

            case PixelColor.Black:
            default:
                return blackSprite;
        }
    }

    private Color GetFallbackColor(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red: return Color.red;
            case PixelColor.Blue: return Color.blue;
            case PixelColor.Yellow: return Color.yellow;
            case PixelColor.Green: return Color.green;
            case PixelColor.Black:
            default: return Color.black;
        }
    }

    private string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return "(null)";
        }

        string path = target.name;
        Transform parent = target.parent;

        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }

        return path;
    }

    private void ReleaseGeneratedResources()
    {
        if (generatedSprite != null)
        {
            if (
                spriteRenderer != null &&
                spriteRenderer.sprite == generatedSprite
            )
            {
                spriteRenderer.sprite = null;
            }

            Destroy(generatedSprite);
            generatedSprite = null;
        }

        if (generatedTexture != null)
        {
            Destroy(generatedTexture);
            generatedTexture = null;
        }
    }

    private void OnDisable()
    {
        if (buildCoroutine != null)
        {
            StopCoroutine(buildCoroutine);
            buildCoroutine = null;
        }
    }
    // PlayerPixelArtSpawner.cs 클래스 내부에 추가

    /// <summary>
    /// 픽셀 콜라이더 생성 등에서 사용할, 텍스처 1픽셀이 차지하는 월드 유닛 크기입니다.
    /// </summary>
    public float TileSizeWorld => 1f / pixelsPerUnit;

    /// <summary>
    /// 월드 타일 좌표(x, y)에 대응하는, 이 오브젝트를 기준으로 한 로컬 좌표를 계산합니다.
    /// 생성된 스프라이트의 pivot이 (0.5, 0.5)이기 때문에 아래 공식으로 정확히 대응됩니다.
    /// </summary>
    public bool TryGetLocalPosition(int worldTileX, int worldTileY, out Vector3 localPosition)
    {
        localPosition = Vector3.zero;

        if (!TryGetTextureCoord(worldTileX, worldTileY, out int textureX, out int textureY))
            return false;

        float localX = (textureX + 0.5f - textureWidth / 2f) / pixelsPerUnit;
        float localY = (textureY + 0.5f - textureHeight / 2f) / pixelsPerUnit;

        localPosition = new Vector3(localX, localY, 0f);
        return true;
    }

    private void OnDestroy()
    {
        ReleaseGeneratedResources();
    }
}
