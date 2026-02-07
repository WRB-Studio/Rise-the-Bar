using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float checkDistance = 1.0f;
    public Vector2 minMaxIdleSpeed;
    private float idleSpeed = 0.05f;
    public Vector2 minMaxNormalSpeed;
    private float normalSpeed = 1f;
    public float jumpForce = 50;
    public Vector2 jumpDirection = new Vector2(1, 1);

    public float bottomDeadZone = -5f;
    public GameObject explosionPrefab;

    private GameObject currentBar;
    private Vector2 stuckPositionFix;
    private Platform currentPlatform;
    private bool isDied = false;

    [HideInInspector] public Vector2 startPoint;
    [HideInInspector] public bool isInit = false;

    public static Player instance;


    private void Awake()
    {
        instance = this;
    }

    public void init()
    {
        StartCoroutine(stuckPositionFixRoutine());

        startPoint = transform.position;

        isInit = true;
    }

    public void updateCall()
    {
        if (GameHandler.instance.isGameOver)
            return;

        if (transform.position.y <= bottomDeadZone)
        {
            die();

            PlatformController.instance.gameOverDestruction();

            return;
        }

        movement();
    }

    private void movement()
    {
        if (!BarRaiser.instance.firstTouch)
            return;

        // increase speed by difficulty
        float currentDifficulty = GameHandler.instance.currentDifficulty;
        normalSpeed = Mathf.Lerp(minMaxNormalSpeed.x, minMaxNormalSpeed.y, currentDifficulty);
        idleSpeed = Mathf.Lerp(minMaxIdleSpeed.x, minMaxIdleSpeed.y, currentDifficulty);

        //controll the speed by platform bar
        if (currentPlatform)
        {
            //Idle speed if no bar on current platform or created bar isnt fallen or is not on ground.
            if (!currentPlatform.barObject || (currentPlatform.barObject && !currentPlatform.barObject.getHasFallen()) || !isGrounded())
            {
                transform.Translate(Vector2.right * idleSpeed * Time.deltaTime);
            }
            else if ((currentPlatform.barObject && currentPlatform.barObject.getHasFallen()))
            {
                transform.Translate(Vector2.right * normalSpeed * Time.deltaTime * 2);
            }
        }
        else if (!currentPlatform && currentBar)
            transform.Translate(Vector2.right * normalSpeed * Time.deltaTime);
    }

    private IEnumerator stuckPositionFixRoutine()
    {
        yield return null;

        // fix position stuck by jumping.
        while (true)
        {
            stuckPositionFix = transform.position;
            yield return new WaitForSeconds(0.2f);

            if (Vector2.Distance(stuckPositionFix, transform.position) < 0.02f)
                jump();
        }
    }

    public void jump()
    {
        if (!isGrounded())
            return;

        AudioController.playJump();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.AddForce(jumpDirection * jumpForce);
    }

    private bool isGrounded()
    {
        Vector2 position = transform.position; // Die aktuelle Position des Objekts
        Vector2 direction = Vector2.down; // Die Richtung des Raycasts (nach unten)
        float distance = transform.localScale.y; // Die Entfernung des Raycasts

        RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, distance);
        foreach (var hit in hits)
        {
            if (hit.collider != null && (hit.collider.tag == "Platform" || hit.collider.tag == "Bar"))
            {
                return true;
            }
        }

        return false;
    }

    public void die()
    {
        if (isDied)
            return;

        isDied = true;
        GameObject newExplosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(newExplosion, 5);
        GetComponent<SpriteRenderer>().enabled = false;
        AudioController.playDie();
        GetComponent<Rigidbody2D>().simulated = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Platform")
        {
            currentPlatform = collision.gameObject.GetComponent<Platform>();
        }
        else if (collision.gameObject.CompareTag("Bar") && currentBar != collision.gameObject)
        {
            Collider2D barCollider = collision.gameObject.GetComponent<Collider2D>();
            float barTopY = barCollider.bounds.max.y;
            float barLeftX = barCollider.bounds.min.x;
            float barRightX = barCollider.bounds.max.x;
            float playerLeftX = GetComponent<Collider2D>().bounds.min.x;
            float playerRightX = GetComponent<Collider2D>().bounds.max.x;

            // Prüfe, ob der Spieler links neben der Bar steht
            if (playerRightX < barLeftX &&
                Mathf.Abs(transform.position.y - barCollider.bounds.center.y) < barCollider.bounds.size.y / 2)
            {
                jump();
            }
            // Prüfe, ob der Spieler auf der Bar steht
            else if (transform.position.y > barTopY && playerLeftX < barRightX && playerRightX > barLeftX)
            {
                currentBar = collision.gameObject;
            }
        }
    }

}
