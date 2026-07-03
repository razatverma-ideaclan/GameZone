using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoSceneBuilder
{
    static AutoSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            // Run once per compilation/assembly reload session
            if (!SessionState.GetBool("AutoSceneBuilt_V4", false))
            {
                SessionState.SetBool("AutoSceneBuilt_V4", true);
                Debug.Log("AutoSceneBuilder: Automatically building scene on delayCall...");
                FlappyBirdSceneBuilder.BuildScene();
                Debug.Log("AutoSceneBuilder: Automatically built scene successfully!");
            }
        };
    }
}
