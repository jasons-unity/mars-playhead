using UnityEngine;

public class PlayTone : MonoBehaviour
{

    //public AudioClip tone;
    public double frequency = 440.0;
    private double increment;
    private double phase;
    private double sampling_frequency = 48000.0;

    public float gain = 0f;

    private float _sensitivity;
    private Vector3 _mouseReference;
    private Vector2 _touchReference;
    private Vector3 _mouseOffset;
    private Vector3 _touchOffset;
    private Vector3 _rotation;
    private Vector3 _scale;
    private bool _isRotating;
    private bool _isScaling;
    private Vector3 _minimumScale;
    private Vector3 _startScale;

    void Start ()
    {
        _sensitivity = 0.4f;
        _rotation = Vector3.zero;
        _minimumScale = transform.localScale;
    }
     
    void Update()
    {
  
        if(_isRotating)
        {
            // offset
            _mouseOffset = (Input.mousePosition - _mouseReference);
         
            // apply rotation
            _rotation.y = -(_mouseOffset.x + _mouseOffset.y) * _sensitivity;
         
            // rotate
            transform.Rotate(_rotation);
         
            // store mouse
            _mouseReference = Input.mousePosition;
        }
        if(_isScaling)
        {
            transform.localScale += new Vector3(
                0,
                0,
                (Input.mousePosition.y - _mouseReference.y) * Time.deltaTime * _sensitivity);

            // store mouse
            _mouseReference = Input.mousePosition;
        }

    }
     
    void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.touchCount >= 2)
            {
                _isScaling = true;
            }
            else
            {
                _isRotating = true;
            }
        }


        // store mouse
        _mouseReference = Input.mousePosition;
    }
     
    void OnMouseUp()
    {
        // rotating flag
        _isRotating = false;
        _isScaling = false;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        increment = frequency * 2.0 * Mathf.PI / sampling_frequency;

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += increment;
            data[i] = (float) (gain * Mathf.Sin((float) phase));

            if (channels == 2)
            {
                data[i + 1] = data[i];
            }

            if (phase > (Mathf.PI * 2))
            {
                phase = 0.0;
            }
        }
        
    }
}
