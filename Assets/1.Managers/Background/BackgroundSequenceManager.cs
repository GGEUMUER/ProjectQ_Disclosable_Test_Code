using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 배경 요소(산, 풀, 구름) 등장 연출의 순차적 제어
/// </summary>
/// <remarks>
/// 각 배경 요소의 애니메이션을 정해진 시간 간격으로 재생하여,
/// 게임 시작 시 시각적 깊이감과 동적인 연출 효과 제공 목적.
/// 코루틴을 통해 비동기적으로 시퀀스를 관리하며, 완료 시 콜백 호출 가능.
/// </remarks>
public class BackgroundSequenceManager : MonoBehaviour
{
    [Header("Groups")]
    [SerializeField] private BackgroundIntro _mountain;
    [SerializeField] private BackgroundIntro[] _grasses;
    [SerializeField] private BackgroundIntro[] _clouds;

    [Header("Timing")]
    [SerializeField] private float _stepDelay = 0.3f;
    [SerializeField] private float _finalWaitDelay = 1.0f; 

    private WaitForSeconds _waitStepDelay;
    private WaitForSeconds _waitFinalDelay;

    private void Awake()
    {
        _waitStepDelay = new WaitForSeconds(_stepDelay);
        _waitFinalDelay = new WaitForSeconds(_finalWaitDelay);
    }
    
    private void OnDestroy()
    {
        // GameObject 파괴 시 실행 중인 모든 코루틴 정지
        StopAllCoroutines();
    }

    /// <summary>
    /// 배경 등장 시퀀스 시작
    /// </summary>
    /// <param name="onComplete">시퀀스 완료 후 실행될 콜백</param>
    public void StartSequence(Action onComplete = null)
    {
        StartCoroutine(SequenceRoutine(onComplete));
    }

    /// <summary>
    /// 설정된 시간 간격에 따라 배경 요소를 순차적으로 활성화하는 코루틴
    /// </summary>
    /// <remarks>
    /// 각 배경 그룹(산, 풀, 구름)을 _stepDelay 간격으로 재생.
    /// 모든 시퀀스 완료 후 _finalWaitDelay 만큼 추가 대기하고 onComplete 콜백 호출.
    /// </remarks>
    private IEnumerator SequenceRoutine(Action onComplete)
    {
        if(_mountain != null) _mountain.Play();
        
        yield return _waitStepDelay;

        foreach (var grass in _grasses)
        {
            if(grass != null) grass.Play();
        }

        yield return _waitStepDelay;

        foreach (var cloud in _clouds)
        {
            if(cloud != null) cloud.Play();
        }

        yield return _waitFinalDelay;

        onComplete?.Invoke();
    }
}
