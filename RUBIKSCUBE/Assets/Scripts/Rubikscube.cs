using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubikscube : MonoBehaviour
{
    private Camera mainCamera;

    [SerializeField] private GameObject cubeMulti = null;
    private List<GameObject> tabCube;
    private GameObject centralPos;
    [SerializeField] private int size = 3;
    [SerializeField] private float offset = 0.5f;
    [SerializeField] private float detectEpsilon = 0.1f;
    [SerializeField] private float animFinishedEpsilon = 0.1f;
    [SerializeField] private float camOffset = -4.0f;
    [SerializeField] public uint shuffleNb = 10u;
    [SerializeField, Range(0.0f, 10.0f)] private float faceTurnSensibility = 1.0f;

    //
    [SerializeField] private float faceTurnSpeed = 1.5f;
    [SerializeField] private float animTurnSpeed = 1.5f;

    private bool shuffle = false;

    private bool completed = false;

    //center of the rubiks cube
    private Vector3 center = Vector3.zero;

    //if the user's is holding the left button of the mouse
    private bool rightHolding = false;

    //boolean that chooses the rotate plane
    private bool chooseRotatePlane = false;

    //game object that was pointed by the mouse when right click was pressed
    private GameObject  grabedFace = null;

    //position that collide with the raycast of the movingPlane
    private Vector3     oldPoint = Vector3.zero;

    //Size of the rubiks cube from its center
    private float       rubiksSize = 0.0f;

    //the list of cube moving when user is holding
    private List<GameObject> movingCube;

    //the plane of the face that is moving when user is holding
    private Plane movingPlane;

    //the plane of the face that is moving when user is holding
    private Plane ctrlPlane;

    //the coroutine running when user release a face to get it to a good place
    private IEnumerator animCoroutine;

    //boolean to know if the anmation has finished
    private bool animRunning = false;


    [SerializeField] private float speed = 500;

    //
    private GameObject myRotatePoint;

    // check parent 
    private bool haveRotatePointParent = false;

    // Start is called before the first frame update
    void Start()
    {
        // ============================= Cube =============================  // 
        
        centralPos  = new GameObject();
        tabCube     = new List<GameObject>();
        movingCube  = new List<GameObject>();
        myRotatePoint= new GameObject();

        centralPos.transform.position = new Vector3(size / 2, size / 2, size / 2);

        int i = 0;
        int j = 0;
        int k = 0;
        Vector3 pos = new Vector3(i, j, k);

        rubiksSize = (((float)size) / 2.0f) - offset;
        center = new Vector3(rubiksSize, rubiksSize, rubiksSize);

        //construction of cube 
        for (; i < size ; i ++)
        {
            pos.x = i;
            for (j = 0; j < size; j++)
            {
                pos.y = j;
                for (k = 0; k < size; k++)
                {
                    pos.z = k;
                    if ((pos.x < size - 1 && pos.x > 0) && (pos.y < size - 1 && pos.y > 0) && (pos.z < size - 1 && pos.z > 0))
                    { }
                    else
                        tabCube.Add(Instantiate(cubeMulti, pos, Quaternion.identity));
                }
            }
        }

        //set parent of cube
        foreach (GameObject tab in tabCube)
        {
            tab.transform.parent = centralPos.transform;
        }

        myRotatePoint.transform.parent = centralPos.transform;
        // check face after this and hide some of this 

        // ============================= Camera =============================  // 

        mainCamera = Camera.main;

        mainCamera.transform.position = new Vector3(size / 2, size/2, -size + camOffset);
        mainCamera.transform.LookAt(centralPos.transform);

    }

    void ShuffleCube()
    {
        shuffle = true;
        // ============================= SuffleCube ========================= //
        for (uint nb = 0; nb < shuffleNb; nb++)
        {
            int random  = Random.Range(0, tabCube.Count);
            int rand    = Random.Range(0, 2);//forward, up, right

            if (rand == 1)
                movingPlane = new Plane(tabCube[random].transform.up, tabCube[random].transform.up * Vector3.Dot(tabCube[random].transform.up, tabCube[random].transform.position));
            else if (rand == 2)
                movingPlane = new Plane(tabCube[random].transform.forward, tabCube[random].transform.forward * Vector3.Dot(tabCube[random].transform.forward, tabCube[random].transform.position));
            else
                movingPlane = new Plane(tabCube[random].transform.right, tabCube[random].transform.right * Vector3.Dot(tabCube[random].transform.right, tabCube[random].transform.position));

            List<Quaternion> orientationQuaternion = new List<Quaternion>();
            orientationQuaternion.Add(Quaternion.AngleAxis(90.0f, movingPlane.normal));
            orientationQuaternion.Add(Quaternion.AngleAxis(-90.0f, movingPlane.normal));
            orientationQuaternion.Add(Quaternion.AngleAxis(180.0f, movingPlane.normal));

            int orientationRand = Random.Range(0, orientationQuaternion.Count);

            GetCubeFromPlane();

            myRotatePoint.transform.rotation = orientationQuaternion[orientationRand];

            ClearParent();
        }
    }

    void CheckCompleted()
    {
        foreach (GameObject cube in tabCube)
        {
            foreach (GameObject comparedCube in tabCube)
            {
                if (!(Vector3.Distance(cube.transform.forward, comparedCube.transform.forward) <= animFinishedEpsilon))
                {
                    completed = false;
                    return;
                }
            }
        }

        completed = true;
    }

    private void OnDrawGizmos()
    {
        if (myRotatePoint && !completed)
            Gizmos.DrawWireSphere(myRotatePoint.transform.position, 0.5f);
        if (myRotatePoint && completed)
            Gizmos.DrawSphere(myRotatePoint.transform.position, 0.5f);
    }

    Quaternion GetProperOrientation()
    {
        List<Quaternion> orientationQuaternion = new List<Quaternion>();
        orientationQuaternion.Add(Quaternion.identity);
        orientationQuaternion.Add(Quaternion.AngleAxis(90.0f, movingPlane.normal));
        orientationQuaternion.Add(Quaternion.AngleAxis(-90.0f, movingPlane.normal));
        orientationQuaternion.Add(Quaternion.AngleAxis(180.0f, movingPlane.normal));

        Quaternion returnQuat = Quaternion.identity;
        float angle = float.MaxValue;

        foreach (Quaternion quaternion in orientationQuaternion)
        {
            float newAngle = Quaternion.Angle(myRotatePoint.transform.rotation, quaternion);
            if (newAngle < angle)
            {
                angle = newAngle;
                returnQuat = quaternion;
            }
        }

        return returnQuat;
    }

    IEnumerator SetBackProperly()
    {
        Quaternion properOrientation = GetProperOrientation();
        animRunning = true;

        while (Quaternion.Angle(myRotatePoint.transform.rotation, properOrientation) >= animFinishedEpsilon)
        {
            myRotatePoint.transform.rotation = Quaternion.Slerp(myRotatePoint.transform.rotation, properOrientation, animTurnSpeed * 1.0f/(float)size * Time.deltaTime);
        
            yield return null;
        }
        myRotatePoint.transform.rotation = properOrientation;
        animRunning = false;
        ClearParent();

        yield break;
    }

    void GetGrabedFace()
    {
        rightHolding        = true;
        chooseRotatePlane   = false;

        //Create a ray from the Mouse click position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            oldPoint    = hit.point;
            grabedFace  = hit.collider.gameObject;
            ctrlPlane = new Plane(grabedFace.transform.forward, grabedFace.transform.forward * Vector3.Dot(grabedFace.transform.forward, grabedFace.transform.position));
        }
    }

    bool ChooseMovingFace()
    {
        //Create a ray from the Mouse click position
        Ray         ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit  hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 grabDist = (hit.point - oldPoint);
            if (grabDist.sqrMagnitude >= (float)size * faceTurnSensibility)
            {

                oldPoint = hit.point;
                float rightRate     = Mathf.Abs(Vector3.Dot(grabedFace.transform.up, grabDist));
                float upRate        = Mathf.Abs(Vector3.Dot(grabedFace.transform.right, grabDist));

                if (upRate >= rightRate)
                {
                    movingPlane = new Plane(grabedFace.transform.up, grabedFace.transform.up * Vector3.Dot(grabedFace.transform.up, grabedFace.transform.position));
                    return true;
                }

                movingPlane = new Plane(grabedFace.transform.right, grabedFace.transform.right * Vector3.Dot(grabedFace.transform.right, grabedFace.transform.position));
                return true;
            }
        }

        return false;
    }

    void GetMovingFace()
    {
        chooseRotatePlane = true;
        GetCubeFromPlane();

        haveRotatePointParent = true;

        oldPoint = Input.mousePosition;
    }

    void GetCubeFromPlane()
    {
        foreach (GameObject gameObject in tabCube)
        {
            float distToPlane = movingPlane.GetDistanceToPoint(gameObject.transform.position);
            if (distToPlane <= detectEpsilon && distToPlane >= -detectEpsilon)
            {
                movingCube.Add(gameObject);
            }
        }

        myRotatePoint.transform.position = Vector3.zero;
        myRotatePoint.transform.rotation = Quaternion.identity;
        foreach (GameObject cub in movingCube)
        {
            myRotatePoint.transform.position += cub.transform.position;
        }

        if (movingCube.Count != 0.0f)
            myRotatePoint.transform.position /= movingCube.Count;

        foreach (GameObject cub in movingCube)
        {
            cub.transform.parent = myRotatePoint.transform;
        }
    }

    void MoveFace()
    {
        Vector3 newPoint = Input.mousePosition;

        float moveRate = Vector3.Dot(Vector3.Cross(ctrlPlane.normal,movingPlane.normal), (newPoint - oldPoint));

        Vector3 planeCenter = center + movingPlane.normal * movingPlane.distance;

        Quaternion rotate       = Quaternion.AngleAxis(faceTurnSpeed * 1.0f/(float)size, movingPlane.normal);
        rotate                  = Quaternion.SlerpUnclamped(Quaternion.identity, rotate, Time.deltaTime * moveRate);

        myRotatePoint.transform.rotation = myRotatePoint.transform.rotation * rotate;

        oldPoint = newPoint;
    }

    void ClearParent()
    {
        foreach (GameObject cub in movingCube)
        {
            cub.transform.parent = centralPos.transform;
        }

        movingCube.Clear();
        
        haveRotatePointParent = false;
    }


    // Update is called once per frame
    void Update()
    {
        if (!shuffle)
        {
            ShuffleCube();
        }

        if (!completed)
            CheckCompleted();

        //Detect when there is a mouse click
        if (Input.GetButton("Fire2") && !animRunning)
        {
            if (!rightHolding)
            {
                GetGrabedFace();
            }
            else
            {
                if (!chooseRotatePlane)
                {
                    if (ChooseMovingFace())
                    {

                        GetMovingFace();
                    }
                }
                else
                {
                    MoveFace();
                }
            }
        }
        else
        {
            if (haveRotatePointParent)
            {
                if (!animRunning)
                {
                    animCoroutine = SetBackProperly();
                    StartCoroutine(animCoroutine);
                }
            }

            if (Input.GetButton("Fire1") && !animRunning)
            {
                float verti = Input.GetAxis("Mouse X") * speed;
                float hori = Input.GetAxis("Mouse Y") * speed;

                centralPos.transform.Rotate(hori * Time.deltaTime, -verti * Time.deltaTime, 0, Space.World);
            }

            chooseRotatePlane   = false;
            rightHolding        = false;
        }
    }
}
