using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.6f, 0.7f)] // 給它一個不同的顏色，例如青色
public class CameraControlNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    [Header("攝影機目標")]
    [Tooltip("將場景中代表目標鏡位的空物件的「名字」填寫在這裡")]
    public string targetTransformName;

    [Tooltip("攝影機移動到目標位置所需的時間（秒）")]
    public float transitionDuration = 1.0f;
    
    [Tooltip("是否要等待攝影機移動完成後，才繼續下一個節點？")]
    public bool waitForTransition = true;

    protected override void Init()
    {
        base.Init();
        name = "Camera Control";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}