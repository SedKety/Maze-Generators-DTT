using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Settings settings;

    [SerializeField] private Slider widthSlider;
    [SerializeField] private Slider heightSlider;

    [SerializeField] private TextMeshProUGUI currentMazeText;
    private int currentMazeIndex;
    private int mazeCount;

    void Start()
    {
        mazeCount = settings.algorithms.Length;
    }


    void UpdateSettings()
    {
        settings.mazeIndex = currentMazeIndex;
        settings.height = (int)heightSlider.value;
        settings.width = (int)widthSlider.value;
    }

    public void OnGenerationButtonPushed()
    {
        UpdateSettings();
        settings.CallGenerationFunc();
    }

    public void OnNextMazeButtonPushed()
    {
        currentMazeIndex++;

        //To allow looping
        if(currentMazeIndex >= mazeCount)
        {
            currentMazeIndex = 0;
        }
        UpdateMazeNameText();
    }
    public void OnPreviousMazeButtonPushed()
    {
        currentMazeIndex--;

        //To allow looping
        if(currentMazeIndex < 0)
        {
            currentMazeIndex = mazeCount - 1;
        }
        UpdateMazeNameText();
    }
    private void UpdateMazeNameText()
    {
        currentMazeText.text = settings.algorithms[currentMazeIndex].name;
    }
}
