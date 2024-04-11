using Unity.Netcode;
using UnityEngine;
using System.IO;
using System.IO.Ports;

public class CarController : NetworkBehaviour
{
    [SerializeField] private float forwardAccel = 8f;
    [SerializeField] private float reverseAccel = 4f;
    [SerializeField] private float turnStrength = 100f;
    [SerializeField] private float dragOnGround = 2f;
    [SerializeField] private float dragInAir = 0.1f;
    [SerializeField] private float maxWheelTurn = 25f;
    [SerializeField] private float groundRayLength = 0.5f;
    [SerializeField] private Rigidbody RB;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] private Transform leftFrontWheel, rightFrontWheel;
    [SerializeField] private Transform PlayerCameraPrefab;
    private float speedInput, turnInput, joyStickYValue;
    private bool grounded;
    private Transform PlayerCamera;

    // Serial port
    SerialPort serialPort;

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        // Only the local player should run this code
        if (!IsOwner) return;

        // Set the GameManager's player to this
        GameManager.Instance.SetLocalPlayer(transform);

        // Instantiate a Camera to follow player
        PlayerCamera = Instantiate(PlayerCameraPrefab);
        PlayerCamera.GetComponent<CameraController>().Player = transform;

        // Attempt to open the serial port
        try 
        {
            // Open the serial port
            serialPort = new SerialPort("COM5", 9600);
            serialPort.Open();
        }
        catch (IOException e)
        {
            Debug.LogError("Serial Port no open: " + e.Message);
        }
    }

    private void Update() {
        // Only the local player should run this code
        if (!IsOwner) return;

        // Can only move the car once the game is started
        if (!GameManager.Instance.DidGameStart()) return;

        // Sync transform with Rigidbody
        transform.rotation = RB.rotation;
        transform.position = RB.position;

        // Turn wheels based on horizontal input
        leftFrontWheel.localRotation = Quaternion.Euler(leftFrontWheel.localRotation.eulerAngles.x, (turnInput * maxWheelTurn) - 180, leftFrontWheel.localRotation.eulerAngles.x);
        rightFrontWheel.localRotation = Quaternion.Euler(rightFrontWheel.localRotation.eulerAngles.x, turnInput * maxWheelTurn, rightFrontWheel.localRotation.eulerAngles.x);

        // Determine the speed based on input
        // speedInput = 0f;
        // if (Input.GetAxis("Vertical") > 0) {
        //     speedInput = Input.GetAxis("Vertical") * forwardAccel * 1000f;
        // } else if (Input.GetAxis("Vertical") < 0) {
        //     speedInput = Input.GetAxis("Vertical") * reverseAccel * 1000f;
        // }

        // Determine the turn based on input
        // turnInput = Input.GetAxis("Horizontal");
        
        if (serialPort.IsOpen) {
            // Read data from the serial port
            string data = serialPort.ReadLine();
            string[] values = data.Split(',');

            joyStickYValue = float.Parse(values[1]);
            turnInput = -1 * float.Parse(values[0]); // Set the turn input
        } else {
            joyStickYValue = Input.GetAxis("Vertical");
            turnInput = Input.GetAxis("Horizontal");
        }

        // Determine the speed based on joystick input
        speedInput = 0f;
        if (joyStickYValue > 0) {
            speedInput = joyStickYValue * forwardAccel * 1000f;
        } else if (joyStickYValue < 0) {
            speedInput = joyStickYValue * reverseAccel * 1000f;
        }

    }

    private void FixedUpdate()
    {
        // Only the local player should run this code
        if (!IsOwner) return;

        // Detect if the car is grounded
        RaycastHit hit;
        grounded = false;
        if (Physics.Raycast(groundRayPoint.position, -RB.transform.up, out hit, groundRayLength, whatIsGround)) {
            grounded = true;
        }

        // If car is on ground, then player can turn and move the car forwards and backwards
        // Change drag based on whether the car is on the ground or not 
        if (grounded) {
            RB.drag = dragOnGround;

            // Turn the car based on horizontal input, do not turn if the car is not moving forwards or backwards
            float t = turnInput * turnStrength * joyStickYValue * Time.fixedDeltaTime;  // Rotation strength
            Quaternion q = Quaternion.AngleAxis(t, new Vector3(0, 1, 0));  // Quaternion to rotate around Y axis
            RB.MoveRotation(RB.rotation * q);

            // Move the car forwards or backwards based on vertical input
            RB.AddRelativeForce(Vector3.forward * speedInput * Time.fixedDeltaTime);
        } else {
            RB.drag = dragInAir;
        }
    }
}