using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private Dictionary<PixelColor, int> collectedBlocks = new Dictionary<PixelColor, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBlockData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeBlockData()
    {
        collectedBlocks[PixelColor.Red] = 0;
        collectedBlocks[PixelColor.
            Blue] = 0;
        collectedBlocks[PixelColor.Yellow] = 0;
        collectedBlocks[PixelColor.Green] = 0;
    }

    public void AddBlock(PixelColor color, int amount = 1)
    {
        if (color == PixelColor.None) return;

        if (collectedBlocks.ContainsKey(color))
        {
            collectedBlocks[color] += amount;
            Debug.Log($"[{color}] ÇÈ¼¿ È¹µæ! ÇöÀç ÃÑ ¼ö·®: {collectedBlocks[color]}");
        }
    }

    public int GetBlockCount(PixelColor color)
    {
        if (collectedBlocks.TryGetValue(color, out int count))
        {
            return count;
        }
        return 0;
    }
}