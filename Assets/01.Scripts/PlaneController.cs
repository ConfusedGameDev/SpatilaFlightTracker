using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class PlaneController : MonoBehaviour
{
    public GameObject plane,planeModel;  // Reference to the plane object
    public GameObject earth;  // Reference to the Earth (sphere) object

    public float  initialLatitude, initialLongitude, currentLongitude, currentLatitude,finalLatitude, finalLongitude = 35.6586f;   // Input for latitude (positive for N, negative for S)
    
    public float altitude = 100f;       // Offset/altitude from Earth's surface

    private float earthRadius;          // Earth radius based on sphere size

    public LineRenderer lineRenderer;
    void Start()
    {
        // Assuming Earth is a sphere and the radius is half its scale in Unity units
        earthRadius = earth.transform.localScale.x / 2f;
    }
    [Range(0,35)]
   public int currentPos;
    [Range(0,35)]
   public int currentPosOffset;
    void Update()
    {
        if (earth && plane)
        {
            // Convert longitude and latitude to radians
           

            // Set the plane's position relative to the Earth
            plane.transform.position = earth.transform.position +getCoordinates(currentLatitude, currentLongitude);

            // Optionally, orient the plane so it's facing "forward" relative to the Earth's surface
            plane.transform.LookAt(earth.transform.position);
            planeModel.transform.LookAt(earth.transform.position+ getCoordinates(finalLatitude, finalLongitude));

            if (lineRenderer != null)
            {
                DrawCurve(lineRenderer, GenerateGreatCirclePath(initialLatitude, initialLongitude, finalLatitude, finalLongitude, 35));
                if (currentPos < lineRenderer.positionCount)
                {
                    plane.transform.position = lineRenderer.GetPosition(currentPos);
                    planeModel.transform.LookAt(lineRenderer.GetPosition((currentPos + ((currentPos + currentPosOffset < lineRenderer.positionCount) ? currentPosOffset : 0))));
                }
            }
            
        }
    }

    public Vector3 getCoordinates(float lat, float lon)
    {
        float lonRad = lat * Mathf.Deg2Rad;
        float latRad = lon * Mathf.Deg2Rad;

        

        float a = earthRadius*altitude * Mathf.Cos(lonRad);
        float x = a * Mathf.Cos(latRad);
        float y = earthRadius * altitude * Mathf.Sin(lonRad);
        float z = a * Mathf.Sin(latRad);

        return new Vector3(x, y, z);

     }

    public List<Vector3> GenerateGreatCirclePath(float lat1, float lon1, float lat2, float lon2, int resolution)
    {
        // Get the start and end positions in Cartesian coordinates
        Vector3 start = getCoordinates(lat1, lon1);
        Vector3 end = getCoordinates(lat2, lon2);

        // List to store points along the curve
        List<Vector3> curvePoints = new List<Vector3>();

        // Add the start point
        curvePoints.Add(earth.transform.position + start);

        // Perform spherical interpolation between the two points
        for (int i = 1; i < resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 interpolated = earth.transform.position + Vector3.Slerp(start, end, t);
            curvePoints.Add(interpolated);
        }

        // Add the end point
        curvePoints.Add(earth.transform.position + end);

        return curvePoints;
    }

    // Method to draw the path using a LineRenderer or similar
    public void DrawCurve(LineRenderer lineRenderer, List<Vector3> curvePoints)
    {
        lineRenderer.positionCount = curvePoints.Count;
        lineRenderer.SetPositions(curvePoints.ToArray());
    }
}
