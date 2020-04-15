using UnityEngine;

public class HexFeatureManager : MonoBehaviour, IHexMeshChunkModule
{

    public Transform FeatureCube;

    private Transform m_container;


    public void Clear()
    {
        if(m_container != null)
        {
            Destroy(m_container);
        }
        m_container = new GameObject("FeaturesContainer").transform;
        m_container.SetParent(transform, false);


    }

    public void Apply()
    {

    }

    public void AddFeature(Vector3 position)
    {
        Transform instance = Instantiate(FeatureCube);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.PerturbVector(position);
        instance.SetParent(m_container, false);
    }

}
