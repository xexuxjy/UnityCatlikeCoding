using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public Transform Swivel;
    public Transform Stick;

    public HexGrid HexGrid;


    public float StickMinZoom;
    public float StickMaxZoom;

    public float SwivelMinZoom;
    public float SwivelMaxZoom;

    public float MoveSpeedMinZoom;
    public float MoveSpeedMaxZoom;

    public float RotationSpeed;

    float m_zoom = 1.0f;
    float m_rotationAngle;

    static HexMapCamera instance;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    void Awake()
    {
        instance = this;

    }

    private void OnEnable()
    {
        instance = this;
        ValidatePosition();
    }

    public static bool Locked
    {
        set
        {
            if (instance != null)
            {
                instance.enabled = !value;
            }
        }
    }

    public static void ValidatePosition()
    {
        instance.AdjustPosition(0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0.0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0.0f)
        {
            AdjustRotation(rotationDelta);
        }
        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");

        if (xDelta != 0.0f || zDelta != 0.0f)
        {
            AdjustPosition(xDelta, zDelta);
        }




    }



    private void AdjustZoom(float zoomDelta)
    {
        m_zoom = Mathf.Clamp01(m_zoom + zoomDelta);
        float distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, m_zoom);
        Stick.localPosition = new Vector3(0, 0, distance);

        float angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, m_zoom);
        Swivel.localRotation = Quaternion.Euler(angle, 0, 0);


    }
    private void AdjustRotation(float rotationDelta)
    {
        m_rotationAngle += rotationDelta * RotationSpeed * Time.deltaTime;
        if (m_rotationAngle < 0f)
        {
            m_rotationAngle += 360;
        }
        if (m_rotationAngle > 360)
        {
            m_rotationAngle -= 360;
        }


        transform.localRotation = Quaternion.Euler(0f, -m_rotationAngle, 0f);
    }


    private void AdjustPosition(float xDelta, float zDelta)
    {
        float distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, m_zoom) * Time.deltaTime;

        Vector3 position = transform.localPosition;
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0, zDelta).normalized;

        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));

        position += direction * damping * distance;

        position = HexGrid.Wrap ? WrapPosition(position) : ClampPosition(position);

        transform.localPosition = position;

    }

    private Vector3 ClampPosition(Vector3 position)
    {

        float xMax = (HexGrid.CellCountX - 0.5f) * (HexMetrics.InnerDiameter);
        position.x = Mathf.Clamp(position.x, 0, xMax);

        float zMax = (HexGrid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
        position.z = Mathf.Clamp(position.z, 0, zMax);

        return position;
    }

    private Vector3 WrapPosition(Vector3 position)
    {
        //float xMax = (HexGrid.CellCountX - 0.5f) * HexMetrics.InnerDiameter;
        //position.x = Mathf.Clamp(position.x, 0f, xMax);

        float width = HexGrid.CellCountX * HexMetrics.InnerDiameter;
        while (position.x < 0f)
        {
            position.x += width;
        }
        while (position.x > width)
        {
            position.x -= width;
        }


        float zMax = (HexGrid.CellCountZ - 1) * (1.5f * HexMetrics.InnerDiameter);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        HexGrid.CenterMap(position.x);
        return position;

    }


}
