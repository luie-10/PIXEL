using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelFlyEffect : MonoBehaviour
{
    private RectTransform targetUI;
    private PixelColor blockColor;

    [SerializeField]private float flySpeed = 1.0f;
    [SerializeField] private float acceleration = 10.0f;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    public void Setup(RectTransform uiTarget, PixelColor color, Color visualColor)
    {
        targetUI = uiTarget;
        blockColor = color;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = visualColor;
        }
    }

    private void Update()
    {
        if (targetUI == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetWorldPosition = GetTargetWorldPosition();

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, flySpeed * Time.deltaTime);
        flySpeed += Time.deltaTime * acceleration; //°¡¼Óµµ

        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.3f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddBlock(blockColor, 1);
            }
            Destroy(gameObject);
        }
    }

    private Vector3 GetTargetWorldPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return transform.position;
            }
        }

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetUI.position);
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        worldPoint.z = 0f;

        return worldPoint;
    }
}