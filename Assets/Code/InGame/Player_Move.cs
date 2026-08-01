using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 2.5f;

    [Header("Spawn Manager Reference")]
    [SerializeField] private SpawnManager spawnManager;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // SpawnManager 자동 탐색 (씬에 하나만 존재하는 경우)
        if (spawnManager == null)
        {
            spawnManager = FindObjectOfType<SpawnManager>();
        }
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(inputX, inputY);

        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
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