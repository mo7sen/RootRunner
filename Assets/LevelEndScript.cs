using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndScript : MonoBehaviour
{
    public string NextScene;
    void OnCollisionEnter2D(Collision2D collision)
    {
        SceneManager.LoadScene(NextScene);
    }
}
