using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject sound;
    AudioSource audioSource;
    public TextMeshProUGUI songname;
    public GameObject playbutton;
    public GameObject ship;
    string path;
    bool trackSelected = false;

    void Update() 
    {
        ship.transform.Rotate(Vector3.forward * Time.fixedDeltaTime * 10f);
    }

    public void OpenExplorer()
    {
        StartCoroutine(loadMusic());
    }

    public void StartGame()
    {
        if (trackSelected == true)
        {
            DontDestroyOnLoad(sound);
            SceneManager.LoadScene("PlayMode");
            audioSource.Stop();
        }
    }

    IEnumerator loadMusic()
    {
        path = EditorUtility.OpenFilePanel("Overwrite with mp3", "", "mp3,wav");

        if (path != null)
        {
            WWW www = new WWW("file:///" + path);
            yield return www;
            songname.text = www.url;
            audioSource = sound.GetComponent<AudioSource>();
            audioSource.clip = www.GetAudioClip();
            audioSource.Play();
            playbutton.SetActive(true);
            trackSelected = true;
        }
        else
        {
            Debug.Log("Please Select Track Again");
        }
    }
}
