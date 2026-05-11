using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class AutoReturnToBase : MonoBehaviour
{
    [Header("База")]
    [SerializeField] private Transform basePoint;
    [SerializeField] private float signalRange = 100f;

    [Header("Возврат")]
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float stoppingDistance = 2f;

    [Header("Управление")]
    [SerializeField] private InputActionReference returnAction;
    [SerializeField] private InputActionReference cancelAction;

    private NavMeshAgent agent;
    private RobotController robotController;
    private Rigidbody rb;
    private List<WheelCollider> wheelColliders = new List<WheelCollider>();

    private bool isReturning;
    private bool signalLost;
    private Vector3 positionBeforeReturn;
    private Quaternion rotationBeforeReturn;

    public bool IsReturning => isReturning;
    public bool SignalLost => signalLost;
    public float DistanceToBase => basePoint ? Vector3.Distance(transform.position, basePoint.position) : 0f;
    public float SignalStrength => basePoint ? Mathf.Clamp01(1f - DistanceToBase / signalRange) : 0f;

    public event Action OnReturnStarted;
    public event Action OnReturnCancelled;
    public event Action OnBaseReached;
    public event Action OnSignalLostEvent;
    public event Action OnSignalRestored;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = returnSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.enabled = false; 

        robotController = GetComponent<RobotController>();
        rb = GetComponent<Rigidbody>();

        wheelColliders.AddRange(GetComponentsInChildren<WheelCollider>());

        returnAction.action.Enable();
        cancelAction.action.Enable();
        returnAction.action.performed += OnReturnPerformed;
        cancelAction.action.performed += OnCancelPerformed;
    }

    private void OnDestroy()
    {
        returnAction.action.performed -= OnReturnPerformed;
        cancelAction.action.performed -= OnCancelPerformed;
        returnAction.action.Disable();
        cancelAction.action.Disable();
    }

    private void Start()
    {
        if (basePoint == null)
        {
            Debug.LogError("AutoReturnToBase: не назначена базовая точка!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (basePoint == null) return;

        if (!isReturning)
        {
            if (DistanceToBase > signalRange)
                StartReturn(automatic: true);
        }

        if (isReturning && agent.enabled)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                ArriveAtBase();
            }
        }
    }

    private void OnReturnPerformed(InputAction.CallbackContext ctx) => StartReturn(automatic: false);
    private void OnCancelPerformed(InputAction.CallbackContext ctx) => CancelReturn();

    public void StartReturn(bool automatic)
    {
        if (isReturning) return;

        isReturning = true;
        signalLost = automatic;

        positionBeforeReturn = transform.position;
        rotationBeforeReturn = transform.rotation;

        if (robotController != null)
            robotController.enabled = false;

        foreach (var wc in wheelColliders)
        {
            if (wc != null)
                wc.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        agent.enabled = true;
        if (agent.isOnNavMesh)
            agent.SetDestination(basePoint.position);
        else
            Debug.LogWarning("Робот не на NavMesh! Проверьте bake.");

        OnReturnStarted?.Invoke();
        if (automatic) OnSignalLostEvent?.Invoke();
    }

    public void CancelReturn()
    {
        if (!isReturning) return;

        isReturning = false;
        signalLost = false;

        agent.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var wc in wheelColliders)
        {
            if (wc != null)
            {
                wc.enabled = true;
                wc.motorTorque = 0f;
                wc.brakeTorque = 0f;
            }
        }

        if (robotController != null)
            robotController.enabled = true;

        OnReturnCancelled?.Invoke();
        if (signalLost) OnSignalRestored?.Invoke();
    }

    private void ArriveAtBase()
    {
        isReturning = false;
        signalLost = false;

        agent.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var wc in wheelColliders)
        {
            if (wc != null)
            {
                wc.enabled = true;
                wc.motorTorque = 0f;
                wc.brakeTorque = 0f;
            }
        }

        if (robotController != null)
            robotController.enabled = true;

        OnBaseReached?.Invoke();
        if (signalLost) OnSignalRestored?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        if (basePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(basePoint.position, signalRange);
        }
    }
}