﻿using System.Collections.Generic;
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
        if (File.Exists(filePath))
        {
            LoadScene();
        }
    }

    private void Update()
    {
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

        SaveableObject[] saveableObjects = GameObject.FindObjectsOfType<SaveableObject>();
        foreach (SaveableObject so in saveableObjects)
        {
            if (!so.ShouldSave)
                continue;

            TransformData data = new TransformData();
            data.id = so.UniqueId;
            data.position = so.transform.position;
            data.rotation = so.transform.rotation;
            data.scale = so.transform.localScale;
            data.prefabPath = so.PrefabPath;

            ISaveData saveData = so.GetComponent<ISaveData>();
            if (saveData != null)
            {
                data.clickCount = saveData.ClickCount;
            }
            else
            {
                data.clickCount = 0;
            }

            sceneData.transforms.Add(data);
        }

        string json = JsonUtility.ToJson(sceneData, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Scène sauvegardée !");
    }

    public void LoadScene()
    {
        if (!File.Exists(filePath))
        {
            Debug.Log("Aucune sauvegarde trouvée.");
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

                ISaveData saveData = so.GetComponent<ISaveData>();
                if (saveData != null)
                {
                    saveData.ClickCount = data.clickCount;
                }
            }
            else
            {
                Debug.LogWarning("Objet non trouvé pour l'id : " + data.id + ". Instanciation du prefab.");
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
                        ISaveData newSaveData = newObj.GetComponent<ISaveData>();
                        if (newSaveData != null)
                        {
                            newSaveData.ClickCount = data.clickCount;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Prefab non trouvé dans Resources pour le chemin : " + data.prefabPath);
                    }
                }
            }
        }

        foreach (KeyValuePair<string, SaveableObject> kvp in saveableDict)
        {
            if (!savedIds.Contains(kvp.Key))
            {
                Debug.Log("Destruction de l'objet non sauvegardé : " + kvp.Value.gameObject.name);
                Destroy(kvp.Value.gameObject);
            }
        }
        Debug.Log("Scène chargée !");
    }
}
