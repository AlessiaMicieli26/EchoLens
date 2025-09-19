
using UnityEngine;


public class Credits : MonoBehaviour
{

    [SerializeField] public Transform target; // l'oggetto da cui copiare la posizione

    private void OnEnable()
    {
        transform.position = target.position;

    }
}
