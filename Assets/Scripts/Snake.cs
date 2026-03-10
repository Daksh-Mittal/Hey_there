using Globals;
using SteeringCalcs;
using UnityEngine;

public class Snake : MonoBehaviour
{
    // Obstacle avoidance parameters (see the assignment spec for an explanation).
    public AvoidanceParams AvoidParams;

    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // Use this as the arrival radius for all states where the steering behaviour == arrive.
    public float ArriveRadius;

    // Parameters controlling transitions in/out of the Aggro state.
    public float AggroRange;
    public float DeAggroRange;

    // Reference to the frog (the target for the Aggro state).
    public GameObject Frog;

    // The patrol point (the target for the PatrolAway state).
    public Transform PatrolPoint;

    // The snake's initial position (the target for the PatrolHome and Harmless states).
    private Vector2 _home;

    // Debug rendering config
    private float _debugHomeOffset = 0.3f;

    // References for gameobject controls
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;

    // Current Snake FSM State
    public SnakeState State;

    // Snake FSM states (to be completed by you)
    public enum SnakeState : int
    {
        TODO = 0
    }

    // Snake FSM events (to be completed by you)
    public enum SnakeEvent : int
    {
        TODO = 0
    }

    // Direction IDs used by the snake animator (please don't edit these).
    private enum Direction : int
    {
        Up = 0,
        Left = 1,
        Down = 2,
        Right = 3
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        _home = transform.position;
    }

    // Our common FSM approach has been setup for you.
    // This is an event-first FSM, where events can be triggered by FixedUpdateEvents().
    // Then FSM_State() processes the current FSM state.
    // UpdateAppearance() is called at the end to update the snake's appearance.
    void FixedUpdate()
    {
        // Events triggered by each fixed update tick
        FixedUpdateEvents();

        // Update the Snake behaviour based on the current FSM state
        FSM_State();

        // Configure final appearance of the snake
        UpdateAppearance();
    } 

    // Trigger Events for each fixed update tick, using a trigger first FSM implementation
    void FixedUpdateEvents()
    {
        
    }


    // Process the current FSM state, using an event first FSM implementation
    // This currently has a zero steering force.
    // You need to implement the steering logic depending on the FSM state.
    void FSM_State()
    {
        Vector2 desiredVel = Vector2.zero;

        // Convert the desired velocity to a force, then apply it.
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
    }

    private void SetState(SnakeState newState)
    {
        if (newState != State)
        {
            // Can uncomment this for debugging purposes.
            //Debug.Log(name + " switching state to " + newState.ToString());

            State = newState;
        }
    }

    private void HandleEvent(SnakeEvent e)
    {
    }
    private void UpdateAppearance()
    {
        // Update the snake's colour to provide a visual indication of its state.
        // This is for you to implement

        // Update the Snake visual based on the direction it's moving
        // (please don't modify this block)
        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            // Determine the bearing of the snake in degrees (between -180 and 180)
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;

            if (angle > -135.0f && angle <= -45.0f) // Down
            {
                transform.up = new Vector2(0.0f, -1.0f);
                _animator.SetInteger("Direction", (int)Direction.Down);
            }
            else if (angle > -45.0f && angle <= 45.0f) // Right
            {
                transform.up = new Vector2(1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Right);
            }
            else if (angle > 45.0f && angle <= 135.0f) // Up
            {
                transform.up = new Vector2(0.0f, 1.0f);
                _animator.SetInteger("Direction", (int)Direction.Up);
            }
            else // Left
            {
                transform.up = new Vector2(-1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Left);
            }
        }

        // Display the Snake home position as a cross
        Debug.DrawLine(_home + new Vector2(-_debugHomeOffset, -_debugHomeOffset), _home + new Vector2(_debugHomeOffset, _debugHomeOffset), Color.magenta);
        Debug.DrawLine(_home + new Vector2(-_debugHomeOffset, _debugHomeOffset), _home + new Vector2(_debugHomeOffset, -_debugHomeOffset), Color.magenta);
    }
}
