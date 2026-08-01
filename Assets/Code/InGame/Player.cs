using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float AK = 0.5f;
    [SerializeField] private RectTransform pixelUIButton;
    [SerializeField] private float attackCoolTime = 0.5f;

    private bool canAttack = true;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (canAttack && collision.gameObject.CompareTag("Enemy"))
        {
            StartCoroutine(AttackRoutine(collision.gameObject));
        }
    }

    private IEnumerator AttackRoutine(GameObject enemyObj)
    {
        canAttack = false;

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(AK, pixelUIButton);
        }

        yield return new WaitForSeconds(attackCoolTime);
        canAttack = true;
    }
}