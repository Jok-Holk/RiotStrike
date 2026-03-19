
using UnityEngine;

public class Minimap : MonoBehaviour
{

    [SerializeField] private Transform Player;
    private void Update()
    {
        Vector3 newPos = Player.position;
        newPos.y = transform.position.y;
        tranform.position = newPos;
    }




}
