using System.Collections.Generic;
using UnityEngine;

public interface ICraftableBall
{
    Transform Transform { get; }
    string CraftBallType { get; }
    void CancelCraftingVisual();

    //Inused now
    void ApplyCraftForce(Vector2 direction);
}

[System.Serializable]
public class BallRequirement
{
    public string ballType;
    public int count;      
}

[System.Serializable]
public class CraftRecipe
{
    public string recipeName;
    public List<BallRequirement> requirements; 
    public GameObject previewPrefab;           
    public GameObject finalPrefab;             
}