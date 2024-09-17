using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Unity.VisualScripting;
using Assets.OVR.Scripts;
public class ServerActionRecording : NetworkBehaviour
{
    public GameObject recordButton;
    public GameObject stopButton;
    public GameObject recordSelect;
    public TMP_Text timeText;
    private TMP_Dropdown record;
    private bool isRecording = false;
    private float recordingTime = 0.0f;
    private float recordInterval = 1.0f / 35.0f;
    private float timer;
    public Transform serverCameraTransform;
    public Transform Centerposition;
    private List<Vector3> camerapositions;
    private List<Quaternion> camerarotations;
    private List<List<Vector3>> positionrecords;
    private List<List<Quaternion>> rotationrecords;
    [SyncVar]
    private List<List<Vector3>> Synpositionrecords;
    [SyncVar]
    private List<List<Quaternion>> Synrotationrecords;
    //[SyncVar]
    //private bool SynisRecording;
    

    void Start()
    {
        if(isServer){
            recordButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
            stopButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
            record = recordSelect.GetComponent<TMP_Dropdown>();
            camerapositions = new List<Vector3>();
            camerarotations = new List<Quaternion>();
            positionrecords = new List<List<Vector3>>();
            rotationrecords = new List<List<Quaternion>>();
            timeText.text = "0.0s";
        }
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
        // SynisRecording = isRecording;
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
            recordSelect.SetActive(true);
            positionrecords.Add(camerapositions);
            rotationrecords.Add(camerarotations);

            // Syn the record data to all clients
            SynrecordData(positionrecords, rotationrecords);

            // Add the option to recordSelect dropdown
            record.options.Add(new TMP_Dropdown.OptionData("Record " + NumOfSevRecords().ToString()));

        }
    }
    private int NumOfSevRecords(){
        return positionrecords.Count;
    }
    public int NumOfRecords(){
        return Synpositionrecords.Count;
    }
    public int Numofactions(int index){
        return Synpositionrecords[index].Count;
    }
    public Vector3 GetCamerapositions(int id, int index){
        if (id < NumOfRecords() && index < Numofactions(id)){
            return Synpositionrecords[id][index];
        }
        return new Vector3(0, 0, 0);
    }
    public Quaternion GetCamerarotations(int id, int index){
        if (id < NumOfRecords() && index < Numofactions(id)){
            return Synrotationrecords[id][index];
        }
        return new Quaternion(0, 0, 0, 0);
    }
    public void ClearCameraposition(){
        camerapositions = new List<Vector3>();
    }
    public void ClearCamerarotation(){
        camerarotations = new List<Quaternion>();
    }
    [ClientRpc]
    private void SynrecordData(List<List<Vector3>> posi, List<List<Quaternion>> rota){
        Synpositionrecords = posi;
        Synrotationrecords = rota;
    }
}