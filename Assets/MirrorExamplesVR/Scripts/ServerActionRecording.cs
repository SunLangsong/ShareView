using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Unity.VisualScripting;
public class ServerActionRecording : NetworkBehaviour
{
    public GameObject recordButton;
    public GameObject stopButton;
    public TMP_Text timeText;
    private bool isRecording = false;
    private float recordingTime = 0.0f;
    private float recordInterval = 1.0f / 60.0f;
    private float timer;
    public Transform serverCameraTransform;
    public Transform Centerposition;
    private List<Vector3> camerapositions;
    private List<Quaternion> camerarotations;
    [SyncVar]
    private List<Vector3> Syncamerapositions = new List<Vector3>();
    [SyncVar]
    private List<Quaternion> Syncamerarotations = new List<Quaternion>();
    [SyncVar]
    private bool SynisRecording;
    

    void Start()
    {
        recordButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
        stopButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
        camerapositions = new List<Vector3>();
        camerarotations = new List<Quaternion>();
        timeText.text = "0.0s";
        
    }

    void Update()
    {
        if (isRecording && isServer)
        {
            timer += Time.deltaTime;

            // Record the action of the camera as the time interval
            if (timer >= recordInterval)
            {
                camerapositions.Add(serverCameraTransform.position);
                camerarotations.Add(serverCameraTransform.rotation);
                timer = 0;
            }
            recordingTime += Time.deltaTime;
            timeText.text = recordingTime.ToString("F1") + "s";
        }else if(isClient && !isServer){

        }
    }

    void ToggleRecording()
    {
        isRecording = !isRecording;
        SynisRecording = isRecording;
        recordingTime = 0;  // Reset the recording time
        if (isRecording)
        {
            // Start to record the action
            recordButton.SetActive(false);
            stopButton.SetActive(true);
            ClearCameraposition();
            ClearCamerarotation();
        }
        else
        {
            // Stop to record the action
            recordButton.SetActive(true);
            stopButton.SetActive(false);
            Syncamerapositions = camerapositions;
            Syncamerarotations = camerarotations;
            Debug.Log(camerapositions.Count);
            
        }
    }
    public List<Vector3> GetCamerapositions(){
        return Syncamerapositions;
    }
    public List<Quaternion> GetCamerarotations(){
        return Syncamerarotations;
    }
    public void ClearCameraposition(){
        camerapositions = new List<Vector3>();
        Syncamerapositions = new List<Vector3>();
    }
    public void ClearCamerarotation(){
        camerarotations = new List<Quaternion>();
        Syncamerarotations = new List<Quaternion>();
    }
    public bool IsRecording(){
        return SynisRecording;
    }
}