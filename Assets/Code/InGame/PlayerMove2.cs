using UnityEngine;

/// <summary>
/// 캐릭터가 항상 마우스 방향을 바라보도록 회전시키고,
/// WASD/방향키 입력으로 월드 기준 자유 이동을 처리합니다.
/// </summary>
public class PlayerMouseFacingMovement : MonoBehaviour
{
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private PlayerPixelBody pixelBody;
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (pixelBody == null) pixelBody = GetComponent<PlayerPixelBody>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Update()
    {
        RotateTowardMouse();
        Move();
    }

    private void RotateTowardMouse()
    {
        if (targetCamera == null) return;

        Vector3 mouseWorldPos = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;

        Vector2 direction = mouseWorldPos - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 moveDir = new Vector2(horizontal, vertical);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        float speedMultiplier = pixelBody != null ? pixelBody.MoveSpeedMultiplier : 1f;
        transform.position += (Vector3)(moveDir * baseMoveSpeed * speedMultiplier * Time.deltaTime);
    }
}
