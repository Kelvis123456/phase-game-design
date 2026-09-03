using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Object pool para ParticleSystem. Llama Play(key, position, color).
// Definir las entradas en el Inspector y asignar prefabs.
public class VFXPool : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public string key;
        public ParticleSystem prefab;
        [Range(1, 10)] public int preloadCount;
    }

    [SerializeField] private Entry[] _entries;

    private Dictionary<string, Queue<ParticleSystem>> _pools;
    private Dictionary<string, ParticleSystem> _prefabs;

    private void Awake()
    {
        Services.Register(this);
        _pools = new Dictionary<string, Queue<ParticleSystem>>();
        _prefabs = new Dictionary<string, ParticleSystem>();

        foreach (var e in _entries)
        {
            _prefabs[e.key] = e.prefab;
            _pools[e.key] = new Queue<ParticleSystem>();
            for (int i = 0; i < e.preloadCount; i++)
                _pools[e.key].Enqueue(CreateInstance(e.key));
        }
    }

    public void Play(string key, Vector3 position, Color color)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogWarning($"[VFXPool] Key '{key}' no encontrado.");
            return;
        }

        var ps = pool.Count > 0 ? pool.Dequeue() : CreateInstance(key);
        ps.transform.position = position;

        var main = ps.main;
        main.startColor = color;

        ps.gameObject.SetActive(true);
        ps.Play();

        float lifetime = main.duration + main.startLifetime.constantMax;
        StartCoroutine(ReturnAfter(ps, key, lifetime));
    }

    private IEnumerator ReturnAfter(ParticleSystem ps, string key, float delay)
    {
        yield return new WaitForSeconds(delay);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        _pools[key].Enqueue(ps);
    }

    private ParticleSystem CreateInstance(string key)
    {
        var ps = Instantiate(_prefabs[key], transform);
        ps.gameObject.SetActive(false);
        return ps;
    }
}
