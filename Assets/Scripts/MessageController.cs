using UnityEngine;
/// <summary>
/// 消息分发及动作监听
/// </summary>
/// <param name="str">message</param>
public delegate void ObserverMessageRecv(string str);
public class MessageController : MonoBehaviour
{
    #region 全局消息处理
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
        Debug.Log(string.Format("<color=#00DEFF>[事件监听消息]：{0:G}</color>", msg));
    }
    #endregion
}
