using System.Collections;
using UnityEngine;

public class NormalEnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Search }

    [Header("State")]
    [SerializeField] private State currentState = State.Patrol;

    [Header("Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float moveSpeed = 3f, viewDistance = 7f, viewAngle = 180f;
    [SerializeField] private LayerMask playerLayer, obstacleLayer;
    [SerializeField] private float attackRange = 1.2f, attackCooldown = 1.5f, waitAtPatrolPoint = 1.5f, searchDuration = 3f;
    [SerializeField] private int attackDamage = 10;

    private int patrolIndex;
    private Vector2 lastKnownPos;
    private float stateTimer, lastAttack = -999f;
    private Vector2 facing = Vector2.down;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerTransform = p.transform;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:   IdleUpdate(); break;
            case State.Patrol: PatrolUpdate(); break;
            case State.Chase:  ChaseUpdate(); break;
            case State.Attack: AttackUpdate(); break;
            case State.Search: SearchUpdate(); break;
        }
    }

    void ChangeState(State newState)
    {
        currentState = newState;
        if (newState == State.Search) stateTimer = searchDuration;
        if (newState == State.Idle) stateTimer = waitAtPatrolPoint;
    }

    void IdleUpdate()
    {
        if (SeePlayer()) { ChangeState(State.Chase); return; }
        if ((stateTimer -= Time.deltaTime) <= 0f) ChangeState(State.Patrol);
    }

    void PatrolUpdate()
    {
        if (SeePlayer()) { ChangeState(State.Chase); return; }
        if (patrolPoints == null || patrolPoints.Length == 0) { ChangeState(State.Idle); return; }

        var target = patrolPoints[patrolIndex].position;
        MoveTo(target);

        if (Vector2.Distance(transform.position, target) < 0.2f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            ChangeState(State.Idle);
        }
    }

    void ChaseUpdate()
    {
        if (SeePlayer())
        {
            lastKnownPos = playerTransform.position;
            if (Vector2.Distance(transform.position, lastKnownPos) <= attackRange) { ChangeState(State.Attack); return; }
            MoveTo(lastKnownPos);
        }
        else ChangeState(State.Search);
    }

    void AttackUpdate()
    {
        if (playerTransform == null) { ChangeState(State.Patrol); return; }
        float d = Vector2.Distance(transform.position, playerTransform.position);
        
        if (d > attackRange) { ChangeState(State.Chase); return; }

        var dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        if (dir != Vector2.zero) facing = dir;

        if (Time.time >= lastAttack + attackCooldown)
        {
            DoAttack();
            lastAttack = Time.time;
        }
    }

    void SearchUpdate()
    {
        if (SeePlayer()) { ChangeState(State.Chase); return; }

        if (Vector2.Distance(transform.position, lastKnownPos) > 0.2f)
        {
            MoveTo(lastKnownPos);
        }
        else
        {
            // Осматриваемся на месте
            facing = Quaternion.Euler(0, 0, 90 * Time.deltaTime) * facing;
            if ((stateTimer -= Time.deltaTime) <= 0f) ChangeState(State.Patrol);
        }
    }

    void MoveTo(Vector3 pos)
    {
        var dir = ((Vector2)pos - (Vector2)transform.position);
        if (dir != Vector2.zero) facing = dir.normalized;
        
        if (rb != null)
            rb.MovePosition(Vector2.MoveTowards(rb.position, pos, moveSpeed * Time.deltaTime));
        else
            transform.position = Vector2.MoveTowards(transform.position, pos, moveSpeed * Time.deltaTime);
    }

    bool SeePlayer()
    {
        if (playerTransform == null) return false;
        var toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        if (dist > viewDistance) return false;
        if (Vector2.Angle(facing, toPlayer) > viewAngle * 0.5f) return false;

        var hit = Physics2D.Raycast(transform.position, toPlayer.normalized, dist, obstacleLayer | playerLayer);
        return hit.collider != null && hit.collider.transform == playerTransform;
    }

    void DoAttack()
    {
        Debug.Log($"Attack {attackDamage}");
        if (sr) StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        var c = sr.color; sr.color = Color.red; yield return new WaitForSeconds(0.12f); sr.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, viewDistance);
        Gizmos.color = Color.cyan;
        var l = Quaternion.Euler(0, 0, viewAngle * 0.5f) * facing; 
        var r = Quaternion.Euler(0, 0, -viewAngle * 0.5f) * facing;
        Gizmos.DrawRay(transform.position, l * viewDistance); 
        Gizmos.DrawRay(transform.position, r * viewDistance); 
        Gizmos.DrawRay(transform.position, facing * viewDistance);
    }
}