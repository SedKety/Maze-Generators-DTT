using UnityEngine;

[CreateAssetMenu(fileName = "Readme", menuName = "Scriptable Objects/Readme")]
public class BetterReadme : ScriptableObject
{
    [TextArea(10, 50)]
    public string content;
} 
