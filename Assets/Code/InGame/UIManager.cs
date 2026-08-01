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

        if (pixelListText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (PixelColor color in System.Enum.GetValues(typeof(PixelColor)))
            {
                int count = GameManager.Instance.GetBlockCount(color);
                sb.AppendLine($"{color.ToString().ToUpper()} : {count}");
            }

            pixelListText.text = sb.ToString();
        }

        if (redPixelText != null)
            redPixelText.text = $"R : {GameManager.Instance.GetBlockCount(PixelColor.Red)}";

        if (bluePixelText != null)
            bluePixelText.text = $"B : {GameManager.Instance.GetBlockCount(PixelColor.Blue)}";

        if (yellowPixelText != null)
            yellowPixelText.text = $"Y : {GameManager.Instance.GetBlockCount(PixelColor.Yellow)}";

        if (greenPixelText != null)
            greenPixelText.text = $"G : {GameManager.Instance.GetBlockCount(PixelColor.Green)}";
    }
}