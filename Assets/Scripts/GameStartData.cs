using System;
using System.Collections.Generic;
using MapGeneration;
using Message;
using UnityEngine;

[Serializable]
public class GameStartData
{
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