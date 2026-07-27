using UnityEngine;

public class PlayerController2D : MonoBehaviour
{

    public float moveSpeed = 2.5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(inputX, inputY);

        // 대각선 이동 시 속도 증가 방지 (벡터 길이가 1을 넘으면 1로 정규화)
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    void FixedUpdate()
    {
        // 상하좌우 속도 일괄 적용
        // (Unity 2023.1 / Unity 6 이상 버전은 rb.linearVelocity 사용)
        rb.velocity = moveInput * moveSpeed;
    }
}