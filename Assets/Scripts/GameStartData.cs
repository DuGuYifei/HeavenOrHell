using System;
using System.Collections.Generic;
using MapGeneration;
using Message;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class GameStartData: MonoBehaviour
{
    #region Singleton
    
    private static GameStartData instance;

    public static GameStartData Instance
    {
        get => instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this);
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    #endregion

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == Consts.GameScene)
        {
            instance = null;
            DestroyImmediate(gameObject);
        }
    }

    public List<CharacterData> characters = new ();
    public MapInfoContainer mapInfoContainer;
    public int playerId;
}


[Serializable]
public class CharacterData
{
    public int id;
    public CharacterType type;
    public bool isPlayer;
    public Vector3 spawnPosition;
}