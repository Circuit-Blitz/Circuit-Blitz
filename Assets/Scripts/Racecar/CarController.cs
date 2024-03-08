using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public Rigidbody RB;
    public float forwardAccel = 8f, reverseAccel = 4f, maxSpeed = 50f, turnStrength = 100f, gravityForce = 10f, dragOnGround = 2f, dragInAir = 0.1f;
    
    private float speedInput, turnInput;
    
    private bool grounded;
    
    public LayerMask whatIsGround;
    public float groundRayLength = 0.5f;
    public Transform groundRayPoint;
    
    private Vector3 currentEulerAngles;

    // Start is called before the first frame update
    void Start()
    {
        currentEulerAngles = transform.rotation.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        
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
        
        transform.eulerAngles = currentEulerAngles;
        transform.position = RB.transform.position;
    }
    
    private void FixedUpdate() {
        
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
