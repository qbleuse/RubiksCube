using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UImanager : MonoBehaviour
{
    [SerializeField] private GameObject myCube;
    [SerializeField] private Slider sliderShuffle = null;
    [SerializeField] private Slider sliderSize = null;
    [SerializeField] private Text sizeText = null;
    [SerializeField] private Text ShuffleText = null;

    private uint saveShuffleNb = 50;
    private int saveSize = 3;

    void Start()
    {

        if (PlayerPrefs.HasKey("Size"))
            saveSize = (int)PlayerPrefs.GetFloat("Size");
        if (PlayerPrefs.HasKey("Shuffle"))
            saveShuffleNb = (uint)PlayerPrefs.GetFloat("Shuffle");

        sliderShuffle.minValue = 0;
        sliderShuffle.maxValue = 100;
        sliderShuffle.wholeNumbers = true;
        sliderShuffle.value = saveShuffleNb;

        sliderSize.minValue = 0;
        sliderSize.maxValue = 10;
        sliderSize.wholeNumbers = true;
        sliderSize.value = saveSize;

    }

    public void ValueNbrShuffle(float val)
    {
        saveShuffleNb = (uint)val;
        ShuffleText.text = "Shuffle : " + saveShuffleNb;
    }

    public void ValueSize(float val)
    {
        saveSize = (int)val;
        sizeText.text = "Size : " + saveSize;
    }

    public void ReloadLevelOui()
    {
        PlayerPrefs.SetFloat("Size", saveSize);
        PlayerPrefs.SetFloat("Shuffle", saveShuffleNb);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
