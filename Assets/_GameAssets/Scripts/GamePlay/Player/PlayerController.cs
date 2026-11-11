using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private KeyCode movementkey;
    [SerializeField] private float movementSpeed;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float playerHeight;
    [SerializeField] private float groundDrag;
    [Header("JumpSettings")]
    [SerializeField] private KeyCode JumpKey;
    [SerializeField] private float JumpForce;
    [SerializeField] private bool canjump;
    [SerializeField] private float jumpCooldown;
    [Header("Slide Settings")]
    [SerializeField] private KeyCode slideKey;
    [SerializeField] private bool sliding;
    [SerializeField] private float slidemultiplier;
    [SerializeField] private float slideDrag;
    private StateController _statecontroller;
    private void SetJump()
    {
        _playerrigidbody.linearVelocity = new Vector3(_playerrigidbody.linearVelocity.x, 0f, _playerrigidbody.linearVelocity.z);
        _playerrigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }
    private Vector3 _movementDirection;
    private float _HorizontalInput, _VerticalInput;
    [SerializeField] private Transform _playerorientation;
    private Rigidbody _playerrigidbody;
    private void Awake()
    {
        _playerrigidbody = GetComponent<Rigidbody>();
        _playerrigidbody.freezeRotation = true;
        _statecontroller = GetComponent<StateController>();
    }
    private void Setinput()
    {
        _HorizontalInput = Input.GetAxis("Horizontal");
        _VerticalInput = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(JumpKey) && canjump && IsGrounded())
        {
            canjump = false;
            SetJump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        if (Input.GetKeyDown(slideKey))
        {
            sliding = true;

        }
        else if (Input.GetKeyUp(slideKey))
        {
            sliding = false;
        }
    }
    private void Update()
    {
        Setinput();
        SetPlayerDrag();
        LimitPlayerSpeed();
        SetStates();
    }
    private void FixedUpdate()
    {
        Movement();
    }
    private void Movement()
    {
         
        if (sliding)
        {
            _playerrigidbody.AddForce(_movementDirection.normalized * movementSpeed * slidemultiplier, ForceMode.Force);
        }
        _movementDirection = _playerorientation.forward * _VerticalInput + _playerorientation.right * _HorizontalInput;

    }
    private void ResetJump()
    {
        canjump = true;
    }
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);
    }
    private void SetPlayerDrag()
    {
        if (sliding)
        {
            _playerrigidbody.linearDamping = slideDrag;
        }
        else
        {
            _playerrigidbody.linearDamping = groundDrag;
        }
    }
    private void LimitPlayerSpeed()
    {
        Vector3 flatVelocity = new Vector3(_playerrigidbody.linearVelocity.x, 0f, _playerrigidbody.linearVelocity.z);
        if (flatVelocity.magnitude > movementSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * movementSpeed;
            _playerrigidbody.linearVelocity = new Vector3(limitedVelocity.x, _playerrigidbody.linearVelocity.y, limitedVelocity.z);
        }
    }
    private void SetStates()
    {
        var movementDirection = _playerorientation.forward * _VerticalInput + _playerorientation.right * _HorizontalInput;
        var isgrounded = IsGrounded();
        var currentstate = _statecontroller.GetCurrentState();
        var isSliding = IsSliding();
        var newstate = currentstate switch
        {
            _ when movementDirection == Vector3.zero && isgrounded && sliding => PlayerState.Idle,
            _ when movementDirection != Vector3.zero && isgrounded && sliding => PlayerState.Moving,
            _ when movementDirection == Vector3.zero && isgrounded && !sliding => PlayerState.Slideidle,
            _ when movementDirection != Vector3.zero && isgrounded && !sliding => PlayerState.Sliding,
            _ when !isgrounded => PlayerState.Jumping,
            _ => currentstate

        };
        if( newstate != currentstate)
        {
            _statecontroller.ChangeState(newstate);
        }

    }
    private bool IsSliding()
    {
        return sliding;
    }
}