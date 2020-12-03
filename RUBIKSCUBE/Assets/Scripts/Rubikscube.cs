using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubikscube : MonoBehaviour
{
    [SerializeField] private GameObject cubeMulti = null;
    private List<GameObject> tabCube;
    [SerializeField] private int size = 3;
    [SerializeField] private float offset = 0.5f;
    [SerializeField] private float detectEpsilon = 0.1f;

    //center of the rubiks cube
    private Vector3 center = Vector3.zero;

    //if the user's is holding the left button of the mouse
    private bool leftHolding = false;

    //Size of the rubiks cube from its center
    private float rubiksSize = 0.0f;

    //the list of cube moving when user's is holding
    private List<GameObject> movingCube;

    // Start is called before the first frame update
    void Start()
    {
        tabCube = new List<GameObject>();
        movingCube = new List<GameObject>();

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

        // check face after this and hide some of this 
    }

    private void OnDrawGizmos()
    {
    }

    void GetMovingCube()
    {
        //Create a ray from the Mouse click position
        Ray         ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit  hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log(hit.normal);
            Debug.Log(center);
            Plane plane = new Plane(-hit.normal, center + hit.normal * rubiksSize);
           

            foreach (GameObject gameObject in tabCube)
            {
                if (plane.GetDistanceToPoint(gameObject.transform.position) <= detectEpsilon)
                {
                    Debug.Log(plane.GetDistanceToPoint(gameObject.transform.position) + "   " + gameObject.name);
                    movingCube.Add(gameObject);
                }
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        //Detect when there is a mouse click
        if (Input.GetMouseButton(0))
        {
            if (!leftHolding)
            {
                leftHolding = true;
                GetMovingCube();
            }
            else
            {
                
            }
        }
        else
        {
            leftHolding = false;
        }
    }
}
