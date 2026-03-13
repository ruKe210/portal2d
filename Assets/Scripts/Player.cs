using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    public float maxSpeed = 30f;
    public float moveSpeed = 5f;
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
        bool movingHorizontally = false;

        if (Input.GetKey(KeyCode.A))
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
            movingHorizontally = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
            movingHorizontally = true;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector2(rb.velocity.x, 10f);
        }

        // 没按左右键时，快速减速水平速度
        if (!movingHorizontally)
        {
            rb.velocity = new Vector2(rb.velocity.x * 0.85f, rb.velocity.y);
        }
    }
    
    public void MakePortalConnection()
    {
        this.GreenPortal.targetPortal = this.RedPortal;
        this.RedPortal.targetPortal = this.GreenPortal;
    }
}
