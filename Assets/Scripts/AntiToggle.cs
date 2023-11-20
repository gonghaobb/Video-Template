using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class AntiToggle : MonoBehaviour
{
    [SerializeField] private UnityEvent<bool> onValueChanged;
    private Toggle m_Toggle;

    private void Awake()
    {
        m_Toggle = GetComponent<Toggle>();
        m_Toggle.onValueChanged.AddListener(AntiValueChanged);
    }

    private void AntiValueChanged(bool value)
    {
        onValueChanged.Invoke(!value);
    }
}
