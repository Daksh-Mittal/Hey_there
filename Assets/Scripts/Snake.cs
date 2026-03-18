using Globals;
using SteeringCalcs;
using UnityEngine;

public class Snake : MonoBehaviour
{
    private float _sleepTimer = 0.0f;

    public AvoidanceParams AvoidParams;

    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    public float ArriveRadius;

    public float AggroRange;
    public float DeAggroRange;

    public GameObject Frog;
    public Transform PatrolPoint;

    private Vector2 _home;

    private float _debugHomeOffset = 0.3f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;

    private Vector2 _fleeTarget;

    public SnakeState State;

    public enum SnakeState : int
    {
        PatrolAway = 0,
        PatrolHome = 1,
        Attack = 2,
        Harmless = 3,
        Snooze = 4,
        Fleeing = 5   // New
    }

    public enum SnakeEvent : int
    {
        FrogInRange = 0,
        FrogOutOfRange = 1,
        BitFrog = 2,
        ReachedTarget = 3,
        TimerOff = 4,
        HitByBubble = 5,   
        NotScared = 6      
    }

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

    void FixedUpdate()
    {
        FixedUpdateEvents();
        FSM_State();
        UpdateAppearance();
    }

    void FixedUpdateEvents()
    {
        if (State == SnakeState.Snooze)
        {
            _sleepTimer += Time.fixedDeltaTime;

            if (_sleepTimer >= Constants.SLEEP_TIME)
            {
                HandleEvent(SnakeEvent.TimerOff);
                _sleepTimer = 0.0f;
            }
        }

        if (State == SnakeState.Fleeing)
        {
            if (Vector2.Distance(transform.position, _fleeTarget) <= Constants.TARGET_REACHED_TOLERANCE)
            {
                HandleEvent(SnakeEvent.NotScared);
            }
        }

        if (Frog != null && State != SnakeState.Fleeing)
        {
            float distToFrog = Vector2.Distance(transform.position, Frog.transform.position);

            if (distToFrog <= AggroRange)
                HandleEvent(SnakeEvent.FrogInRange);

            if (distToFrog > DeAggroRange)
                HandleEvent(SnakeEvent.FrogOutOfRange);
        }

        Vector2 target = transform.position;

        if (State == SnakeState.PatrolAway)
            target = PatrolPoint.position;

        else if (State == SnakeState.PatrolHome || State == SnakeState.Harmless)
            target = _home;

        if (State == SnakeState.PatrolAway ||
            State == SnakeState.PatrolHome ||
            State == SnakeState.Harmless)
        {
            if (Vector2.Distance(transform.position, target) <= Constants.TARGET_REACHED_TOLERANCE)
            {
                HandleEvent(SnakeEvent.ReachedTarget);
            }
        }
    }

    void FSM_State()
    {
        Vector2 desiredVel = Vector2.zero;

        if (State == SnakeState.PatrolAway)
        {
            desiredVel = Steering.Arrive(transform.position, PatrolPoint.position, ArriveRadius, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.PatrolHome)
        {
            desiredVel = Steering.Arrive(transform.position, _home, ArriveRadius, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.Attack)
        {
            desiredVel = Steering.Seek(transform.position, Frog.transform.position, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.Harmless)
        {
            desiredVel = Steering.Arrive(transform.position, _home, ArriveRadius, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.Snooze)
        {
            desiredVel = Vector2.zero;
        }
        else if (State == SnakeState.Fleeing)
        {
            desiredVel = Steering.SeekDirect(transform.position, _fleeTarget, MaxSpeed); // move to flee target
        }

        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
    }

    private void SetState(SnakeState newState)
    {
        if (newState != State)
        {
            if (newState == SnakeState.Fleeing && Frog != null)
            {
                Vector2 dir = (transform.position - Frog.transform.position).normalized;
                _fleeTarget = (Vector2)transform.position + dir * 5f; // distance tweakable
            }

            State = newState;
        }
    }

    private void HandleEvent(SnakeEvent e)
    {
        if (e == SnakeEvent.HitByBubble && State != SnakeState.Fleeing)
        {
            SetState(SnakeState.Fleeing);
            return;
        }

        if (State == SnakeState.Fleeing)
        {
            if (e == SnakeEvent.NotScared)
                SetState(SnakeState.PatrolHome);

            return;
        }

        if (State == SnakeState.PatrolAway)
        {
            if (e == SnakeEvent.ReachedTarget)
                SetState(SnakeState.PatrolHome);

            else if (e == SnakeEvent.FrogInRange)
                SetState(SnakeState.Attack);
        }

        else if (State == SnakeState.PatrolHome)
        {
            if (e == SnakeEvent.ReachedTarget)
                SetState(SnakeState.PatrolAway);

            else if (e == SnakeEvent.FrogInRange)
                SetState(SnakeState.Attack);
        }

        else if (State == SnakeState.Attack)
        {
            if (e == SnakeEvent.FrogOutOfRange)
                SetState(SnakeState.PatrolHome);

            else if (e == SnakeEvent.BitFrog)
                SetState(SnakeState.Snooze);
        }

        else if (State == SnakeState.Snooze)
        {
            if (e == SnakeEvent.TimerOff)
                SetState(SnakeState.Harmless);
        }

        else if (State == SnakeState.Harmless)
        {
            if (e == SnakeEvent.ReachedTarget)
                SetState(SnakeState.PatrolHome);
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Frog"))
        {
            HandleEvent(SnakeEvent.BitFrog);
        }

        if (collider.CompareTag("Bubble"))
        {
            HandleEvent(SnakeEvent.HitByBubble);
        }
    }

    private void UpdateAppearance()
    {
        if (State == SnakeState.PatrolAway)
            _sr.color = new Color(0.5f, 0.5f, 0.5f);

        else if (State == SnakeState.PatrolHome)
            _sr.color = new Color(1f, 1f, 1f);

        else if (State == SnakeState.Attack)
            _sr.color = new Color(1f, 0.1f, 0.1f);

        else if (State == SnakeState.Harmless)
            _sr.color = new Color(0.2f, 0.9f, 0.2f);

        else if (State == SnakeState.Snooze)
            _sr.color = new Color(0.2f, 0.2f, 0.9f);

        else if (State == SnakeState.Fleeing)
            _sr.color = new Color(0.9f, 0.7f, 0.2f); 

        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;

            if (angle > -135 && angle <= -45)
            {
                transform.up = Vector2.down;
                _animator.SetInteger("Direction", 2);
            }
            else if (angle > -45 && angle <= 45)
            {
                transform.up = Vector2.right;
                _animator.SetInteger("Direction", 3);
            }
            else if (angle > 45 && angle <= 135)
            {
                transform.up = Vector2.up;
                _animator.SetInteger("Direction", 0);
            }
            else
            {
                transform.up = Vector2.left;
                _animator.SetInteger("Direction", 1);
            }
        }
    }
}
