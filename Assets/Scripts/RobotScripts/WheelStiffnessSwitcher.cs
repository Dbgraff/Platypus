using UnityEngine;

public class WheelStiffnessSwitcher : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider[] wheelColliders;

    [Header("Terrain Layer Stiffness Settings")]
    [Tooltip("Индекс соответствует порядку слоёв в Terrain Layers")]
    [SerializeField] private SurfaceStiffness[] surfaceStiffnesses;

    private Terrain currentTerrain;

    [System.Serializable]
    public struct SurfaceStiffness
    {
        public string name;
        [Range(0f, 2f)]
        public float forwardStiffness;
        [Range(0f, 2f)]
        public float sidewaysStiffness;
    }

    void Start()
    {
        currentTerrain = Terrain.activeTerrain;
        if (currentTerrain == null)
            currentTerrain = FindAnyObjectByType<Terrain>();

        if (currentTerrain == null)
        {
            Debug.LogError("WheelStiffnessSwitcher: Terrain не найден!");
            enabled = false;
            return;
        }

        if (wheelColliders == null || wheelColliders.Length == 0)
            wheelColliders = GetComponentsInChildren<WheelCollider>();

        if (surfaceStiffnesses == null || surfaceStiffnesses.Length == 0)
        {
            Debug.LogWarning("WheelStiffnessSwitcher: Массив surfaceStiffnesses пуст. Будут использованы значения по умолчанию.");
            enabled = false;
        }
    }

    void FixedUpdate()
    {
        foreach (WheelCollider wc in wheelColliders)
        {
            if (wc == null || !wc.isGrounded) continue;

            WheelHit hit;
            if (wc.GetGroundHit(out hit))
            {
                // Определяем слой террейна в точке контакта колеса
                int layerIndex = GetDominantTerrainLayer(hit.point);
                if (layerIndex >= 0 && layerIndex < surfaceStiffnesses.Length)
                {
                    SurfaceStiffness target = surfaceStiffnesses[layerIndex];

                    // Продольное трение
                    WheelFrictionCurve forwardFriction = wc.forwardFriction;
                    forwardFriction.stiffness = target.forwardStiffness;
                    wc.forwardFriction = forwardFriction;

                    // Поперечное трение
                    WheelFrictionCurve sidewaysFriction = wc.sidewaysFriction;
                    sidewaysFriction.stiffness = target.sidewaysStiffness;
                    wc.sidewaysFriction = sidewaysFriction;
                }
            }
        }
    }

    /// <summary>
    /// Возвращает индекс доминирующего текстурного слоя террейна в мировой точке.
    /// </summary>
    private int GetDominantTerrainLayer(Vector3 worldPosition)
    {
        if (currentTerrain == null) return -1;

        Vector3 terrainLocalPos = worldPosition - currentTerrain.transform.position;
        Vector3 normalizedPos = new Vector3(
            terrainLocalPos.x / currentTerrain.terrainData.size.x,
            0,
            terrainLocalPos.z / currentTerrain.terrainData.size.z
        );

        if (normalizedPos.x < 0 || normalizedPos.x > 1 || normalizedPos.z < 0 || normalizedPos.z > 1)
            return -1;

        float[,,] alphamaps = currentTerrain.terrainData.GetAlphamaps(
            (int)(normalizedPos.x * currentTerrain.terrainData.alphamapWidth),
            (int)(normalizedPos.z * currentTerrain.terrainData.alphamapHeight),
            1, 1
        );

        int maxIndex = 0;
        float maxWeight = alphamaps[0, 0, 0];
        for (int i = 1; i < alphamaps.GetLength(2); i++)
        {
            if (alphamaps[0, 0, i] > maxWeight)
            {
                maxWeight = alphamaps[0, 0, i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}