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

    [Header("Tile Colors")]
    [SerializeField] private Color blackColor = Color.black;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color greenColor = Color.green;

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

            generatedTexture.SetPixel(
                textureX,
                textureY,
                GetColorByEnum(tile.color)
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

    private Color GetColorByEnum(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red:
                return redColor;

            case PixelColor.Blue:
                return blueColor;

            case PixelColor.Yellow:
                return yellowColor;

            case PixelColor.Green:
                return greenColor;

            case PixelColor.Black:
            default:
                return blackColor;
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