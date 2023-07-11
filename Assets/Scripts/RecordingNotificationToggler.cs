using UnityEngine;

public class RecordingNotificationToggler : MonoBehaviour
{
    [SerializeField] private GameObject recordingNotifcation;
    [SerializeField] private EventBus eventBus;

    private void Start()
    {
        eventBus = EventBusHelper.GetEventBus(eventBus);
        eventBus.OnRecordingStarted.AddListener(EnableRecordingNotification);
        eventBus.OnRecordingEnded.AddListener(DisableRecordingNotification);
    }

    private void EnableRecordingNotification()
    {
        recordingNotifcation.SetActive(true);
    }

    private void DisableRecordingNotification()
    {
        recordingNotifcation.SetActive(false);
    }
}
