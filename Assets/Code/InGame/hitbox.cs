using UnityEngine;

/// <summary>
/// 조립된 캐릭터의 살아있는 픽셀 1칸에 대응하는 "피격 판정" 콜라이더에 붙는 마커 컴포넌트입니다.
/// 실제 데미지 처리는 Enemy.cs가 태그("Player")를 확인한 뒤
/// GetComponentInParent<PlayerHealthController>()로 부모(플레이어 루트)에서 직접 처리하므로,
/// 이 스크립트 자체는 어떤 칸(x, y)의 콜라이더인지 식별 정보만 들고 있습니다.
/// 추후 "맞은 칸에 비례한 데미지" 등으로 확장할 때 사용할 수 있습니다.
/// </summary>
public class PixelBodyHitbox : MonoBehaviour
{
    public int TileX { get; private set; }
    public int TileY { get; private set; }

    public void SetTileCoord(int x, int y)
    {
        TileX = x;
        TileY = y;
    }
}
