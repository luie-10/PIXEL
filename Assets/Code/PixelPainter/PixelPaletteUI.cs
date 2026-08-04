using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PixelPaletteUI : MonoBehaviour
{
    [System.Serializable]
    public struct ColorButtonInfo
    {
        public PixelColor pixelColor;
        public Button button;
        public TextMeshProUGUI countText; // 잔여 수량 표시 텍스트 (검은색은 "∞" 등으로 표시)
    }

    [Header("Palette Buttons")]
    public ColorButtonInfo[] colorButtons;

    [Header("References")]
    public PixelPainter painter;

    private void Start()
    {
        // 각 버튼 클릭 이벤트 연결
        foreach (var info in colorButtons)
        {
            PixelColor col = info.pixelColor;
            if (info.button != null)
            {
                info.button.onClick.AddListener(() => OnSelectColor(col));
            }
        }

        UpdatePaletteUI();
    }

    public void OnSelectColor(PixelColor color)
    {
        if (painter != null)
        {
            painter.SetSelectedColor(color);
        }
    }




    // UI 수량 텍스트 갱신 함수
    public void UpdatePaletteUI()
    {
        if (GameManager.Instance == null) return;

        foreach (var info in colorButtons)
        {
            if (info.countText == null) continue;

            // 검은색은 무한으로 표시
            if (info.pixelColor == PixelColor.Black)
            {
                info.countText.text = "∞";
            }
            else
            {
                // GameManager에서 보유한 총 픽셀 수량 불러오기
                int count = GameManager.Instance.GetTotalSavedBlockCount(info.pixelColor);
                info.countText.text = count.ToString();
            }
        }
    }
}