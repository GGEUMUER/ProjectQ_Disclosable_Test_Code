using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapshotGhostTailTest : MonoBehaviour
{
    public SkeletonRenderer target;
    public Transform anchor;

    [Header("Init Vars")]
    public int count = 3; // Tail count
    public float step = 0.5f; // Tail fall back step
    public float interval = 0.2f; // Tail interval
    public float lifetime = 0.52f; // Tail lifeTime
    public Vector2 backDir = Vector2.left; // back side
    public bool inheitSorting = true; // sorting and layer copy

    // Start is called before the first frame update
    void Start()
    {
        target = GetComponent<SkeletonRenderer>();
        anchor = GetComponent<Transform>();
    }

    public void PlayBurst(Vector2 dir)
    {
        StartCoroutine(CoBurst(dir));
    }
    IEnumerator CoBurst(Vector2 dir)
    {
        if(!target) yield break;
        
        var srcMeshFilter = target.GetComponent<MeshFilter>();
        var srcMeshRenderer = target.GetComponent<MeshRenderer>();

        if(!srcMeshFilter || !srcMeshRenderer || srcMeshFilter.sharedMesh == null)
        {
            yield break;
        }

        Vector3 basePos;

        if (anchor != null)
        {
            basePos = anchor.position;
        }
        else
        {
            basePos = target.transform.position;
        }

        if(dir.sqrMagnitude > 0.0001f)
        {
            dir = dir.normalized;
        }
        else
        {
            dir = Vector2.left;
        }

        for(int i = 0; i < count; ++i)
        {
            var ghostGo = new GameObject($"{target.gameObject.name}_SnapshotGhostTail_{i}");
            var mf = ghostGo.AddComponent<MeshFilter>();
            var mr = ghostGo.AddComponent <MeshRenderer>();
            //ghostGo.GetComponent<SnapPosition>().enabled = false;
            //ghostGo.GetComponent<BoxCollider2D>().enabled = false;
            //ghostGo.GetComponent<Character>().enabled = false;
            //아래는 테스트용 스크립트. 실 적용시 해제해야 함.
            //ghostGo.GetComponent<TailTestStarter>().enabled = false;
            mf.sharedMesh = Instantiate(srcMeshFilter.sharedMesh);

            mr.sharedMaterials = (Material[])srcMeshRenderer.sharedMaterials.Clone();

            ghostGo.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            ghostGo.transform.localScale = target.transform.lossyScale;

            if (inheitSorting)
            {
                mr.sortingLayerID = srcMeshRenderer.sortingLayerID;
                mr.sortingOrder = srcMeshRenderer.sortingOrder - 1;
            }

            ghostGo.layer = target.gameObject.layer;
            
            var pos = basePos + (Vector3)(dir * step * (i + 1));
            ghostGo.transform.position = pos;

            StartCoroutine(FadeAndDestroy(mr, lifetime));

            if(interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    IEnumerator FadeAndDestroy(MeshRenderer meshRenderer, float life)
    {
        if (!meshRenderer) yield break;

        var block = new MaterialPropertyBlock();
        float time = 0f;
        while (time < life)
        {
            time += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(time / life);

            block.SetColor("_Color", new Color(1f, 1f, 1f, a));
            meshRenderer.SetPropertyBlock(block);

            yield return null;
        }
        if (meshRenderer)
        {
           Destroy(meshRenderer.gameObject);
        }

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
