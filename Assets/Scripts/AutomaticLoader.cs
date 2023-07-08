using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutomaticLoader : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;

    private void Start()
    {
        sceneLoader.LoadScene("Space");
    }
}
