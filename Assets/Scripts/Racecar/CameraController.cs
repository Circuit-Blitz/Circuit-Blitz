using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform Player;

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 targetPosition = Player.transform.position - Player.transform.forward * 7.5f + Vector3.up * 5f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.2f);

        Quaternion targetAngle = Quaternion.LookRotation(Player.transform.position - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetAngle, 0.1f);
    }
}
