using System;
using System.Collections.Generic;

[Serializable]
public class PixelTileData
{
    public int x;
    public int y;
    public PixelColor color;
    public bool isAttackTile;
}

[Serializable]
public class PixelArtData
{
    public List<PixelTileData> tiles = new List<PixelTileData>();
}
