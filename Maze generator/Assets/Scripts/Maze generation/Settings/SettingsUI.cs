using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Settings settings;

    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private TMP_InputField stepTimeInput;

    [SerializeField] private TextMeshProUGUI currentMazeText;
    private int currentMazeIndex;
    private int mazeCount;

    void Start()
    {
        mazeCount = settings.algorithms.Length;
        stepTimeInput.text = settings.generationDelay.ToString();
        UpdateMazeNameText();
    }


    void UpdateSettings()
    {
        settings.mazeIndex = currentMazeIndex;
        settings.generationDelay = float.Parse(stepTimeInput.text);
        settings.height = int.Parse(heightInput.text);
        settings.width = int.Parse(widthInput.text);
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
