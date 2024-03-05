using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public Rigidbody RB;
    public float forwardAccel = 8f, reverseAccel = 4f, maxSpeed = 50f, turnStrength = 100f;
    
    private float speedInput, turnInput;
    
    // Start is called before the first frame update
    void Start()
    {
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
        
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrength * Time.deltaTime, 0));
        
        transform.position = RB.transform.position;
    }
    
    private void FixedUpdate() {
        if (Mathf.Abs(speedInput) > 0) {
            RB.AddForce(transform.forward * speedInput);
        }
    }
}
