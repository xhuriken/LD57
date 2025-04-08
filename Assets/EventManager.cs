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
        eventCheckTimer += Time.deltaTime;
        if (eventCheckTimer >= eventCheckInterval)
        {
            eventCheckTimer = 0f;
            CheckAllEvents();
        }
    }

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


    private bool EvaluateEventCondition(GameEvent ge)
    {
        if (ge.eventName == "GrayVioletBall")
        {
            var redBalls = FindObjectsOfType<RedBall>();
            return (redBalls.Length >= 10);
        }
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
