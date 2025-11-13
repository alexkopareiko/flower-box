using UnityEngine;

public interface IGameModule
{
    public abstract void Load();

    public bool IsLoaded { get; } 
}
