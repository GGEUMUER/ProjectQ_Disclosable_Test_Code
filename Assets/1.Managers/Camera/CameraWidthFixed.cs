using UnityEngine;
using Cinemachine;


[ExecuteInEditMode] // 플레이하지 않아도 에디터에서 즉시 반영됨
public class CameraWidthFixed : MonoBehaviour
{
    [Header("보여줄 가로 너비 (Unit)")]
    public float targetWidth = 30f;

    private CinemachineVirtualCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    void Update()
    {
        if (vcam == null) return;

        // 현재 화면 비율 계산
        float currentAspect = (float)Screen.width / Screen.height;

        // 가로 30을 맞추기 위한 Size 역산
        float newSize = (targetWidth * 0.5f) / currentAspect;

        // 버추얼 카메라의 렌즈 사이즈 변경
        vcam.m_Lens.OrthographicSize = newSize;
    }
}