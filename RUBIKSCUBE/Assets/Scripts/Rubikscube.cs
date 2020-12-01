using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubikscube : MonoBehaviour
{
    [SerializeField] private GameObject cubeMulti = null;
    private List<GameObject> tabCube;
    [SerializeField] private int size = 3;
    // Start is called before the first frame update
    void Start()
    {
        tabCube = new List<GameObject>();

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

        // check face after this and hide some of this 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
