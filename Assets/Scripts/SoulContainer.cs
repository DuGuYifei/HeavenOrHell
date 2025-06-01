using UnityEngine;

public class SoulContainer : CharacterContainer
{
    [SerializeField] private SoulType soulType;

    #region Properties

    public SoulType SoulType => soulType;

    #endregion
}

public enum SoulType
{
    Dog,
    Psychologist,
    Detective
}