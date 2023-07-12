using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventBus : MonoBehaviour
{
    public UnityEvent OnRecordingStarted;
    public UnityEvent OnRecordingEnded;
    public UnityEvent OnFollowRequested;
    public UnityEvent OnStayRequested;
}
