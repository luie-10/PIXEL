using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using TMPro;

public enum EditorTool
{
    Pen,         // 펜 (색상 및 픽셀 설치)
    Eraser,      // 지우개 (삭제)
    Hand,        // 손바닥 (타일맵 둘러보기)
    Magnifier,   // 돋보기 (마우스 스크롤로 확대/축소)
    AttackSelect // 공격 범위 타일 지정 모드
}

public class PixelPainter : MonoBehaviour
{
    [Header("Tilemap Reference")]
    public Tilemap pixelDrawTilemap;   // 도트가 그려지는 타일맵
    public Tilemap attackRangeTilemap; // 공격 인식 범위가 그려지는 레이어 타일맵

    [Header("Color & Range Tiles")]
    public TileBase blackTile;  // 기본 무한 검은색 타일
    public TileBase redTile;
    public TileBase blueTile;
    public TileBase yellowTile;
    public TileBase greenTile;
    public TileBase attackTile; // 공격 범위 표기용 타일 (예: 주황색/붉은색 반투명 타일)

    [Header("Block Count Limit (설치 제한)")]
    public int minTotalBlocks = 4;   // 최소 설치 필수 픽셀 수
    public int maxTotalBlocks = 100; // 최대 설치 가능 픽셀 수

    [Header("Camera & Bounds (카메라 제한)")]
    public Camera mainCamera;
    public Vector2 mapMinBounds = new Vector2(-10, -10);
    public Vector2 mapMaxBounds = new Vector2(10, 10);
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float zoomSensitivity = 1.5f;

    [Header("UI Reference")]
    public GameObject editorUIPanel;       // 기존 하단 도구/팔레트 패널 (숨김용)
    public GameObject attackSelectUIPanel; // 공격 범위 지정용 안내 패널
    public PixelPaletteUI paletteUI;
    public TextMeshProUGUI warningText;    // 경고 텍스트 UI
    public TextMeshProUGUI countInfoText;  // 현재/최대 개수 및 공격타일 정보 UI

    [Header("Current State")]
    public EditorTool currentTool = EditorTool.Pen;
    public PixelColor currentSelectedColor = PixelColor.Black;

    private Coroutine warningCoroutine;
    private Vector3 dragOrigin;

    // 공격 타일 저장을 위한 좌표 목록
    private HashSet<Vector3Int> attackTilePositions = new HashSet<Vector3Int>();

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (warningText != null)
            warningText.gameObject.SetActive(false);

        if (attackSelectUIPanel != null)
            attackSelectUIPanel.SetActive(false);
    }

    private void Start()
    {
        UpdateCountUI();
    }

    private void Update()
    {
        // UI 버튼/패널 클릭 시 타일맵 조작 방지
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleToolInput();
        ClampCameraPosition();
    }

    // ==========================================
    // 1. 도구별 입력 처리
    // ==========================================
    private void HandleToolInput()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int targetCellPos = pixelDrawTilemap.WorldToCell(mouseWorldPos);

        switch (currentTool)
        {
            case EditorTool.Pen:
                if (Input.GetMouseButtonDown(0))
                {
                    TryPaintPixel(targetCellPos);
                }
                break;

            case EditorTool.Eraser:
                if (Input.GetMouseButton(0))
                {
                    TryErasePixel(targetCellPos);
                }
                break;

            case EditorTool.Hand:
                HandleHandPan();
                break;

            case EditorTool.Magnifier:
                HandleMagnifierZoom();
                break;

            case EditorTool.AttackSelect:
                if (Input.GetMouseButtonDown(0))
                {
                    ToggleAttackTile(targetCellPos);
                }
                break;
        }
    }

    // ==========================================
    // 2. 픽셀 설치 로직 (펜 도구)
    // ==========================================
    private void TryPaintPixel(Vector3Int targetCellPos)
    {
        if (pixelDrawTilemap.HasTile(targetCellPos))
            return;

        int currentPlacedCount = GetActualPlacedPixelCount();
        if (currentPlacedCount >= maxTotalBlocks)
        {
            ShowWarning($"최대 설치 개수({maxTotalBlocks}개)를 초과했습니다!");
            return;
        }

        if (!IsFirstPixel() && !IsAdjacentToExistingPixel(targetCellPos))
        {
            ShowWarning("기존 픽셀과 이어지게 찍어야 합니다!");
            return;
        }

        if (currentSelectedColor == PixelColor.Black)
        {
            pixelDrawTilemap.SetTile(targetCellPos, blackTile);
            UpdateCountUI();
            return;
        }

        if (GameManager.Instance != null)
        {
            int currentCount = GameManager.Instance.GetTotalSavedBlockCount(currentSelectedColor);

            if (currentCount <= 0)
            {
                ShowWarning($"{GetColorNameKR(currentSelectedColor)} 픽셀이 부족합니다!");
                return;
            }

            TileBase targetTile = GetTileByColor(currentSelectedColor);
            if (targetTile != null)
            {
                pixelDrawTilemap.SetTile(targetCellPos, targetTile);
                GameManager.Instance.UsePixel(currentSelectedColor, 1);

                if (paletteUI != null) paletteUI.UpdatePaletteUI();
                UpdateCountUI();
            }
        }
    }

    // ==========================================
    // 3. 지우개 및 공격 범위 지정 로직
    // ==========================================
    private void TryErasePixel(Vector3Int targetCellPos)
    {
        if (pixelDrawTilemap.HasTile(targetCellPos))
        {
            pixelDrawTilemap.SetTile(targetCellPos, null);

            // 만약 지운 자리에 공격 타일이 지정되어 있었다면 함께 제거
            if (attackRangeTilemap != null && attackRangeTilemap.HasTile(targetCellPos))
            {
                attackRangeTilemap.SetTile(targetCellPos, null);
                attackTilePositions.Remove(targetCellPos);
            }

            UpdateCountUI();
        }
    }

    /// <summary>
    /// 그려진 픽셀 위를 클릭해 공격 인식 타일을 지정/해제하는 함수
    /// </summary>
    private void ToggleAttackTile(Vector3Int targetCellPos)
    {
        if (!pixelDrawTilemap.HasTile(targetCellPos))
        {
            ShowWarning("픽셀을 그린 위치에만 공격 타일을 지정할 수 있습니다.");
            return;
        }

        int maxAttackTiles = GetMaxAttackTileCount();

        if (attackTilePositions.Contains(targetCellPos))
        {
            attackTilePositions.Remove(targetCellPos);
            if (attackRangeTilemap != null) attackRangeTilemap.SetTile(targetCellPos, null);
        }
        else
        {
            if (attackTilePositions.Count >= maxAttackTiles)
            {
                ShowWarning($"공격 타일은 최대 {maxAttackTiles}개(전체 픽셀의 1/4 반올림)까지만 지정 가능합니다!");
                return;
            }

            attackTilePositions.Add(targetCellPos);
            if (attackRangeTilemap != null) attackRangeTilemap.SetTile(targetCellPos, attackTile);
        }

        UpdateCountUI();
    }

    // ==========================================
    // 4. 모드 전환 및 저장/복귀
    // ==========================================

    /// <summary>
    /// '다음으로' 버튼 클릭 시 호출
    /// - 도트 그리기 단계: 공격 타일 지정 모드로 전환
    /// - 공격 타일 지정 단계: 저장 후 게임 씬으로 이동
    /// </summary>
    public void OnClickNextButton()
    {
        // 1단계: 도트 그리기 완료 후 공격 타일 지정 모드로 전환
        if (currentTool != EditorTool.AttackSelect)
        {
            int totalPixels = GetActualPlacedPixelCount();

            if (totalPixels < minTotalBlocks)
            {
                ShowWarning($"최소 {minTotalBlocks}개 이상의 픽셀을 그려야 합니다! (현재 {totalPixels}개)");
                return;
            }

            if (editorUIPanel != null) editorUIPanel.SetActive(false);
            if (attackSelectUIPanel != null) attackSelectUIPanel.SetActive(true);

            currentTool = EditorTool.AttackSelect;
            UpdateCountUI();

            int maxAttackTiles = GetMaxAttackTileCount();
            Debug.Log($"공격 범위 지정 모드 전환 완료. (전체 픽셀: {totalPixels}개 / 지정 가능 공격 타일: {maxAttackTiles}개)");
        }
        // 2단계: 이미 공격 타일 지정 모드일 때 한 번 더 누르면 완료 및 저장 처리
        else
        {
            OnClickCompleteAndSave();
        }
    }

    /// <summary>
    /// 공격 타일 지정 완료 후 데이터 저장 및 게임 씬 이동
    /// </summary>
    public void OnClickCompleteAndSave()
    {
        PixelArtData saveData = new PixelArtData();
        BoundsInt bounds = pixelDrawTilemap.cellBounds;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (pixelDrawTilemap.HasTile(pos))
            {
                TileBase currentTile = pixelDrawTilemap.GetTile(pos);
                PixelColor color = GetColorFromTile(currentTile);

                PixelTileData tileData = new PixelTileData
                {
                    x = pos.x,
                    y = pos.y,
                    color = color,
                    isAttackTile = attackTilePositions.Contains(pos)
                };

                saveData.tiles.Add(tileData);
            }
        }

        // JSON 파일 저장
        PixelSaveSystem.SavePixelArt(saveData);

        // 비동기 로딩으로 GameScene 이동
        LoadingSceneManager.LoadScene("GameScene");
    }

    // ==========================================
    // 5. 손바닥(이동) & 돋보기 & 카메라 제한
    // ==========================================
    private void HandleHandPan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 difference = dragOrigin - mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mainCamera.transform.position += difference;
        }
    }

    private void HandleMagnifierZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = mainCamera.orthographicSize - (scroll * zoomSensitivity);
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }

    private void ClampCameraPosition()
    {
        Vector3 camPos = mainCamera.transform.position;

        float vertExtent = mainCamera.orthographicSize;
        float horizExtent = vertExtent * mainCamera.aspect;

        float minX = mapMinBounds.x + horizExtent;
        float maxX = mapMaxBounds.x - horizExtent;
        float minY = mapMinBounds.y + vertExtent;
        float maxY = mapMaxBounds.y - vertExtent;

        if (minX > maxX) camPos.x = (mapMinBounds.x + mapMaxBounds.x) / 2f;
        else camPos.x = Mathf.Clamp(camPos.x, minX, maxX);

        if (minY > maxY) camPos.y = (mapMinBounds.y + mapMaxBounds.y) / 2f;
        else camPos.y = Mathf.Clamp(camPos.y, minY, maxY);

        mainCamera.transform.position = camPos;
    }

    // ==========================================
    // 6. UI 버튼 연결용 메서드
    // ==========================================
    public void SelectPenTool() => currentTool = EditorTool.Pen;
    public void SelectEraserTool() => currentTool = EditorTool.Eraser;
    public void SelectHandTool() => currentTool = EditorTool.Hand;
    public void SelectMagnifierTool() => currentTool = EditorTool.Magnifier;

    public void SetSelectedColor(PixelColor newColor)
    {
        currentSelectedColor = newColor;
        currentTool = EditorTool.Pen;
    }

    // ==========================================
    // 7. 유틸리티 및 카운트
    // ==========================================
    private int GetActualPlacedPixelCount()
    {
        int count = 0;
        BoundsInt bounds = pixelDrawTilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (pixelDrawTilemap.HasTile(pos)) count++;
        }
        return count;
    }

    private int GetMaxAttackTileCount()
    {
        int totalPixels = GetActualPlacedPixelCount();
        return Mathf.RoundToInt(totalPixels / 4.0f);
    }

    private bool IsFirstPixel() => GetActualPlacedPixelCount() == 0;

    private bool IsAdjacentToExistingPixel(Vector3Int cellPos)
    {
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int( 0,  1, 0), new Vector3Int( 0, -1, 0),
            new Vector3Int(-1,  0, 0), new Vector3Int( 1,  0, 0),
            new Vector3Int(-1,  1, 0), new Vector3Int( 1,  1, 0),
            new Vector3Int(-1, -1, 0), new Vector3Int( 1, -1, 0)
        };

        foreach (var dir in directions)
        {
            if (pixelDrawTilemap.HasTile(cellPos + dir)) return true;
        }
        return false;
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(CoShowWarning(message));
    }

    private IEnumerator CoShowWarning(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        warningText.gameObject.SetActive(false);
    }

    private void UpdateCountUI()
    {
        if (countInfoText == null) return;

        int currentCount = GetActualPlacedPixelCount();

        if (currentTool == EditorTool.AttackSelect)
        {
            int maxAttackCount = GetMaxAttackTileCount();
            countInfoText.text = $"공격 타일 지정: \n {attackTilePositions.Count} / {maxAttackCount}";
        }
        else
        {
            countInfoText.text = $"설치된 픽셀: \n {currentCount} / {maxTotalBlocks} (최소 {minTotalBlocks}개)";
        }
    }

    private TileBase GetTileByColor(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Black: return blackTile;
            case PixelColor.Red: return redTile;
            case PixelColor.Blue: return blueTile;
            case PixelColor.Yellow: return yellowTile;
            case PixelColor.Green: return greenTile;
            default: return null;
        }
    }

    private PixelColor GetColorFromTile(TileBase tile)
    {
        if (tile == blackTile) return PixelColor.Black;
        if (tile == redTile) return PixelColor.Red;
        if (tile == blueTile) return PixelColor.Blue;
        if (tile == yellowTile) return PixelColor.Yellow;
        if (tile == greenTile) return PixelColor.Green;

        return PixelColor.Black;
    }

    private string GetColorNameKR(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red: return "빨간색";
            case PixelColor.Blue: return "파란색";
            case PixelColor.Yellow: return "노란색";
            case PixelColor.Green: return "초록색";
            default: return color.ToString();
        }
    }
}