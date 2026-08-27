using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SnapPosition : MonoBehaviour
{
    public List<Transform> snapPositions;
    private bool isDragging = false;
    private Camera cam;
    private int selectIndex = -1;
    private int targetIndex = -1;
    //[SerializeField] bool _nowAnimation = true;
    public Transform nearest;
    public GameSceneManager gameSceneManager;
    SkeletonAnimation _skeletonAnimation;
    void Start()
    {
        cam = Camera.main;
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    void Update()
    {
        if(_skeletonAnimation == null) { return; }
        Debug.Log(_skeletonAnimation.AnimationState.GetCurrent(0));

        if (isDragging)
        {
            var current_Anim = _skeletonAnimation.AnimationState.GetCurrent(0);

            if (current_Anim.Animation.Name != "Stand_Hand")
            {
                _skeletonAnimation.AnimationState.SetAnimation(0, "Stand_Hand", true);
            }
        }
        else
        {
            var current_Anim = _skeletonAnimation.AnimationState.GetCurrent(0);

            if (current_Anim.Animation.Name != "Idle" && current_Anim.Animation.Name =="Stand")
            {
                //_skeletonAnimation.AnimationState.SetAnimation(0, "Stand", false);
                _skeletonAnimation.AnimationState.AddAnimation(0, "Idle", true,0);
            }
            if (current_Anim.Animation.Name != "Idle" && current_Anim.Animation.Name != "Stand")
            {
                _skeletonAnimation.AnimationState.SetAnimation(0, "Stand", false);
                _skeletonAnimation.AnimationState.AddAnimation(0, "Idle", true, 0);
            }
        }
        //_skeletonAnimation.AnimationState.SetAnimation(0, "Stand_Stop", true);



        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
            if (hit != null && hit.transform == transform)
            {
                isDragging = true;
                selectIndex = transform.parent.GetSiblingIndex();
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            transform.position = mouseWorldPos;
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            float nearestDistance = Mathf.Infinity;

            foreach (var snapPos in snapPositions)
            {
                float dist = Vector2.Distance(mouseWorldPos, snapPos.position);
                if (dist < nearestDistance)
                {
                    nearest = snapPos;
                    nearestDistance = dist;
                }
            }

            if (nearest != null && nearest.childCount > 0)
            {
                targetIndex = nearest.GetSiblingIndex();
                nearest.GetChild(0).parent = snapPositions[selectIndex];
                transform.parent = nearest;
                ((BatchPhase)gameSceneManager.ReturnCurrentPhase()).ChangeBatch(selectIndex, targetIndex);
            }
        }
    }

    //void OnStandAnimEnd(TrackEntry trackEntry)
    //{
    //    Debug.Log("In");
    //    if (trackEntry.Animation.Name == "Stand")
    //        _nowAnimation = false;
    //    _nowAnimation = true;
    //}
    public bool ReturnIsDragging()
    {
        return isDragging;
    }

    private void OnDisable()
    {
        isDragging = false;
    }
}
