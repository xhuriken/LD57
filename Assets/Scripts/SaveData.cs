using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TransformData
{
    public string id;             
    public Vector3 position;    
    public Quaternion rotation; 
    public Vector3 scale;         
    public string prefabPath;   
    public int clickCount;        
}

[Serializable]
public class SceneData
{
    public int playerPoints;
    public bool neverCraft;
    public List<TransformData> transforms = new List<TransformData>(); 
}
