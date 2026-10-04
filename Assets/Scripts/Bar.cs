using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(HingeJoint2D))]
public class Bar : MonoBehaviour
{
    public float tipOverSpeed = 1f;
    public float upperAngleLimitBreak = 1f;

    private Platform platformParent;
    private bool hasFallen;
    private bool released;
    private BoxCollider2D coll;
    private Rigidbody2D body;
    private HingeJoint2D hinge;

    public float Length => coll.size.y * Mathf.Abs(transform.lossyScale.y);

    private void Awake()
    {
        coll = GetComponent<BoxCollider2D>();
        body = GetComponent<Rigidbody2D>();
        hinge = GetComponent<HingeJoint2D>();
    }

    public void SetLength(float length)
    {
        if (released)
            return;

        float delta = Mathf.Max(0.001f, length) - Length;
        Vector3 scale = transform.localScale;
        float parentScaleY = transform.parent ? Mathf.Abs(transform.parent.lossyScale.y) : 1f;
        scale.y += delta / (coll.size.y * Mathf.Max(0.0001f, parentScaleY));
        transform.localScale = scale;
        transform.position += Vector3.up * (delta / 2f);
    }

    public void tipOverToTheRight(Platform platform)
    {
        if (released)
            return;

        released = true;
        platformParent = platform;
        Physics2D.SyncTransforms();
        body.simulated = true;
        Vector2 topLeft = new Vector2(coll.bounds.min.x, coll.bounds.max.y);
        body.AddForceAtPosition(new Vector2(1f, 1f) * tipOverSpeed, topLeft);
        StartCoroutine(CheckIsReady());
    }

    private IEnumerator CheckIsReady()
    {
        yield return new WaitForSeconds(0.2f);
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            if (hinge.enabled && hinge.jointAngle >= upperAngleLimitBreak)
            {
                hinge.enabled = false;
                break;
            }
            if (body.IsSleeping() || (body.linearVelocity.sqrMagnitude <= 0.0001f && Mathf.Abs(body.angularVelocity) <= 1f))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }
        hasFallen = true;
    }

    public bool getHasFallen() => hasFallen;
    public Platform getPlatformParent() => platformParent;
}
