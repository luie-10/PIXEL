using UnityEngine;

/// <summary>
/// SettingsManager.currentControlType 값에 따라
/// 마우스 방향 이동과 키보드 8방향 이동 중 하나만 활성화합니다.
/// </summary>
public class PlayerMovementRouter : MonoBehaviour
{
    [SerializeField] private PlayerMouseFacingMovement mouseFacingMovement;
    [SerializeField] private PlayerEightWayMovement eightWayMovement;

    private void Awake()
    {
        if (mouseFacingMovement == null) mouseFacingMovement = GetComponent<PlayerMouseFacingMovement>();
        if (eightWayMovement == null) eightWayMovement = GetComponent<PlayerEightWayMovement>();
    }

    private void OnEnable()
    {
        ApplyControlType();

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ControlTypeChanged += ApplyControlType;
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ControlTypeChanged -= ApplyControlType;
    }

    private void ApplyControlType()
    {
        if (SettingsManager.Instance == null) return;

        bool useMouseFacing = SettingsManager.Instance.currentControlType == ControlType.RotateAndMove;

        if (mouseFacingMovement != null) mouseFacingMovement.enabled = useMouseFacing;
        if (eightWayMovement != null) eightWayMovement.enabled = !useMouseFacing;
    }
}
