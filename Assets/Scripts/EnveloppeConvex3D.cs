using System.Collections.Generic;
using UnityEngine;

public class convexeIncremental3DScript : MonoBehaviour
{
    private class Face
    {
        public int a, b, c;

        public Vector3 normal;
        public float d;

        public Face[] neighbors = new Face[3];

        public List<int> outsidePoints = new List<int>();
        public bool visible;
    }

    private struct HorizonEdge
    {
        public int a, b;
        public Face neighbor;
        public int neighborSlot;

        public HorizonEdge(int a, int b, Face neighbor, int neighborSlot)
        {
            this.a = a;
            this.b = b;
            this.neighbor = neighbor;
            this.neighborSlot = neighborSlot;
        }
    }
    
    private List<Vector3> arrayPoint = new List<Vector3>();
    private HashSet<Face> faces = new HashSet<Face>();

    private float epsilon = 1e-5f;

    public List<int> ComputeHull(List<Vector3> points)
    {
        arrayPoint = new List<Vector3>(points);

        StartIncremental();

        return ConvertFacesToTriangles();
    }

    void StartIncremental()
    {
        faces.Clear();

        if (arrayPoint.Count < 4)
        {
            Debug.LogWarning("Il faut au moins 4 points non-coplanaires pour l'enveloppe convexe 3D.");
            return;
        }

        if (!BuildInitialTetrahedron())
        {
            Debug.LogWarning("Impossible de construire un tétraèdre initial");
            return;
        }

        DistributeInitialPoints();

        var iterations = 0;
        while (true)
        {
            var faceWithPoints = GetFaceWithOutsidePoints();
            if (faceWithPoints == null)
                break;

            var eyeIndex = GetFurthestPoint(faceWithPoints);

            var visibleFaces = GetVisibleFaces(faceWithPoints, eyeIndex);
            var horizon = GetHorizon(visibleFaces);
            var orphans = GetOrphanedPoints(visibleFaces, eyeIndex);

            RemoveFaces(visibleFaces);
            var newFaces = BuildCone(horizon, eyeIndex);

            RedistributeOrphans(newFaces, orphans);

            iterations++;
            if (iterations > arrayPoint.Count * 2)
            {
                Debug.LogError($"Convex 3D : trop d'itérations ({iterations}), abandon. Bug probable.");
                break;
            }
        }

        var pointsOutside = 0;
        var maxDistOut = 0f;
        for (var i = 0; i < arrayPoint.Count; i++)
        {
            var p = arrayPoint[i];
            foreach (var f in faces)
            {
                var dist = SignedDistance(f, p);
                if (dist > epsilon)
                {
                    pointsOutside++;
                    if (dist > maxDistOut) maxDistOut = dist;
                    break;
                }
            }
        }
        if (pointsOutside > 0)
        {
            Debug.LogWarning($"Convex 3D : {pointsOutside} points sont à l'extérieur du hull final (max dist = {maxDistOut:F4}). Itérations : {iterations}, faces : {faces.Count}");
        }
        else
        {
            Debug.Log($"Convex 3D OK : {iterations} itérations, {faces.Count} faces.");
        }
    }

    bool BuildInitialTetrahedron()
    {
        var i0 = 0;
        var i1 = 1;
        var i2 = 2;
        var i3 = 3;

        var p0 = arrayPoint[i0];
        var p1 = arrayPoint[i1];
        var p2 = arrayPoint[i2];
        var p3 = arrayPoint[i3];
        
        var planeNormal = Vector3.Cross(p1 - p0, p2 - p0);
        if (planeNormal.sqrMagnitude < epsilon * epsilon)
            return false;

        var planeD = Vector3.Dot(planeNormal, p0);
        
        var distP3 = Vector3.Dot(planeNormal, p3) - planeD;
        if (Mathf.Abs(distP3) < epsilon)
            return false;

        var f0 = MakeFaceOriented(i0, i1, i2, p3); // face opposée à p3
        var f1 = MakeFaceOriented(i0, i1, i3, p2); // face opposée à p2
        var f2 = MakeFaceOriented(i0, i2, i3, p1); // face opposée à p1
        var f3 = MakeFaceOriented(i1, i2, i3, p0); // face opposée à p0

        LinkFaces(f0, f1);
        LinkFaces(f0, f2);
        LinkFaces(f0, f3);
        LinkFaces(f1, f2);
        LinkFaces(f1, f3);
        LinkFaces(f2, f3);

        return true;
    }

    Face MakeFaceOriented(int a, int b, int c, Vector3 outsidePoint)
    {
        var pa = arrayPoint[a];
        var pb = arrayPoint[b];
        var pc = arrayPoint[c];

        var normal = Vector3.Cross(pb - pa, pc - pa);
        var distOut = Vector3.Dot(normal, outsidePoint) - Vector3.Dot(normal, pa);

        if (distOut > 0f)
        {
            return MakeFace(a, c, b);
        }
        return MakeFace(a, b, c);
    }

    Face MakeFace(int a, int b, int c)
    {
        var face = new Face();
        face.a = a;
        face.b = b;
        face.c = c;

        var pa = arrayPoint[a];
        var pb = arrayPoint[b];
        var pc = arrayPoint[c];

        face.normal = Vector3.Cross(pb - pa, pc - pa);
        face.d = Vector3.Dot(face.normal, pa);

        faces.Add(face);
        return face;
    }

    void LinkFaces(Face f1, Face f2)
    {
        for (var i = 0; i < 3; i++)
        {
            int u1, v1;
            GetEdge(f1, i, out u1, out v1);

            for (var j = 0; j < 3; j++)
            {
                int u2, v2;
                GetEdge(f2, j, out u2, out v2);

                if (u1 == v2 && v1 == u2)
                {
                    f1.neighbors[i] = f2;
                    f2.neighbors[j] = f1;
                    return;
                }
            }
        }
    }

    void DistributeInitialPoints()
    {
        var usedIndices = new HashSet<int>();
        foreach (var f in faces)
        {
            usedIndices.Add(f.a);
            usedIndices.Add(f.b);
            usedIndices.Add(f.c);
        }

        for (var i = 0; i < arrayPoint.Count; i++)
        {
            if (usedIndices.Contains(i)) continue;

            var p = arrayPoint[i];
            foreach (var f in faces)
            {
                if (SignedDistance(f, p) > epsilon)
                {
                    f.outsidePoints.Add(i);
                    break; // Une seule face suffit.
                }
            }
        }
    }

    Face GetFaceWithOutsidePoints()
    {
        foreach (var f in faces)
        {
            if (f.outsidePoints.Count > 0)
                return f;
        }
        return null;
    }

    int GetFurthestPoint(Face face)
    {
        var bestIndex = face.outsidePoints[0];
        var bestDist = SignedDistance(face, arrayPoint[bestIndex]);

        for (var i = 1; i < face.outsidePoints.Count; i++)
        {
            var idx = face.outsidePoints[i];
            var dist = SignedDistance(face, arrayPoint[idx]);
            if (dist > bestDist)
            {
                bestDist = dist;
                bestIndex = idx;
            }
        }

        return bestIndex;
    }

    List<Face> GetVisibleFaces(Face startFace, int eyeIndex)
    {
        var visible = new List<Face>();
        var queue = new Queue<Face>();
        var eye = arrayPoint[eyeIndex];

        startFace.visible = true;
        visible.Add(startFace);
        queue.Enqueue(startFace);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            for (var i = 0; i < 3; i++)
            {
                var neighbor = current.neighbors[i];
                if (neighbor == null) continue;
                if (neighbor.visible) continue;

                if (SignedDistance(neighbor, eye) > epsilon)
                {
                    neighbor.visible = true;
                    visible.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visible;
    }

    float SignedDistance(Face face, Vector3 p)
    {
        return Vector3.Dot(face.normal, p) - face.d;
    }


    List<HorizonEdge> GetHorizon(List<Face> visibleFaces)
    {
        var horizon = new List<HorizonEdge>();

        foreach (var f in visibleFaces)
        {
            for (var i = 0; i < 3; i++)
            {
                var neighbor = f.neighbors[i];
                if (neighbor == null) continue;
                if (neighbor.visible) continue;

                int u, v;
                GetEdge(f, i, out u, out v);

                var slot = SlotOfNeighbor(neighbor, f);
                horizon.Add(new HorizonEdge(u, v, neighbor, slot));
            }
        }

        return horizon;
    }

    void GetEdge(Face f, int slot, out int u, out int v)
    {
        // slot=0 -> (b, c), slot=1 -> (c, a), slot=2 -> (a, b)
        if (slot == 0) { u = f.b; v = f.c; }
        else if (slot == 1) { u = f.c; v = f.a; }
        else { u = f.a; v = f.b; }
    }

    int SlotOfNeighbor(Face f, Face other)
    {
        if (f.neighbors[0] == other) return 0;
        if (f.neighbors[1] == other) return 1;
        if (f.neighbors[2] == other) return 2;
        return -1;
    }

    List<int> GetOrphanedPoints(List<Face> visibleFaces, int eyeIndex)
    {
        var orphans = new List<int>();
        foreach (var f in visibleFaces)
        {
            foreach (var idx in f.outsidePoints)
            {
                if (idx == eyeIndex) continue;
                orphans.Add(idx);
            }
        }
        return orphans;
    }

    void RemoveFaces(List<Face> facesToRemove)
    {
        foreach (var f in facesToRemove)
        {
            for (var i = 0; i < 3; i++)
            {
                var n = f.neighbors[i];
                if (n == null) continue;
                if (n.visible) continue;

                var slot = SlotOfNeighbor(n, f);
                if (slot != -1) n.neighbors[slot] = null;
            }

            faces.Remove(f);
        }
    }

    List<Face> BuildCone(List<HorizonEdge> horizon, int eyeIndex)
    {
        var newFaces = new List<Face>();
        var pendingByVertex = new Dictionary<int, (Face face, int slot)>();

        foreach (var edge in horizon)
        {
            var tri = MakeFace(edge.a, edge.b, eyeIndex);
            newFaces.Add(tri);
            
            tri.neighbors[2] = edge.neighbor;
            edge.neighbor.neighbors[edge.neighborSlot] = tri;
            TryConnectLateralEdge(pendingByVertex, edge.b, tri, 0);
            TryConnectLateralEdge(pendingByVertex, edge.a, tri, 1);
        }

        WarnIfPendingNotEmpty(pendingByVertex);
        return newFaces;
    }

    void TryConnectLateralEdge(
        Dictionary<int, (Face face, int slot)> pending,
        int vertex, Face face, int slot)
    {
        if (pending.TryGetValue(vertex, out var other))
        {
            face.neighbors[slot] = other.face;
            other.face.neighbors[other.slot] = face;
            pending.Remove(vertex);
        }
        else
        {
            pending[vertex] = (face, slot);
        }
    }

    void WarnIfPendingNotEmpty(Dictionary<int, (Face face, int slot)> pending)
    {
        if (pending.Count != 0)
        {
            Debug.LogWarning($"BuildCone : {pending.Count} arêtes latérales non-reconnectées");
        }
    }

    void RedistributeOrphans(List<Face> newFaces, List<int> orphans)
    {
        foreach (var idx in orphans)
        {
            var p = arrayPoint[idx];
            foreach (var f in newFaces)
            {
                if (SignedDistance(f, p) > epsilon)
                {
                    f.outsidePoints.Add(idx);
                    break;
                }
            }
        }
    }

    List<int> ConvertFacesToTriangles()
    {
        List<int> triangles = new List<int>();

        foreach (var f in faces)
        {
            triangles.Add(f.a);
            triangles.Add(f.b);
            triangles.Add(f.c);
        }

        return triangles;
    }

    public List<Vector3> GetVertices()
    {
        return arrayPoint;
    }

    public int GetFaceCount()
    {
        return faces.Count;
    }
}