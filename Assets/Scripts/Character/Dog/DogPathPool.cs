using System.Collections.Generic;
using Character;
using UnityEngine;

public class DogPathPool : MonoBehaviour
{
    public DogPathContainer prefab;
    public int initialPoolSize = 50;
    public List<Color> pathColors;

    private readonly Queue<DogPathContainer> _pool = new();

    private void Start()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    public DogPathContainer GetFromPool(int charId)
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);
            obj.SetColor(pathColors[charId % pathColors.Count]);
            return obj;
        }

        var newObj = Instantiate(prefab, transform);
        newObj.SetColor(pathColors[charId % pathColors.Count]);
        return newObj;
    }

    public void ReturnToPool(DogPathContainer obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }
}