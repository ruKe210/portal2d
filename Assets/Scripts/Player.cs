using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    public float maxSpeed = 30f;
    public bool flag=false;


    public Portal RedPortal;
    public Portal GreenPortal;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }
   void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            rb.AddForce(Vector3.left * Time.deltaTime * 30, ForceMode2D.Impulse);
        }
        if (Input.GetKey(KeyCode.D))
        {
            rb.AddForce(Vector3.right * Time.deltaTime * 30, ForceMode2D.Impulse);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * Time.deltaTime * 3000, ForceMode2D.Impulse);
        }
    }
    
    public void MakePortalConnection()
    {
        this.GreenPortal.targetPortal = this.RedPortal;
        this.RedPortal.targetPortal = this.GreenPortal;
    }
}
