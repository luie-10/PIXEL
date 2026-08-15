using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerPixelArtSpawner : MonoBehaviour
{
    [Header("Sprite Display")]
    [Tooltip("플레이어 외형을 표시하는 SpriteRenderer입니다. 비어 있으면 자동 탐색합니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprite Settings")]
    [SerializeField, Min(0.01f)] private float pixelsPerUnit = 16f;

    [Tooltip("체크하면 저장된 그림의 위아래가 반전됩니다.")]
    [SerializeField] private bool flipY;

    [Header("Tile Sprites")]
    [Tooltip("각 타일 색상(타입)에 대응하는 스프라이트를 연결하세요.")]
    [SerializeField] private Sprite blackSprite;
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite greenSprite;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;
    private Coroutine buildCoroutine;

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
        // 게임 씬의 다른 초기화 코드가 끝난 뒤 적용합니다.
        yield return null;

        BuildAndApplySprite();

        buildCoroutine = null;
    }

    public bool BuildAndApplySprite()
    {
        if (!FindTargetRenderer())
        {
            Debug.LogError(
                "[PlayerPixelArtSpawner] 플레이어 하위에서 SpriteRenderer를 찾지 못했습니다.",
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
                "[PlayerPixelArtSpawner] JSON은 존재하지만 저장된 타일이 없습니다.",
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
                    $"[PlayerPixelArtSpawner] 범위 밖 타일을 건너뜁니다: " +
                    $"원본 ({tile.x}, {tile.y}), 변환 ({textureX}, {textureY})",
                    this
                );

                continue;
            }

            // 지정된 PixelColor 타일의 Sprite로부터 색상을 추출하여 설정
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
            "[PlayerPixelArtSpawner] 생성 Sprite가 Renderer에 할당되지 않았습니다.",
            this
        );

        Debug.Log(
            $"[PlayerPixelArtSpawner] 적용 완료\n" +
            $"대상: {GetHierarchyPath(spriteRenderer.transform)}\n" +
            $"저장 타일: {loadedData.tiles.Count}개\n" +
            $"적용 타일: {appliedTileCount}개\n" +
            $"텍스처 크기: {width}x{height}\n" +
            $"Pixels Per Unit: {pixelsPerUnit}",
            this
        );

        WarnIfAnimatorCanOverwriteSprite();

        return true;
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
                "잘못된 Renderer가 선택되면 인스펙터의 Sprite Renderer에 직접 연결하세요.",
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
            "[PlayerPixelArtSpawner] 적용 대상에 활성화된 Animator가 있습니다. " +
            "애니메이션 클립이 SpriteRenderer.sprite를 제어하면 생성한 도트가 다시 덮어써질 수 있습니다.",
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

        // Texture2D에서 Read/Write가 가능하지 않을 수 있는 경우 고려
        try
        {
            // Sprite의 Rect 내 중앙 픽셀 색상을 가져옴
            Rect rect = targetSprite.rect;
            int x = Mathf.FloorToInt(rect.x + rect.width * 0.5f);
            int y = Mathf.FloorToInt(rect.y + rect.height * 0.5f);

            return texture.GetPixel(x, y);
        }
        catch
        {
            // Read/Write Enabled 옵션이 꺼져 있을 때의 기본 예외 처리
            Debug.LogWarning(
                $"[PlayerPixelArtSpawner] {targetSprite.name} 텍스처의 Read/Write 설정이 꺼져 있습니다. Inspector에서 'Read/Write'를 체크해주세요.",
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

    private void OnDestroy()
    {
        ReleaseGeneratedResources();
    }
}