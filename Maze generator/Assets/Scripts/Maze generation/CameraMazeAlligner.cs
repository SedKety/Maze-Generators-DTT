using UnityEngine;

public class CameraMazeAlligner : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Settings settings;

    private void Start()
    {
        settings.OnGenerationEvent += AllignCamera;

        if (Application.isMobilePlatform)
        {
            Screen.orientation = ScreenOrientation.PortraitUpsideDown;

            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }
    }
    private void AllignCamera(MazeGenerationAlgorithm MGA, int width, int height, float generationDelay)
    {
        var highestVal = width >= height ? width : height;
        highestVal += 20;

        cam.transform.position = new Vector3(0, highestVal, 0);
    }
}
