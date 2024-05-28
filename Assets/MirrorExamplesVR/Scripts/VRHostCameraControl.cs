using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using Unity.VisualScripting;
public class VRHostCameraControl : NetworkBehaviour
{
    // public Transform serverCameraTransform;
    private TMP_Dropdown dropdownfps;
    public TMP_Text fpsText;
    public TMP_Dropdown mask;
    public GameObject panel;
    private GameObject maincamera;
    // private GameObject subcamera;
    double startTime;
    int count;
    private float nextFrameTime = 0.0f;
    // InputDevice leftHandDevice;
    // InputDevice rightHandDevice;
    public Transform Centerposition;
    private ServerActionRecording actionrecord;
    private int index = 0;
    // Send the action of main camera to all clients
    [SyncVar]
    private Vector3 syncedPosition;
    [SyncVar]
    private Quaternion syncedRotation;
    // Send the action of VR origin to all clients
    [SyncVar]
    private Vector3 ServerCenterposition;
    [SyncVar]
    private Quaternion ServerCenterRatation;
    // Send the fps message to all clients
    [SyncVar]
    private float frameRateInterval;
    // Send the masktype to all clients
    [SyncVar]
    private int masktype;
    
    void Start(){
        maincamera = GameObject.FindWithTag("MainCamera");

        actionrecord = GameObject.FindObjectOfType<ServerActionRecording>().GetComponent<ServerActionRecording>();

        dropdownfps = GameObject.FindGameObjectWithTag("Fpselect").GetComponent<TMP_Dropdown>();

        frameRateInterval = 1.0f / 3.0f;

        fpsText = GameObject.FindGameObjectWithTag("Fpsdisplay").GetComponent<TMP_Text>();

        masktype = mask.value;
    }
    void Update()
    {
        if (isServer)
        {
            // Get the action of main camera from the Server
            syncedPosition = maincamera.transform.position;
            syncedRotation = maincamera.transform.rotation;

            // Get the action of VR origin from the Server
            ServerCenterposition = Centerposition.position;
            ServerCenterRatation = Centerposition.rotation;
            
            // Update the Client fps for all clients by the choice of the server 
            dropdownfps.onValueChanged.AddListener(delegate {
                DropdownValueChanged(dropdownfps);
            });

            // Update the Client mask for all clients by the choice of the server 
            mask.onValueChanged.AddListener(delegate {
                masktype = mask.value;
                // UpdateClientMask(mask.value);
            });
        }
        if (isClient && !isServer) // Make sure the change is on clients
        {
            // int index = 0;
            if (Time.time < nextFrameTime)
            {
                return;
                
            }else{
                // Change the view data of all clients

                // Close the action control of the Client HMD
                // maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
                maincamera.GetComponent<TrackedPoseDriver>().enabled = false;
                if(actionrecord.GetCamerapositions().Count > 0){
                    // Control the action of the Clients' main camera by the data from the Server
                    maincamera.transform.position = actionrecord.GetCamerapositions()[index];
                    Debug.Log(index);
                    maincamera.transform.rotation = actionrecord.GetCamerarotations()[index];
                    
                    // Control the action of the Clients' VR origin by the data from the Server
                    // Centerposition.position = ServerCenterposition;
                    // Centerposition.rotation = ServerCenterRatation;
                    index += (60 * frameRateInterval).ConvertTo<int>(); 
                    Debug.Log(actionrecord.GetCamerapositions().Count);
                    if(index >= actionrecord.GetCamerapositions().Count){
                        actionrecord.ClearCameraposition();
                        actionrecord.ClearCamerarotation();
                        index = 0;
                    }
                }else{
                    // Control the action of the Clients' VR origin by the data from the Server
                    Centerposition.position = ServerCenterposition;
                    Centerposition.rotation = ServerCenterRatation;

                    // Control the action of the Clients' main camera by the data from the Server
                    maincamera.transform.position = syncedPosition;
                    maincamera.transform.rotation = syncedRotation;
                    Debug.Log(actionrecord.GetCamerapositions().Count);
                }

                UpdateClientMask(masktype);
                
            }
            nextFrameTime = Time.time + frameRateInterval;   
        }
        Countfps(); 
    }
    void DropdownValueChanged(TMP_Dropdown change)
    {
        // Change the interval between two frames
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
    //[ClientRpc]
    void UpdateClientMask(int type)
    {
        // Change the Mask of the Clients by the Choice Selected by the Server 
        switch(type){
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
    }
    
}
