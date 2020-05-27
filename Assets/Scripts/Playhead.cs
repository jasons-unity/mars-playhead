using System.Collections.Generic;
using System.Linq;
using Unity.MARS;
using UnityEngine;
using UnityEngine.UI;

//[ExecuteInEditMode]
public class Playhead : MonoBehaviour
{
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private List<Vector3> _worldVertices;
    private LandmarkController _polygonLandmark;
    private LandmarkOutputPolygon _boundingRec;
    private Proxy _thisProxy;
    private bool _playing;
    private bool _init;
    private GameObject _endCube;
    private float _playAreaWidth;
    private GameObject _playAreaOutline;
    private Toggle _lockPlayArea;
    private Proxy[] _proxies;

    // Start is called before the first frame update
    private void Start()
    {
        _proxies = FindObjectsOfType<Proxy>();
        _polygonLandmark = FindObjectOfType<LandmarkController>();
        _boundingRec = _polygonLandmark.output as LandmarkOutputPolygon;
        if (_boundingRec != null) _boundingRec.dataChanged += GetParentSize;

        var toggles = FindObjectsOfType<Toggle>().ToList();
        _lockPlayArea = toggles.Find(toggle => toggle.name == "LockPlayArea");

        // Make playhead invisible
        //GetComponent<Renderer>().enabled = false;

    }

    private void Update()
    {
        var proxies = _proxies.ToList();
        _thisProxy = proxies.Find(proxy => proxy.name == "PlayheadProxy");
        // if this is the first pass, init playhead starting point and ending point
        if (_thisProxy.queryState == QueryState.Tracking && _init == false && _playing == false)
        {
            _init = true;
        }
        UpdatePlayHeadPosition();
        
    }

    private void UpdatePlayHeadPosition()
    {
        if (_worldVertices == null || _worldVertices.Count == 0) return;
        // recalculate start and end each time in case plane has expanded from additional scan
        _startPosition = _worldVertices[1];
        
        _endPosition = _worldVertices[0];

        // determine the width of play area
        _playAreaWidth = Vector3.Distance(_worldVertices[1], _worldVertices[2]);

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
        if (_lockPlayArea.isOn) return;
        if (_boundingRec != null) _worldVertices = _boundingRec.worldVertices;
        if (_playAreaOutline == null)
        {
            _playAreaOutline = new GameObject();
            _playAreaOutline.AddComponent<LineRenderer>();
        }

        var lineRenderer = _playAreaOutline.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0,_worldVertices[0]);
        lineRenderer.SetPosition(1, _worldVertices[1]);
        lineRenderer.SetPosition(2, _worldVertices[2]);
        lineRenderer.SetPosition(3, _worldVertices[3]);
        lineRenderer.SetPosition(4, _worldVertices[0]);
        lineRenderer.widthCurve = AnimationCurve.Linear(0,0.01f, 1, 0.01f);

    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "ToneCube")
        {
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
            var playTone = other.GetComponentInParent<PlayTone>();
            playTone.gain = 0.0f;
        }
    }

}
