using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SaveableObject : MonoBehaviour
{
    [SerializeField] private string uniqueId;
    [SerializeField] private string prefabPath; // Chemin relatif dans Resources pour recréer l'objet
    [SerializeField] private bool shouldSave = true; // Détermine si cet objet doit être sauvegardé

    public string UniqueId => uniqueId;
    public string PrefabPath => prefabPath;
    public bool ShouldSave { get { return shouldSave; } set { shouldSave = value; } }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Génère un ID unique si le champ est vide (en mode édition)
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
        // Si le chemin de prefab n'est pas défini, on peut le définir ici par défaut.
        if (string.IsNullOrEmpty(prefabPath))
        {
            // On suppose que le nom de l'objet correspond au nom du prefab dans Resources.
            prefabPath = gameObject.name;
            EditorUtility.SetDirty(this);
        }
    }
#endif

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }
    }

    // Permet d'attribuer un ID lors de l'instanciation d'un objet manquant
    public void SetUniqueId(string id)
    {
        uniqueId = id;
    }
}
