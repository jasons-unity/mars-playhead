using System.Collections.Generic;
using System.Linq;
using Unity.MARS;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

//[ExecuteInEditMode]
public class Playhead : MonoBehaviour
{
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private List<Vector3> _wordVertices;
    public LandmarkController polygonLandmark;
    private SimulatedObjectsManager _simManager;
    public Proxy thisProxy;
    private bool _playing;
    private bool _init;
    private GameObject _endCube;
    private float _playAreaWidth;

    // Start is called before the first frame update
    private void Start()
    {
        
        _simManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
        
        var boundingRec = polygonLandmark.output as LandmarkOutputPolygon;
        if (boundingRec != null) boundingRec.dataChanged += GetParentSize;
        // find leftmost and rightmost gameobjects to know start and end position of playhead

        // Need to add a buffer value to append time to end position in be able to adjust loop

        // Make playhead invisible
        //GetComponent<Renderer>().enabled = false;
        
        
    }

    private void Update()
    {
        // if this is the first pass, init playhead starting point and ending point
        if (thisProxy.queryState == QueryState.Tracking && _init == false && _playing == false)
        {
            _init = true;
        }
        UpdatePlayHeadPosition();
    }

    private void UpdatePlayHeadPosition()
    {
        if (_wordVertices == null || _wordVertices.Count == 0) return;
        // recalculate start and end each time in case plane has expanded from additional scan
        _startPosition = _wordVertices[1];
        _endPosition = _wordVertices[0];
        
        // determine the width of play area
        _playAreaWidth = Vector3.Distance(_wordVertices[1], _wordVertices[2]);
        _startPosition += new Vector3( _playAreaWidth / 2,0, 0);
        
        // set playhead width to match play area width
        var localScale = gameObject.transform.localScale;
        localScale = new Vector3(0.1f,0.1f,_playAreaWidth);
        gameObject.transform.localScale = localScale;

        // first time play
        if (_init)
        {
            _init = false;
            
            // place proxy at start
            gameObject.transform.parent.position = _startPosition;
            
            
            // if endcube doesn't exist
            if (!_endCube)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "endCube";
                var cubeRigid = cube.AddComponent<Rigidbody>();
                cubeRigid.useGravity = false;
                cubeRigid.isKinematic = true;
                _endCube = new GameObject();
                cube.transform.parent = _endCube.transform;
                cube.transform.position += new Vector3(0.5f,0f,0.5f);

            }
            _playing = true;
        }
        if (_playing)
        {
            // move playhead forward
            gameObject.transform.parent.position += gameObject.transform.parent.right * 0.01f;
            
            // reposition endcube in case plane scan has resized play area
            _endCube.transform.position = _endPosition;
            
            var endCubeScale = new Vector3(_playAreaWidth, 0.5f, 0.1f);
            
            _endCube.transform.localScale = endCubeScale;
            var boxCollider = _endCube.GetComponentsInChildren<BoxCollider>().FirstOrDefault();
            boxCollider.transform.localScale = endCubeScale;
        }
    }

    private void GetParentSize(ICalculateLandmarks l)
    {
        var boundingRec = l as LandmarkOutputPolygon;
        if (boundingRec != null) _wordVertices = boundingRec.worldVertices;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "ToneCube")
        {
            Debug.Log("PlayTone");
            var playTone = other.GetComponentInParent<PlayTone>();
            playTone.frequency = Mathf.Abs(other.transform.rotation.eulerAngles.y) * 55;
            playTone.gain = 0.5f;
        }

        if(other.name == "endCube")
            gameObject.transform.parent.position = _startPosition;
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.name == "ToneCube")
        {
            Debug.Log("StopPlayTone");
            var playTone = other.GetComponentInParent<PlayTone>();
            playTone.gain = 0.0f;
        }
    }

}
