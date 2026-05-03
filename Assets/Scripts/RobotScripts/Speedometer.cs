using UnityEngine;
using TMPro;
public class Speedometer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;

    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();    
    }

    void Update()
    {
        float speed = rb.linearVelocity.magnitude * 3.6f; // Convert from m/s to km/h
        speedText.text = $"Speed: {speed:F1} km/h";
    }
}
