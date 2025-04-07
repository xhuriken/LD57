using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SaveableObject : MonoBehaviour
{
    [SerializeField] private string uniqueId;
    [SerializeField] private string prefabPath;
    [SerializeField] private bool shouldSave = true; 

    public string UniqueId => uniqueId;
    public string PrefabPath => prefabPath;
    public bool ShouldSave { get { return shouldSave; } set { shouldSave = value; } }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
        if (string.IsNullOrEmpty(prefabPath))
        {
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

    public void SetUniqueId(string id)
    {
        uniqueId = id;
    }
}
