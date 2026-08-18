using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Stop Panel")]
    public GameObject StopPannel;

    [Header("Pixel Count Single Text")]
    public TextMeshProUGUI pixelListText;

    [Header("Individual Pixel Count Texts")]
    public TextMeshProUGUI redPixelText;
    public TextMeshProUGUI bluePixelText;
    public TextMeshProUGUI yellowPixelText;
    public TextMeshProUGUI greenPixelText;

    private bool isPaused = false;

    private void Start()
    {
        if (StopPannel != null)
        {
            StopPannel.SetActive(false);
        }
    }

    public void TogglePixelInventory()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            OpenInventory();
        }
        else
        {
            CloseInventory();
        }
    }

    public void CloseInventory()
    {
        if (StopPannel != null)
        {
            StopPannel.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void OpenInventory()
    {
        UpdatePixelUI();
        if (StopPannel != null)
        {
            StopPannel.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    private void UpdatePixelUI()
    {
        if (GameManager.Instance == null) return;

        // 인벤토리 패널은 "이번 라운드 획득량"이 아니라
        // 실제로 조립/스킬에 사용되는 "누적 보유량(totalSavedBlocks)"을 보여줘야 하므로
        // GetBlockCount 대신 GetTotalSavedBlockCount를 사용합니다.
        if (pixelListText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (PixelColor color in System.Enum.GetValues(typeof(PixelColor)))
            {
                int count = GameManager.Instance.GetTotalSavedBlockCount(color);
                sb.AppendLine($"{color.ToString().ToUpper()} : {count}");
            }

            pixelListText.text = sb.ToString();
        }

        if (redPixelText != null)
            redPixelText.text = $"R : {GameManager.Instance.GetTotalSavedBlockCount(PixelColor.Red)}";

        if (bluePixelText != null)
            bluePixelText.text = $"B : {GameManager.Instance.GetTotalSavedBlockCount(PixelColor.Blue)}";

        if (yellowPixelText != null)
            yellowPixelText.text = $"Y : {GameManager.Instance.GetTotalSavedBlockCount(PixelColor.Yellow)}";

        if (greenPixelText != null)
            greenPixelText.text = $"G : {GameManager.Instance.GetTotalSavedBlockCount(PixelColor.Green)}";
    }
}
