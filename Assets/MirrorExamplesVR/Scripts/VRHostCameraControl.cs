using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using System;
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

    private float nextsecond = 0.0f;

    public Transform Centerposition;
    private ServerActionRecording actionrecord;
    // The index of record to play 
    private int recordtype = 0;
    // The index of the action in the record selected
    private int index = 0;
    // Check the condition of the record selected
    private bool play = false;
    Vector3 start = new Vector3(-110f, -70f, -2f);
    Vector3 end = new Vector3(0, 0, 0);
    private List<Image> lines = new List<Image>();
    public Image lineImagePrefab;
    public Material lineMaterial;
    public float lineWidth = 20f;
    public float lineDisplayDuration = 0.9f;
    // Send the action of main camera to all clients
    //[SyncVar]
    private Vector3 syncedPosition;

    private Vector3 TempPosition;
    //[SyncVar]
    private Quaternion syncedRotation;

    private Quaternion TempRotation;

    private Quaternion PreRotation;

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
            TempPosition = maincamera.transform.position;
            TempRotation = maincamera.transform.rotation;
        }
    }
    void Update()
    {
        if (isServer)
        {

            Countfps();
            
            // Get the action of main camera from the Server
            SyncTransform(maincamera.transform.position, maincamera.transform.rotation);

            //syncedPosition = maincamera.transform.position;
            //syncedRotation = maincamera.transform.rotation;
            //PreRotation = maincamera.transform.rotation;

            // Get the action of VR origin from the Server
            ServerCenterposition = Centerposition.position;
            ServerCenterRatation = Centerposition.rotation;

        }
        if (isClient && !isServer) // Make sure the change is on clients
        {
            
            if (Time.time < nextFrameTime)
            {
                // Control the action of the Clients' main camera by the data from the Server
                if(masktype == 3){
                    subcamera.transform.position = TempPosition;
                    subcamera.transform.rotation = TempRotation;
                    maincamera.transform.position = TempPosition; 
                    //maincamera.transform.rotation = syncedRotation;
                }else if(masktype == 5){
                    
                    maincamera.transform.position = TempPosition;
                    maincamera.transform.rotation = TempRotation;
                    subcamera.transform.position = new Vector3(TempPosition.x + 6.5f, TempPosition.y + 2.0f, TempPosition.z + 40f);
                    
                }else{
                    maincamera.transform.position = TempPosition;
                    maincamera.transform.rotation = TempRotation;
                }
                return;
                
            }else{
                if(play == true && actionrecord.NumOfRecords() > 0){
                    // Control the action of the Clients' main camera by the data from the Server
                    if(masktype == 3){
                        subcamera.transform.position = actionrecord.GetCamerapositions(recordtype, index);
                        subcamera.transform.rotation = actionrecord.GetCamerarotations(recordtype, index);
                    }else{
                        TempPosition = actionrecord.GetCamerapositions(recordtype, index);
                        TempRotation = actionrecord.GetCamerarotations(recordtype, index);
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
                    }
                    else if(masktype == 5){
                        // Get the rotation of main and sub camera
                        Quaternion qsubvari = Quaternion.Inverse(PreRotation)*subcamera.transform.rotation;
                        Quaternion qmainvari = Quaternion.Inverse(maincamera.transform.rotation)*syncedRotation;

                        // Get the rotation between the main and sub camera(with direction)
                        Quaternion qvarui = Quaternion.Inverse(qsubvari) * qmainvari;
                        //Vector3 eulerRotation = qvarui.eulerAngles;
                        Vector3 eulerRotation = qvarui * Vector3.forward;
                        //float uiRotation = eulerRotation.z + eulerRotation.x * Mathf.Sign(Mathf.Cos(eulerRotation.y * Mathf.Deg2Rad));

                        // Get the projection of rotation on xy plane
                        eulerRotation.z = 0;
                        eulerRotation.Normalize();
                        float uiRotation = Mathf.Atan2(eulerRotation.y, eulerRotation.x) * Mathf.Rad2Deg;

                        // Get the rotation angle between main and sub camera(no direction)
                        rotangle = CalMotionAngle(qsubvari, qmainvari);

                        // Update the camera data
                        PreRotation = subcamera.transform.rotation;
                        maincamera.transform.position = TempPosition;
                        maincamera.transform.rotation = TempRotation;
                        subcamera.transform.position = new Vector3(TempPosition.x + 6.5f, TempPosition.y + 2.0f, TempPosition.z + 40f);

                        // Draw the line when the angle between main and sub camera is over 10. 
                        if(rotangle >= 10){
                            Mark.SetActive(true);
                            Mark.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0.0f, 0.0f, uiRotation + 180);

                            // Calculate the end point of the line
                            end = CalculateEndPoint(start, uiRotation + 180, rotangle / 2);

                            // Draw the line with the two points 
                            DrawLineInMask(start, end, uiRotation + 180);

                            // Reset the start point
                            start.Set(end.x, end.y, end.z);

                            //Mark.GetComponent<RectTransform>().localRotation = new Quaternion(0.0f, 0.0f, qvarui.x +  qvarui.z, qvarui.w);
                            //Mark.GetComponent<RectTransform>().localRotation.eulerAngles = new Vector3(0, 0,qvarui.z);
                            Mark.GetComponent<RectTransform>().sizeDelta = new Vector2(Math.Abs(rotangle) / 2, 10);
                        }else{
                            Mark.SetActive(false);
                        }
                        Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", -2.0f / 1500.0f * Math.Abs(rotangle) + 0.23f);
                        Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", -2.0f / 1500.0f * Math.Abs(rotangle) + 0.18f);
                    }
                    TempPosition = syncedPosition;
                    TempRotation = syncedRotation;
                }
                nextFrameTime = Time.time + frameRateInterval;
                
                // Initiate the start point
                if(nextsecond <= nextFrameTime - 1.5f){
                    nextsecond = nextFrameTime;
                    start = new Vector3(-110f, -70f, -2f);
                    end = new Vector3(0, 0, 0);
                    lines.ForEach(img => Destroy(img.gameObject));
                    lines.Clear();
                }
                Countfps();       
            }
            
        }          
    }
    float CalMotionAngle(Quaternion q1, Quaternion q2)
    {
        // Calulate the angle between two Quaternions
        Vector3 from = q1 * Vector3.forward;
        Vector3 to = q2 * Vector3.forward;
        float angleInDegrees = Vector3.Angle(from, to);

        // Use the cross product to calculate the direction of the rotation
        // Vector3 crossProduct = Vector3.Cross(from, to);
        // float sign = Mathf.Sign(crossProduct.x + crossProduct.y + crossProduct.z);

        // Nomalize the angle
        angleInDegrees /= frameRateInterval * 3;
        angleInDegrees = Mathf.Ceil(angleInDegrees / 10) * 10;

        return angleInDegrees;
    }
    Vector3 CalculateEndPoint(Vector3 start, float angle, float distance)
    {
        // Change the angle to arc
        float angleInRadians = angle * Mathf.Deg2Rad;

        // Calulate the coordinate of the end point
        float x = start.x + distance * Mathf.Cos(angleInRadians);
        float y = start.y + distance * Mathf.Sin(angleInRadians);

        // If the line is on a panel, the data of z will not change
        return new Vector3(x, y, start.z);
    }
    // Draw the line between the two points
    void DrawLineInMask(Vector3 start, Vector3 end, float angle)
    {
        // Create a new line object
        Image line = Instantiate(lineImagePrefab);

        lines.Add(line);

        // Set the Background_mask as the parent object
        line.transform.SetParent(Image_Mask1.transform, false);

        // Get the RectTransform to set the line
        RectTransform lineRectTransform = line.GetComponent<RectTransform>();

        // Set the distance of the line
        float distance = Vector3.Distance(start, end);
        lineRectTransform.sizeDelta = new Vector2(distance, 5f); // 5f is the width of the line 

        // Set the transform of the middle of the two point as the position of the line
        Vector3 midpoint = (start + end) / 2f;
        lineRectTransform.localPosition = midpoint;

        // Set the rotation of the line
        lineRectTransform.localRotation = Quaternion.Euler(0, 0, angle);

        // Set the duration time of the line
        // StartCoroutine(RemoveLineAfterDelay(line, lineDisplayDuration));
    }

    // Remove the line after specify time
    private IEnumerator RemoveLineAfterDelay(Image lineRenderer, float duration)
    {
        // Wait for specify time
        yield return new WaitForSeconds(duration);

        // Make the line invisible
        lineRenderer.enabled = false;

        // Remove the line from the list and destroy the object
        lines.Remove(lineRenderer);
        Destroy(lineRenderer.gameObject);
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
