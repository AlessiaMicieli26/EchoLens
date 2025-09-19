
using UnityEngine;


public class Copy_position : MonoBehaviour
{

    [SerializeField] public Transform target; // l'oggetto da cui copiare la posizione

    private void OnEnable()
    {
        transform.position = target.position;

    }
}

