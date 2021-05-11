using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointSpawns : MonoBehaviour
{
    SongScript songScripts;
    public Text score;
    int scoreCount;

    // Start is called before the first frame update

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "BassPoint")
        {
            scoreCount += 100;
            score.text = scoreCount.ToString();
            Destroy(collision.gameObject);
            Debug.Log("+1");
            Debug.Log("Object Destroyed");

        }
    }
}