using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ServerActionRecording : MonoBehaviour
{
    public GameObject recordButton;
    public GameObject stopButton;
    public TMP_Text timeText;
    private bool isRecording = false;
    private float recordingTime = 0.0f;

    void Start()
    {
        recordButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
        stopButton.GetComponent<Button>().onClick.AddListener(ToggleRecording);
        timeText.text = "0.0s";
    }

    void Update()
    {
        if (isRecording)
        {
            recordingTime += Time.deltaTime;
            timeText.text = recordingTime.ToString("F1") + "s";
        }
    }

    void ToggleRecording()
    {
        isRecording = !isRecording;
        recordingTime = 0;  // 重置录制时间
        if (isRecording)
        {
            // 更新按钮文本以显示"Stop Recording"
            recordButton.SetActive(false);
            stopButton.SetActive(true);
            
            // 这里可以添加启动录制的代码
        }
        else
        {
            // 更新按钮文本以显示"Start Recording"
            recordButton.SetActive(true);
            stopButton.SetActive(false);
            // 这里可以添加停止录制的代码
        }
    }
}