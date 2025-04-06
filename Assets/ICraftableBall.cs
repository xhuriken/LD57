using System.Collections.Generic;
using UnityEngine;

public interface ICraftableBall
{
    // Accès au transform de la balle
    Transform Transform { get; }
    // Propriété indiquant le type de balle (ex: "RedBall", "BlueBall")
    string CraftBallType { get; }
    // Méthode pour désactiver le visuel de sélection
    void CancelCraftingVisual();
    // (Optionnel) Méthode pour appliquer une force ou un effet (ici, le déplacement se fera via tween)
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
    public GameObject previewPrefab;           // Prévisualisation affichée si la recette est matchée
    public GameObject finalPrefab;             // Objet final à instancier lors du craft
}