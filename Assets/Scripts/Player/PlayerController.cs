using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpVelocity = 12f;
    [SerializeField] float gravityScale = 3.5f;
    [SerializeField] float maxFallSpeed = 20f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float jumpCutMultiplier = 0.45f;

    [Header("Ground")]
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.34f, 0.16f);

    [Header("Visual")]
    [SerializeField] SpriteRenderer visual;
    [SerializeField] PlayerSpriteAnimator animator;

    Rigidbody2D _rb;
    InputAction _move;
    InputAction _jump;

    float _moveInput;
    float _coyote;
    float _jumpBuffer;
    bool _cutJump;
    bool _facingRight = true;
    Vector3 _spawnPosition;
    PhysicsMaterial2D _noFriction;

    public Vector2 SpawnPosition
    {
        get => _spawnPosition;
        set => _spawnPosition = value;
    }

    public bool IsGrounded { get; private set; }

    public void Bind(SpriteRenderer spriteRenderer, Transform ground)
    {
        visual = spriteRenderer;
        groundCheck = ground;
        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();
        animator?.Bind(spriteRenderer);
    }

    public void SetSprites(Sprite idle, Sprite run)
    {
        SetAnimationFrames(
            idle != null ? new[] { idle } : null,
            run != null ? new[] { run } : null);
    }

    public void SetAnimationFrames(Sprite[] idle, Sprite[] run)
    {
        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();
        if (animator == null)
            animator = gameObject.AddComponent<PlayerSpriteAnimator>();
        animator.Bind(visual);
        animator.SetFrames(idle, run);
        if (visual != null && idle != null && idle.Length > 0)
            visual.sprite = idle[0];
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = gravityScale;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _noFriction = new PhysicsMaterial2D
        {
            friction = 0f,
            bounciness = 0f
        };
        _noFriction.name = "PlayerNoFriction";
        _rb.sharedMaterial = _noFriction;

        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();

        BindInput();
        _spawnPosition = transform.position;
    }

    void OnEnable()
    {
        BindInput();
        InputSystem.actions?.FindActionMap("Player")?.Enable();
        _move?.Enable();
        _jump?.Enable();
    }

    void BindInput()
    {
        if (_move != null && _jump != null)
            return;

        var actions = InputSystem.actions;
        if (actions == null)
        {
            Debug.LogError("Project-wide Input Actions is not assigned.");
            return;
        }

        _move = actions.FindAction("Player/Move") ?? actions.FindAction("Move");
        _jump = actions.FindAction("Player/Jump") ?? actions.FindAction("Jump");
    }

    void Update()
    {
        if (_move != null)
            _moveInput = _move.ReadValue<Vector2>().x;

        if (_jump != null && _jump.WasPressedThisFrame())
            _jumpBuffer = jumpBufferTime;

        if (_jump != null && _jump.WasReleasedThisFrame() && _rb.linearVelocity.y > 0f)
            _cutJump = true;

        if (_jumpBuffer > 0f)
            _jumpBuffer -= Time.deltaTime;

        UpdateFacingAndSprite();

        if (transform.position.y < -12f)
            Respawn();
    }

    void FixedUpdate()
    {
        ProbeGround();

        var velocity = _rb.linearVelocity;
        velocity.x = _moveInput * moveSpeed;

        if (_jumpBuffer > 0f && _coyote > 0f)
        {
            velocity.y = jumpVelocity;
            _jumpBuffer = 0f;
            _coyote = 0f;
            _cutJump = false;
        }

        if (_cutJump)
        {
            if (velocity.y > 0f)
                velocity.y *= jumpCutMultiplier;
            _cutJump = false;
        }

        if (velocity.y < -maxFallSpeed)
            velocity.y = -maxFallSpeed;

        _rb.linearVelocity = velocity;
    }

    void ProbeGround()
    {
        var origin = groundCheck != null ? groundCheck.position : transform.position;
        IsGrounded = Physics2D.OverlapBox(origin, groundCheckSize, 0f, GameLayers.GroundMask);

        if (IsGrounded)
            _coyote = coyoteTime;
        else
            _coyote -= Time.fixedDeltaTime;
    }

    void UpdateFacingAndSprite()
    {
        if (_moveInput > 0.05f)
            _facingRight = true;
        else if (_moveInput < -0.05f)
            _facingRight = false;

        if (visual != null)
        {
            var scale = visual.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (_facingRight ? 1f : -1f);
            visual.transform.localScale = scale;

            if (animator == null)
                animator = GetComponent<PlayerSpriteAnimator>();
            animator?.SetLocomotion(IsGrounded, Mathf.Abs(_moveInput) > 0.05f);
        }
    }

    public void Respawn()
    {
        _rb.linearVelocity = Vector2.zero;
        transform.position = _spawnPosition;
    }

    void OnDrawGizmosSelected()
    {
        var origin = groundCheck != null ? groundCheck.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, groundCheckSize);
    }
}
