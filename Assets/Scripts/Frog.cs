using UnityEngine;
using SteeringCalcs;
using Globals;
using UnityEngine.InputSystem;


public class Frog : MonoBehaviour
{
    // Steering parameters
    public enum FrogSteeringType : int
    {
        Seek = 0,
        Arrive = 1
    }
    public FrogSteeringType SteeringType;
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // The arrival radius is set up to be dynamic, depending on how far away
    // the player left-clicks from the frog
    public float ArrivePct;
    public float MinArriveRadius;
    public float MaxArriveRadius;
    private float _arriveRadius;

    // Turn this off to make it easier to see overshooting when seek is used
    // instead of arrive
    public bool HideFlagOnceReached;

    // References to various objects in the scene that we want to be able to modify
    private Transform _flag;
    private SpriteRenderer _flagSr;
    private Animator _animator;
    private Rigidbody2D _rb;
    private InputAction _moveTargetAction;
    private Vector2? _lastClickPos;

    void Start()
    {
        // Initialise the various object references
        _flag = GameObject.Find("Flag").transform;
        _flagSr = _flag.GetComponent<SpriteRenderer>();
        _flagSr.enabled = false;

        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _lastClickPos = _rb.transform.position;
        _arriveRadius = MinArriveRadius;

        _moveTargetAction = InputSystem.actions.FindAction("Attack");
    }

    void Update()
    {
        // Check whether the player left-clicked
        
        if (_moveTargetAction.WasPressedThisFrame())
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            _lastClickPos = (Vector2)Camera.main.ScreenToWorldPoint(mousePos);

            // Set the arrival radius dynamically.
            _arriveRadius = Mathf.Clamp(ArrivePct * ((Vector2)_lastClickPos - (Vector2)transform.position).magnitude, MinArriveRadius, MaxArriveRadius);

            _flag.position = (Vector2)_lastClickPos + new Vector2(0.55f, 0.55f);
            _flagSr.enabled = true;
        }
    }

    void FixedUpdate()
    {
        
        Move();
        UpdateAppearance();
    }

    private void Move()
    {
        Vector2 desiredVel = Vector2.zero;

        // If the last-clicked position is non-null, move there. Otherwise do nothing.
        if (_lastClickPos != null)
        {
            if (((Vector2)_lastClickPos - (Vector2)gameObject.transform.position).magnitude > Constants.TARGET_REACHED_TOLERANCE)
            {
                if (SteeringType == FrogSteeringType.Seek)
                {
                    desiredVel = Steering.SeekDirect(gameObject.transform.position, (Vector2)_lastClickPos, MaxSpeed);
                }
                else if (SteeringType == FrogSteeringType.Arrive)
                {
                    desiredVel = Steering.ArriveDirect(gameObject.transform.position, (Vector2)_lastClickPos, _arriveRadius, MaxSpeed);
                }
            }
            else
            {
                _lastClickPos = null;

                if (HideFlagOnceReached)
                {
                    _flagSr.enabled = false;
                }
            }
        }

        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
    }

    private void UpdateAppearance()
    {
        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            _animator.SetBool("Walking", true);
            transform.up = _rb.linearVelocity;
        }
        else
        {
            _animator.SetBool("Walking", false);
        }
    }
}
