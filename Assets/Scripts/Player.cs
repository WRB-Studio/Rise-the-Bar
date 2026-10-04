using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    public Vector2 minMaxIdleSpeed;
    public Vector2 minMaxNormalSpeed;
    public float jumpForce = 50f;
    public Vector2 jumpDirection = new Vector2(1, 1);
    public float bottomDeadZone = -5f;
    public GameObject explosionPrefab;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer sprite;
    private PhysicsMaterial2D movementMaterial;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private ContactFilter2D groundFilter;
    private Platform currentPlatform;
    private Bar currentBar;
    private Vector2 previousPosition;
    private float stuckCheckTimer;
    private float movementSpeed;
    private bool isDied;

    [HideInInspector] public Vector2 startPoint;
    [HideInInspector] public bool isInit;
    public static Player instance;
    public Vector2 Position => body.position;

    private void Awake()
    {
        instance = this;
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        // Side friction must not hold the character in the air at a bridge's raised edge.
        movementMaterial = new PhysicsMaterial2D("Player movement") { friction = 0f, bounciness = 0f };
        bodyCollider.sharedMaterial = movementMaterial;
        groundFilter = new ContactFilter2D { useTriggers = false };
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void init()
    {
        startPoint = body.position;
        previousPosition = body.position;
        isInit = true;
    }

    public void updateCall()
    {
        if (!GameHandler.instance.isGameOver && body.position.y <= bottomDeadZone)
        {
            die();
            PlatformController.instance.gameOverDestruction();
        }
    }

    private void FixedUpdate()
    {
        if (!isInit || isDied || !GameHandler.instance || GameHandler.instance.isGameOver)
            return;

        if (!BarRaiser.instance.firstTouch)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            previousPosition = body.position;
            return;
        }

        bool grounded = UpdateGroundSupport();
        float difficulty = GameHandler.instance.currentDifficulty;
        if (grounded)
        {
            movementSpeed = Mathf.Lerp(minMaxNormalSpeed.x, minMaxNormalSpeed.y, difficulty);
            if (currentPlatform)
            {
                movementSpeed = currentPlatform.barObject && currentPlatform.barObject.getHasFallen()
                    ? movementSpeed * 2f
                    : Mathf.Lerp(minMaxIdleSpeed.x, minMaxIdleSpeed.y, difficulty);
            }
        }

        // Keep the requested speed in the air; a side collision can temporarily zero actual velocity.
        body.linearVelocity = new Vector2(movementSpeed, body.linearVelocity.y);
        stuckCheckTimer += Time.fixedDeltaTime;
        if (stuckCheckTimer >= 0.2f)
        {
            if (movementSpeed > 0f && grounded && Vector2.Distance(previousPosition, body.position) < 0.02f)
                jump();
            previousPosition = body.position;
            stuckCheckTimer = 0f;
        }
    }

    private bool UpdateGroundSupport()
    {
        currentPlatform = null;
        currentBar = null;
        float probeDistance = Mathf.Max(0.05f, bodyCollider.bounds.extents.y + 0.02f);
        int count = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, probeDistance);
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = groundHits[i];
            if (hit.normal.y < 0.5f || hit.distance >= nearestDistance)
                continue;

            if (hit.collider.CompareTag("Platform"))
            {
                currentPlatform = hit.collider.GetComponent<Platform>();
                currentBar = null;
                nearestDistance = hit.distance;
            }
            else if (hit.collider.CompareTag("Bar"))
            {
                currentBar = hit.collider.GetComponent<Bar>();
                currentPlatform = null;
                nearestDistance = hit.distance;
            }
        }
        return currentPlatform || currentBar;
    }

    public void jump()
    {
        if (isDied || GameHandler.instance.isGameOver || !BarRaiser.instance.firstTouch || body.linearVelocity.y > 0.1f || !UpdateGroundSupport())
            return;

        AudioController.playJump();
        body.AddForce(jumpDirection * jumpForce);
    }

    public void die()
    {
        if (isDied)
            return;

        isDied = true;
        Destroy(Instantiate(explosionPrefab, transform.position, Quaternion.identity), 5f);
        sprite.enabled = false;
        AudioController.playDie();
        body.simulated = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Bar"))
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.x < -0.5f)
            {
                jump();
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (movementMaterial)
            Destroy(movementMaterial);
        if (instance == this)
            instance = null;
    }
}
