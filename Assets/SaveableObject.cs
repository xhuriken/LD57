using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SaveableObject : MonoBehaviour
{
    [SerializeField] private string uniqueId;
    [SerializeField] private string prefabPath; // Chemin relatif dans Resources pour recr�er l'objet
    [SerializeField] private bool shouldSave = true; // D�termine si cet objet doit �tre sauvegard�

    public string UniqueId => uniqueId;
    public string PrefabPath => prefabPath;
    public bool ShouldSave { get { return shouldSave; } set { shouldSave = value; } }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // G�n�re un ID unique si le champ est vide (en mode �dition)
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
        // Si le chemin de prefab n'est pas d�fini, on peut le d�finir ici par d�faut.
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
