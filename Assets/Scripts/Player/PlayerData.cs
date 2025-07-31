using UnityEngine;

[CreateAssetMenu(fileName = "New Player Data", menuName = "Data/PlayerData")]
public class PlayerData : ScriptableObject
{
    public float moveSpeed = 5f;
    public bool faceMouse = true;
}
