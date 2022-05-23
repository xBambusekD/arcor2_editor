using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;

public class SplineExamDemo : MonoBehaviour
{
    // Start is called before the first frame update
    public Spline spline;

    public GameObject start;

    public MeshBender bender;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        spline.nodes[0].Position = start.transform.localPosition;
        bender.ComputeIfNeeded();
    }
}
