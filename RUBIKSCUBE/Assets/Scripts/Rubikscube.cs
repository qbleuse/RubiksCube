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
    [SerializeField] private float speed = 500;
    // Start is called before the first frame update
    void Start()
    {
        // ============================= Cube =============================  // 
        
        centralPos = new GameObject();
        tabCube = new List<GameObject>();

        centralPos.transform.position = new Vector3(size / 2, size / 2, size / 2);

        int i = 0;
        int j = 0;
        int k = 0;
        Vector3 pos = new Vector3(i, j, k);
        
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

        mainCamera.transform.position = new Vector3(size / 2, size/2, -size - 4);
        mainCamera.transform.LookAt(centralPos.transform);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            float verti = Input.GetAxis("Mouse X") * speed;
            float hori = Input.GetAxis("Mouse Y") * speed;

            centralPos.transform.Rotate(hori * Time.deltaTime ,-verti * Time.deltaTime, 0,Space.World); 

        }
    }
}
