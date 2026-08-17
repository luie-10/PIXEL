using UnityEngine;

/// <summary>
/// 키보드 입력을 8방향 벡터로 변환해 이동시킵니다.
/// 회전하지 않고, 좌우 이동 방향에 따라 스프라이트를 좌우로 반전(Flip)합니다.
/// </summary>
public class PlayerEightWayMovement : MonoBehaviour
{
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private PlayerPixelBody pixelBody;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 moveDir = new Vector2(horizontal, vertical);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        float speedMultiplier = pixelBody != null ? pixelBody.MoveSpeedMultiplier : 1f;
        transform.position += (Vector3)(moveDir * baseMoveSpeed * speedMultiplier * Time.deltaTime);

        if (spriteRenderer != null)
        {
            if (horizontal > 0.01f) spriteRenderer.flipX = false;
            else if (horizontal < -0.01f) spriteRenderer.flipX = true;
        }
    }
}
