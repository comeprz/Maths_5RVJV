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
            Debug.LogWarning("Il faut au moins 4 points non-coplanaires pour l'enveloppe convexe 3D");
            return;
        }

        if (!BuildInitialTetrahedron())
        {
            Debug.LogWarning("Impossible de construire un tétraèdre initial");
            return;
        }

        DistributeInitialPoints();

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
        }
    }

    bool BuildInitialTetrahedron()
    {
        // On prend simplement les 4 premiers points
        // p0, p1, p2 non-colinéaires et p3 non-coplanaire avec eux
        var i0 = 0;
        var i1 = 1;
        var i2 = 2;
        var i3 = 3;

        var p0 = arrayPoint[i0];
        var p1 = arrayPoint[i1];
        var p2 = arrayPoint[i2];
        var p3 = arrayPoint[i3];

        // p0, p1, p2 ne sont pas colinéaires
        var planeNormal = Vector3.Cross(p1 - p0, p2 - p0);
        if (planeNormal.sqrMagnitude < epsilon * epsilon)
            return false;

        var planeD = Vector3.Dot(planeNormal, p0);

        // p3 n'est pas dans le plan de (p0, p1, p2)
        var distP3 = Vector3.Dot(planeNormal, p3) - planeD;
        if (Mathf.Abs(distP3) < epsilon)
            return false;

        Face base_, side1, side2, side3;
        if (distP3 > 0f)
        {
            // (p0, p1, p2) a sa normale vers p3 -> il faut inverser pour la base
            base_ = MakeFace(i0, i2, i1);
            side1 = MakeFace(i0, i1, i3);
            side2 = MakeFace(i1, i2, i3);
            side3 = MakeFace(i2, i0, i3);
        }
        else
        {
            base_ = MakeFace(i0, i1, i2);
            side1 = MakeFace(i1, i0, i3);
            side2 = MakeFace(i2, i1, i3);
            side3 = MakeFace(i0, i2, i3);
        }

        LinkFaces(base_, side1);
        LinkFaces(base_, side2);
        LinkFaces(base_, side3);
        LinkFaces(side1, side2);
        LinkFaces(side2, side3);
        LinkFaces(side3, side1);

        return true;
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
        // Récupérer les 4 sommets utilisés par le tétraèdre pour les exclure
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

        // Pour chaque triangle (edge.a, edge.b, eyeIndex) :
        //   - arête (eye, a) "sort" du sommet a -> on enregistre dans leftMap
        //   - arête (b, eye) "entre" dans le sommet b -> on enregistre dans rightMap
        // Chaque sommet du horizon apparaît dans exactement 2 arêtes du horizon
        // (une fois comme "a", une fois comme "b"), donc à la fin chaque entrée
        // de leftMap a son partenaire dans rightMap.
        var leftMap = new Dictionary<int, Face>();
        var rightMap = new Dictionary<int, Face>();

        foreach (var edge in horizon)
        {
            var tri = MakeFace(edge.a, edge.b, eyeIndex);
            newFaces.Add(tri);

            tri.neighbors[2] = edge.neighbor;
            edge.neighbor.neighbors[edge.neighborSlot] = tri;

            leftMap[edge.a] = tri;
            rightMap[edge.b] = tri;
        }

        foreach (var kv in leftMap)
        {
            var vertex = kv.Key;
            var leftTri = kv.Value;
            var rightTri = rightMap[vertex];

            leftTri.neighbors[1] = rightTri;
            rightTri.neighbors[0] = leftTri;
        }

        return newFaces;
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