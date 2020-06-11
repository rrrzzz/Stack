using System.Collections;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class PlatformMover : MonoBehaviour
{
    public Transform currentGround;
    public float sideShiftAmount;
    public float transitionSpeed;
    public float upShiftAmount;
    public float movementSpeed;
    public int blocks = 30;
    public GameObject text;
    public float perfectDeviation = 0.01f;
    public float torqueAmount = 30;
    public bool isTestMode = true;
    

    private Vector3 _offsetFromCenter;
    private Transform _transform;
    private MeshRenderer _movingRenderer;
    private Vector3 _leftGroundEdge;
    private Vector3 _rightGroundEdge;
    private int _currentBlocks;
    private bool _isMovingAlongZ;
    private Vector3 _movementDirection;
    private bool _isMoving = true;



    // Start is called before the first frame update
    void Start()
    {
        _transform = transform;
        _movingRenderer = _transform.GetComponent<MeshRenderer>();
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isMoving = false;
            CheckLanding();
        }
        
        //use moveTowards to avoid lag? look up code in partisans
        if (_isMoving && !isTestMode)
        {
            var s = Mathf.PingPong((Time.realtimeSinceStartup) * transitionSpeed, sideShiftAmount * 2) - sideShiftAmount;
            var temp = transform.position;
        
            if(_isMovingAlongZ) temp.z = currentGround.position.z + s;
            else temp.x = currentGround.position.x + s;
            _transform.position = temp;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            _isMovingAlongZ = !_isMovingAlongZ;
            
            Debug.Log(_transform.position);
        }

        if (Input.GetKey(KeyCode.Keypad4))
        {
            _transform.Translate(-_transform.right * (movementSpeed * Time.deltaTime));
        }

        if (Input.GetKey(KeyCode.Keypad6))
        {
            _transform.Translate(_transform.right * (movementSpeed * Time.deltaTime));
        }
        
        if (Input.GetKey(KeyCode.Keypad8))
        {
            _transform.Translate(Vector3.forward * (movementSpeed * Time.deltaTime));
        }
        
        if (Input.GetKey(KeyCode.Keypad2))
        {
            _transform.Translate(-Vector3.forward * (movementSpeed * Time.deltaTime));
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _transform.localScale = new Vector3(currentGround.localScale.x,currentGround.localScale.y,currentGround.localScale.z);
            ShiftUpGround();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _currentBlocks = blocks;
        }

        if (_currentBlocks > 0)
        {
            CheckLanding();
            _currentBlocks--;
        }
    }

    void CheckLanding()
    {
        _isMoving = false;
        text.SetActive(false);
        
        var rightMovingEdge = _transform.position + _offsetFromCenter;
        var leftMovingEdge = _transform.position - _offsetFromCenter;

        if (Mathf.Abs(GetEdgeAxisVal(rightMovingEdge) - GetEdgeAxisVal(_rightGroundEdge)) < perfectDeviation)
        {
             ShiftDown();
             currentGround = Instantiate(gameObject).transform;
             Destroy(currentGround.GetComponent<PlatformMover>());
             _isMovingAlongZ = !_isMovingAlongZ;
             Init();
             text.SetActive(true);
             _isMoving = true;
             return;
        }
        
        var leftEdgeRay = new Ray(leftMovingEdge, Vector3.down);
        var rightEdgeRay = new Ray(rightMovingEdge, Vector3.down);

        if (RayToBasePlatform(leftEdgeRay)) ProcessRayHit(isLeftHit: true, leftMovingEdge, _rightGroundEdge);
        else if (RayToBasePlatform(rightEdgeRay)) ProcessRayHit(isLeftHit: false, rightMovingEdge, _leftGroundEdge);
        else Debug.Log("No hit!");
    }

    private void ProcessRayHit(bool isLeftHit, Vector3 movingEdge, Vector3 groundEdge)
    {
        Debug.Log(isLeftHit);
        Vector3 scale = _transform.localScale;

        var newGroundScale = Mathf.Abs(GetEdgeAxisVal(movingEdge) - GetEdgeAxisVal(groundEdge));
        var movementDir = isLeftHit ? _movementDirection : -_movementDirection;
        _transform.position = movingEdge + movementDir * newGroundScale / 2.0f;
        ShiftDown();

        var platformSize = _isMovingAlongZ ? _movingRenderer.bounds.size.z : _movingRenderer.bounds.size.x;
        var cutOffScale = platformSize - newGroundScale;
        
        var cutoffPos = groundEdge;
        cutoffPos.y = _transform.position.y;
        
        var displacement = isLeftHit ? cutOffScale / 2 : -cutOffScale / 2;
        cutoffPos.x += _isMovingAlongZ ? 0 : displacement;
        cutoffPos.z += _isMovingAlongZ ? displacement : 0;

        var cutOffPart = Instantiate(gameObject, cutoffPos, _transform.rotation);
        Destroy(cutOffPart.GetComponent<PlatformMover>());

        var temp = scale;
        temp.x = _isMovingAlongZ ? temp.x : cutOffScale;
        temp.z = _isMovingAlongZ ? cutOffScale : temp.z;
        cutOffPart.transform.localScale = temp;
        var cutOffRb = cutOffPart.GetComponent<Rigidbody>();
        cutOffRb.isKinematic = false;
        cutOffRb.useGravity = true;

        var rotationAxis = _isMovingAlongZ ? Vector3.right : Vector3.forward;
        var torque = isLeftHit ? -torqueAmount : torqueAmount;
        cutOffRb.AddTorque(rotationAxis * torque);
        
        temp.x = _isMovingAlongZ ? temp.x : newGroundScale;
        temp.z = _isMovingAlongZ ? newGroundScale : temp.z;
        _transform.localScale = temp;
        
        StartCoroutine(DestroyAfterTime(cutOffPart));
        currentGround = Instantiate(gameObject).transform;
        Destroy(currentGround.GetComponent<PlatformMover>());
        _isMovingAlongZ = !_isMovingAlongZ;
        Init();
        _isMoving = true;
    }

    float GetEdgeAxisVal(Vector3 edge) => _isMovingAlongZ ? edge.z : edge.x; 


    void ShiftUpGround() => _transform.position = currentGround.position + Vector3.up * (_movingRenderer.bounds.extents.y * 2 + upShiftAmount);
    void ShiftDown() => _transform.position += Vector3.down * upShiftAmount;

    bool RayToBasePlatform(Ray ray) => Physics.Raycast(ray, upShiftAmount + _movingRenderer.bounds.extents.y * 2.2f);

    IEnumerator DestroyAfterTime(GameObject o)
    {
        yield return new WaitForSeconds(2);
        Destroy(o);
    }

    void Init()
    {
        ShiftUpGround();

        _movementDirection = _isMovingAlongZ ? Vector3.forward : Vector3.right;
        
        _offsetFromCenter = _isMovingAlongZ ? 
            _movementDirection * _movingRenderer.bounds.extents.z : _movementDirection * _movingRenderer.bounds.extents.x;

        _leftGroundEdge = currentGround.position - _offsetFromCenter;
        _rightGroundEdge = currentGround.position + _offsetFromCenter;
    }
}