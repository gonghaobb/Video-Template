using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class JumpScene : MonoBehaviour
{
    [SerializeField] int _sceneIndex;
    [SerializeField] InputAction _ActionDown;
    private void Awake()
    {
        _ActionDown.performed += ctx => { OnTriggleDown(ctx); };
    }

    private void OnTriggleDown(InputAction.CallbackContext ctx)
    {
        SceneManager.LoadScene(_sceneIndex);
    }
    public void OnEnable()
    {
        _ActionDown.Enable();
    }
    public void OnDisable()
    {
        _ActionDown.Disable();
    }
}
