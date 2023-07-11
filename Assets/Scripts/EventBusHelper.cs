using UnityEngine;

public static class EventBusHelper
{
    public static EventBus GetEventBus(EventBus eventBus = null)
    {
        if(eventBus != null) { return eventBus; }
        return GameObject.FindWithTag("EventBus").GetComponent<EventBus>();
    }
}
