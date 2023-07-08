using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit : MonoBehaviour
{
    [SerializeField] private float orbitSpeed = 20f;

    private void Update()
    {
        transform.RotateAround(Vector3.zero, Vector3.up, orbitSpeed * Time.deltaTime);
    }
}
