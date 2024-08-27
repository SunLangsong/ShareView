using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.XR.CoreUtils;
public class VRHostCameraControl : NetworkBehaviour
{
    
    public GameObject panel;
    private GameObject maincamera;
    public GameObject subcamera;
    public GameObject fovcamera;
    public GameObject Image_Mask;
    public GameObject Image_Mask1;
    public GameObject fpsSelect;
    public GameObject maskSelect;
    public GameObject recordStart;
    public GameObject recordTime;
    public GameObject recordSelect;
    public GameObject LeftHand;
    public GameObject RightHand;
    public GameObject Mark;
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
    //[SyncVar]
    private Vector3 syncedPosition;
    //[SyncVar]
    private Quaternion syncedRotation;

    private Quaternion PreRotation;

    // [SyncVar]
    private int rotlevel;

    private float rotangle;
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

        rotlevel = 0;

        rotangle = 0.0f;

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

            PreRotation = maincamera.transform.rotation;
            
            masktype = mask.value;

            // Update the Client fps for all clients by the choice of the server 
            dropdownfps.onValueChanged.AddListener(delegate {
                DropdownValueChanged(dropdownfps);
            });

            // Update the Client mask for all clients by the choice of the server 
            mask.onValueChanged.AddListener(delegate {
                masktype = mask.value;
                UpdateClientMask(mask.value);
            });
        }
        if (isClient && !isServer) {
            // Close the action control of the Client HMD
            // maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            maincamera.GetComponent<TrackedPoseDriver>().enabled = false;
        }
    }
    void Update()
    {
        if (isServer)
        {

            Countfps();
            
            // Get the action of main camera from the Server
            SyncTransform(maincamera.transform.position, maincamera.transform.rotation);

            //rotlevel = CalMotionLevel(PreRotation, maincamera.transform.rotation);
            //syncedPosition = maincamera.transform.position;
            //syncedRotation = maincamera.transform.rotation;
            //PreRotation = maincamera.transform.rotation;

            // Get the action of VR origin from the Server
            ServerCenterposition = Centerposition.position;
            ServerCenterRatation = Centerposition.rotation;

            /*if (Time.time < nextFrameTime)
            {
                return;
                
            }else{
            }    
            nextFrameTime = Time.time + frameRateInterval;*/
        }
        if (isClient && !isServer) // Make sure the change is on clients
        {
            if (Time.time < nextFrameTime)
            {
                return;
                
            }else{
                if(play == true && actionrecord.NumOfRecords() > 0){
                    // Control the action of the Clients' main camera by the data from the Server
                    if(masktype == 3){
                        subcamera.transform.position = actionrecord.GetCamerapositions(recordtype, index);
                        subcamera.transform.rotation = actionrecord.GetCamerarotations(recordtype, index);
                    }else{
                        maincamera.transform.position = actionrecord.GetCamerapositions(recordtype, index);
                        maincamera.transform.rotation = actionrecord.GetCamerarotations(recordtype, index);
                    }       
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
                    // Centerposition.position = ServerCenterposition;
                    // Centerposition.rotation = ServerCenterRatation;

                    // Control the action of the Clients' main camera by the data from the Server
                    if(masktype == 3){
                        subcamera.transform.position = syncedPosition;
                        subcamera.transform.rotation = syncedRotation;
                        maincamera.transform.position = syncedPosition; 
                        //maincamera.transform.rotation = syncedRotation;
                    }else if(masktype == 5){
                        Quaternion qsubvari = Quaternion.Inverse(PreRotation)*subcamera.transform.rotation;
                        Quaternion qmainvari = Quaternion.Inverse(maincamera.transform.rotation)*syncedRotation;
                        Quaternion qvarui = Quaternion.Inverse(qsubvari) * qmainvari;
                        rotangle = CalMotionAngle(qsubvari, qmainvari);
                        PreRotation = subcamera.transform.rotation;
                        maincamera.transform.position = syncedPosition;
                        maincamera.transform.rotation = syncedRotation;
                        subcamera.transform.position = new Vector3(syncedPosition.x + 6.5f, syncedPosition.y + 2.0f, syncedPosition.z + 40f);
                        if(rotangle != 0){
                            //Mark.SetActive(true);
                            Mark.GetComponent<RectTransform>().localRotation = new Quaternion(0.0f, 0.0f, qvarui.x +  qvarui.z, qvarui.w);
                            //Mark.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0,qvarui.z);
                            Mark.GetComponent<RectTransform>().sizeDelta = new Vector2(50 * rotlevel / 2, 10);
                        }else{
                            Mark.SetActive(false);
                        }
                        Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", -2.0f / 1500.0f * rotangle + 0.23f);
                        Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", -2.0f / 1500.0f * rotangle + 0.18f);
                        /*switch(rotlevel){
                            case 0:
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", 0.23f);
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", 0.18f);
                                break;
                            case 1:
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", 0.20f);
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", 0.15f);
                                break;
                            case 2:
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", 0.17f);
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", 0.12f);
                                break;
                            case 3:
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", 0.14f);
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", 0.09f);
                                break;
                            case 4:
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", 0.11f);
                                Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", 0.06f);
                                break;
                            default:
                                
                                break;
                                
                        }*/
                    }else{
                        maincamera.transform.position = syncedPosition;
                        maincamera.transform.rotation = syncedRotation;
                    }
                }
            // int index = 0;
            
                nextFrameTime = Time.time + frameRateInterval;
                Countfps();       
            }    
        }
    }
    float CalMotionAngle(Quaternion q1, Quaternion q2)
    {
        // Calulate the dot between two Quaternions
        float dotProduct = Quaternion.Dot(q1, q2);
        
        // Limitate the dot between [-1, 1]
        dotProduct = Mathf.Clamp(dotProduct, -1.0f, 1.0f);
        
        // Calculate the angle in Pi Radis
        float angleInRadians = Mathf.Acos(dotProduct) * 2.0f;
        
        // Change the angle to °
        float angleInDegrees = Mathf.Abs(angleInRadians * Mathf.Rad2Deg);

        angleInDegrees /= frameRateInterval * 3;

        angleInDegrees = Mathf.Ceil(angleInDegrees / 10) * 10;

        Debug.Log(angleInDegrees);

        /*int level = 0;
        if (angleInDegrees >= 0.0f && angleInDegrees < 10.0f){
            level = 0;
        }
        else if (angleInDegrees >= 10.0f && angleInDegrees <= 30.0f){
            level = 1;
        }else if(angleInDegrees >30.0f && angleInDegrees <= 50.0f){
            level = 2;
        }else if(angleInDegrees >50.0f && angleInDegrees <= 70.0f){
            level = 3;
        }else if(angleInDegrees >70.0f && angleInDegrees <= 90.0f){
            level = 4;
        }*/

        return angleInDegrees;
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
                frameRateInterval = 1.0f / 15.0f;
                break;
            case 3:
                frameRateInterval = 1.0f / 25.0f;
                break;
            case 4:
                frameRateInterval = 1.0f / 35.0f;
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
    [ClientRpc]
    void UpdateClientMask(int type)
    {
        if (isClient && !isServer){
            // Change the Mask of the Clients by the Choice Selected by the Server 
            switch(type){
                case 0:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(false);
                    break;
                case 1:
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov2(false);
                    panel.GetComponent<Volume>().enabled = true;              
                    break;
                case 2:
                    panel.GetComponent<Volume>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(false);
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = true;
                    break;
                case 3:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(false);
                    SetCameraToLimitFov(true);
                    break;
                case 4:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov2(false);
                    SetCameraToLimitFov1(true);
                    break;
                case 5:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(true);
                    break;
            default:
                    break;
            }
        }
        
    }
    void SetCameraToLimitFov(bool flag){
        subcamera.SetActive(flag);
        Image_Mask.SetActive(flag);
        maincamera.GetComponent<TrackedPoseDriver>().enabled = flag;
        maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        LeftHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;
        RightHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;        
    }
    void SetCameraToLimitFov1(bool flag){
        fovcamera.SetActive(flag);
        if(flag){
            maincamera.GetComponent<Camera>().rect = new Rect(0.2f, 0.2f, 0.6f, 0.6f);
            maincamera.GetComponent<Camera>().fieldOfView = 38.3f;
        }else{
            maincamera.GetComponent<Camera>().rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            maincamera.GetComponent<Camera>().fieldOfView = 60f;
        }
        
    }
    void SetCameraToLimitFov2(bool flag){
        Image_Mask1.SetActive(flag);
        if(flag){
            subcamera.SetActive(flag);
            subcamera.GetComponent<TrackedPoseDriver>().enabled = flag;
            subcamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            maincamera.GetComponent<Camera>().fieldOfView = 50f;
            subcamera.GetComponent<Camera>().fieldOfView = 80f;
        }else{
            subcamera.GetComponent<TrackedPoseDriver>().enabled = flag;
            subcamera.GetComponent<Camera>().fieldOfView = 80f;
            maincamera.GetComponent<Camera>().fieldOfView = 60f;
            subcamera.SetActive(flag);
            //subcamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        }
        
        LeftHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;
        RightHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;        
    }
    
    [ClientRpc]
    private void SyncTransform(Vector3 pos, Quaternion rot){
        syncedPosition = pos;
        syncedRotation = rot;
    }
    // Initiate the record to play
    [ClientRpc]
    private void Rectherecordtype(int type, bool flag, int start){
        recordtype = type;
        play = flag;
        index = start;
    }
    
}
