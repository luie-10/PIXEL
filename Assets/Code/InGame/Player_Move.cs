using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Move Settings")]
    public float moveSpeed = 2.5f;
    public float rotateSpeed = 180f; // RotateAndMove 모드 시 회전 속도 (도/초)

    [Header("Spawn Manager Reference")]
    [SerializeField] private SpawnManager spawnManager;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private ControlType currentControlType = ControlType.RotateAndMove;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // SettingsManager에서 저장된 조작 타입 가져오기
        if (SettingsManager.Instance != null)
        {
            currentControlType = SettingsManager.Instance.currentControlType;
            Debug.Log($"[PlayerController2D] 적용된 조작 방식: {currentControlType}");
        }

        // SpawnManager 자동 탐색
        if (spawnManager == null)
        {
            spawnManager = FindObjectOfType<SpawnManager>();
        }
    }

    void Update()
    {
        // 씬 진행 중 실시간 세팅 변경에 대응 (필요 시)
        if (SettingsManager.Instance != null)
        {
            currentControlType = SettingsManager.Instance.currentControlType;
        }

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        if (currentControlType == ControlType.RotateAndMove)
        {
            // 1. 회전하며 전진 모드
            // A/D (좌/우): 회전 처리
            transform.Rotate(0f, 0f, -inputX * rotateSpeed * Time.deltaTime);

            // W/S (상/하): 현재 바라보는 방향(UpVector) 기준 전진/후진
            moveInput = transform.up * inputY;
        }
        else if (currentControlType == ControlType.FlipAnd8Way)
        {
            // 2. 8방향 이동 + 좌우 반전 모드
            moveInput = new Vector2(inputX, inputY);

            if (moveInput.magnitude > 1f)
            {
                moveInput.Normalize();
            }

            // 좌우 방향에 따른 스프라이트 반전 (SpriteRenderer 사용 시)
            if (spriteRenderer != null)
            {
                if (inputX > 0f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (inputX < 0f)
                {
                    spriteRenderer.flipX = true;
                }
            }
            else
            {
                // SpriteRenderer가 없을 경우 Transform Scale 이용
                if (inputX > 0f)
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
                else if (inputX < 0f)
                {
                    transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (currentControlType == ControlType.RotateAndMove)
        {
            rb.velocity = moveInput * moveSpeed;
        }
        else if (currentControlType == ControlType.FlipAnd8Way)
        {
            rb.velocity = moveInput * moveSpeed;
        }
    }

    // 위치 이동 처리 후 물리 이동 한계를 Clamp로 제한
    void LateUpdate()
    {
        if (spawnManager == null) return;

        Vector3 clampedPosition = transform.position;

        // SpawnManager의 최소/최대 좌표 범위 내로 제한
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, spawnManager.SpawnAreaMin.x, spawnManager.SpawnAreaMax.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, spawnManager.SpawnAreaMin.y, spawnManager.SpawnAreaMax.y);

        transform.position = clampedPosition;
    }
}