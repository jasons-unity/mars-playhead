using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.MARS;
using UnityEngine;

[ExecuteInEditMode]
public class Playhead : MonoBehaviour
{
    float speed = 5f;
    private Vector3 startPosition;
    private Vector3 endPosition;
    public LandmarkController polygonLandmark;

    // Start is called before the first frame update
    void Start()
    {
        var gos = FindObjectOfType<GameObject>();
        // find leftmost and rightmost gameobjects to know start and end position of playhead

        // Need to add a buffer value to append time to end position in be able to adjust loop

        // Make playhead invisible
        //GetComponent<Renderer>().enabled = false;
        startPosition = new Vector3(0,0,0);
        endPosition = new Vector3(10,0,0);
    }
    
    void Update()
    {
        //UpdatePlayheadPosition(gameObject);
        var edgeOutput = polygonLandmark.output as LandmarkOutputPolygon;
        //var BoundingRect = edgeOutput.AvailableLandmarkDefinitions;
        
        if (edgeOutput == null)
        {
            // The assigned landmark is not set to type “edge”
        }
        else
        {
            // Do something with the data in edgeOutput, such
            //Debug.Log(edgeOutput.pose + " -> " + edgeOutput.pose);
            //Debug.Log(edgeOutput.localVertices.Count);
        }
    }

    public void test(LandmarkController l)
    {
        var edgeOutput = l.output as LandmarkOutputPolygon;
        Debug.Log(edgeOutput.pose + " -> " + edgeOutput.pose);
        Debug.Log(edgeOutput.worldVertices.Count);
    }

    void UpdatePlayheadPosition(GameObject go)
    {

        go.transform.Translate(Vector3.right * Time.deltaTime * speed);

        Debug.Log(Vector3.Distance(go.transform.position, endPosition).ToString());

        if (Vector3.Distance(go.transform.position, endPosition) < 1)
        {
            go.transform.position = startPosition;
        }

    }

    public void GetParentSize()
    {
        // Can I get the size of the generated plane?
    }

}
