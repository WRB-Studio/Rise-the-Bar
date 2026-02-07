using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bar : MonoBehaviour
{
    public float tipOverSpeed = 1f;
    public float upperAngleLimitBreak = 1f;

    private Platform platformParent;
    private bool hasFallen = false;
    private Collider2D coll;
    private Rigidbody2D rb;
    private HingeJoint2D hingeJointComp;


    private void Start()
    {
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        hingeJointComp = GetComponent<HingeJoint2D>();
    }

    public void tipOverToTheRight(Platform platform)
    {
        platformParent = platform;
        // get top left points of object
        Vector2 topLeftPosition = new Vector2(coll.bounds.min.x, coll.bounds.max.y);

        // force for tip over
        rb.simulated = true;
        Vector2 tipOverForce = new Vector2(1, 1) * tipOverSpeed;
        rb.AddForceAtPosition(tipOverForce, topLeftPosition);

        StartCoroutine(CheckIsReady());
    }

    private IEnumerator CheckIsReady()
    {
        yield return new WaitForSeconds(0.2f);

        // check if bar has fallen
        while (rb.linearVelocity.magnitude > 0.01f)
        {
            yield return null;

            if (hingeJointComp.jointAngle >= upperAngleLimitBreak)
            {
                hingeJointComp.enabled = false;
                break;
            }
        }

        hasFallen = true;
    }

    public bool getHasFallen()
    {
        return hasFallen;
    }

    public Platform getPlatformParent()
    {
        return platformParent;
    }

}
