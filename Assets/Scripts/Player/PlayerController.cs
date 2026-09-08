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
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.34f, 0.28f);

    [Header("Visual")]
    [SerializeField] SpriteRenderer visual;
    [SerializeField] PlayerSpriteAnimator animator;
    [SerializeField] bool playGetupOnStart = true;
    [SerializeField]
    [Tooltip("Sole source for Visual local Y (boots to floor). Edit this — not Visual Transform. Applied on Awake/Bind.")]
    float visualFeetOffset = -0.2f;

    [Header("Getup Collider")]
    [SerializeField] bool shrinkColliderDuringGetup = true;
    [SerializeField] Vector2 getupColliderSize = new Vector2(0.7f, 0.42f);
    // Bottom aligned with standing capsule (offset y 0.629543, size y 1.705085).
    [SerializeField] Vector2 getupColliderOffset = new Vector2(0.1400892f, -0.013f);

    Rigidbody2D _rb;
    CapsuleCollider2D _capsule;
    InputAction _move;
    InputAction _jump;

    float _moveInput;
    float _coyote;
    float _jumpBuffer;
    bool _cutJump;
    bool _facingRight = true;
    Vector3 _spawnPosition;
    PhysicsMaterial2D _noFriction;
    bool _introGetupStarted;
    bool _wasControlLocked;
    Vector2 _standingColliderSize;
    Vector2 _standingColliderOffset;
    bool _hasStandingCollider;
    Sprite[] _idleUnarmed;
    Sprite[] _idleArmed;
    bool _hasOathblade;

    public Vector2 SpawnPosition
    {
        get => _spawnPosition;
        set => _spawnPosition = value;
    }

    public bool IsGrounded { get; private set; }

    /// <summary>True after picking up 污光断剑·残誓 — armed idle / future attack unlock.</summary>
    public bool HasOathblade => _hasOathblade;

    public bool IsControlLocked => animator != null && animator.IsBusy;

    public void Bind(SpriteRenderer spriteRenderer, Transform ground)
    {
        visual = spriteRenderer;
        groundCheck = ground;
        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();
        animator?.Bind(spriteRenderer);
        CacheCapsule();
        ApplyVisualFeetOffset(visualFeetOffset);
        SyncGroundCheckToCapsule();
    }

    /// <summary>Standing / getup capsules; getup should share the standing bottom so shrink does not lift the root.</summary>
    public void ConfigureColliders(Vector2 standingSize, Vector2 standingOffset, Vector2 getupSize, Vector2 getupOffset)
    {
        CacheCapsule();
        getupColliderSize = getupSize;
        getupColliderOffset = getupOffset;
        _standingColliderSize = standingSize;
        _standingColliderOffset = standingOffset;
        _hasStandingCollider = true;
        if (_capsule == null)
            return;
        if (!IsControlLocked)
        {
            _capsule.size = standingSize;
            _capsule.offset = standingOffset;
        }
        SyncGroundCheckToCapsule();
    }

    /// <summary>
    /// Read tuneable capsules for bake: live CapsuleCollider2D while standing;
    /// cached standing if currently in getup shrink; getup from Inspector fields (or live capsule in getup).
    /// </summary>
    public void GetColliderTune(
        out Vector2 standingSize,
        out Vector2 standingOffset,
        out Vector2 getupSize,
        out Vector2 getupOffset)
    {
        CacheCapsule();
        bool inGetupShrink = IsControlLocked && shrinkColliderDuringGetup;

        if (inGetupShrink && _hasStandingCollider)
        {
            standingSize = _standingColliderSize;
            standingOffset = _standingColliderOffset;
        }
        else if (_capsule != null)
        {
            standingSize = _capsule.size;
            standingOffset = _capsule.offset;
        }
        else if (_hasStandingCollider)
        {
            standingSize = _standingColliderSize;
            standingOffset = _standingColliderOffset;
        }
        else
        {
            standingSize = new Vector2(0.5535295f, 1.705085f);
            standingOffset = new Vector2(0.1400892f, 0.629543f);
        }

        if (inGetupShrink && _capsule != null)
        {
            getupSize = _capsule.size;
            getupOffset = _capsule.offset;
        }
        else
        {
            getupSize = getupColliderSize;
            getupOffset = getupColliderOffset;
        }
    }

    /// <summary>
    /// Keep the foot OverlapBox centered on the standing capsule bottom.
    /// Half the box hangs below the feet so small physics contact gaps still count as grounded.
    /// </summary>
    public void SyncGroundCheckToCapsule()
    {
        if (!_hasStandingCollider)
            CacheCapsule();
        if (!_hasStandingCollider)
            return;

        float bottom = _standingColliderOffset.y - _standingColliderSize.y * 0.5f;
        if (groundCheck != null)
            groundCheck.localPosition = new Vector3(_standingColliderOffset.x, bottom, 0f);

        // Narrower than capsule (avoid wall→ground false positives); tall enough to bridge contact skin.
        groundCheckSize = new Vector2(_standingColliderSize.x * 0.74f, 0.28f);
    }

    public void ApplyVisualFeetOffset(float localY)
    {
        visualFeetOffset = localY;
        if (visual == null)
            return;
        var t = visual.transform;
        var p = t.localPosition;
        p.y = visualFeetOffset;
        t.localPosition = p;
    }

    /// <summary>Raycast down and park capsule bottom on the first Ground hit.</summary>
    public void SnapToGround(float maxDistance = 4f)
    {
        CacheCapsule();
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
        if (_capsule == null)
            return;

        float bottom = _capsule.offset.y - _capsule.size.y * 0.5f;
        var origin = (Vector2)transform.position + Vector2.up * Mathf.Max(0.2f, _capsule.size.y * 0.5f);
        var hit = Physics2D.Raycast(origin, Vector2.down, maxDistance, GameLayers.GroundMask);
        if (hit.collider == null)
            return;

        var p = transform.position;
        p.y = hit.point.y - bottom;
        transform.position = p;
        if (_rb != null)
        {
            // Keep body pose in sync — otherwise Interpolate lerps from a stale rb.position on unlock.
            _rb.position = p;
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetSprites(Sprite idle, Sprite run)
    {
        SetAnimationFrames(
            idle != null ? new[] { idle } : null,
            run != null ? new[] { run } : null);
    }

    public void SetAnimationFrames(
        Sprite[] idle,
        Sprite[] run,
        Sprite[] getup = null,
        Sprite[] jump = null,
        Sprite[] idleArmed = null,
        bool equipOathblade = false)
    {
        _idleUnarmed = idle;
        _idleArmed = idleArmed;
        _hasOathblade = equipOathblade;

        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();
        if (animator == null)
            animator = gameObject.AddComponent<PlayerSpriteAnimator>();
        animator.Bind(visual);
        animator.SetFrames(ActiveIdleFrames(), run);
        animator.SetJumpFrames(jump);
        animator.SetGetupFrames(getup);

        if (visual == null)
            return;

        if (getup != null && getup.Length > 0 && getup[0] != null && playGetupOnStart)
            visual.sprite = getup[0];
        else
        {
            var idleNow = ActiveIdleFrames();
            if (idleNow != null && idleNow.Length > 0 && idleNow[0] != null)
                visual.sprite = idleNow[0];
        }
    }

    /// <summary>Equip 残誓 and switch to armed idle when available.</summary>
    public void EquipOathblade()
    {
        SetOathblade(true);
    }

    public void SetOathblade(bool equipped)
    {
        _hasOathblade = equipped;
        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();
        if (animator == null)
            return;

        animator.SetIdleFrames(ActiveIdleFrames());
    }

    [ContextMenu("Equip Oathblade (preview)")]
    void ContextEquipOathblade()
    {
        EquipOathblade();
    }

    [ContextMenu("Unequip Oathblade (preview)")]
    void ContextUnequipOathblade()
    {
        SetOathblade(false);
    }

    Sprite[] ActiveIdleFrames()
    {
        if (_hasOathblade && _idleArmed != null && _idleArmed.Length > 0 && _idleArmed[0] != null)
            return _idleArmed;
        return _idleUnarmed;
    }

    /// <summary>Play one-shot getup and lock move/jump until it finishes.</summary>
    public bool PlayGetupIntro()
    {
        if (animator == null)
            animator = GetComponent<PlayerSpriteAnimator>();
        if (animator == null || !animator.HasGetup)
            return false;

        _moveInput = 0f;
        _jumpBuffer = 0f;
        _cutJump = false;
        _facingRight = true;
        if (visual != null)
        {
            var scale = visual.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            visual.transform.localScale = scale;
        }

        bool started = animator.PlayGetup();
        if (started)
        {
            // Getup capsule + pinned physics first, then snap — avoids Dynamic depenetration lift.
            ApplyGetupCollider(true);
            SetIntroPhysicsPinned(true);
            SnapToGround();
            if (_rb != null)
                _rb.position = transform.position;
        }
        return started;
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

        CacheCapsule();
        ApplyVisualFeetOffset(visualFeetOffset);
        SyncGroundCheckToCapsule();
        BindInput();
        _spawnPosition = transform.position;
    }

    void Start()
    {
        if (!Application.isPlaying)
            return;

        // Scene may serialize Kinematic from edit-mode builds; force gameplay body.
        if (_rb != null)
            _rb.bodyType = RigidbodyType2D.Dynamic;

        if (playGetupOnStart && !_introGetupStarted)
        {
            _introGetupStarted = true;
            if (PlayGetupIntro())
            {
                _spawnPosition = transform.position;
                return;
            }
        }

        if (_rb != null)
            _rb.gravityScale = gravityScale;
        SnapToGround();
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
        bool locked = IsControlLocked;
        if (_wasControlLocked && !locked)
            EndIntroLock();
        _wasControlLocked = locked;

        if (locked)
        {
            _moveInput = 0f;
            _cutJump = false;

            // Keep jump buffer so a press near the end of getup still fires after unlock.
            if (_jump != null && _jump.WasPressedThisFrame())
                _jumpBuffer = jumpBufferTime;
            if (_jumpBuffer > 0f)
                _jumpBuffer -= Time.deltaTime;

            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;

            animator?.SetLocomotion(IsGrounded, false, _rb != null ? _rb.linearVelocity.y : 0f);
            if (transform.position.y < -12f)
                HandleFatalFall();
            return;
        }

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
            HandleFatalFall();
    }

    void FixedUpdate()
    {
        ProbeGround();

        var velocity = _rb.linearVelocity;

        if (IsControlLocked)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

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
        CacheCapsule();

        Vector2 origin;
        Vector2 size = groundCheckSize;
        if (_capsule != null)
        {
            // Live capsule (standing or getup) — do not trust a stale GroundCheck transform alone.
            float bottom = _capsule.offset.y - _capsule.size.y * 0.5f;
            origin = (Vector2)transform.position + new Vector2(_capsule.offset.x, bottom);
            size = new Vector2(_capsule.size.x * 0.74f, Mathf.Max(0.28f, groundCheckSize.y));
        }
        else
        {
            origin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        }

        IsGrounded = Physics2D.OverlapBox(origin, size, 0f, GameLayers.GroundMask);

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
            float vy = _rb != null ? _rb.linearVelocity.y : 0f;
            animator?.SetLocomotion(IsGrounded, Mathf.Abs(_moveInput) > 0.05f, vy);
        }
    }

    public void ApplyHitKnockback(Vector2 velocity)
    {
        if (_rb == null || IsControlLocked)
            return;

        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = velocity;
        _coyote = 0f;
        _jumpBuffer = 0f;
        _cutJump = false;
    }

    public void Respawn()
    {
        ApplyGetupCollider(false);
        SetIntroPhysicsPinned(false);
        _wasControlLocked = false;
        _rb.linearVelocity = Vector2.zero;
        transform.position = _spawnPosition;
        if (_rb != null)
            _rb.position = _spawnPosition;
        _moveInput = 0f;
        _jumpBuffer = 0f;
        _cutJump = false;
        SnapToGround();
    }

    void HandleFatalFall()
    {
        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.Kill();
        else
            Respawn();
    }

    void CacheCapsule()
    {
        if (_capsule == null)
            _capsule = GetComponent<CapsuleCollider2D>();
        if (_capsule == null || _hasStandingCollider)
            return;

        _standingColliderSize = _capsule.size;
        _standingColliderOffset = _capsule.offset;
        _hasStandingCollider = true;
    }

    void ApplyGetupCollider(bool getup)
    {
        if (!shrinkColliderDuringGetup)
            return;

        CacheCapsule();
        if (_capsule == null || !_hasStandingCollider)
            return;

        if (getup)
        {
            _capsule.size = getupColliderSize;
            _capsule.offset = getupColliderOffset;
        }
        else
        {
            _capsule.size = _standingColliderSize;
            _capsule.offset = _standingColliderOffset;
        }
    }

    void SetIntroPhysicsPinned(bool pinned)
    {
        if (_rb == null)
            return;
        if (pinned)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;
            // Kinematic avoids Dynamic depenetration lifting the root off the snap pose.
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.position = transform.position;
        }
        else
        {
            // Sync before Dynamic so interpolation does not ease down from an old body pose.
            var interp = _rb.interpolation;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.position = transform.position;
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = gravityScale;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.interpolation = interp;
        }
    }

    void EndIntroLock()
    {
        ApplyGetupCollider(false);
        // Intro already snapped while pinned; skip SnapToGround here (it + Interpolate read as a drop).
        SetIntroPhysicsPinned(false);
        _spawnPosition = transform.position;
    }

    void OnDrawGizmosSelected()
    {
        CacheCapsule();
        Vector2 origin;
        Vector2 size = groundCheckSize;
        if (_capsule != null)
        {
            float bottom = _capsule.offset.y - _capsule.size.y * 0.5f;
            origin = (Vector2)transform.position + new Vector2(_capsule.offset.x, bottom);
            size = new Vector2(_capsule.size.x * 0.74f, Mathf.Max(0.28f, groundCheckSize.y));
        }
        else
        {
            origin = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, size);
    }
}
