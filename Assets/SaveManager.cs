using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private string filePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "sceneData.json");
        Debug.Log("Fichier de sauvegarde : " + filePath);
    }

    private void Start()
    {
        // Charge la sauvegarde au d�marrage si elle existe
        if (File.Exists(filePath))
        {
            LoadScene();
        }
    }

    private void Update()
    {
        // Pour tester la sauvegarde et le chargement via S et L
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveScene();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadScene();
        }
    }

    public void SaveScene()
    {
        SceneData sceneData = new SceneData();
        sceneData.playerPoints = GameManager.Instance.globalNumber;

        // R�cup�rer tous les SaveableObject pr�sents dans la sc�ne
        SaveableObject[] saveableObjects = GameObject.FindObjectsOfType<SaveableObject>();
        foreach (SaveableObject so in saveableObjects)
        {
            // Ne sauvegarder que ceux marqu�s comme devant �tre sauvegard�s
            if (!so.ShouldSave)
                continue;

            TransformData data = new TransformData();
            data.id = so.UniqueId;
            data.position = so.transform.position;
            data.rotation = so.transform.rotation;
            data.scale = so.transform.localScale;
            data.prefabPath = so.PrefabPath;
            sceneData.transforms.Add(data);
        }

        string json = JsonUtility.ToJson(sceneData, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Sc�ne sauvegard�e !");
    }

    public void LoadScene()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("Aucune sauvegarde trouv�e.");
            return;
        }

        string json = File.ReadAllText(filePath);
        SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

        GameManager.Instance.globalNumber = sceneData.playerPoints;
        GameManager.Instance.numberText.text = sceneData.playerPoints.ToString();

        SaveableObject[] currentSaveables = GameObject.FindObjectsOfType<SaveableObject>();
        Dictionary<string, SaveableObject> saveableDict = new Dictionary<string, SaveableObject>();
        foreach (SaveableObject so in currentSaveables)
        {
            saveableDict[so.UniqueId] = so;
        }

        HashSet<string> savedIds = new HashSet<string>();
        foreach (TransformData data in sceneData.transforms)
        {
            savedIds.Add(data.id);

            if (saveableDict.ContainsKey(data.id))
            {
                SaveableObject so = saveableDict[data.id];
                so.transform.position = data.position;
                so.transform.rotation = data.rotation;
                so.transform.localScale = data.scale;
            }
            else
            {
                Debug.LogWarning("Objet non trouv� pour l'id : " + data.id + ". Instanciation du prefab.");
                if (!string.IsNullOrEmpty(data.prefabPath))
                {
                    GameObject prefab = Resources.Load<GameObject>(data.prefabPath);
                    if (prefab != null)
                    {
                        GameObject newObj = Instantiate(prefab, data.position, data.rotation);
                        newObj.transform.localScale = data.scale;
                        SaveableObject newSO = newObj.GetComponent<SaveableObject>();
                        if (newSO != null)
                        {
                            newSO.SetUniqueId(data.id);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Prefab non trouv� dans Resources pour le chemin : " + data.prefabPath);
                    }
                }
            }
        }

        // D�truire les objets pr�sents qui n'�taient pas sauvegard�s
        foreach (KeyValuePair<string, SaveableObject> kvp in saveableDict)
        {
            if (!savedIds.Contains(kvp.Key))
            {
                Debug.Log("Destruction de l'objet non sauvegard� : " + kvp.Value.gameObject.name);
                Destroy(kvp.Value.gameObject);
            }
        }
        Debug.Log("Sc�ne charg�e !");
    }
}
