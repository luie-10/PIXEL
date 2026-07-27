using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private float size;
    public float HP = 10f;
    [SerializeField] private float speed = 2.0f;
    public PixelColor colorBlock;

    [Header("Death Fly Effect")]
    [SerializeField] private GameObject pixelEffectPrefab;

    [SerializeField] private float changeDirectionInterval = 2.0f;
    private Vector2 moveDirection;
    private float timer;

    private void Start()
    {
        SetRandomDirection();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= changeDirectionInterval)
        {
            SetRandomDirection();
            timer = 0f;
        }

        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    private void SetRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        moveDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad)).normalized;
    }

    public void TakeDamage(float damage, RectTransform targetUIButton)
    {
        HP -= damage;
        if (HP <= 0)
        {
            Die(targetUIButton);
        }
    }

    private void Die(RectTransform targetUIButton)
    {
        if (pixelEffectPrefab != null && targetUIButton != null)
        {
            GameObject effectObj = Instantiate(pixelEffectPrefab, transform.position, Quaternion.identity);
            PixelFlyEffect flyScript = effectObj.GetComponent<PixelFlyEffect>();

            if (flyScript != null)
            {
                Color visualColor = GetColorFromType(colorBlock);
                flyScript.Setup(targetUIButton, colorBlock, visualColor);
            }
        }

        Destroy(gameObject);
    }

    private Color GetColorFromType(PixelColor color)
    {
        switch (color)
        {
            case PixelColor.Red: return Color.red;
            case PixelColor.Blue: return Color.blue;
            case PixelColor.Yellow: return Color.yellow;
            case PixelColor.Green: return Color.green;
            default: return Color.white;
        }
    }
}