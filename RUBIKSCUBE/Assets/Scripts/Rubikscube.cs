using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubikscube : MonoBehaviour
{
    /* MEMBER VARIABLES */

    /*====================== Cube Components ======================*/

    //The cube GameObject that will be spawned to make the rubiks cube (a cube with each face having a different color)
    [SerializeField] private GameObject cubeMulti = null;

    //all the cubes of the rubiks cube
    private List<GameObject> tabCube;

    //the parent of all cubes, serve as a pivot point to rotate the cube on itself.
    private GameObject centralPos;

    //the size of half of a cubeMulti
    [SerializeField] private float offset = 0.5f;

    /*====================== Camera ======================*/

    //The Camera that shows the rubiks cube to the user
    private Camera mainCamera;

    //Camera offset away from the cube (usually multiplied by size to always see the whole cube)
    [SerializeField] private float camOffset = -4.0f;

    //Speed at which the camera rotate to
    [SerializeField] private float speed = 500;

    /*====================== UI Member ======================*/

    //the number cube rot of the number of cube in the rubiks cube
    [SerializeField] private int size = 3;

    //boolean to know if the cube has been shuffled at least once (would be used in a button to shuffle the cube after completion)
    private bool shuffle = false;

    //the nb of time the cube should be suffled, can be considered as a level of difficulty
    [SerializeField] public uint shuffleNb = 10u;

    //has the cube been resolved yet
    private bool completed = false;

    //the max difference there should be between rotation of cubes (cause all cubes with same rotation is a completed cube)
    [SerializeField] private float checkCompletedEpsilon = 0.1f;

    /*====================== Move Face Behavior ======================*/

    //the list of cube moving when user is holding
    private List<GameObject> movingCube;

    //if the user's is holding the left button of the mouse
    private bool rightHolding = false;

    //game object that was pointed by the mouse when right click was pressed
    private GameObject grabedFace = null;

    //boolean that chooses the rotate plane
    private bool chooseRotatePlane = false;

    //the plane of the face that is moving when user is holding
    private Plane movingPlane;

    //the plane of the face that is moving when user is holding
    private Plane ctrlPlane;

    //The game object that moves and becomes a temporary parent of the moving cubes to move them around
    private GameObject myRotatePoint;

    //the max distance between plane and cube that is needed to be added to moving cube
    [SerializeField] private float detectEpsilon = 0.1f;

    //Used to know when the face should be moving in a direction
    [SerializeField, Range(0.0f, 10.0f)] private float faceTurnSensibility = 1.0f;

    //The speed at which the face will turn
    [SerializeField] private float faceTurnSpeed = 1.5f;

    //position that collide with the raycast of the movingPlane
    private Vector3 oldPoint = Vector3.zero;

    // check parent 
    private bool haveRotatePointParent = false;

    /*====================== Animation Behavior ======================*/
    
    //The difference between orientation needed and having before rotation will be considered completed
    [SerializeField] private float animFinishedEpsilon = 0.1f;
    
    //The speed at which a face turn when animating
    [SerializeField] private float animTurnSpeed = 1.5f;

    //the coroutine running when user release a face to get it to a good place
    private IEnumerator animCoroutine;

    //boolean to know if the anmation has finished
    private bool animRunning = false;

    /* METHODS */

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

    Quaternion SimpleRotate(Quaternion inOrientation, Vector3 rotateAxis,  float rotateAngle, float slerpComponent = 1.0f)
    {
        Quaternion rotate = Quaternion.AngleAxis(rotateAngle, rotateAxis);
        rotate = Quaternion.SlerpUnclamped(Quaternion.identity, rotate, Time.deltaTime * slerpComponent);

        return inOrientation * rotate;
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
                if (!(Vector3.Distance(cube.transform.forward, comparedCube.transform.forward) <= checkCompletedEpsilon))
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

        Debug.Log( faceTurnSpeed * (1.0f / (float)size) * moveRate);

        myRotatePoint.transform.rotation = SimpleRotate(myRotatePoint.transform.rotation, movingPlane.normal, faceTurnSpeed * (1.0f / (float)size), moveRate);

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

        if (!completed)
            CheckCompleted();
    }


    // Update is called once per frame
    void Update()
    {
        if (!shuffle)
        {
            ShuffleCube();
        }

   

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
