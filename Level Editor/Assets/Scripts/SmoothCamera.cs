using UnityEngine;
using System.Collections;

//TODO(ugur): Implement an unsigned float struct.
using Real32 = System.Single;
using Units = System.Single;

//TODO(ugur): Implement smoothZoom coroutine (and maybe let other scripts call it)
public class SmoothCamera : MonoBehaviour {



    [Range(0.0f,1.0f)]
    public Real32 loosenessPercentage = 0.112f; //%target per frame
    [Range(0.0f, 1.0f)]
    public Real32 swipeContinuity = 0.8f;
    [Range(0.5f, 1.5f)]
    public Real32 swipeSensitivity = 0.8f;
    public uint deadzonePixels = 5;
    public Transform platform;
    [SerializeField]
    private Camera platformCamera;
    private Vector3 platformPivot = Vector3.zero;
    private Vector3 swipeOrigin = Vector3.zero;
    private float ScrollSpeed = -4f;
    private int DefaultFoV = 40;
    private int MaxFoV = 47;
    private int MinFov = 27;

    //private bool isRotating = false;
    //private Queue angleQueue = default(Queue);

    private bool
        isInDeadzone(Real32 deltaPixels)
    {
        return Mathf.Abs(deltaPixels) <= deadzonePixels;
    }

    private int signOf(Real32 number)
    {
        int result = (int)(number / Mathf.Abs(number));
        Debug.Assert(result != 0);
        return result;
    }

    private Real32 getForcePerPixels()
    {
        return (Real32) (swipeSensitivity / ((Screen.width / 2) * 0.1));
    }

    private Real32 calculateSwipeForce(Real32 deltaPixels)
    {
        return Mathf.Abs(deltaPixels) * getForcePerPixels();
    }

    private Real32 calculateRotationAngle(Real32 _swipe_force)
    {
        Debug.Assert(_swipe_force >= 0.0f);
        Real32 result = 
            2 * Mathf.PI * (_swipe_force);
        return result;
    }

    IEnumerator rotateSmooth(Vector3 _pivot, Vector3 _about_axis,
                        Real32 _target_angle)
    {
        Debug.Assert(_target_angle >= 0.0f);
        while (_target_angle > 0.0f)
        {
            Real32 rotationPerFrame = loosenessPercentage * _target_angle;
            platformCamera.transform.RotateAround(
                _pivot, _about_axis, rotationPerFrame);

            _target_angle -= rotationPerFrame;

            yield return null; 
        }
    }

    private void rotateHorizontal(Real32 deltaPixels)
    {
        if (!isInDeadzone(deltaPixels))
        {
            Real32 swipeForce = calculateSwipeForce(deltaPixels);
            Real32 rotationAngle = calculateRotationAngle(swipeForce);
            Vector3 aboutAxis = signOf(deltaPixels) * Vector3.up;
            if (true)//!isRotating)
            {
                StartCoroutine(rotateSmooth(platformPivot, aboutAxis, rotationAngle));
            }
            else
            {
                //queueAngle(rotationAngle);
            }
        }
    }

    private void Awake()
    {
        platformCamera = this.GetComponent(typeof(Camera)) as Camera;
        platformPivot = platform.position;
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeOrigin = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 currentPosition = Input.mousePosition;
            Vector3 swipeVector = currentPosition - swipeOrigin;

            rotateHorizontal(swipeVector.x);

            swipeOrigin = Vector3.Lerp(swipeOrigin, currentPosition, 1 - swipeContinuity);
        }

        var cameraZoom = Input.GetAxis("Mouse ScrollWheel") * ScrollSpeed;

        platformCamera.fieldOfView = Mathf.Clamp(
            platformCamera.fieldOfView + cameraZoom, MinFov, MaxFoV);

    }
}
