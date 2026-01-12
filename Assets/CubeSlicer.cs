using System.Collections.Generic;
using UnityEngine;

public class CubeSlicer : MonoBehaviour
{
    public Material capMaterial;
    public float force = 3f;

    public void Slice(Vector3 planePoint, Vector3 planeNormal)
    {
        // Convert world-space plane to local space
        Vector3 localPlaneNormal = transform.InverseTransformDirection(planeNormal);
        Vector3 localPlanePoint = transform.InverseTransformPoint(planePoint);
        Plane plane = new Plane(localPlaneNormal, localPlanePoint);

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        List<Vector3> leftVerts = new();
        List<Vector3> rightVerts = new();
        List<int> leftTris = new();
        List<int> rightTris = new();
        List<Vector3> cutPoints = new();

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            Vector3 va = verts[a];
            Vector3 vb = verts[b];
            Vector3 vc = verts[c];

            // Use local-space vertices directly with local-space plane
            bool sa = plane.GetSide(va);
            bool sb = plane.GetSide(vb);
            bool sc = plane.GetSide(vc);

            int sideCount = (sa ? 1 : 0) + (sb ? 1 : 0) + (sc ? 1 : 0);

            if (sideCount == 0)
                AddTriangle(leftVerts, leftTris, va, vb, vc);
            else if (sideCount == 3)
                AddTriangle(rightVerts, rightTris, va, vb, vc);
            else
                SplitTriangle(plane, va, vb, vc, sa, sb, sc,
                              leftVerts, leftTris,
                              rightVerts, rightTris,
                              cutPoints);
        }

        CreatePiece(leftVerts, leftTris, cutPoints, -planeNormal);
        CreatePiece(rightVerts, rightTris, cutPoints, planeNormal);

        Destroy(gameObject);
    }

    void AddTriangle(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int start = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(start); t.Add(start + 1); t.Add(start + 2);
    }

    void SplitTriangle(
        Plane plane,
        Vector3 a, Vector3 b, Vector3 c,
        bool sa, bool sb, bool sc,
        List<Vector3> leftV, List<int> leftT,
        List<Vector3> rightV, List<int> rightT,
        List<Vector3> cutPts)
    {
        // Simplified: works well for cubes
        Vector3[] v = { a, b, c };
        bool[] s = { sa, sb, sc };

        List<Vector3> pos = new();
        List<Vector3> neg = new();

        for (int i = 0; i < 3; i++)
            if (s[i]) pos.Add(v[i]);
            else neg.Add(v[i]);

        // Handle case: 1 vertex on positive side, 2 on negative side
        if (pos.Count == 1 && neg.Count == 2)
        {
            Vector3 i1 = Intersect(plane, pos[0], neg[0]);
            Vector3 i2 = Intersect(plane, pos[0], neg[1]);

            cutPts.Add(i1);
            cutPts.Add(i2);

            AddTriangle(rightV, rightT, pos[0], i1, i2);
            AddTriangle(leftV, leftT, neg[0], neg[1], i1);
            AddTriangle(leftV, leftT, neg[1], i2, i1);
        }
        // Handle case: 2 vertices on positive side, 1 on negative side
        else if (pos.Count == 2 && neg.Count == 1)
        {
            Vector3 i1 = Intersect(plane, pos[0], neg[0]);
            Vector3 i2 = Intersect(plane, pos[1], neg[0]);

            cutPts.Add(i1);
            cutPts.Add(i2);

            AddTriangle(leftV, leftT, neg[0], i1, i2);
            AddTriangle(rightV, rightT, pos[0], pos[1], i1);
            AddTriangle(rightV, rightT, pos[1], i2, i1);
        }
    }

    Vector3 Intersect(Plane p, Vector3 a, Vector3 b)
    {
        // Plane intersection in local space
        Vector3 direction = b - a;
        float denominator = Vector3.Dot(p.normal, direction);
        if (Mathf.Abs(denominator) < 0.0001f)
            return (a + b) * 0.5f; // Parallel, return midpoint
        
        float t = (Vector3.Dot(p.normal, p.normal * -p.distance) - Vector3.Dot(p.normal, a)) / denominator;
        return a + direction * t;
    }

    void CreatePiece(List<Vector3> v, List<int> t, List<Vector3> cap, Vector3 normal)
    {
        GameObject go = new GameObject("Slice");
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.transform.localScale = Vector3.one;

        Mesh m = new Mesh();
        m.vertices = v.ToArray();
        m.triangles = t.ToArray();
        m.RecalculateNormals();

        go.AddComponent<MeshFilter>().mesh = m;
        go.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;

        MeshCollider mc = go.AddComponent<MeshCollider>();
        mc.convex = true;
        mc.sharedMesh = m;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.AddForce(normal * force, ForceMode.Impulse);

        // Add CubeSlicer component so the piece can be sliced again
        CubeSlicer slicer = go.AddComponent<CubeSlicer>();
        slicer.capMaterial = this.capMaterial;
        slicer.force = this.force;
    }
}
