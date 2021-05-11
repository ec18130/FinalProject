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

    void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("AUDIOSOURCE");
        if (objs.Length > 1)
        {
            Destroy(sound);
        }

        DontDestroyOnLoad(sound);
        audioSource = sound.GetComponent<AudioSource>();
    }

    void Update() 
    {
        ship.transform.Rotate(Vector3.forward * Time.fixedDeltaTime * 10f);
    }

    public void OpenExplorer()
    {

        path = EditorUtility.OpenFilePanel("Overwrite with mp3", "", "mp3,wav");
        if (path != null)
        {
            WWW www = new WWW("file:///" + path);
            songname.text = www.url;
            audioSource.clip = www.GetAudioClip();
            audioSource.Play();
            playbutton.SetActive(true);
            trackSelected = true;
        }
    }

    public void StartGame()
    {
        if (trackSelected == true)
        {
            audioSource.Stop();
            SceneManager.LoadScene("PlayMode");
        }
    }
}
