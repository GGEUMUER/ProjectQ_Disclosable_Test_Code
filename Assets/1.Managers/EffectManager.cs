using Core.Units;
using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager _instance { get; private set; }

    public enum EndMode
    {
        Emission,
        NonEmission
    }

    [SerializeField]
    public enum SkillList
    {
        Heal,
        Shield,
        Andrea,
        Walter,        
    }

    public enum HitEffectList
    {
        Therion,
        Hranger,
        Walter,
        Hmagician
    }
    [System.Serializable]
    public struct SkillVFXEntry
    {
        public int baseStatId;
        public UnitAct act;
        public GameObject prefab;
        public EndMode endMode;

        [Tooltip("If effect's mode is Loop, this value will be used for emission On Off Time")]
        public float loopTime;

        [Tooltip("If there no particle, it will be used for fallback time")]
        public float fallbackLifeTime;
    }

    [Header("Skill maps")]
    public List<SkillVFXEntry> skillVFXEntries = new();

    Dictionary<(int, UnitAct), SkillVFXEntry> _maps = new();


    [Header("Basic Hit Effect")]
    public GameObject hitPrefab;
    public float hitFallbackLife = 1f;

    [Header("Effects")]
    public List<GameObject> effects = new List<GameObject>();
    public List<GameObject> hitEffects = new List<GameObject>();

    [Header("EffectContainer")]
    public List<GameObject> effectsContainer = new List<GameObject>();

    [Header("Flags")]
    public bool onlyDeactivate;
    public Transform test;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        { 
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _maps.Clear();
        foreach(var e in skillVFXEntries)
        {
            if (!e.prefab) continue;
            _maps[(e.baseStatId, e.act)] = e;
        }
    }

    public void PlaySkillOnPos(int casterBaseStatId, UnitAct act, Vector3 pos, float time, bool deactivate = false)
    {
        if(!_maps.TryGetValue((casterBaseStatId, act), out var map)) return;

        var fx = Instantiate(map.prefab, pos, Quaternion.identity);

        StartCoroutine(LoopStopEmissionAfterTimeParam(fx, time));
    }

    public void PlayNormalHitEffectPrefab(Vector3 pos)
    {
        var fx = gameObject;
        fx = Instantiate(hitPrefab, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);
        Destroy(fx, 1.5f);
    }

    public void PlaySkillOnPos2(SkillList sl, Vector3 pos, float time, bool reverse, EndMode mode, bool deactivate = false)
    {
        var fx = gameObject;
        if (reverse == false)
        {
            fx = Instantiate(effects[(int)sl], new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);
        }
        else
        {
            fx = Instantiate(effects[(int)sl], new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(0f, 180f, 0f));
        }

        StartCoroutine(LoopStopEmissionAfterTimeParam(fx, time));
    }

    public void HitSkillEffectOnPos(HitEffectList hel, Vector3 pos, float time, bool reverse)
    {
        var fx = gameObject;
        if(reverse == false)
        {
            fx = Instantiate(hitEffects[(int)hel], new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);
        }
        else
        {
            fx = Instantiate(hitEffects[(int)hel], new Vector3(pos.x, pos.y, pos.z), Quaternion.Euler(0f, 180f, 0f));
        }

        Destroy(fx, time);
    }

    IEnumerator OneShotEnd(GameObject fx, float fallbackLifeTime, bool deactivate = false)
    {
        var ps = fx.GetComponentInChildren<ParticleSystem>(true);

        if(ps != null)
        {
            yield return new WaitUntil(() => !ps.IsAlive(true));
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(.01f, fallbackLifeTime));
        }

        EndFX(fx, deactivate);
    }
    IEnumerator LoopStopEmissionAfterTimeParam(GameObject fx, float time, bool deactivate = false)
    {
        var pss = fx.GetComponentsInChildren<ParticleSystem>(true);

        yield return new WaitForSeconds(time);

        foreach(var e in pss)
        {
            var em = e.emission;
            em.enabled = false;
        }

        yield return new WaitForSeconds(1f);

        EndFX(fx, deactivate);
    }

    void EndFX(GameObject fx, bool deactivate)
    {
        if(deactivate) fx.SetActive(false);
        else Destroy(fx.gameObject);
    }
}
