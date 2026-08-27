using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BothPlay : MonoBehaviour
{
    private ParticleSystem ps;
    private bool wasPlaying = false;

    public UnityEngine.Events.UnityEvent onParticleEvent;

    private void Awake()
    {
        ps = this.gameObject.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (!wasPlaying && ps.isPlaying)
        {
            wasPlaying = true;
            onParticleEvent?.Invoke();  // 시작 이벤트
            Debug.Log("파티클 시작!");
        }

        if (!ps.IsAlive())
        {
            wasPlaying = false;  // 꺼지면 다시 감지 가능
        }
    }
}
