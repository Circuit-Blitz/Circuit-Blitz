using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarController : NetworkBehaviour
{
    
    [SerializeField] private float forwardAccel = 8f;
    [SerializeField] private float reverseAccel = 4f;
    [SerializeField] private float turnStrength = 100f;
    [SerializeField] private float gravityForce = 10f;
    [SerializeField] private float dragOnGround = 2f;
    [SerializeField] private float dragInAir = 0.1f;
    [SerializeField] private float maxWheelTurn = 25f;
    [SerializeField] private float groundRayLength = 0.5f;
    [SerializeField] private Rigidbody RB;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] private Transform leftFrontWheel, rightFrontWheel;
    [SerializeField] private Transform PlayerCameraPrefab;
    private float speedInput, turnInput;
    private bool grounded;
    private Vector3 currentEulerAngles;
    private Transform PlayerCamera;

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Get the Map
        MapManager GameMapManager = GameObject.Find("Map").GetComponent<MapManager>();

        // Set player position to random spawn pad
        Transform SpawnPad = GameMapManager.SpawnPads[Random.Range(0, GameMapManager.SpawnPads.Length)];
        RB.position = SpawnPad.position + Vector3.up * 5;

        // Instantiate a Camera to follow player
        PlayerCamera = Instantiate(PlayerCameraPrefab);
        PlayerCamera.GetComponent<CameraController>().Player = transform;

        currentEulerAngles = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        // If this car is not yours, you cant drive it
        if (!IsOwner) return;

        speedInput = 0f;

        if (Input.GetAxis("Vertical") > 0) {
            speedInput = Input.GetAxis("Vertical") * forwardAccel * 1000f;
        } else if (Input.GetAxis("Vertical") < 0) {
            speedInput = Input.GetAxis("Vertical") * reverseAccel * 1000f;
        }

        
        turnInput = Input.GetAxis("Horizontal");
        
        if (grounded) {
            currentEulerAngles += new Vector3(0f, turnInput * turnStrength * Time.deltaTime * Input.GetAxis("Vertical"), 0);
        }
        
        leftFrontWheel.localRotation = Quaternion.Euler(leftFrontWheel.localRotation.eulerAngles.x, (turnInput * maxWheelTurn) - 180, leftFrontWheel.localRotation.eulerAngles.x);
        rightFrontWheel.localRotation = Quaternion.Euler(rightFrontWheel.localRotation.eulerAngles.x, turnInput * maxWheelTurn, rightFrontWheel.localRotation.eulerAngles.x);
        
        transform.eulerAngles = currentEulerAngles;
        transform.position = RB.transform.position;
    }
    
    private void FixedUpdate() {
        // If this car is not yours, you cant drive it
        if (!IsOwner) return;

        grounded = false;

        RaycastHit hit;
        
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength, whatIsGround)) {
            grounded = true;
            
            currentEulerAngles = (Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation).eulerAngles;
        }
        
        if (grounded) {
            RB.drag = dragOnGround;

            if (Mathf.Abs(speedInput) > 0) {
                RB.AddForce(transform.forward * speedInput);
            }
        } else {
            RB.drag = dragInAir;

            RB.AddForce(Vector3.up * -gravityForce * 100f);
        }
    }
}
