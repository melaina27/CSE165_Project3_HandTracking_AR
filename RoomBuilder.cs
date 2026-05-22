using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using Unity.AI.Navigation;

public class RoomBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject quadPrefab;

    [Header("Optional debug materials")]
    public Material floorMaterial;
    public Material wallMaterial;        // ignored if debugColorWalls = true

    [Header("NavMesh")]
    public Transform navMeshRoot;

    [Header("Thickness")]
    public float wallThickness = 0.02f;
    public float floorThickness = 0.02f;

    [Header("Debug")]
    public bool debugColorWalls = false;  // each wall gets a unique color, names are logged

    void Awake()
    {
        Debug.Log("[RoomBuilder] Awake.");
    }

    void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError("[RoomBuilder] MRUK.Instance is NULL.");
            return;
        }
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
    }

    void OnSceneLoaded()
    {
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            Debug.LogError("[RoomBuilder] No room found.");
            return;
        }

        Debug.Log($"[RoomBuilder] Room loaded. Walls: {room.WallAnchors.Count}, Floor: {(room.FloorAnchor != null)}");

        if (room.FloorAnchor != null)
            SpawnFloorFromPolygon(room.FloorAnchor);

        int i = 0;
        foreach (var wall in room.WallAnchors)
            SpawnWallQuad(wall, $"WallQuad_{i++}");
    }

    void SpawnFloorFromPolygon(MRUKAnchor anchor)
    {
        if (anchor.PlaneBoundary2D == null || anchor.PlaneBoundary2D.Count < 3)
        {
            Debug.LogWarning("[RoomBuilder] Floor anchor has no polygon, falling back to rectangle.");
            if (anchor.PlaneRect.HasValue)
            {
                var size = anchor.PlaneRect.Value.size;
                var quad = Instantiate(quadPrefab, anchor.transform);
                quad.name = "FloorQuad";
                quad.transform.localPosition = Vector3.zero;
                quad.transform.localRotation = Quaternion.Euler(0, 180, 0);
                quad.transform.localScale = new Vector3(size.x, size.y, floorThickness);
                quad.layer = LayerMask.NameToLayer("Surface");
                if (floorMaterial != null) quad.GetComponent<Renderer>().material = floorMaterial;
                if (navMeshRoot != null) quad.transform.SetParent(navMeshRoot, true);
            }
            return;
        }

        // Build a custom mesh from the polygon outline
        var floor = new GameObject("FloorPolygon");
        floor.transform.SetParent(anchor.transform, false);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localRotation = Quaternion.identity;
        floor.layer = LayerMask.NameToLayer("Surface");

        var mf = floor.AddComponent<MeshFilter>();
        var mr = floor.AddComponent<MeshRenderer>();
        var mc = floor.AddComponent<MeshCollider>();

        Mesh mesh = BuildPolygonMesh(anchor.PlaneBoundary2D);
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;

        if (floorMaterial != null) mr.material = floorMaterial;
        else mr.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0f, 1f, 1f, 0.3f) };

        if (navMeshRoot != null) floor.transform.SetParent(navMeshRoot, true);

        Debug.Log($"[RoomBuilder] Floor polygon built with {anchor.PlaneBoundary2D.Count} vertices.");
    }

    Mesh BuildPolygonMesh(IList<Vector2> boundary)
    {
        Mesh m = new Mesh();
        m.name = "FloorPolygonMesh";

        int n = boundary.Count;
        Vector3[] verts = new Vector3[n];
        for (int i = 0; i < n; i++)
            verts[i] = new Vector3(boundary[i].x, boundary[i].y, 0f);

        // Fan triangulation — works for convex and most concave room shapes
        List<int> tris = new List<int>();
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }

        m.vertices = verts;
        m.triangles = tris.ToArray();
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    void SpawnWallQuad(MRUKAnchor anchor, string label)
    {
        if (!anchor.PlaneRect.HasValue) return;
        var size = anchor.PlaneRect.Value.size;

        var quad = Instantiate(quadPrefab, anchor.transform);
        quad.name = label;
        quad.transform.localRotation = Quaternion.Euler(0, 180, 0);
        quad.transform.localScale = new Vector3(size.x, size.y, wallThickness);
        quad.transform.localPosition = new Vector3(0, 0, -wallThickness / 2f);
        quad.layer = LayerMask.NameToLayer("Surface");

        var renderer = quad.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (debugColorWalls)
            {
                // Unique color per wall for debugging
                var mat = new Material(Shader.Find("Unlit/Color"));
                float hue = (label.GetHashCode() & 0xFF) / 255f;
                mat.color = Color.HSVToRGB(hue, 0.7f, 1f);
                renderer.material = mat;
                Debug.Log($"[RoomBuilder] Spawned {label} (size {size.x:F2} × {size.y:F2})");
            }
            else if (wallMaterial != null)
            {
                renderer.material = wallMaterial;
            }
        }

        if (navMeshRoot != null)
            quad.transform.SetParent(navMeshRoot, true);

        var modifier = quad.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = 1;
    }
}