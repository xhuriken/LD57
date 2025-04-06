using System.Collections.Generic;
using UnityEngine;

public interface ICraftableBall
{
    // Acc�s au transform de la balle
    Transform Transform { get; }
    // Propri�t� indiquant le type de balle (ex: "RedBall", "BlueBall")
    string CraftBallType { get; }
    // M�thode pour d�sactiver le visuel de s�lection
    void CancelCraftingVisual();
    // (Optionnel) M�thode pour appliquer une force ou un effet (ici, le d�placement se fera via tween)
    void ApplyCraftForce(Vector2 direction);
}

[System.Serializable]
public class BallRequirement
{
    public string ballType; // Par exemple "RedBall" ou "BlueBall"
    public int count;       // Nombre requis
}

[System.Serializable]
public class CraftRecipe
{
    public string recipeName;
    public List<BallRequirement> requirements; // La composition requise
    public GameObject previewPrefab;           // Pr�visualisation affich�e si la recette est match�e
    public GameObject finalPrefab;             // Objet final � instancier lors du craft
}