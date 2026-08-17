using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조립된 픽셀 데이터를 기준으로 실제 물리 콜라이더를 자동 생성합니다.
/// 모든 살아있는 픽셀에는 "피격 콜라이더"를 만들고,
/// 공격 타일로 지정된 픽셀에는 "공격 콜라이더"를 추가로 만듭니다.
/// 두 콜라이더는 서로 다른 오브젝트, 다른 스크립트로 완전히 분리되어 있습니다.
/// </summary>
[RequireComponent(typeof(PlayerPixelBody))]
[RequireComponent(typeof(PlayerPixelArtSpawner))]
public class PixelBodyColliderBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPixelBody pixelBody;
    [SerializeField] private PlayerPixelArtSpawner spawner;

    [Header("Collider Settings")]
    [Tooltip("피격(바디) 콜라이더에 부여할 태그입니다. Enemy.cs가 이 태그로 플레이어를 인식합니다.")]
    [SerializeField] private string bodyColliderTag = "Player";

    private readonly Dictionary<(int x, int y), GameObject> bodyColliderObjects = new Dictionary<(int, int), GameObject>();
    private readonly Dictionary<(int x, int y), GameObject> attackColliderObjects = new Dictionary<(int, int), GameObject>();

    private Transform bodyColliderRoot;
    private Transform attackColliderRoot;

    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
        if (spawner == null) spawner = GetComponent<PlayerPixelArtSpawner>();
    }

    private void OnEnable()
    {
        if (pixelBody != null)
        {
            pixelBody.BodyInitialized += RebuildAllColliders;
            pixelBody.PixelBroken += HandlePixelBroken;
        }
    }

    private void OnDisable()
    {
        if (pixelBody != null)
        {
            pixelBody.BodyInitialized -= RebuildAllColliders;
            pixelBody.PixelBroken -= HandlePixelBroken;
        }
    }

    private void RebuildAllColliders()
    {
        ClearAllColliders();

        if (bodyColliderRoot == null)
        {
            GameObject rootObj = new GameObject("BodyHitboxes");
            rootObj.transform.SetParent(transform, false);
            bodyColliderRoot = rootObj.transform;
        }

        if (attackColliderRoot == null)
        {
            GameObject rootObj = new GameObject("AttackHitboxes");
            rootObj.transform.SetParent(transform, false);
            attackColliderRoot = rootObj.transform;
        }

        float tileSize = spawner.TileSizeWorld;

        foreach (PlayerPixelBody.PixelTileInfo info in pixelBody.GetAliveTileInfos())
        {
            if (!spawner.TryGetLocalPosition(info.x, info.y, out Vector3 localPos))
                continue;

            CreateBodyCollider(info.x, info.y, localPos, tileSize);

            if (info.isAttackTile)
            {
                CreateAttackCollider(info.x, info.y, localPos, tileSize);
            }
        }
    }

    private void CreateBodyCollider(int x, int y, Vector3 localPos, float tileSize)
    {
        GameObject obj = new GameObject($"Body_{x}_{y}");
        obj.transform.SetParent(bodyColliderRoot, false);
        obj.transform.localPosition = localPos;
        obj.tag = bodyColliderTag;

        BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
        box.size = new Vector2(tileSize, tileSize);
        box.isTrigger = true;

        obj.AddComponent<PixelBodyHitbox>();

        bodyColliderObjects[(x, y)] = obj;
    }

    private void CreateAttackCollider(int x, int y, Vector3 localPos, float tileSize)
    {
        GameObject obj = new GameObject($"Attack_{x}_{y}");
        obj.transform.SetParent(attackColliderRoot, false);
        obj.transform.localPosition = localPos;

        BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
        box.size = new Vector2(tileSize, tileSize);
        box.isTrigger = true;

        PixelAttackHitbox hitbox = obj.AddComponent<PixelAttackHitbox>();

        hitbox.Init(pixelBody);

        attackColliderObjects[(x, y)] = obj;
    }

    private void HandlePixelBroken(int x, int y)
    {
        if (bodyColliderObjects.TryGetValue((x, y), out GameObject bodyObj))
        {
            if (bodyObj != null) Destroy(bodyObj);
            bodyColliderObjects.Remove((x, y));
        }

        if (attackColliderObjects.TryGetValue((x, y), out GameObject attackObj))
        {
            if (attackObj != null) Destroy(attackObj);
            attackColliderObjects.Remove((x, y));
        }
    }

    private void ClearAllColliders()
    {
        foreach (var obj in bodyColliderObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        bodyColliderObjects.Clear();

        foreach (var obj in attackColliderObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        attackColliderObjects.Clear();
    }
}
