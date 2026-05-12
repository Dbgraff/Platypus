using System.Collections.Generic;
using UnityEngine;
using System;

public class GasAnalyzer : MonoBehaviour
{
    [SerializeField] private float detectionThreshold = 0.1f;

    public bool IsInGasZone { get; private set; }
    public GasType? CurrentGasType { get; private set; }
    public float CurrentConcentration { get; private set; }

    public event Action OnGasDetected;
    public event Action OnGasCleared;
    public event Action<float> OnConcentrationChanged;

    private HashSet<GasZone> activeZones = new HashSet<GasZone>();

    private void Start()
    {
        activeZones.Clear();
        IsInGasZone = false;
        CurrentGasType = null;
        CurrentConcentration = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<GasZone>(out var zone))
            activeZones.Add(zone);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<GasZone>(out var zone))
            activeZones.Remove(zone);
    }
    

    private void Update()
    {
        float maxConcentration = 0f;
        GasZone strongestZone = null;
        foreach (var zone in activeZones)
        {
            if (zone != null && zone.Concentration > maxConcentration)
            {
                maxConcentration = zone.Concentration;
                strongestZone = zone;
            }
        }

        bool wasInGas = IsInGasZone;
        float prevConcentration = CurrentConcentration;

        if (strongestZone != null)
        {
            CurrentConcentration = maxConcentration;
            CurrentGasType = strongestZone.Type;
            IsInGasZone = maxConcentration >= detectionThreshold;
        }
        else
        {
            CurrentConcentration = 0f;
            CurrentGasType = null;
            IsInGasZone = false;
        }

        if (IsInGasZone && !wasInGas) OnGasDetected?.Invoke();
        else if (!IsInGasZone && wasInGas) OnGasCleared?.Invoke();
        if (Math.Abs(prevConcentration - CurrentConcentration) > 0.01f)
            OnConcentrationChanged?.Invoke(CurrentConcentration);

        if (IsInGasZone)
            Debug.Log($"ГАЗ: {CurrentGasType}, концентрация: {CurrentConcentration:F2}");
    }
}