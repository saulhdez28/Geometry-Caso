using System.Collections;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle,
            Walking,
            Running,
            Attacking
        }

        public enum AttackMode
        {
            None = 0,
            Jab = 1,
            KO = 2,
            HighKick = 3,
            LowKick = 4,
            Uppercut = 5
        }

        [Header("Enemy")]

        [SerializeField]
        private float MoveSpeed = 2.0f;

        [SerializeField]
        private float SprintSpeed = 5.335f;

        [SerializeField]
        private float RotationSmoothTime = 0.12f;

        [SerializeField]
        private float _rotationVelocity = 0.1f;

        [SerializeField]
        private float SpeedChangeRate = 10.0f;

        [SerializeField]
        private float AttackRange = 2.0f;

        [SerializeField]
        private float AttackDuration = 1.0f;

        [SerializeField]
        private Transform PlayerTransform;

        [SerializeField]
        private float minDistance = 2.0f;

        [SerializeField]
        private AudioClip LandingAudioClip;

        [SerializeField]
        private AudioClip[] FootstepAudioClips;

        [SerializeField]
        [Range(0, 1)]
        private float FootstepAudioVolume = 0.2f;

        [SerializeField]
        private float GroundedRadius = 0.28f;

        [SerializeField]
        private LayerMask GroundLayers;

        [SerializeField]
        private float GroundedOffset = -0.14f;

        private float _speed;
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDMotionSpeed;
        private int _animIDAttack;

        private Animator _animator;
        private CharacterController _controller;
        private NavMeshAgent _navMeshAgent;

        private EnemyState _currentState = EnemyState.Idle;
        private AttackMode _currentAttackMode = AttackMode.None;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<CharacterController>();
            _navMeshAgent = GetComponent<NavMeshAgent>();

            AssignAnimationIDs();
            _navMeshAgent.updateRotation = false;
        }

        private void Update()
        {
            GroundedCheck();
            HandleState();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            bool ground = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            _animator.SetBool(_animIDGrounded, ground);
        }

        private void HandleState()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

            switch (_currentState)
            {
                case EnemyState.Idle:
                    if (distanceToPlayer <= AttackRange && CanAttack())
                    {
                        _currentState = EnemyState.Attacking;
                        StartCoroutine(AttackRoutine());
                    }
                    else if (distanceToPlayer > AttackRange && distanceToPlayer <= _navMeshAgent.stoppingDistance)
                    {
                        _currentState = EnemyState.Walking;
                    }
                    else if (distanceToPlayer > _navMeshAgent.stoppingDistance)
                    {
                        _currentState = EnemyState.Running;
                    }
                    break;

                case EnemyState.Walking:
                    Move(MoveSpeed);
                    if (distanceToPlayer <= AttackRange && CanAttack())
                    {
                        _currentState = EnemyState.Attacking;
                        StartCoroutine(AttackRoutine());
                    }
                    else if (distanceToPlayer > _navMeshAgent.stoppingDistance)
                    {
                        _currentState = EnemyState.Running;
                    }
                    break;

                case EnemyState.Running:
                    Move(SprintSpeed);
                    if (distanceToPlayer <= AttackRange && CanAttack())
                    {
                        _currentState = EnemyState.Attacking;
                        StartCoroutine(AttackRoutine());
                    }
                    else if (distanceToPlayer <= _navMeshAgent.stoppingDistance)
                    {
                        _currentState = EnemyState.Walking;
                    }
                    break;

                case EnemyState.Attacking:
                    // Aquí no necesitamos hacer nada directamente, ya que la corutina se encarga de esto
                    break;
            }
        }

        private IEnumerator AttackRoutine()
        {
            SetAttackMode();

            _animator.SetTrigger(_animIDAttack);

            yield return new WaitForSeconds(AttackDuration);


            _animator.ResetTrigger(_animIDAttack);
            yield return new WaitForSeconds(1);
            _animator.SetInteger("Mode", 0);
            _currentState = EnemyState.Idle;

        }

        private void SetAttackMode()
        {
            //_currentAttackMode = (AttackMode)UnityEngine.Random.Range(1, 6);

            //if (_currentAttackMode == AttackMode.KO)
            //{
            //    _currentAttackMode = AttackMode.Jab;
            //}

            _animator.SetInteger("Mode", 1);
        }

        private bool CanAttack()
        {
            return _speed < 0.01f && _animator.GetBool(_animIDGrounded);
        }

        private void Move(float speed)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

            if (distanceToPlayer <= minDistance)
            {
                _speed = Mathf.Lerp(_speed, 0f, SpeedChangeRate * Time.deltaTime / minDistance);
            }
            else
            {
                _speed = Mathf.MoveTowards(_speed, speed, SpeedChangeRate * Time.deltaTime);

            }

            if (_speed < 0.01f)
            {
                _speed = 0f;
            }

            _navMeshAgent.SetDestination(PlayerTransform.position);

            if (_navMeshAgent.velocity.sqrMagnitude > 0.01f)
            {
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, Mathf.Atan2(_navMeshAgent.velocity.x, _navMeshAgent.velocity.z) * Mathf.Rad2Deg, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            _animator.SetFloat(_animIDSpeed, _speed);
            _animator.SetFloat(_animIDMotionSpeed, 1);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}
