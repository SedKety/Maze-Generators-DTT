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

        float generationDelay = 0;
        int height = 0;
        int width = 0;
        if (heightInput.text != string.Empty)
        {
            height = int.Parse(heightInput.text);

        }
        if (widthInput.text != string.Empty)
        {
            width = int.Parse(widthInput.text);
        }

        if (stepTimeInput.text != string.Empty)
        {
            generationDelay = float.Parse(stepTimeInput.text);
            if (generationDelay < 0) generationDelay = 0;
        }

        //Make sure there always are cells to validate
        height = height <= 0 ? 5 : height;
        width = width <= 0 ? 5 : width;

        settings.height = height;
        settings.width = width;
        settings.generationDelay = generationDelay;
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
        if (currentMazeIndex >= mazeCount)
        {
            currentMazeIndex = 0;
        }
        UpdateMazeNameText();
    }
    public void OnPreviousMazeButtonPushed()
    {
        currentMazeIndex--;

        //To allow looping
        if (currentMazeIndex < 0)
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