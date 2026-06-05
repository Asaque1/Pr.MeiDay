using UnityEngine;

[CreateAssetMenu(fileName = "CardDataSet", menuName = "Scriptable Objects/CardDataSet")]
public class CardDataSet : ScriptableObject
{
    public string name = string.Empty;
    public string[] hints = null;
    public Sprite[] hintsImg = null;
}
