using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    [Header("Attack")]
    public float AK = 0.5f;

    [SerializeField] private RectTransform pixelUIButton;
    [SerializeField] private float attackCoolTime = 0.5f;

    private bool canAttack = true;

    private void Awake()
    {
        EnsurePixelArtSpawner();
    }

    private void EnsurePixelArtSpawner()
    {
        // 플레이어 루트에 적용 컴포넌트가 없으면 자동 추가합니다.
        PlayerPixelArtSpawner spawner =
            GetComponent<PlayerPixelArtSpawner>();

        if (spawner == null)
        {
            spawner = gameObject.AddComponent<PlayerPixelArtSpawner>();

            Debug.Log(
                "[Player] PlayerPixelArtSpawner가 없어 자동으로 추가했습니다.",
                this
            );
        }

        spawner.enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canAttack)
        {
            return;
        }

        if (!collision.gameObject.CompareTag("Enemy"))
        {
            return;
        }

        StartCoroutine(AttackRoutine(collision.gameObject));
    }

    private IEnumerator AttackRoutine(GameObject enemyObject)
    {
        canAttack = false;

        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(AK, pixelUIButton);
        }

        yield return new WaitForSeconds(attackCoolTime);

        canAttack = true;
    }
}