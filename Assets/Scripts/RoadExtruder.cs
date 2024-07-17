using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class RoadExtruder : MonoBehaviour
{
    #region Variables
    #region Serialized
    [SerializeField] protected SplineContainer _splineContainer;
    [SerializeField] protected MeshFilter _meshFilter;

    [SerializeField, Min(1)] protected int _resolution = 5;
    [SerializeField, Range(0.1f, 10f)] protected float _width = 3f;
    #endregion
    #region Private
    protected List<Vector3> verts1 = new();
    protected List<Vector3> verts2 = new();
    #endregion
    #region Properties
    int SplineCount => _splineContainer.Splines.Count;
    #endregion
    #endregion

    #region Methods
    #region Unity Methods
    protected virtual void OnEnable()
    {
        Spline.Changed += OnSplineChange;
    }

    protected virtual void OnDisable()
    {
        Spline.Changed += OnSplineChange;
    }
#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            _meshFilter.mesh = BuildMesh();

        Gizmos.color = Color.blue;
        for(int i = 0; i < verts1.Count; i++)
        {
            Vector3 p1 = transform.TransformPoint(verts1[i]);
            Vector3 p2 = transform.TransformPoint(verts2[i]);

            Gizmos.DrawSphere(p1, 0.3f);
            Gizmos.DrawSphere(p2, 0.3f);
            Gizmos.DrawLine(p1,p2);
        }
    }

    protected virtual void Reset()
    {
        transform.parent.TryGetComponent(out _splineContainer);
        TryGetComponent(out _meshFilter);
    }
#endif
    #endregion

    #region Callbacks
    protected virtual void OnSplineChange(Spline spline, int point, SplineModification modification)
    {
        if (!_splineContainer.Splines.Contains(spline)) return;

        _meshFilter.mesh = BuildMesh();
    }
    #endregion

    #region Private
    protected virtual void SampleSpline(int index, float t, out Vector3 p1, out Vector3 p2)
    {
        _splineContainer.Splines[index].Evaluate(t, out float3 pos, out float3 fwd, out float3 up);

        float3 right = Vector3.Cross(fwd, up).normalized;
        p1 = pos + (right * _width);
        p2 = pos - (right * _width);
    }

    protected virtual void GetVertices()
    {
        verts1.Clear();
        verts2.Clear();

        float step = 1f / _resolution;
        Vector3 p1, p2;

        for (int splineIndex = 0; splineIndex < SplineCount; splineIndex++)
        {
            for (int i = 0; i < _resolution; i++)
            {
                float t = step * i;
                SampleSpline(splineIndex, t, out p1, out p2);
                verts1.Add(p1);
                verts2.Add(p2);
            }
            SampleSpline(splineIndex, 1f, out p1, out p2);
            verts1.Add(p1);
            verts2.Add(p2);
        }
    }

    protected virtual Mesh BuildMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        int offset = 0;

        GetVertices();

        for(int splineIndex=0; splineIndex < SplineCount; splineIndex++)
        {
            int splineOffset = (int)_resolution * splineIndex;
            splineOffset += splineIndex;

            for(int i=1; i<_resolution+1; i++)
            {
                int vertOffset = splineOffset + i;
                Vector3 p1 = verts1[vertOffset - 1];
                Vector3 p2 = verts2[vertOffset - 1];
                Vector3 p3 = verts1[vertOffset];
                Vector3 p4 = verts2[vertOffset];

                offset = 4 * (int)_resolution * splineIndex;
                offset += 4 * (i - 1);

                int t1 = offset;
                int t2 = offset + 2;
                int t3 = offset + 3;

                int t4 = offset + 3;
                int t5 = offset + 1;
                int t6 = offset;

                verts.AddRange(new List<Vector3>() { p1, p2, p3, p4 });
                tris.AddRange(new List<int>() { t1, t2, t3, t4, t5, t6});
            }
        }
        
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        //mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }
    #endregion
    #endregion
}
