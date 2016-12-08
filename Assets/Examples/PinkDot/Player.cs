using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour {

    [SerializeField]
    float _maxVelocity = 10.0f;

    Rigidbody2D _rigidbody;

	// Use this for initialization
	void Awake () {
        _rigidbody = GetComponent<Rigidbody2D>();
    }
	
	// Update is called once per frame
	void FixedUpdate () {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 upVelocity = new Vector2(0, _maxVelocity);

            if (_rigidbody.velocity.y == 0)
            {
                _rigidbody.velocity += upVelocity;
            }
        }

        if(Input.GetKey(KeyCode.RightArrow))
        {
            Vector2 rightVelocity = new Vector2(_maxVelocity, 0);
            _rigidbody.velocity += rightVelocity;
        }

        if(Input.GetKey(KeyCode.LeftArrow))
        {
            Vector2 leftVelocity = new Vector2(-_maxVelocity, 0);
            _rigidbody.velocity += leftVelocity;
        }

        if(!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        {
            if (_rigidbody.velocity.y == 0)
            {
                _rigidbody.angularVelocity = 0;
                //_rigidbody.velocity = Vector2.zero;
                _rigidbody.drag = 0.5f;
            }
        }

        ClampVelocity();
	}

    void ClampVelocity()
    {
        var velocity = _rigidbody.velocity;
        float horizontalVelocity = velocity.x;
        float verticalVelocity = velocity.y;

        if ( Mathf.Abs(horizontalVelocity) > _maxVelocity)
        {
            horizontalVelocity = Mathf.Sign(velocity.x) * _maxVelocity;
        }

        if ( verticalVelocity > _maxVelocity )
        {
            verticalVelocity = _maxVelocity;
        }

        _rigidbody.velocity = new Vector2(horizontalVelocity, verticalVelocity);
    }

    void OnDrawGizmos()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        Vector2 p = transform.position;
        Vector2 v = _rigidbody.velocity;
        Ray r = new Ray(p, v);
        Vector2 q = r.GetPoint(v.magnitude);
        Gizmos.DrawLine(p, q);
    }


}
