using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로딩 씬 UI 갱신 전담 클래스 (View)
/// LoadingController로부터 데이터를 받아 UI에 표시
/// </summary>
public class LoadingUI : MonoBehaviour
{
    // 로딩 진행률 표시용 UI Image
    // fillAmount 속성 제어로 로딩 바 연출
    [SerializeField] private Image _loadingFillImage;

    // 로딩 진행률 텍스트(%) 표시용 TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI _loadingText;

    // 로딩 상태 설명 텍스트 표시용 TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI _statusText;

    // 마지막으로 업데이트된 진행률(%) 값
    // 불필요한 텍스트 UI 업데이트 방지용 최적화 변수
    private int _lastProgressPercent = -1;

    /// <summary>
    /// 로딩 진행률 UI 업데이트
    /// </summary>
    /// <param name="progress">진행률 (0.0 ~ 1.0)</param>
    public void UpdateProgress(float progress)
    {
        _loadingFillImage.fillAmount = progress;

        // 진행률을 0-100 정수 백분율로 변환
        int currentPercent = Mathf.RoundToInt(progress * 100f);
        
        // 성능 최적화: 정수 진행률 변경 시에만 텍스트 갱신
        // 매 프레임 문자열 할당 및 가비지 생성 방지
        if (currentPercent != _lastProgressPercent)
        {
            _lastProgressPercent = currentPercent;
            
            // TMP_Text.SetText(string, int) 오버로드 사용
            // 정수 변환 과정의 박싱(Boxing) 및 가비지 생성 회피
            _loadingText.SetText("{0}%", currentPercent); 
        }
    }

    /// <summary>
    /// 로딩 상태 텍스트 설정
    /// </summary>
    /// <param name="status">표시할 상태 메시지</param>
    public void SetStatusText(string status)
    {
        _statusText.text = status;
    }
}