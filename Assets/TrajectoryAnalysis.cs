using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class TrajectoryAnalysis : MonoBehaviour
{
    public string filePath = @"C:\Users\ryagm\Downloads\singlefloor_trajectory.csv"; // Path to the input CSV
    public GameObject[] boxes;
    private Dictionary<int, float> participantDistances = new Dictionary<int, float>();

    // Start is called before the first frame update
    void Start()
    {

        var boxesKeyValue = new Dictionary<int, GameObject>();
        foreach (var box in boxes)
        {
            boxesKeyValue.Add(box.GetComponent<BoxInfo>().Id, box);
        }

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                var routeLines = new GameObject("routeLines ");
                var participantId = 0;

                // Reading each line (row) in the CSV file
                while ((line = reader.ReadLine()) != null)
                {
                    // Split the row into columns based on a comma
                    string[] columns = line.Split(',');
                    var route = new List<int>();

                    for (int i = 2; i < columns.Length; i++)
                    {
                        var boxId = 0;
                        var column = columns[i];

                        if (column[0] == '"')
                        {
                            boxId = int.Parse(column.Substring(1));
                        }
                        else if (column == "Route")
                        {
                            continue;
                        }
                        else if (column[column.Length - 1] == '"')
                        {
                            boxId = int.Parse(column.Substring(0, column.Length - 1));
                        }
                        else
                        {
                            boxId = int.Parse(column);
                        }

                        route.Add(boxId);
                    }

                    var participantRoute = new GameObject("route " + participantId);
                    participantRoute.transform.parent = routeLines.transform;

                    var totalDistance = 0f;
                    for (var i = 0; i < route.Count - 1; i++)
                    {
                        var lineSegment = new GameObject("line " + i);
                        var lineRenderer1 = lineSegment.AddComponent<LineRenderer>();
                        lineRenderer1.startWidth = 0.1f;
                        lineRenderer1.endWidth = 0.5f;
                        lineRenderer1.material = new Material(Shader.Find("Sprites/Default"));

                        var startingPoint = boxesKeyValue[route[i]].transform.position;
                        var endPoint = boxesKeyValue[route[i + 1]].transform.position;

                        Debug.Log($"Start Point: {startingPoint}, End Point: {endPoint}");
                        
                        var startNavMeshPoint = GetClosestPointOnNavMesh(startingPoint);
                        var endNavMeshPoint = GetClosestPointOnNavMesh(endPoint);
                        Debug.Log($"Start NavMesh Point: {startNavMeshPoint}, End NavMesh Point: {endNavMeshPoint}");

                        totalDistance += CalculateAndVisualizePath(startingPoint, endPoint, lineRenderer1);
                        lineSegment.transform.parent = participantRoute.transform;
                        CreateMeshFromLineRenderer(lineRenderer1);
                    }

                    // Log distance and store it in the dictionary
                    Debug.Log("Calculated distance for participant with ID: " + participantId + " is " + totalDistance + " meters.");
                    participantDistances[participantId] = totalDistance;

                    participantId++;
                }

                // Write distances to CSV
                WriteDistancesToCsv();
            }

            var grid = CalculateCombinedBounds();
            CreateGrid(grid, 0.5f);
        }
        catch (Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }
    }

    float CalculateAndVisualizePath(Vector3 startPoint, Vector3 endPoint, LineRenderer lineRenderer)
    {
        // Find the closest points on the NavMesh
        Vector3 startNavMeshPoint = GetClosestPointOnNavMesh(startPoint);
        Vector3 endNavMeshPoint = GetClosestPointOnNavMesh(endPoint);

        // Calculate the path
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        if (UnityEngine.AI.NavMesh.CalculatePath(startNavMeshPoint, endNavMeshPoint, UnityEngine.AI.NavMesh.AllAreas, path))
        {
            Debug.Log($"Successfully calculated path from {startPoint} to {endPoint}. Status: {path.status}");
        }
        else
        {
            Debug.LogError($"Failed to calculate path from {startPoint} to {endPoint}");
            return 0f;  // If the path calculation fails, return 0
        }

        // Check the path corners
        if (path.corners.Length < 2)
        {
            Debug.LogWarning($"Path from {startPoint} to {endPoint} has insufficient corners (Count: {path.corners.Length})");
            return 0f; // No valid path
        }

        // Draw the path using a LineRenderer
        DrawPath(path, lineRenderer);

        // Calculate the path distance
        return CalculatePathDistance(path);
    }

    Vector3 GetClosestPointOnNavMesh(Vector3 position)
    {
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }
        Debug.LogWarning($"Could not find NavMesh point near position: {position}");
        return position;  // Fallback if no point found (shouldn't happen if NavMesh is set up properly)
    }

    void DrawPath(UnityEngine.AI.NavMeshPath path, LineRenderer lineRenderer)
    {
        if (lineRenderer != null && path.corners.Length > 0)
        {
            lineRenderer.positionCount = path.corners.Length;
            lineRenderer.SetPositions(path.corners);
            Debug.Log("Path drawn with " + path.corners.Length + " corners.");
        }
    }

    float CalculatePathDistance(UnityEngine.AI.NavMeshPath path)
    {
        if (path.corners.Length < 2)
        {
            return 0f; // No path or path with only one point
        }

        float totalDistance = 0f;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return totalDistance;
    }

    void WriteDistancesToCsv()
    {
        string outputPath = @"C:\Users\ryagm\Downloads\distances_one-floor.csv"; // Updated file name
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("ParticipantID,TotalDistance");
            foreach (var entry in participantDistances)
            {
                writer.WriteLine($"{entry.Key},{entry.Value}");
            }
        }

        Debug.Log("Distances saved to " + outputPath);
    }

    void CreateMeshFromLineRenderer(LineRenderer lineRenderer)
    {
        var meshFilter = lineRenderer.gameObject.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        meshFilter.mesh = mesh;
        int count = lineRenderer.positionCount;
        Vector3[] positions = new Vector3[count];
        lineRenderer.GetPositions(positions);

        Vector3[] vertices = new Vector3[count * 2];
        int[] triangles = new int[(count - 1) * 6];
        Vector2[] uv = new Vector2[count * 2];

        float width = 0.1f; // Set the width of the mesh

        // Create vertices
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Vector3.Cross(Vector3.up, positions[i]).normalized * width / 2;
            vertices[i * 2] = positions[i] + offset;
            vertices[i * 2 + 1] = positions[i] - offset;

            uv[i * 2] = new Vector2(0, i / (float)(count - 1));
            uv[i * 2 + 1] = new Vector2(1, i / (float)(count - 1));
        }

        // Create triangles
        for (int i = 0; i < count - 1; i++)
        {
            int start = i * 2;
            int next = start + 2;
            int triangleIndex = i * 6;
            triangles[triangleIndex] = start;
            triangles[triangleIndex + 1] = next;
            triangles[triangleIndex + 2] = start + 1;
            triangles[triangleIndex + 3] = next;
            triangles[triangleIndex + 4] = next + 1;
            triangles[triangleIndex + 5] = start + 1;
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        lineRenderer.gameObject.AddComponent<MeshCollider>();
    }

    void CreateGrid(Bounds grid, float boxSize)
    {
        var colors = GenerateHitmapColors(20);
        var boxes = new GameObject("BoxColliders");
        // Calculate the starting point based on the pivot
        Vector3 startPoint = grid.min;

        var gridSizeX = Mathf.CeilToInt(grid.size.x / boxSize);
        var gridSizeY = Mathf.CeilToInt(grid.size.y / boxSize);
        var gridSizeZ = Mathf.CeilToInt(grid.size.z / boxSize);
        var halfBoxes = new Vector3(boxSize, boxSize, boxSize) / 2;

        var values = new int[gridSizeX, gridSizeY, gridSizeZ];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    // Calculate the position for each box collider
                    Vector3 position = startPoint + new Vector3(x * boxSize, y * boxSize, z * boxSize);
                    Collider[] hitColliders = Physics.OverlapBox(position, halfBoxes, Quaternion.identity);
                    foreach (var collider in hitColliders)
                    {
                        var isLine = collider.gameObject.name.Contains("line");
                        if (isLine)
                        {
                            values[x,y,z] += 1;
                        }
                    }
                }
            }
        }
    }

    void CreateBoxCollider(Vector3 position, float boxSize, GameObject boxes)
    {
        //GameObject boxObject = new GameObject("BoxCollider");
        //boxObject.transform.position = position;
        //BoxCollider boxCollider = boxObject.AddComponent<BoxCollider>();
        //boxCollider.size = new Vector3(boxSize,boxSize,boxSize); // Set the size of the box collider
        //boxObject.transform.parent = boxes.transform;

        Collider[] hitColliders = Physics.OverlapBox(position, new Vector3(boxSize, boxSize, boxSize) / 2, Quaternion.identity);
        foreach (var collider in hitColliders)
        {
            var isLine = collider.gameObject.name.Contains("line");
            if (isLine)
            {
                Debug.Log(isLine);
            }
        }
    }

    public Bounds CalculateCombinedBounds()
    {
        Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool isFirstBounds = true;

        // Get all MeshRenderers in the scene
        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();

        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            // Get the MeshFilter component attached to the MeshRenderer
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();

            // Ensure the MeshFilter and its mesh are valid
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            // Get the bounds of the current MeshRenderer
            Bounds meshBounds = meshRenderer.bounds;

            // Expand the combined bounds to include the mesh bounds
            if (isFirstBounds)
            {
                combinedBounds = meshBounds;
                isFirstBounds = false;
            }
            else
            {
                combinedBounds.Encapsulate(meshBounds);
            }
        }

        return combinedBounds;
    }


    public Color[] GenerateHitmapColors(int numColors)
    {
        Color[] colors = new Color[numColors];

        for (int i = 0; i < numColors; i++)
        {
            float t = (float)i / (numColors - 1); // Normalize the value between 0 and 1

            // Interpolating from blue to green to red
            if (t < 0.5f)
            {
                // From blue to green
                colors[i] = Color.Lerp(Color.blue, Color.green, t * 2);
            }
            else
            {
                // From green to red
                colors[i] = Color.Lerp(Color.green, Color.red, (t - 0.5f) * 2);
            }
        }

        return colors;
    }
}
