using UnityEngine;
using UnityEngine.SceneManagement;

public class GotoScene : MonoBehaviour
{
    public void Go(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
