using Spine.Unity.Examples;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CharacterGhostTailCorutine : MonoBehaviour
{
    // 스켈레톤고스트는 하이드헤이어라키로 가려져 있음. 실제로는 게임오브젝트로 생성되는 것을 확인
    public SkeletonGhost ghost;
    public Transform anchor;

    [Header("Vars")]
    public int cnt = 3;
    public float stepLength = 0.18f;
    public float intervalTempo = 0.03f;
    public bool detachGhost = true;
    public Vector2 backDir = Vector2.left; 
    // 일단 캐릭터 뒤쪽으로만 고정. 나중에, 스킬 처리를 진행할 때, 스킬 공격 시
    // 거기에 콜라이더를 달고, 해당 콜라이더가 달린 물체의 포지션의 바라보는 방향을 기준으로 피격원을 구해
    // 잔상을 만드는 쪽으로

    HashSet<SkeletonGhostRenderer> ghostHashset = new();
    Coroutine _handler;

    public void Init(Transform pos, SkeletonGhost ghost) //, Vector2 backDir)
    {
        this.ghost = ghost;
        this.anchor = ghost ? ghost.transform : pos;
        //this.anchor = pos;
        Debug.Log("In");
        //if(backDir.sqrMagnitude > 0.0001f)
        //{
        //    backDir = backDir.normalized;
        //}
        //else
        //{
        //    backDir = Vector2.left;
        //} // 만약 피격원이 있는 경우에 해제하고 사용하기
    }

    public void PlayGhostAction()
    {
        if (_handler != null) 
            StopCoroutine(_handler);

        _handler = StartCoroutine(GhostAction());
    }

    IEnumerator GhostAction()
    {
        if (ghost == null || anchor == null) yield break;
        ghostHashset.Clear();

        float originalSpwanRate = ghost.spawnInterval;
        int originalMax = ghost.maximumGhosts;
        ghost.maximumGhosts = Mathf.Max(originalMax, cnt);
        ghost.enabled = true;

        for (int i = 0; i < cnt; i++)
        {
            //            ghost.enabled = true;
            //          Debug.Log($"[Ghost] enable (#{i + 1})");

            ghost.spawnInterval = 0f;
            yield return null; // 1프레임만 출력하게
            //ghost.enabled = false;
            ghost.spawnInterval = 999999f;

            SkeletonGhostRenderer ghostRenderer = FindNewestGhostEffect();

            if (ghostRenderer != null)
            {
                ghostRenderer.transform.position = anchor.position + (Vector3)(backDir * stepLength * (i + 1));
                if (detachGhost) ghostRenderer.transform.SetParent(null, true);

                Debug.Log($"[Ghost] placed (#{i + 1}) at {ghostRenderer.transform.position}");
                var vp = Camera.main ? Camera.main.WorldToViewportPoint(ghostRenderer.transform.position) : new Vector3(-1, -1, -1);
                Debug.Log($"[Ghost] placed (#{i + 1}) vp={vp}");
            }
            else Debug.LogWarning($"[Ghost] NOT FOUND (#{i + 1})");


            if (intervalTempo > 0f) yield return new WaitForSeconds(intervalTempo);
        }
        yield return null;
        ghost.enabled = false;

        ghost.spawnInterval = originalSpwanRate;
        ghost.maximumGhosts = originalMax;
    }

    SkeletonGhostRenderer FindNewestGhostEffect()
    {
        //var effects = ghost.GetComponentsInChildren<SkeletonGhostRenderer>(true);
        var effects = Resources.FindObjectsOfTypeAll<SkeletonGhostRenderer>();
        foreach (var effect in effects)
        {
            if (!effect || !effect.name.StartsWith(ghost.gameObject.name)) continue;
            if (!ghostHashset.Contains(effect))
            {
                ghostHashset.Add(effect);
                Debug.Log($"[Ghost] NEW -> {effect.name}");
                return effect;
            }
        }
        return null;
    }
}
