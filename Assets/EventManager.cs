using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class GameEvent
{
    public string eventName;
    public bool isDone = false;
    public UnityEvent eventAction;
}

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public List<GameEvent> gameEvents = new List<GameEvent>();

    private float eventCheckTimer = 0f;
    private float eventCheckInterval = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // ADDED: On accumule le temps, et toutes les X secondes, on check les événements
        eventCheckTimer += Time.deltaTime;
        if (eventCheckTimer >= eventCheckInterval)
        {
            eventCheckTimer = 0f;
            CheckAllEvents();
        }
    }

    // ADDED: Parcourt tous les événements, si un event n'est pas fait ET que sa condition est remplie, on le déclenche
    private void CheckAllEvents()
    {
        foreach (GameEvent ge in gameEvents)
        {
            if (!ge.isDone && EvaluateEventCondition(ge))
            {
                TriggerEvent(ge.eventName);
            }
        }
    }

    // ADDED: Gère la condition de chaque événement
    // Ici, on montre la condition "GrayVioletBall" => 2 RedBall présents
    private bool EvaluateEventCondition(GameEvent ge)
    {
        // Si l'événement s'appelle "GrayVioletBall"
        if (ge.eventName == "GrayVioletBall")
        {
            // On check le nombre de RedBall dans la scène
            var redBalls = FindObjectsOfType<RedBall>();
            // Retourne true si exactement 2 RedBall sont présents
            return (redBalls.Length == 2);
        }
        // Tu peux ajouter d'autres conditions ici (else if ...)
        return false;
    }

    public void TriggerEvent(string eventName)
    {
        GameEvent ge = gameEvents.Find(e => e.eventName == eventName);
        if (ge != null && !ge.isDone)
        {
            ge.isDone = true;
            if (ge.eventAction != null)
            {
                ge.eventAction.Invoke();
            }
            Debug.Log("[EventManager] Event triggered: " + eventName);
        }
    }

    public void ResetEvent(string eventName)
    {
        GameEvent ge = gameEvents.Find(e => e.eventName == eventName);
        if (ge != null)
        {
            ge.isDone = false;
        }
    }
}
