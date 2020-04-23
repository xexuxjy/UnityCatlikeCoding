using UnityEngine;

public class HexFeatureManager : MonoBehaviour, IHexMeshChunkModule
{

    public HexFeatureCollection[] UrbanFeatureCollections;
    public HexFeatureCollection[] FarmFeatureCollections;
    public HexFeatureCollection[] PlantFeatureCollections;


    private Transform m_container;


    public void Clear()
    {
        if (m_container != null)
        {
            Destroy(m_container.gameObject);
        }
        m_container = new GameObject("FeaturesContainer").transform;
        m_container.SetParent(transform, false);


    }

    public void Apply()
    {

    }

    public Transform PickFeaturePrefab(HexFeatureCollection[] collections,int level,float hash,float choice)
    {
        float[] chances = HexMetrics.GetFeatureThresholds(level);
        for(int i=0;i<chances.Length;++i)
        {
            if(hash < chances[i])
            {
                return collections[i].Pick(choice);
            }
        }
        return null;
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hexHash = HexMetrics.SampleHashGrid(position);
        if (hexHash.a >= 0.25f * cell.UrbanDensityLevel)
        {
            return;
        }


        Transform urbanFeature = PickFeaturePrefab(UrbanFeatureCollections, cell.UrbanDensityLevel - 1, hexHash.a,hexHash.choice);
        Transform farmFeature = PickFeaturePrefab(FarmFeatureCollections, cell.FarmDensityLevel - 1, hexHash.b, hexHash.choice);
        Transform plantFeature = PickFeaturePrefab(PlantFeatureCollections, cell.PlantDensityLevel - 1, hexHash.c, hexHash.choice);

        float defaultMax = 1000;
        Transform chosenTransform = null;
        float usedHash = Mathf.Min(urbanFeature != null ? hexHash.a : defaultMax, farmFeature != null ? hexHash.b : defaultMax, plantFeature != null? hexHash.c:defaultMax);
        
        if (usedHash == hexHash.a)
        {
            chosenTransform = urbanFeature;
        }
        else if(usedHash == hexHash.b)
        {
            chosenTransform = farmFeature;
        }
        else if(usedHash == hexHash.c)
        {
            chosenTransform = plantFeature;
        }

        if (chosenTransform != null)
        {
            Transform instance = Instantiate(chosenTransform);
            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.PerturbVector(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hexHash.randomRotation, 0f);
            instance.SetParent(m_container, false);
        }
    }
}
