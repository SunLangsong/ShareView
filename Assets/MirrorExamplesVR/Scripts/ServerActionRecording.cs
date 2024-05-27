using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
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
    [SyncVar]
    private List<Vector3> camerapositions = new List<Vector3>();
    [SyncVar]
    private List<Quaternion> camerarotations = new List<Quaternion>();
    

    void Start()
    {
        recordButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
        stopButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
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
        }
    }

    void ToggleRecording()
    {
        isRecording = !isRecording;
        recordingTime = 0;  // Reset the recording time
        if (isRecording)
        {
            // Start to record the action
            recordButton.SetActive(false);
            stopButton.SetActive(true);
            camerapositions.Clear();
            camerarotations.Clear();
        }
        else
        {
            // Stop to record the action
            recordButton.SetActive(true);
            stopButton.SetActive(false);
            Debug.Log(camerapositions[1]);
            
        }
    }
}