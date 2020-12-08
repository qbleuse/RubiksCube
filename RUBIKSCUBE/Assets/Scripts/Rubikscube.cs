using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubikscube : MonoBehaviour
{
    private Camera mainCamera;

    [SerializeField] private GameObject cubeMulti = null;
    private List<GameObject> tabCube;
    private List<GameObject> rotatePoint;
    private GameObject centralPos;
    [SerializeField] private int size = 3;
    [SerializeField] private float offset = 0.5f;
    [SerializeField] private float detectEpsilon = 0.1f;
    [SerializeField] private float animFinishedEpsilon = 0.1f;
    [SerializeField] private float camOffset = -4.0f;
    [SerializeField, Range(0.0f, 10.0f)] private float faceTurnSensibility = 1.0f;

    //
    [SerializeField] private float faceTurnSpeed = 1.5f;

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
        rotatePoint = new List<GameObject>();
        myRotatePoint= new GameObject();

        centralPos.transform.position = new Vector3(size / 2, size / 2, size / 2);

        int i = 0;
        int j = 0;
        int k = 0;
        Vector3 pos = new Vector3(i, j, k);

        rubiksSize = (((float)size) / 2.0f) - offset;
        center = new Vector3(rubiksSize, rubiksSize, rubiksSize);
        
        rotatePoint.Add(Instantiate(centralPos, new Vector3(center.x + center.x, center.y, center.z), Quaternion.identity));
        rotatePoint[rotatePoint.Count - 1].name = "YellowRotatePoint";
        rotatePoint.Add(Instantiate(centralPos, new Vector3(center.x - center.x, center.y, center.z), Quaternion.identity));
        rotatePoint[rotatePoint.Count - 1].name = "WhiteRotatePoint";
        rotatePoint.Add(Instantiate(centralPos, new Vector3(center.x , center.y + center.y, center.z), Quaternion.identity));
        rotatePoint[rotatePoint.Count - 1].name = "OrangeRotatePoint";
        rotatePoint.Add(Instantiate(centralPos, new Vector3(center.x, center.y - center.y, center.z), Quaternion.identity));
        rotatePoint[rotatePoint.Count - 1].name = "RedRotatePoint";
        rotatePoint.Add(Instantiate(centralPos, new Vector3(center.x, center.y , center.z + center.y), Quaternion.identity));
        rotatePoint[rotatePoint.Count - 1].name = "GreenRotatePoint";
        rotatePoint.Add(Instantiate(centralPos, new Vector3(center.x, center.y, center.z - center.y), Quaternion.identity));
        rotatePoint[rotatePoint.Count - 1].name = "BlueRotatePoint";


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

        foreach (GameObject tab in rotatePoint)
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

    private void OnDrawGizmos()
    {
        if (myRotatePoint)
            Gizmos.DrawWireSphere(myRotatePoint.transform.position, 0.5f);
    }

    IEnumerator SetBackProperly(GameObject gameObject, Quaternion border1, Quaternion border2)
    {
        float angleWithBorder1 = Quaternion.Angle(gameObject.transform.rotation,border1);
        float angleWithBorder2 = Quaternion.Angle(gameObject.transform.rotation, border2);
        animRunning = true;
        if (angleWithBorder1 > angleWithBorder2)
        {
            while (Quaternion.Angle(gameObject.transform.rotation, border2) >= animFinishedEpsilon)
            {
                gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, border2, Mathf.Deg2Rad * faceTurnSpeed * 1.0f/(float)size * Time.deltaTime);

                yield return null;
            }

            animRunning = false;
            yield break;
        }

        while (Quaternion.Angle(gameObject.transform.rotation, border1) >= animFinishedEpsilon)
        {
            gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, border1, Mathf.Deg2Rad * faceTurnSpeed * 1.0f / (float)size * Time.deltaTime);

            yield return null;
        }
        animRunning = false;
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
        foreach (GameObject gameObject in tabCube)
        {
            float distToPlane = movingPlane.GetDistanceToPoint(gameObject.transform.position);
            if (distToPlane <= detectEpsilon && distToPlane >= -detectEpsilon)
            {
                movingCube.Add(gameObject);
            }
        }

        //set parent of cube and compare position to do it 
        //foreach (GameObject rotPt in rotatePoint)
        
            GameObject rotPoint = new GameObject();
            foreach (GameObject cub in movingCube)
            {
                rotPoint.transform.position += cub.transform.position;
            }
            rotPoint.transform.position /= movingCube.Count;
            myRotatePoint.transform.position = rotPoint.transform.position;
            myRotatePoint.transform.rotation = Quaternion.identity;
        

        foreach (GameObject cub in movingCube)
        {
            cub.transform.parent = myRotatePoint.transform;
        }

        haveRotatePointParent = true;
        oldPoint = Input.mousePosition;
    }

    void MoveFace()
    {
        Vector3 newPoint = Input.mousePosition;

        float moveRate = Vector3.Dot(Vector3.Cross(ctrlPlane.normal,movingPlane.normal), (newPoint - oldPoint));

        Vector3 planeCenter = center + movingPlane.normal * movingPlane.distance;

        //foreach (GameObject cube in movingCube)
        //{
            Quaternion rotate       = Quaternion.AngleAxis(Mathf.Deg2Rad * faceTurnSpeed * 1.0f/(float)size, movingPlane.normal);
            rotate                  = Quaternion.SlerpUnclamped(Quaternion.identity, rotate, Time.deltaTime * moveRate);

        //cube.transform.localRotation = cube.transform.localRotation * rotate;
        //Debug.Log("current : " + cube.transform.rotation + "    parent : " + cube.transform.parent.rotation); 
        myRotatePoint.transform.rotation = myRotatePoint.transform.rotation * rotate;
        //}

        oldPoint = newPoint;
    }

    void ClearParent()
    {
        foreach (GameObject cub in movingCube)
        {
            cub.transform.parent = centralPos.transform;
        }

        haveRotatePointParent = false;
    }


    // Update is called once per frame
    void Update()
    {

        //Detect when there is a mouse click
        if (Input.GetButton("Fire2"))
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
            if(haveRotatePointParent)
                ClearParent();

            if (Input.GetButton("Fire1"))
            {
                float verti = Input.GetAxis("Mouse X") * speed;
                float hori = Input.GetAxis("Mouse Y") * speed;

                centralPos.transform.Rotate(hori * Time.deltaTime, -verti * Time.deltaTime, 0, Space.World);
            }

            if (movingCube.Count >= 1)
            { 
                movingCube.Clear();
            }
            chooseRotatePlane   = false;
            rightHolding        = false;
        }
    }
}
