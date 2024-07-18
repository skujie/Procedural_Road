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

    [SerializeField, Min(0.1f)] protected float _step = 5;
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
    [SerializeField] private bool _debugPoly;
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            _meshFilter.mesh = BuildMesh();

        Gizmos.color = Color.blue;
        if (!_debugPoly)
        {
            for(int i = 0; i < verts1.Count; i++)
            {
                Vector3 p1 = transform.TransformPoint(verts1[i]);
                Vector3 p2 = transform.TransformPoint(verts2[i]);

                Gizmos.DrawSphere(p1, 0.3f);
                Gizmos.DrawSphere(p2, 0.3f);
                Gizmos.DrawLine(p1,p2);
            }
        }
        else
        {
            foreach(Vector3 vert in _meshFilter.sharedMesh.vertices)
            {
                Gizmos.DrawSphere(transform.TransformPoint(vert), 0.3f);
            }
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

        Vector3 p1, p2;

        for (int splineIndex = 0; splineIndex < SplineCount; splineIndex++)
        {
            float splineLen = _splineContainer.Splines[splineIndex].GetLength();
            float normalizedStep = _step / splineLen;
            int stepCount = Mathf.CeilToInt(1f/normalizedStep);

            for (int i = 0; i < stepCount; i++)
            {
                float t = normalizedStep * i;
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

        //For each spline
        for(int splineIndex=0; splineIndex < SplineCount; splineIndex++)
        {
            float splineLen = _splineContainer.Splines[splineIndex].GetLength();
            float normalizedStep = _step / splineLen;
            int stepCount = Mathf.CeilToInt(1f / normalizedStep);

            //Add the two first vertices
            verts.AddRange(new Vector3[] {verts1[offset], verts2[offset]});

            //For each points pair
            for(int i=1; i< stepCount + 1; i++)
            {
                //Get vertices
                int vertIndex = offset+ i;
                Vector3 p3 = verts1[vertIndex];
                Vector3 p4 = verts2[vertIndex];

                int polyIndex = (offset + i) * 2;
                int t1 = polyIndex;
                int t2 = polyIndex - 1;
                int t3 = polyIndex - 2;

                int t4 = polyIndex;
                int t5 = polyIndex + 1;
                int t6 = polyIndex - 1;

                verts.AddRange(new Vector3[] { p3, p4 });
                tris.AddRange(new int[] { t1, t2, t3, t4, t5, t6});
            }
            offset += stepCount + 1;
        }
        
        for(int i=0; i<tris.Count; i++)
        {
            if(tris[i] < 0)
                Debug.LogWarning("Negative " + tris[i]);
            else if (tris[i] >= verts.Count)
                Debug.LogWarning("Too high " + tris[i]+"/"+verts.Count);
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
