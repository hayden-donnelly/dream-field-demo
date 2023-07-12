using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Follower : MonoBehaviour
{
    [SerializeField] private EventBus eventBus;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float followDistance = 6f;
    private bool isFollowing = false;
    private NavMeshAgent agent;
    private Transform target;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        eventBus = EventBusHelper.GetEventBus(eventBus);
        eventBus.OnFollowRequested.AddListener(() => { isFollowing = true; });
        eventBus.OnStayRequested.AddListener(() => 
        { 
            isFollowing = false; 
            agent.SetDestination(transform.position);
        });
        target = GameObject.FindGameObjectWithTag(targetTag).transform;
    }

    private void Update()
    {
        if(isFollowing) { FollowTarget(); }
    }

    private void FollowTarget()
    {
        if(target == null) 
        {
            Debug.LogWarning("No follow target found");
            return; 
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if(distanceToTarget <= followDistance) 
        {
            agent.SetDestination(transform.position);
            return;
        }
        agent.SetDestination(target.position);
    }
}
