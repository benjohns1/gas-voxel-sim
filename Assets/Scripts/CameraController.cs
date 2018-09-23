using UnityEngine;

public class CameraController : MonoBehaviour
{

    public float panSpeed = 50f;
    public float zoomSpeed = 1000f;
    public Vector2 zoomRange = new Vector2(4f, 80f);
    public float zoomRotationStart = 0.5f;
    public Vector2 zoomRotationRange = new Vector2(10f, 70f);

    private void Start()
    {
        Zoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * panSpeed * Time.deltaTime, Space.World);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * panSpeed * Time.deltaTime, Space.World);
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * panSpeed * Time.deltaTime, Space.World);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * panSpeed * Time.deltaTime, Space.World);
        }

        float zoom = Input.GetAxis("Mouse ScrollWheel");
        if (!Mathf.Approximately(zoom, 0f))
        {
            Zoom(zoom);
        }
    }

    private void Zoom(float zoom)
    {
        float currentZoom = (zoom * zoomSpeed * Time.deltaTime);
        float lerpT = ZoomPosition(currentZoom);
        ZoomRotation(lerpT);
    }

    private float ZoomPosition(float currentZoom)
    {
        float unclampedY = transform.position.y - currentZoom;
        float clampedY = Mathf.Clamp(unclampedY, zoomRange.x, zoomRange.y);
        Vector3 newPosition = new Vector3(transform.position.x, clampedY, transform.position.z);
        transform.position = newPosition;
        return Mathf.InverseLerp(zoomRange.x, zoomRange.y * zoomRotationStart, clampedY);
    }

    private void ZoomRotation(float lerpT)
    {
        float newRotX = Mathf.Lerp(zoomRotationRange.x, zoomRotationRange.y, lerpT);
        Vector3 newRotation = new Vector3(newRotX, transform.eulerAngles.y, transform.eulerAngles.z);
        transform.eulerAngles = newRotation;
    }
}
