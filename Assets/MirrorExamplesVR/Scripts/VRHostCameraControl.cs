using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR;
public class VRHostCameraControl : NetworkBehaviour
{
    // 在Inspector中指定服务器的摄像机对象
    public Transform serverCameraTransform;
    //public Transform serverPlayerTransform;

    public Transform leftHandPosition;
    public Transform rightHandPosition;
    private TMP_Dropdown dropdownfps;
    public TMP_Text fpsText;
    public TMP_Dropdown mask;
    public GameObject panel;
    // 用于存储接收到的数据，可能还需要进一步处理
    private Vector3 receivedPosition;
    private Quaternion receivedRotation;

    private GameObject maincamera;
    private GameObject subcamera;
    double startTime;
    int count;
    private float nextFrameTime = 0.0f;
    InputDevice leftHandDevice;
    InputDevice rightHandDevice;
    public Transform Centerposition;
    //public Quaternion ClientCenterRotation;
    // Send the position and rotation of main camera to all clients from the server 
    [SyncVar]
    private Vector3 syncedPosition;
    [SyncVar]
    private Quaternion syncedRotation;
    [SyncVar]
    private Vector3 ServerCenterposition;
    [SyncVar]
    private Quaternion ServerCenterRatation;
    // Send the fps message to all clients from the server 
    [SyncVar]
    private float frameRateInterval;
    [SyncVar]
    private int masktype;
    
    void Start(){
        maincamera = GameObject.FindWithTag("MainCamera");
        subcamera = GameObject.FindWithTag("SubCamera");
        //leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        //rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        dropdownfps = GameObject.FindGameObjectWithTag("Fpselect").GetComponent<TMP_Dropdown>();
        frameRateInterval = 1.0f / 3.0f;
        fpsText = GameObject.FindGameObjectWithTag("Fpsdisplay").GetComponent<TMP_Text>();
        masktype = mask.value;
    }
    void Update()
    {
        if (isServer)
        {
            // Update the position and rotation of main camera to all clients from the server 
            // RpcUpdateClientCamera(serverCameraTransform.position, serverCameraTransform.rotation);
            // RpcUpdateClientCamera(Camera.main.transform.position, Camera.main.transform.rotation);
            syncedPosition = maincamera.transform.position;
            syncedRotation = maincamera.transform.rotation;
            ServerCenterposition = Centerposition.position;
            ServerCenterRatation = Centerposition.rotation;
            // Update the Client fps to all clients from the server 
            dropdownfps.onValueChanged.AddListener(delegate {
                DropdownValueChanged(dropdownfps);
            });
            mask.onValueChanged.AddListener(delegate {
                masktype = mask.value;
            });
        }
        if (isClient && !isServer) // Make sure the change is on clients
        {
            if (Time.time < nextFrameTime)
            {
                return;
                
            }else{
                // Change the view data of all clients

                // Camera.main.transform.parent.position = receivedPosition;
                // Camera.main.transform.parent.rotation = receivedRotation;
                // maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
                maincamera.GetComponent<TrackedPoseDriver>().enabled = false;
                // Camera.main.transform.parent.position = new Vector3(syncedPosition.x, 0, syncedPosition.z);
            
                // Camera.main.transform.parent.rotation = syncedRotation;
                // maincamera.transform.position = new Vector3(syncedPosition.x, syncedPosition.y, syncedPosition.z);
                maincamera.transform.position = syncedPosition;
                // maincamera.transform.parent.position = new Vector3(syncedPosition.x, syncedPosition.y-1.3614f, syncedPosition.z);
                maincamera.transform.rotation = syncedRotation;
                Centerposition.position = ServerCenterposition;
                Centerposition.rotation = ServerCenterRatation;
                //ClientCenterRotation = ServerCenterRatation;
                Debug.Log(Centerposition.name);
                //leftHandPosition.position += syncedPosition;
                //rightHandPosition.position += syncedPosition;
                switch(masktype){
                    case 0:
                        panel.GetComponent<Volume>().enabled = false;
                        panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                        break;
                    case 1:
                        panel.GetComponent<Volume>().enabled = true;
                        panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                        break;
                    case 2:
                        panel.GetComponent<Volume>().enabled = false;
                        panel.GetComponent<UnityEngine.UI.Image>().enabled = true;
                        break;
                    default:
                        break;
                }
                // serverPlayerTransform.position = receivedPosition;
                // serverPlayerTransform.rotation = receivedRotation;
            }
            nextFrameTime = Time.time + frameRateInterval;   
        }
        Countfps(); 
    }
    void DropdownValueChanged(TMP_Dropdown change)
    {
        // Change the interval between two frames
        // frameRateInterval = 1.0f / ((change.value + 1) * 5);
        switch(change.value){
            case 0:
                frameRateInterval = 1.0f / 3.0f;
                break;
            case 1:
                frameRateInterval = 1.0f / 5.0f;
                break;
            case 2:
                frameRateInterval = 1.0f / 10.0f;
                break;
            case 3:
                frameRateInterval = 1.0f / 15.0f;
                break;
            default:
                break;
        }

    }
    void Countfps(){
        ++count;
        if (Time.time >= startTime + 1)
        {
            float fps = count;
            startTime = Time.time;
            count = 0;
            fpsText.text = $"{fps:0.} fps";
        }
    }
    [ClientRpc]
    void RpcUpdateClientCamera(Vector3 position, Quaternion rotation)
    {
        // Receive the position and rotation for all clients from Server 
        //receivedPosition = position;
        //receivedRotation = rotation;
    }
    
}
