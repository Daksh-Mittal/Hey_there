using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteeringCalcs;

public class Fly : MonoBehaviour
{
    public FlyState State;

    public float StopFleeingRange;
    public float FrogStillFleeRange;
    public float FrogMovingFleeRange;
    public float FrogAlertSpeed;
    public float BubbleFleeRange;

    public float RespawnTime;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Transform _frog;
    private Rigidbody2D _frogRb;

    private FlockSettings _settings;

    private float _timeDead;

    List<Transform> _neighbours;

    public enum FlyState : int
    {
        Flocking = 0,
        Alone = 1,
        Fleeing = 2,
        Dead = 3,
        Respawn = 4
    }

    public enum FlyEvent : int
    {
        JoinedFlock = 0,
        LostFlock = 1,
        ScaredByFrog = 2,
        EscapedFrog = 3,
        CaughtByFrog = 4,
        RespawnTimeElapsed = 5,
        NowAlive = 6,
        ScaredByBubble = 7
    }

    void Start()
    {
        _settings = transform.parent.GetComponent<FlockSettings>();
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        GameObject frog = GameObject.Find("Frog");
        if (frog != null)
        {
            _frog = frog.transform;
            _frogRb = frog.GetComponent<Rigidbody2D>();
        }

        _timeDead = 0.0f;
        _neighbours = new List<Transform>();
    }

    void FixedUpdate()
    {
        UpdateNeighbours();

        FixedUpdateEvents();

        FSM_State();

        UpdateAppearance();
    }
    
    void FixedUpdateEvents()
    {
        if (State == FlyState.Dead)
        {
            _timeDead += Time.fixedDeltaTime;

            if (_timeDead > RespawnTime)
            {
                HandleEvent(FlyEvent.RespawnTimeElapsed);
            }
        }

        if (State == FlyState.Flocking ||
            State == FlyState.Fleeing ||
            State == FlyState.Alone)
        {
            if (_neighbours.Count == 0)
                HandleEvent(FlyEvent.LostFlock);
            else
                HandleEvent(FlyEvent.JoinedFlock);
        }

        if (_frog != null)
        {
            float distToFrog = (transform.position - _frog.position).magnitude;

            if (_frogRb.linearVelocity.magnitude >= FrogAlertSpeed && distToFrog < FrogMovingFleeRange
                || _frogRb.linearVelocity.magnitude < FrogAlertSpeed && distToFrog < FrogStillFleeRange)
            {
                HandleEvent(FlyEvent.ScaredByFrog);
            }

            // Check bubble fear
            GameObject[] bubbles = GameObject.FindGameObjectsWithTag("Bubble");

            foreach (GameObject bubble in bubbles)
            {
                if ((transform.position - bubble.transform.position).magnitude < BubbleFleeRange)
                {
                    HandleEvent(FlyEvent.ScaredByBubble);
                }
            }

            bool safeFromFrog = distToFrog > StopFleeingRange;
            bool safeFromBubble = true;

            foreach (GameObject bubble in bubbles)
            {
                if ((transform.position - bubble.transform.position).magnitude < StopFleeingRange)
                {
                    safeFromBubble = false;
                    break;
                }
            }

            if (safeFromFrog && safeFromBubble)
            {
                HandleEvent(FlyEvent.EscapedFrog);
            }
        }
    }

    private void FSM_State()
    {
        Vector2 desiredVel = Vector2.zero;

        if (State == FlyState.Dead)
        {
            desiredVel = Vector2.zero;
        }
        else if (State == FlyState.Respawn)
        {
            Respawn();
            desiredVel = Vector2.zero;
        }
        else if (State == FlyState.Flocking)
        {
            Vector2 desiredSep = _settings.SeparationWeight *
                Steering.GetSeparation(transform.position, _neighbours, _settings.MaxSpeed);

            Vector2 desiredCoh = _settings.CohesionWeight *
                Steering.GetCohesion(transform.position, _neighbours, _settings.MaxSpeed);

            Vector2 desiredAli = _settings.AlignmentWeight *
                Steering.GetAlignment(_neighbours, _settings.MaxSpeed);

            Vector2 desiredAnch = _settings.AnchorWeight *
                Steering.GetAnchor(transform.position, _settings.AnchorDims);

            Debug.DrawLine(transform.position, (Vector2)transform.position + desiredSep, Color.red);
            Debug.DrawLine(transform.position, (Vector2)transform.position + desiredCoh, Color.green);
            Debug.DrawLine(transform.position, (Vector2)transform.position + desiredAli, Color.blue);
            Debug.DrawLine(transform.position, (Vector2)transform.position + desiredAnch, Color.yellow);

            desiredVel = (desiredSep + desiredCoh + desiredAli + desiredAnch).normalized * _settings.MaxSpeed;
        }
        else if (State == FlyState.Alone)
        {
            Transform nearestFly = null;

            foreach (Transform flockMember in transform.parent)
            {
                if (flockMember.GetComponent<Fly>().State != FlyState.Dead && flockMember != transform)
                {
                    if (nearestFly == null ||
                        (transform.position - flockMember.position).magnitude <
                        (transform.position - nearestFly.position).magnitude)
                    {
                        nearestFly = flockMember;
                    }
                }
            }

            if (nearestFly != null)
            {
                desiredVel = Steering.SeekDirect(transform.position, nearestFly.position, _settings.MaxSpeed);
                Debug.DrawLine(transform.position, nearestFly.position, Color.yellow);
            }
        }
        else if (State == FlyState.Fleeing)
        {
            desiredVel = Steering.FleeDirect(transform.position, _frog.position, _settings.MaxSpeed);
        }

        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, _settings.AccelTime, _settings.MaxAccel);
        _rb.AddForce(steering);
    }

    private void Respawn()
    {
        float randomAngle = Random.Range(-Mathf.PI, Mathf.PI);
        Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));

        transform.position = 20.0f * randomDirection;

        _timeDead = 0.0f;

        HandleEvent(FlyEvent.NowAlive);
    }

    private void UpdateNeighbours()
    {
        _neighbours.Clear();

        foreach (Transform flockMember in transform.parent)
        {
            if (flockMember.GetComponent<Fly>().State != FlyState.Dead &&
                flockMember != transform &&
                (transform.position - flockMember.position).magnitude < _settings.FlockRadius)
            {
                _neighbours.Add(flockMember);
            }
        }
    }

    private void UpdateAppearance()
    {
        _sr.flipX = _rb.linearVelocity.x > 0;

        if (State == FlyState.Flocking)
        {
            _sr.enabled = true;
            _sr.color = new Color(1, 1, 1);
        }
        else if (State == FlyState.Alone)
        {
            _sr.enabled = true;
            _sr.color = new Color(1, 0.52f, 0.01f);
        }
        else if (State == FlyState.Fleeing)
        {
            _sr.enabled = true;
            _sr.color = new Color(0.45f, 0.98f, 0.94f);
        }
        else if (State == FlyState.Dead || State == FlyState.Respawn)
        {
            _sr.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag.Equals("Frog"))
        {
            HandleEvent(FlyEvent.CaughtByFrog);
        }
    }

    private void SetState(FlyState newState)
    {
        if (newState != State)
        {
            State = newState;
        }
    }

    private void HandleEvent(FlyEvent e)
    {
        if (State == FlyState.Dead)
        {
            if (e == FlyEvent.RespawnTimeElapsed)
                SetState(FlyState.Respawn);
        }
        else if (State == FlyState.Respawn)
        {
            if (e == FlyEvent.NowAlive)
                SetState(FlyState.Flocking);
        }
        else
        {
            if (e == FlyEvent.CaughtByFrog)
            {
                SetState(FlyState.Dead);
            }
            else if (State == FlyState.Flocking)
            {
                if (e == FlyEvent.LostFlock)
                    SetState(FlyState.Alone);

                else if (e == FlyEvent.ScaredByFrog || e == FlyEvent.ScaredByBubble)
                    SetState(FlyState.Fleeing);
            }
            else if (State == FlyState.Alone)
            {
                if (e == FlyEvent.JoinedFlock)
                    SetState(FlyState.Flocking);

                else if (e == FlyEvent.ScaredByFrog || e == FlyEvent.ScaredByBubble)
                    SetState(FlyState.Fleeing);
            }
            else if (State == FlyState.Fleeing)
            {
                if (e == FlyEvent.EscapedFrog)
                    SetState(FlyState.Flocking);
            }
        }
    }
}
