using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private string objectTag;
    [SerializeField] private GameObject objectPrefab;
    
    private void Awake()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(objectTag);
        if(obj == null)
        {
            obj = Instantiate(objectPrefab, transform.position, Quaternion.identity);
        }
    }
}
