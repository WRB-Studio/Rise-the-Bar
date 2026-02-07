using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    public Color startBgColor;
    public Color endBgColor;
    public Color startStarColor;
    public Color endStarColor;
    public float colorEndDistance;

    private Player player;
    private Camera mainCamera;
    private List<GameObject> stars;



    public void Awake()
    {
        
    }

    private void Start()
    {
        player = Player.instance;
        Camera.main.backgroundColor = startBgColor;
        stars = new List<GameObject>();
        foreach (Transform child in GameObject.Find("Stars").transform)
        {
            if (child.name == "Star")
            {
                stars.Add(child.gameObject);
            }
        }
    }

    private void Update()
    {
        float distance = Vector2.Distance(player.transform.position, player.startPoint);
        Camera.main.backgroundColor = Color.Lerp(startBgColor, endBgColor, distance / colorEndDistance);

        foreach (GameObject star in stars)
        {
            star.GetComponent<SpriteRenderer>().color = Color.Lerp(startStarColor, endStarColor, distance / colorEndDistance);
        }
    }

}
