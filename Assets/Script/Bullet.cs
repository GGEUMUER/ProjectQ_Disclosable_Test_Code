using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject bulletParticle;
    public float speed = 10f;  // 이동 속도
    public float lifeTime = 3f; // 총알 유지 시간

    private GameObject InstantParticle;
    void Start()
    {
        // 일정 시간 후 자동 삭제
        Destroy(gameObject, lifeTime);
    }

    private void OnEnable()
    {
        InstantParticle=GameObject.Instantiate(bulletParticle);
        InstantParticle.transform.position = transform.position;
        InstantParticle.GetComponent<RangerParticle>().bullet = this.gameObject;
        InstantParticle.GetComponent<RangerParticle>().FirstSet();
    }

    void Update()
    {
        // x축 방향으로 이동
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnDisable()
    {
        Destroy(InstantParticle);
    }
}
