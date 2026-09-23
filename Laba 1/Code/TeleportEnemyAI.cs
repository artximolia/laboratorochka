using System.Collections;
using UnityEngine;

public class TeleportEnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Search, Teleport }

    [Header("State")]
    [SerializeField] private State currentState = State.Patrol;

    [Header("Target & Movement")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float moveSpeed = 3f;

    [Header("detection 360")]
    [SerializeField] private float scanRadius = 1f;
    [SerializeField] private float scanInterval = 0.5f;
    [SerializeField] private LayerMask playerLayer, obstacleLayer;

    [Header("Attack & Search")]
    [SerializeField] private float attackRange = 1.2f, attackCooldown = 1.5f, waitAtPatrolPoint = 1.5f, searchDuration = 3f;
    [SerializeField] private int attackDamage = 15;

    [Header("Teleport Settings")]
    [SerializeField] private float tpCooldown = 5f;
    [SerializeField] private float maxTpDistance = 4f;

    private int patrolIndex;
    private Vector2 lastKnownPos;
    private float stateTimer, lastAttack = -999f, lastScanTime, lastTpTime = -999f;
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
            case State.Idle:     IdleUpdate(); break;
            case State.Patrol:   PatrolUpdate(); break;
            case State.Chase:    ChaseUpdate(); break;
            case State.Attack:   AttackUpdate(); break;
            case State.Search:   SearchUpdate(); break;
            case State.Teleport: TeleportUpdate(); break;
        }
    }

    void ChangeState(State newState)
    {
        currentState = newState;
        if (newState == State.Search) stateTimer = searchDuration;
        if (newState == State.Idle) stateTimer = waitAtPatrolPoint;
    }

    // логика состояний (Idle, Patrol, Chase, Attack, Search, Teleport)

    void IdleUpdate()
    {
        if (Scan360()) { ChangeState(State.Chase); return; }
        if ((stateTimer -= Time.deltaTime) <= 0f) ChangeState(State.Patrol);
    }

    void PatrolUpdate()
    {
        if (Scan360()) { ChangeState(State.Chase); return; }
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
        if (Scan360())
        {
            lastKnownPos = playerTransform.position;
            float dist = Vector2.Distance(transform.position, lastKnownPos);

            if (dist <= attackRange) { ChangeState(State.Attack); return; }

            // условие телепортации: если игрок далеко и кулдаун телепорта прошел
            if (Time.time >= lastTpTime + tpCooldown && dist > 2.5f)
            {
                ChangeState(State.Teleport);
                return;
            }

            MoveTo(lastKnownPos);
        }
        else ChangeState(State.Search);
    }

    void AttackUpdate()
    {
        if (playerTransform == null) { ChangeState(State.Patrol); return; }
        float d = Vector2.Distance(transform.position, playerTransform.position);

        if (d > attackRange) { ChangeState(State.Chase); return; }

        if (Time.time >= lastAttack + attackCooldown)
        {
            DoAttack();
            lastAttack = Time.time;
        }
    }

    void SearchUpdate()
    {
        if (Scan360()) { ChangeState(State.Chase); return; }

        if (Vector2.Distance(transform.position, lastKnownPos) > 0.2f)
        {
            MoveTo(lastKnownPos);
        }
        else
        {
            if ((stateTimer -= Time.deltaTime) <= 0f) ChangeState(State.Patrol);
        }
    }

    void TeleportUpdate()
    {
        // выполняем телепортацию и возвращаемся в состояние Chase
        PerformTeleport();
        lastTpTime = Time.time;
        ChangeState(State.Chase);
    }

    // вспомогательные методы

    /// <summary>
    /// периодическая проверка
    /// </summary>
    bool Scan360()
    {
        if (playerTransform == null) return false;

        if (Time.time < lastScanTime + scanInterval) return currentState == State.Chase;
        lastScanTime = Time.time;

        var toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        if (dist > scanRadius) return false;

        // проверка на наличие препятствий между врагом и игроком
        var hit = Physics2D.Raycast(transform.position, toPlayer.normalized, dist, obstacleLayer | playerLayer);
        return hit.collider != null && hit.collider.transform == playerTransform;
    }

    /// <summary>
    /// логика телепортации 
    /// </summary>
    void PerformTeleport()
    {
        if (playerTransform == null) return;

        Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 targetTpPos = (Vector2)transform.position + dirToPlayer * maxTpDistance;

        // проверяем, есть ли препятствия на пути телепортации
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, maxTpDistance, obstacleLayer);
        if (hit.collider != null)
        {
            // если есть препятствие, телепортируемся на безопасное расстояние перед ним
            targetTpPos = hit.point - dirToPlayer * 0.5f;
        }

        // визуальный эффект телепортации 
        if (sr) StartCoroutine(TpEffect());

        // Перемещаем физическое тело и transform
        transform.position = targetTpPos;
        if (rb != null) rb.position = targetTpPos;

        Debug.Log($"NPC Телепортировался в позицию: {targetTpPos}");
    }

    void MoveTo(Vector3 pos)
    {
        if (rb != null)
            rb.MovePosition(Vector2.MoveTowards(rb.position, pos, moveSpeed * Time.deltaTime));
        else
            transform.position = Vector2.MoveTowards(transform.position, pos, moveSpeed * Time.deltaTime);
    }

    void DoAttack()
    {
        Debug.Log($"TP Enemy Attack {attackDamage}");
        if (sr) StartCoroutine(Flash(Color.red));
    }

    IEnumerator Flash(Color c)
    {
        var orig = sr.color; sr.color = c; yield return new WaitForSeconds(0.12f); sr.color = orig;
    }

    IEnumerator TpEffect()
    {
        yield return Flash(Color.cyan);
    }

    void OnDrawGizmosSelected()
    {
        // зона сканирования
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        // радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}