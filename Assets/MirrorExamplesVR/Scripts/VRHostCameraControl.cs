using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using UnityEngine.UI;
public class VRHostCameraControl : NetworkBehaviour
{
    
    public GameObject panel;
    private GameObject maincamera;
    public GameObject fpsSelect;
    public GameObject maskSelect;
    public GameObject recordStart;
    public GameObject recordTime;
    public GameObject recordSelect;
    private TMP_Dropdown dropdownfps;
    public TMP_Text fpsText;
    private TMP_Dropdown mask;
    private TMP_Dropdown record;
    // The start time to count the current fps
    double startTime = 0;
    // Count how many frames have showed on the display 
    int count;
    // The time to play next frame on all clients
    private float nextFrameTime = 0.0f;

    public Transform Centerposition;
    private ServerActionRecording actionrecord;
    // The index of record to play 
    private int recordtype = 0;
    // The index of the action in the record selected
    private int index = 0;
    // Check the condition of the record selected
    private bool play = false;
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

        frameRateInterval = 1.0f / 3.0f;

        fpsText = GameObject.FindGameObjectWithTag("Fpsdisplay").GetComponent<TMP_Text>();

        if(isServer){
            // Active the fps controller 
            fpsSelect.SetActive(true);
            // Active the mask controller 
            maskSelect.SetActive(true);
            // Active the RecordButton
            recordStart.SetActive(true);
            // Active the RecordText
            recordTime.SetActive(true);
            // Initiate the fpsdropdown
            dropdownfps = fpsSelect.GetComponent<TMP_Dropdown>();
            // Initiate the maskdropdown
            mask = maskSelect.GetComponent<TMP_Dropdown>();
            // Initiate the recorddropdown
            record = recordSelect.GetComponent<TMP_Dropdown>();
            
            masktype = mask.value;

            // Update the Client fps for all clients by the choice of the server 
            dropdownfps.onValueChanged.AddListener(delegate {
                DropdownValueChanged(dropdownfps);
            });

            // Update the Client mask for all clients by the choice of the server 
            mask.onValueChanged.AddListener(delegate {
                masktype = mask.value;
            });
        }
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
                if(play == true && actionrecord.NumOfRecords() > 0){
                    // Control the action of the Clients' main camera by the data from the Server
                    maincamera.transform.position = actionrecord.GetCamerapositions(recordtype, index);
                    maincamera.transform.rotation = actionrecord.GetCamerarotations(recordtype, index);
                    
                    // Control the action of the Clients' VR origin by the data from the Server
                    // Centerposition.position = ServerCenterposition;
                    // Centerposition.rotation = ServerCenterRatation;

                    // Read the record action data with the interval of fps
                    index += (60 * frameRateInterval).ConvertTo<int>(); 

                    // If read all the action of one record, stop reading the record
                    if(index >= actionrecord.Numofactions(recordtype)){
                        index = 0;
                        play = false;
                    }

                }else{
                    // Control the action of the Clients' VR origin by the data from the Server
                    Centerposition.position = ServerCenterposition;
                    Centerposition.rotation = ServerCenterRatation;

                    // Control the action of the Clients' main camera by the data from the Server
                    maincamera.transform.position = syncedPosition;
                    maincamera.transform.rotation = syncedRotation;
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
    // Play the record
    public void PlaytheRecord(){
    
        Rectherecordtype(record.value, true, 0);
    }
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
    // Initiate the record to play
    [ClientRpc]
    private void Rectherecordtype(int type, bool flag, int start){
        recordtype = type;
        play = flag;
        index = start;
    }
    
}
