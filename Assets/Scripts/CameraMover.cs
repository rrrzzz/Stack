using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Transform initialGround;
    public Transform platform;

    public float finalRotationSpeed = 5;
    public float gameLoopRotationSpeed = 2;
    public int rotationLoopCount = 100;

    private bool _isMoving = true;
    private bool _isRotating;
    private bool _isInitExecuted;
    private bool _isAlignedWithPlatform;
    private float _yDistanceToPlatform;
    private bool _isCamZ = true;
    


    void LateUpdate()
    {
        if (!_isInitExecuted)
        {
            SetDistanceFromPlatform();
            _isInitExecuted = true;
        }
        if (_isMoving) MoveCameraToPlatform();
        
        if (Input.GetKeyDown(KeyCode.E)) ShowWholeTower(_isCamZ);

        if (Input.GetKeyDown(KeyCode.J))
        {
            var temp = transform.position;
            temp.y = platform.position.y + _yDistanceToPlatform;
            transform.position = temp;
        }


        if (_isRotating)
        {
            transform.RotateAround(initialGround.position, Vector3.up, 1 * -finalRotationSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(RotateToNewView(transform.position));
            _isCamZ = !_isCamZ;
        }

    }

    private IEnumerator RotateToNewView(Vector3 initialPos)
    {
        float rotAmount = 0;
        yield return new WaitForSeconds(.2f);
        bool isReturn = false;
        while (!isReturn)
        {
            for (int i = 0; i < rotationLoopCount; i++)
            {
                rotAmount += -gameLoopRotationSpeed * Time.deltaTime;
                transform.RotateAround(initialGround.position, Vector3.up, -gameLoopRotationSpeed * Time.deltaTime);
                if (rotAmount < -90)
                {
                    isReturn = true;
                    break;
                }
            }
            yield return null;
        }
    }


    private void ShowWholeTower()
    {
        //different x rotation fucks everything
        _isMoving = false;
        var eyePos = transform.position;
        var groundPos = initialGround.position;
        
        var halfHeight = (platform.position.y - groundPos.y) / 2;
        var camXRot = transform.rotation.eulerAngles.x;
    
        var eyeTowerDist = Mathf.Abs(eyePos.z - groundPos.z);
        var offsetY = Mathf.Tan(Mathf.Deg2Rad * camXRot) * eyeTowerDist;
        var temp = eyePos;
        temp.y = halfHeight + offsetY;
            
        var towerMiddle = groundPos;
        towerMiddle.y = halfHeight;
        var towerMiddleToEye = temp - towerMiddle;
        towerMiddleToEye.Normalize();
        var halfFovY = Camera.main.fieldOfView / 2;
        var towerMiddleToEyeLength = halfHeight / Mathf.Tan(Mathf.Deg2Rad * halfFovY);
        eyePos = towerMiddle + towerMiddleToEye * (towerMiddleToEyeLength + eyeTowerDist);
    
        var lowerFovRayHeight = Mathf.Tan(Mathf.Deg2Rad * (halfFovY + transform.rotation.eulerAngles.x));
        var lowerFovRayToTower = new Vector3(eyePos.x, -lowerFovRayHeight, 1f);
        lowerFovRayToTower.Normalize();
        var z = eyePos.z / -lowerFovRayToTower.z;
        var rayTowerYAxisIntersection = eyePos + lowerFovRayToTower * z;
        var intersectionTowerBottomDistance = groundPos.y - rayTowerYAxisIntersection.y;
        temp = eyePos;
        temp.y += intersectionTowerBottomDistance / 2;
        eyePos = temp;
        transform.position = eyePos;
        _isRotating = true;
    }
    
    private void ShowWholeTower(bool isCamZ)
    {
        _isMoving = false;
        var eyePos = transform.position;
        var groundPos = initialGround.position;

        var halfHeight = (platform.position.y - groundPos.y) / 2;
        var camXRot = transform.rotation.eulerAngles.x;
        
        var eyeTowerDist = Mathf.Abs(isCamZ ? eyePos.z : eyePos.x);
        var offsetY = Mathf.Tan(Mathf.Deg2Rad * camXRot) * eyeTowerDist;
        var temp = eyePos;
        temp.y = halfHeight + offsetY;
            
        var towerMiddle = groundPos;
        towerMiddle.y = halfHeight;
        var towerMiddleToEye = temp - towerMiddle;
        towerMiddleToEye.Normalize();
        var halfFovY = Camera.main.fieldOfView / 2;
        var towerMiddleToEyeLength = halfHeight / Mathf.Tan(Mathf.Deg2Rad * halfFovY);
        eyePos = towerMiddle + towerMiddleToEye * (towerMiddleToEyeLength + eyeTowerDist);

        var lowerFovRayHeight = Mathf.Tan(Mathf.Deg2Rad * (halfFovY + transform.rotation.eulerAngles.x));
        var lowerFovRayToTower = isCamZ ? new Vector3(eyePos.x, -lowerFovRayHeight, 1f) : new Vector3(1f, -lowerFovRayHeight, eyePos.z);
        lowerFovRayToTower.Normalize();
        var lowerRayToTowerAxis = isCamZ ? lowerFovRayToTower.z : lowerFovRayToTower.x;
        var eyePosAxis = isCamZ ? eyePos.z : eyePos.x;
        eyePosAxis = Mathf.Abs(eyePosAxis);
        var axisLength = eyePosAxis / lowerRayToTowerAxis;
        var rayTowerYAxisIntersection = eyePos + lowerFovRayToTower * axisLength;
        var intersectionTowerBottomDistance = groundPos.y - rayTowerYAxisIntersection.y;
        temp = eyePos;
        temp.y += intersectionTowerBottomDistance / 2;
        eyePos = temp;
        transform.position = eyePos;
        _isRotating = true;
    }

    void MoveCameraToPlatform()
    {
        if (Mathf.Abs(transform.position.y - platform.position.y) < _yDistanceToPlatform || Mathf.Abs(transform.position.y - platform.position.y) > _yDistanceToPlatform + 0.1f)
        {
            var z = transform.position.z;
            var temp = transform.position;
            temp.y += 0.3f * Time.deltaTime;
            // transform.Translate(Vector3.up * (0.3f * Time.deltaTime));
            //transform.position = new Vector3(transform.position.x, transform.position.y, z);
            transform.position = temp;
        }
    }
    
    void SetDistanceFromPlatform()
    {
        _yDistanceToPlatform = transform.position.y - platform.position.y;
    }
}