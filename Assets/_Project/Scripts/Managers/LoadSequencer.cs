using System;
using System.Collections.Generic;
using UnityEngine;

public class LoadSequencer : MonoBehaviour
{
    [SerializeField] private List<GameObject> _gameObjects = new List<GameObject>();

    public static event Action LastModuleLoaded;

    public static void StartLastModuleLoaded()
    {
        LastModuleLoaded?.Invoke();
    }

    private void Awake()
    {
        foreach (var item in _gameObjects)
        {
            GameObject obj = Instantiate(item);
            obj.SetActive(true);

            IGameModule module = obj.GetComponent<IGameModule>();
            if (module != null)
                module.Load();
        }

        StartLastModuleLoaded();
    }
}
