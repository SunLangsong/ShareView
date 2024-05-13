using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR;
public class VRHostCameraControl : NetworkBehaviour
{
    // 在Inspector中指定服务器的摄像机对象
    public Transform serverCameraTransform;
    //public Transform serverPlayerTransform;

    public Transform leftHandPosition;
    public Transform rightHandPosition;
    private TMP_Dropdown dropdownfps;
    // 用于存储接收到的数据，可能还需要进一步处理
    private Vector3 receivedPosition;
    private Quaternion receivedRotation;

    private GameObject maincamera;
    private GameObject subcamera;
    private float nextFrameTime = 0.0f;
    private float frameRateInterval;
    InputDevice leftHandDevice;
    InputDevice rightHandDevice;
    [SyncVar]
    private Vector3 syncedPosition;
    [SyncVar]
    private Quaternion syncedRotation;
    
    void Start(){
        maincamera = GameObject.FindWithTag("MainCamera");
        subcamera = GameObject.FindWithTag("SubCamera");
        leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        dropdownfps = GameObject.FindGameObjectWithTag("Fpselect").GetComponent<TMP_Dropdown>();
        frameRateInterval = 1.0f / ((dropdownfps.value + 1) * 5);
    }
    void Update()
    {
        if (isServer)
        {
            // 每个更新周期都发送服务器摄像机的当前位置和旋转
            // Debug.Log("Running as Server.");
            // Debug.Log(serverCameraTransform);
            // RpcUpdateClientCamera(serverCameraTransform.position, serverCameraTransform.rotation);
            //RpcUpdateClientCamera(Camera.main.transform.position, Camera.main.transform.rotation);
            syncedPosition = maincamera.transform.position;
            syncedRotation = maincamera.transform.rotation;
            
        }
        if (isClient && !isServer) // 确保这不是主机，主机不需要从自身接收更新
        {
            if (Time.time < nextFrameTime)
            {
                return;
                
            }else{
                // 在客户端上，将接收到的位置和旋转应用于客户端的主摄像机
                // Debug.Log(receivedPosition);
                //Camera.main.transform.parent.position = receivedPosition;
                //Camera.main.transform.parent.rotation = receivedRotation;
                //maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
                maincamera.GetComponent<TrackedPoseDriver>().enabled = false;
                //Camera.main.transform.parent.position = new Vector3(syncedPosition.x, 0, syncedPosition.z);
            
                //Camera.main.transform.parent.rotation = syncedRotation;
                //maincamera.transform.position = new Vector3(syncedPosition.x, syncedPosition.y, syncedPosition.z);
                maincamera.transform.position = syncedPosition;
                //maincamera.transform.parent.position = new Vector3(syncedPosition.x, syncedPosition.y-1.3614f, syncedPosition.z);
                maincamera.transform.rotation = syncedRotation;
                //leftHandPosition.position += syncedPosition;
                //rightHandPosition.position += syncedPosition;
                //leftHandDevi;
                //Debug.Log(frameRateInterval);
                frameRateInterval = 1.0f / ((dropdownfps.value + 1) * 5);
                //serverPlayerTransform.position = receivedPosition;
                //serverPlayerTransform.rotation = receivedRotation;
            }
            nextFrameTime = Time.time + frameRateInterval;        
        }
    }

    [ClientRpc]
    void RpcUpdateClientCamera(Vector3 position, Quaternion rotation)
    {
        // 在所有客户端上接收并存储服务器摄像机的位置和旋转
        receivedPosition = position;
        receivedRotation = rotation;
    }
    
}
