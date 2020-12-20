using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Rubikscube : MonoBehaviour
{
    #region MEMBER_VARIABLES


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

    private Camera mainCamera;

    //Camera offset away from the cube (usually multiplied by size to always see the whole cube)
    [SerializeField] private float camOffset = -4.0f;

    // zoom speed 
    [SerializeField] private float zoomSpeed = 100;

    // limit zoom
    private float limitZoom = 4.5f;
    private Vector3 savePosCam;

    /*====================== Rotate Cube ======================*/

    [SerializeField] float rotAngularSpeed = 90f;
    [SerializeField] float maxRotAngleDuringOneFrame = 179f;

    Quaternion targetOrientationQuat;
    Vector3 previousMousePos;

    /*====================== UI Member ======================*/

    //the number cube rot of the number of cube in the rubiks cube
    [SerializeField] public int size = 3;

    //boolean to know if the cube has been shuffled at least once (would be used in a button to shuffle the cube after completion)
    private bool shuffle = false;

    //the nb of time the cube should be suffled, can be considered as a level of difficulty
    [SerializeField] public uint shuffleNb = 10u;

    private bool completed = false;

    //the max difference there should be between rotation of cubes (cause all cubes with same rotation is a completed cube)
    [SerializeField] private float checkCompletedEpsilon = 0.1f;

    [SerializeField] private GameObject levelManager;
    private UImanager uiManager;

    [SerializeField] private Text youwin = null;

    /*====================== Move Face Behavior ======================*/

    //the list of cube moving when user is holding
    private List<GameObject> movingCube;

    private bool leftHolding = false;

    private GameObject grabedFace = null;

    private bool chooseRotatePlane = false;

    //the plane of the face that is moving when user is holding
    private Plane movingPlane;

    //the plane of the grabedface
    private Plane ctrlPlane;

    //The game object that moves and becomes a temporary parent of the moving cubes to move them around
    private GameObject myRotatePoint;

    //the max distance between plane and cube that is needed to be added to moving cube
    [SerializeField] private float detectEpsilon = 0.1f;

    [SerializeField, Range(0.0f, 10.0f)] private float faceTurnSensibility = 1.0f;

    [SerializeField] private float faceTurnSpeed = 1.5f;

    private Vector3 oldPoint = Vector3.zero;

    private bool haveRotatePointParent = false;

    /*====================== Animation Behavior ======================*/

    [SerializeField] private float animFinishedEpsilon = 0.1f;

    [SerializeField] private float animTurnSpeed = 1.5f;

    private IEnumerator animCoroutine;

    private bool animRunning = false;
    #endregion

    /* METHODS */

    // Start is called before the first frame update
    void Start()
    {
        // ============================= UI =============================  // 

        youwin.enabled = false;

        if (PlayerPrefs.HasKey("Size"))
            size = (int)PlayerPrefs.GetFloat("Size");
        if (PlayerPrefs.HasKey("Shuffle"))
            shuffleNb = (uint)PlayerPrefs.GetFloat("Shuffle");

        // ============================= Cube =============================  // 

        centralPos      = new GameObject();
        tabCube         = new List<GameObject>();
        movingCube      = new List<GameObject>();
        myRotatePoint   = new GameObject();

        if (size % 2 != 0)
            centralPos.transform.position = new Vector3(size / 2, size / 2, size / 2);
        else
            centralPos.transform.position = new Vector3(size / 2 - offset, size / 2 - offset, size / 2 - offset);

        centralPos.transform.rotation       = Quaternion.identity;
        myRotatePoint.transform.rotation    = Quaternion.identity;

        CreateCube();
        ShuffleCube();

        // ============================= Camera =============================  // 

        CameraSetup();
    }

    // Update is called once per frame
    void Update()
    {
        if (!shuffle)
            return;

        if (Input.GetButton("Fire1") && !animRunning)
            MovingFaceBehavior();
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

            RotateCube();
        }

        // completed ! text activate
        if (completed)
            youwin.enabled = true;

        Zoom();
    }

    #region CAMERA
    // ============================= Camera ========================= //

    void CameraSetup()
    {
        mainCamera = Camera.main;

        mainCamera.transform.position = new Vector3(size / 2, size / 2, -size + camOffset);
        mainCamera.transform.LookAt(centralPos.transform);

        savePosCam = mainCamera.transform.position;

        targetOrientationQuat = centralPos.transform.rotation;
        previousMousePos = Input.mousePosition;
    }

    void RotateCube()
    {
        Vector3 mouseMove = Input.mousePosition - previousMousePos;

        if (Input.GetButton("Fire2") && !animRunning)
        {
            Vector3 rotAxis         = new Vector3(mouseMove.y, -mouseMove.x, 0);
            float rotAngle          = rotAngularSpeed / maxRotAngleDuringOneFrame;

            Quaternion rotQ         = Quaternion.AngleAxis(rotAngle, rotAxis);

            targetOrientationQuat   = rotQ * targetOrientationQuat;
        }

        centralPos.transform.rotation   = targetOrientationQuat;
        previousMousePos                = Input.mousePosition;

        chooseRotatePlane   = false;
        leftHolding        = false;
    }

    void Zoom()
    {
        // zoom camera
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            if (savePosCam.z - limitZoom < mainCamera.transform.position.z)
                mainCamera.transform.position -= new Vector3(0, 0, zoomSpeed * Time.deltaTime);
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            if (savePosCam.z + limitZoom > mainCamera.transform.position.z)
                mainCamera.transform.position += new Vector3(0, 0, zoomSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region COMPLETION
    // ============================= Completion Check ========================= //

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

    #endregion

    #region SETUP_CUBE
    // ============================= Setup Cube ========================= //

    void CreateCube()
    {
        int i = 0;
        int j = 0;
        int k = 0;
        Vector3 pos = new Vector3(i, j, k);

        //construction of cube 
        for (; i < size; i++)
        {
            pos.x = i;
            for (j = 0; j < size; j++)
            {
                pos.y = j;
                for (k = 0; k < size; k++)
                {
                    pos.z = k;
                    if (!((pos.x < size - 1 && pos.x > 0) && (pos.y < size - 1 && pos.y > 0) && (pos.z < size - 1 && pos.z > 0)))
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
    }

    void ShuffleCube()
    {
 
        completed   = false;

        for (uint nb = 0; nb < shuffleNb; nb++)
        {
            int random = Random.Range(0, tabCube.Count);
            int rand = Random.Range(0, 2);//forward, up, right

            if (rand == 1)
                movingPlane = new Plane(Vector3.up, Vector3.up * Vector3.Dot(Vector3.up, tabCube[random].transform.position));
            else if (rand == 2)
                movingPlane = new Plane(Vector3.forward, Vector3.forward * Vector3.Dot(Vector3.forward, tabCube[random].transform.position));
            else
                movingPlane = new Plane(Vector3.right, Vector3.right * Vector3.Dot(Vector3.right, tabCube[random].transform.position));

            List<Quaternion> orientationQuaternion = new List<Quaternion>();
            orientationQuaternion.Add(Quaternion.AngleAxis(90.0f, movingPlane.normal));
            orientationQuaternion.Add(Quaternion.AngleAxis(-90.0f, movingPlane.normal));
            orientationQuaternion.Add(Quaternion.AngleAxis(180.0f, movingPlane.normal));

            int orientationRand = Random.Range(0, orientationQuaternion.Count);

            GetMovingFace();

            myRotatePoint.transform.rotation = orientationQuaternion[orientationRand];

            ClearParent();
        }
        shuffle     = true;
    }

    #endregion

    #region MOVE_FACE
    /*====================== Move Face Behavior ======================*/

    void MovingFaceBehavior()
    {
        if (!leftHolding)// if right mouse button just been pressed get 
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

    Quaternion SimpleRotate(Quaternion inOrientation, Vector3 rotateAxis, float rotateAngle, float slerpComponent = 1.0f)
    {
        Quaternion rotate = Quaternion.AngleAxis(rotateAngle, rotateAxis);
        rotate = Quaternion.SlerpUnclamped(Quaternion.identity, rotate, Time.deltaTime * slerpComponent);

        return inOrientation * rotate;
    }

    /* Get the face that user was pointing the mouse at when pressing left mouse button and create the ctrlPlane with it */
    void GetGrabedFace()
    {
        leftHolding = true;
        chooseRotatePlane = false;

        //Create a ray from the Mouse click position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            oldPoint    = hit.point;
            grabedFace  = hit.collider.gameObject;
            ctrlPlane   = new Plane(grabedFace.transform.forward, grabedFace.transform.forward * Vector3.Dot(grabedFace.transform.forward, grabedFace.transform.position));
        }
    }

    bool ChooseMovingFace()
    {
        //Create a ray from the Mouse click position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 grabDist = (hit.point - oldPoint);
            if (grabDist.sqrMagnitude >= (float)size * faceTurnSensibility)
            {

                oldPoint        = hit.point;
                float upRate    = Mathf.Abs(Vector3.Dot(grabedFace.transform.up, grabDist));
                float rightRate = Mathf.Abs(Vector3.Dot(grabedFace.transform.right, grabDist));


                if (rightRate >= upRate)//if the user's mouse is more on the right axis then it is the up plane that moves
                {
                    movingPlane = new Plane(grabedFace.transform.up, grabedFace.transform.up * Vector3.Dot(grabedFace.transform.up, grabedFace.transform.position));
                    return true;
                }//else, if the user's mouse is move on th up axis, then the movingPlane is the right Plane

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

    /* method that populate movingCube after the movingPlane been chose and place my Rotate Point, to rotate those cubes*/
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

        float moveRate                      = Vector3.Dot(Vector3.Cross(ctrlPlane.normal, movingPlane.normal), (newPoint - oldPoint));
        myRotatePoint.transform.rotation    = SimpleRotate(myRotatePoint.transform.rotation, movingPlane.normal, faceTurnSpeed * (1.0f / (float)size), moveRate);

        oldPoint        = newPoint;
    }

    void ClearParent()
    {
        foreach (GameObject cub in movingCube)
        {
            cub.transform.parent = centralPos.transform;
        }

        movingCube.Clear();

        haveRotatePointParent = false;

        if (!completed && shuffle)
            CheckCompleted();
    }

    #endregion

    #region ANIMATION
    /*====================== Animation Behavior ======================*/

    Quaternion GetProperOrientation()
    {
        /* list all possible rotation for the face */
        List<Quaternion> orientationQuaternion = new List<Quaternion>();
        orientationQuaternion.Add(Quaternion.identity);
        orientationQuaternion.Add(Quaternion.AngleAxis(90.0f, movingPlane.normal));
        orientationQuaternion.Add(Quaternion.AngleAxis(-90.0f, movingPlane.normal));
        orientationQuaternion.Add(Quaternion.AngleAxis(180.0f, movingPlane.normal));

        Quaternion returnQuat   = Quaternion.identity;
        float angle             = float.MaxValue;

        foreach (Quaternion quaternion in orientationQuaternion)
        {
            float newAngle = Quaternion.Angle(myRotatePoint.transform.rotation, quaternion);
            if (newAngle < angle)/* check for smallest angle between possible rotation and current one*/
            {
                angle       = newAngle;
                returnQuat  = quaternion;
            }
        }

        return returnQuat;
    }

    IEnumerator SetBackProperly()
    {
        Quaternion properOrientation    = GetProperOrientation();
        animRunning                     = true;

        /* Slerp until between wanted rot and current rot is below epsilon */
        while (Quaternion.Angle(myRotatePoint.transform.rotation, properOrientation) >= animFinishedEpsilon)
        {
            myRotatePoint.transform.rotation = Quaternion.Slerp(myRotatePoint.transform.rotation, properOrientation, animTurnSpeed * 1.0f / (float)size * Time.deltaTime);

            yield return null;
        }

        myRotatePoint.transform.rotation = properOrientation;//then force the rotation to avoid drifting by quaternion.

        animRunning = false;
        ClearParent();//the movement is considered finished only when anim is finished

        yield break;
    }

    #endregion
}
