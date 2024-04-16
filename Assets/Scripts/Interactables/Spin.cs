using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float SpinStrength = 0.1f;

    // Update is called once per frame
    void Update()
    {
        transform.rotation *= Quaternion.Euler(SpinStrength * Vector3.up);
    }
}