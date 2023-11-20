using UnityEngine;
/// <summary>
/// ��Ϣ�ַ�����������
/// </summary>
/// <param name="str">message</param>
public delegate void ObserverMessageRecv(string str);
public class MessageController : MonoBehaviour
{
    #region ȫ����Ϣ����
    public static event ObserverMessageRecv ObserverEvent;
    private static MessageController instance;
    public static MessageController GetInstance()
    {
        if (MessageController.instance == null)
            MessageController.instance = (Object.FindObjectOfType(typeof(MessageController)) as MessageController);
        return instance;
    }
    public void SetMessage(string msg)
    {
        ObserverEvent(msg);
        Debug.Log(string.Format("<color=#00DEFF>[�¼�������Ϣ]��{0:G}</color>", msg));
    }
    #endregion
}
