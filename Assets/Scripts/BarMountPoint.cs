using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarMountPoint : MonoBehaviour
{
    public GameObject barObject;
    public bool isPassed;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
            isPassed = true;
    }
}
