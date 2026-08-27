using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private BackgroundSequenceManager _bgManager;

    [Header("Character Settings")]
    public GameObject[] characterPrefabs;
    public Transform spawnPoint;          // 시작 지점
    public Transform targetPoint;         // 목표 지점

    [Header("Movement Settings")]
    public float moveDuration = 2.0f;     // 이동 시간
    public float cycleDelay = 1.0f;       // 모든 캐릭터 이동 후 다음 사이클까지 대기

    private WaitForSeconds _waitCharacterInterval;
    private WaitForSeconds _waitCycleDelay;

    public string BattleSceneName { get; private set; } = SceneConstants.BattleScene;

    // 생성된 캐릭터 인스턴스들을 담아둘 리스트
    private List<LobbyCharacter> characterInstances = new List<LobbyCharacter>();

    void Awake()
    {
        _waitCharacterInterval = new WaitForSeconds(0.3f);
        _waitCycleDelay = new WaitForSeconds(cycleDelay);

        InitializePool();
    }

    void OnDestroy()
    {
        DOTween.Kill(this);

        foreach (var character in characterInstances)
        {
            if (character != null)
            {
                DOTween.Kill(character.transform);
            }
        }

        StopAllCoroutines();
    }

    void Start()
    {
        if(AudioManager._instance != null)
        {
            AudioManager._instance.PlayBGMSound(BGMLIST.LOBBY);
        }

        if(_bgManager != null)
        {
            _bgManager.StartSequence(() => {
                StartCoroutine(RandomSequentialMoveRoutine());
            });
        }
    }

    void InitializePool() // 처음에 10종류의 캐릭터를 딱 한 번씩만 생성하여 리스트에 보관
    {
        foreach (GameObject prefab in characterPrefabs)
        {
            GameObject go = Instantiate(prefab);
            go.SetActive(false); // 처음엔 비활성화

            LobbyCharacter character = go.GetComponent<LobbyCharacter>();
            if (character != null)
            {
                characterInstances.Add(character);
            }
        }
    }

    IEnumerator RandomSequentialMoveRoutine()
    {
        while (true)
        {
            // 사이클 시작 전 리스트 순서 랜덤하게 섞기
            ShuffleList(characterInstances);

            // 섞인 순서대로 한 명씩 이동
            foreach (var character in characterInstances)
            {
                // 위치 초기화 및 활성화
                character.transform.position = spawnPoint.position;
                character.gameObject.SetActive(true);

                // 이동 시작 및 완료 대기
                yield return character.MoveToTarget(targetPoint.position, moveDuration).WaitForCompletion();

                // 도착 후 다음 캐릭터를 위해 비활성화
                character.gameObject.SetActive(false);

                // 캐릭터 간 짧은 대기
                yield return _waitCharacterInterval;
            }

            Debug.Log("한 사이클 완료. 순서를 다시 섞습니다.");
            yield return _waitCycleDelay;
        }
    }

    void ShuffleList<T>(List<T> list) // 리스트를 무작위로 섞어주는 메서드
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void OnStartBattle()
    {
        SceneLauncher.LoadScene(BattleSceneName, true);
    }
}
