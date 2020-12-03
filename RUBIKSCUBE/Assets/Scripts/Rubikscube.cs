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

    [SerializeField] private float speed = 500;
    // Start is called before the first frame update
    void Start()
    {
        // ============================= Cube =============================  // 
        
        centralPos  = new GameObject();
        tabCube     = new List<GameObject>();
        movingCube  = new List<GameObject>();

        centralPos.transform.position = new Vector3(size / 2, size / 2, size / 2);

        int i = 0;
        int j = 0;
        int k = 0;
        Vector3 pos = new Vector3(i, j, k);

        rubiksSize = (((float)size) / 2.0f) - offset;
        center = new Vector3(rubiksSize, rubiksSize, rubiksSize);
        
        //construction of cube 
        for(; i < size ; i ++)
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
        foreach(GameObject tab in tabCube)
        {
            tab.transform.parent = centralPos.transform;
        }

        // check face after this and hide some of this 

        // ============================= Camera =============================  // 

        mainCamera = Camera.main;

        mainCamera.transform.position = new Vector3(size / 2, size/2, -size + camOffset);
        mainCamera.transform.LookAt(centralPos.transform);

    }

    private void OnDrawGizmos()
    {
        //if (grabedFace != null)
          //  Gizmos.DrawCube(grabedFace.transform.up * Vector3.Dot(grabedFace.transform.up, grabedFace.transform.position), grabedFace.transform.up * 10.0f + grabedFace.transform.right * 10.0f);
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

        Debug.Log(movingCube.Count);
    }

    void MoveFace()
    {
        //Create a ray from the Mouse click position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float dist = 0.0f;

        if (ctrlPlane.Raycast(ray, out dist))
        {
            Vector3 newPoint = ray.GetPoint(dist);

            float moveRate = Vector3.Dot(Vector3.Cross(ctrlPlane.normal,movingPlane.normal), (newPoint - oldPoint));

            Vector3 planeCenter = center + movingPlane.normal * movingPlane.distance;

            foreach (GameObject cube in movingCube)
            {
                Quaternion rotate       = Quaternion.AngleAxis(moveRate * faceTurnSpeed, movingPlane.normal);
                rotate                  = Quaternion.Slerp(Quaternion.identity, rotate, Time.deltaTime);
                cube.transform.rotation = cube.transform.rotation * rotate;
                cube.transform.position = rotate * (cube.transform.position - planeCenter) + planeCenter;
            }

            oldPoint = newPoint;
        }
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
            chooseRotatePlane = false;
            rightHolding = false;
        }
    }
}
