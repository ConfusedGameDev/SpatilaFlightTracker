using UnityEngine;

public class FlightPath : MonoBehaviour
{
    public Transform sphere; // The sphere object
    public Transform plane; // The plane object
    public Vector2 departureCoords; // Departure coordinates (Latitude, Longitude)
    public Vector2 destinationCoords; // Destination coordinates (Latitude, Longitude)
    public Vector2 currentCoords; // Current coordinates (Latitude, Longitude)
    public float sphereRadius = 10f; // Radius of the sphere
    public float offsetDistance = 1f; // Offset distance for the plane
    public LineRenderer lineRenderer; // The Line Renderer component

    void Update()
    {
        // Convert coordinates to positions
        Vector3 departurePos = LatLonToCartesian(departureCoords, sphereRadius);
        Vector3 destinationPos = LatLonToCartesian(destinationCoords, sphereRadius);
        Vector3 currentPos = LatLonToCartesian(currentCoords, sphereRadius);

        // Position the plane
        Vector3 planePosition = currentPos + (currentPos.normalized * offsetDistance);
        plane.position = planePosition;
        plane.LookAt(sphere.position); // Orient the plane correctly

        // Draw the trajectory
        DrawTrajectory(departurePos, currentPos, destinationPos);
    }

    Vector3 LatLonToCartesian(Vector2 latLon, float radius)
    {
        float lat = Mathf.Deg2Rad * latLon.x; // Convert latitude to radians
        float lon = Mathf.Deg2Rad * latLon.y; // Convert longitude to radians

        float x = radius * Mathf.Cos(lat) * Mathf.Cos(lon);
        float y = radius * Mathf.Sin(lat);
        float z = radius * Mathf.Cos(lat) * Mathf.Sin(lon);

        return new Vector3(x, y, z);
    }

    void DrawTrajectory(Vector3 departure, Vector3 current, Vector3 destination)
    {
        int resolution = 50; // Number of points in the curve
        Vector3[] points = new Vector3[resolution * 2];

        // Generate points for Departure to Current
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            points[i] = Vector3.Slerp(departure, current, t);
        }

        // Generate points for Current to Destination
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            points[resolution + i] = Vector3.Slerp(current, destination, t);
        }

        // Assign points to the Line Renderer
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);

        // Set colors
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.red, 0.0f),
                new GradientColorKey(Color.red, 0.5f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        lineRenderer.colorGradient = gradient;
    }
}
