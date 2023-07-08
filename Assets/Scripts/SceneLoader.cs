using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private const string loadingSceneName = "LoadingScene";
    // The name of the final destination scene.
    private string destinationSceneName;
    // The time to wait before switching to the loading scene.
    public float waitTime = 0.5f;
    // The progress of the scene loading.
    private float progress;
    // The async operation for loading and unloading scenes.
    private AsyncOperation asyncOperation;
    private int currentSceneIndex;

    public void LoadScene(string sceneName)
    {
        destinationSceneName = sceneName;
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Start the coroutine to load the loading scene
        //StartCoroutine(LoadNewScene());
        SceneManager.LoadScene(destinationSceneName);
    }

    // A coroutine that loads the loading scene after a delay
    IEnumerator LoadLoadingScene()
    {
        // Wait for the wait time
        yield return new WaitForSeconds(waitTime);

        // Load the loading scene asynchronously and additively
        asyncOperation = SceneManager.LoadSceneAsync(loadingSceneName, LoadSceneMode.Additive);

        // Wait for the loading scene to be ready
        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        // Set the loading scene as active
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(loadingSceneName));

        asyncOperation = SceneManager.UnloadSceneAsync(currentSceneIndex);

        // Start the coroutine to load the destination scene
        StartCoroutine(LoadDestinationScene());
    }

    // A coroutine that loads the destination scene after the loading scene is active
    IEnumerator LoadDestinationScene()
    {
        // Load the destination scene asynchronously but don't activate it yet
        asyncOperation = 
            SceneManager.LoadSceneAsync(destinationSceneName, LoadSceneMode.Additive);
        asyncOperation.allowSceneActivation = false;

        // Update the progress of the scene loading
        while (!asyncOperation.isDone)
        {
            progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

            // You can use the progress variable to update a UI element or do something else

            // If the scene is ready, activate it
            if (asyncOperation.progress >= 0.9f)
            {
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Unload the loading scene asynchronously
        asyncOperation = SceneManager.UnloadSceneAsync(loadingSceneName);
        Destroy(player);
        player = GameObject.FindGameObjectWithTag("Player");
    }

    IEnumerator LoadNewScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        asyncOperation = 
            SceneManager.LoadSceneAsync(destinationSceneName);
        while(!asyncOperation.isDone) { yield return null; }
        asyncOperation = SceneManager.UnloadSceneAsync(currentSceneIndex);
    }
}
