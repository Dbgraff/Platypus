using UnityEngine;

public class GasZone : MonoBehaviour
{
    [Header("Тип газа")]
    [SerializeField] private GasType gasType = GasType.Chlorine;

    [Range(0f, 1f)]
    [SerializeField] private float concentration = 0.5f;

    [Tooltip("Цвет")]
    [SerializeField] private Color zoneColor;

    [Header("Визуализация")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float minAlpha = 0.05f;
    [SerializeField] private float maxAlpha = 0.4f;

    // Приватные
    private ParticleSystem particles;
    private SphereCollider sphereCollider;
    private Material particleMaterial;

    public GasType Type => gasType;
    public float Concentration => concentration;
    public Color ZoneColor => zoneColor;

    private void OnValidate()
    {
        zoneColor = GetDefaultColor(gasType);
        UpdateVisualsInEditor();
    }

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null) sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        if (sphereCollider.radius < 1f) sphereCollider.radius = 5f;

        if (particlePrefab != null)
        {
            GameObject obj = Instantiate(particlePrefab, transform);
            particles = obj.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var shape = particles.shape;
                shape.radius = sphereCollider.radius;
                ApplyVisualSettings();
            }
        }
    }

    private void Start()
    {
        ApplyVisualSettings();
    }

    private void ApplyVisualSettings()
    {
        if (particles == null) return;

        var main = particles.main;

        float alpha = Mathf.Lerp(minAlpha, maxAlpha, concentration);
        Color col = zoneColor;
        col.a = alpha;
        main.startColor = col;

        var emission = particles.emission;
        emission.rateOverTime = Mathf.Lerp(50, 500, concentration);

        main.startSize = Mathf.Lerp(1.5f, 3.5f, concentration);
    }

    private Color GetDefaultColor(GasType type) => type switch
    {
        GasType.Chlorine => new Color(0.83f, 1f, 0f, 0.3f),
        GasType.Ammonia => new Color(0.7830f, 0.9479f, 1f, 0.3f),
        GasType.HydrogenSulfide => new Color(0.8f, 0.7f, 0.13f, 0.3f),
        _ => Color.gray
    };

    private void UpdateVisualsInEditor()
    {
        if (particles != null) ApplyVisualSettings();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        if (sphereCollider != null)
            Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
    }
}