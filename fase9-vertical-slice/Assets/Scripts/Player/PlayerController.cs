using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement — tunear en Sprint 1")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _jumpForce = 16f;
    [SerializeField] private float _gravity = -35f;
    [SerializeField] private float _coyoteTime = 0.12f;
    [SerializeField] private float _jumpBuffer = 0.1f;
    [SerializeField] private float _maxFallSpeed = -40f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _groundCheckDist = 0.05f;

    [Header("References")]
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Animator _animator;

    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;
    private TimeManager _timeManager;
    private InputReader _input;

    private Vector2 _velocity;
    private float _moveInput;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _isGrounded;
    private bool _wasGrounded;

    public bool FacingRight { get; private set; } = true;
    public PlayerState CurrentState { get; private set; }

    public enum PlayerState { Idle, Walk, Jump, Fall, BulletTime, Hurt }

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimGrounded = Animator.StringToHash("Grounded");
    private static readonly int AnimVelY = Animator.StringToHash("VelocityY");

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _col = GetComponent<CapsuleCollider2D>();
        Services.Register(this);
    }

    private void OnEnable()
    {
        _timeManager = Services.Get<TimeManager>();
        _input = Services.Get<InputReader>();
        _input.OnMove += OnMove;
        _input.OnJumpStarted += OnJumpStarted;
    }

    private void OnDisable()
    {
        if (_input == null) return;
        _input.OnMove -= OnMove;
        _input.OnJumpStarted -= OnJumpStarted;
    }

    private void OnMove(float h) => _moveInput = h;

    private void OnJumpStarted() => _jumpBufferTimer = _jumpBuffer;

    private void FixedUpdate()
    {
        float dt = _timeManager.Delta(TimeManager.Layer.Player);

        CheckGround();
        ApplyGravity(dt);
        HandleCoyote(dt);
        HandleJumpBuffer(dt);

        _velocity.x = _moveInput * _moveSpeed;

        Vector2 move = _velocity * dt;
        move = ResolveCollisions(move);
        _rb.MovePosition(_rb.position + move);

        UpdateFacing();
        UpdateState();
        UpdateAnimator();
    }

    private void CheckGround()
    {
        _wasGrounded = _isGrounded;
        Vector2 origin = (Vector2)transform.position + _col.offset - new Vector2(0, _col.size.y * 0.5f);
        var hit = Physics2D.BoxCast(origin, new Vector2(_col.size.x * 0.9f, 0.05f), 0f,
            Vector2.down, _groundCheckDist, _groundMask);
        _isGrounded = hit.collider != null;

        // Aterrizando este frame
        if (_isGrounded && !_wasGrounded)
        {
            if (Services.TryGet<VFXPool>(out var vfx))
                vfx.Play("LandDust", transform.position, Color.white);
        }
    }

    private void ApplyGravity(float dt)
    {
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f; // pequeña fuerza hacia el suelo para mantener grounded
        else
            _velocity.y = Mathf.Max(_velocity.y + _gravity * dt, _maxFallSpeed);
    }

    private void HandleCoyote(float dt)
    {
        if (_isGrounded) _coyoteTimer = _coyoteTime;
        else _coyoteTimer -= dt;
    }

    private void HandleJumpBuffer(float dt)
    {
        if (_jumpBufferTimer <= 0f) return;
        _jumpBufferTimer -= dt;

        if (_coyoteTimer > 0f)
        {
            _velocity.y = _jumpForce;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;

            if (Services.TryGet<VFXPool>(out var vfx))
                vfx.Play("JumpDust", transform.position, Color.white);
        }
    }

    private Vector2 ResolveCollisions(Vector2 move)
    {
        float skinWidth = 0.01f;
        Vector2 size = _col.size - new Vector2(skinWidth * 2f, skinWidth * 2f);
        Vector2 center = (Vector2)transform.position + _col.offset;

        // Vertical
        if (move.y != 0f)
        {
            float dir = Mathf.Sign(move.y);
            var hit = Physics2D.BoxCast(center, size, 0f, Vector2.up * dir,
                Mathf.Abs(move.y) + skinWidth, _groundMask);
            if (hit.collider)
            {
                move.y = (hit.distance - skinWidth) * dir;
                _velocity.y = 0f;
                if (dir < 0) _isGrounded = true;
            }
        }

        // Horizontal
        if (move.x != 0f)
        {
            float dir = Mathf.Sign(move.x);
            var hit = Physics2D.BoxCast(center, size, 0f, Vector2.right * dir,
                Mathf.Abs(move.x) + skinWidth, _groundMask);
            if (hit.collider)
                move.x = (hit.distance - skinWidth) * dir;
        }

        return move;
    }

    private void UpdateFacing()
    {
        if (_moveInput > 0.1f) FacingRight = true;
        else if (_moveInput < -0.1f) FacingRight = false;

        if (_sprite) _sprite.flipX = !FacingRight;
    }

    private void UpdateState()
    {
        if (_timeManager.IsBulletTimeActive) { CurrentState = PlayerState.BulletTime; return; }
        if (!_isGrounded) { CurrentState = _velocity.y > 0 ? PlayerState.Jump : PlayerState.Fall; return; }
        CurrentState = Mathf.Abs(_moveInput) > 0.1f ? PlayerState.Walk : PlayerState.Idle;
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;
        _animator.SetFloat(AnimSpeed, Mathf.Abs(_moveInput));
        _animator.SetBool(AnimGrounded, _isGrounded);
        _animator.SetFloat(AnimVelY, _velocity.y);
    }

    public void ResetToPosition(Vector2 pos)
    {
        _rb.position = pos;
        _velocity = Vector2.zero;
        _moveInput = 0f;
    }
}
