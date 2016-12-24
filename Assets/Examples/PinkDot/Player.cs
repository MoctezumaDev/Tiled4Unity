using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour {

    [SerializeField]
    float _maxVelocity = 10.0f;

    private float _xMultiplier = 1.0f;
    private float _yMultiplier = 1.0f;

    Rigidbody2D _rigidbody;

	// Use this for initialization
	void Awake () {
        _rigidbody = GetComponent<Rigidbody2D>();
	    _xMultiplier = transform.localScale.x;
	    _yMultiplier = transform.localScale.y;
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 upVelocity = new Vector2(0, _maxVelocity);

            if (_rigidbody.velocity.y == 0)
            {
                _rigidbody.velocity += upVelocity * _yMultiplier;
            }
        }

        if(Input.GetKey(KeyCode.RightArrow))
        {
            Vector2 rightVelocity = new Vector2(_maxVelocity, 0);
            _rigidbody.velocity += rightVelocity * _xMultiplier;
        }

        if(Input.GetKey(KeyCode.LeftArrow))
        {
            Vector2 leftVelocity = new Vector2(-_maxVelocity, 0);
            _rigidbody.velocity += leftVelocity * _xMultiplier;
        }

	    if (!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
	    {
	        if (_rigidbody.velocity.y.Equals(0.0f))
	        {
	            _rigidbody.angularVelocity = 0;
	            _rigidbody.drag = 0.5f * _xMultiplier;
	        }
	        else
	        {
                _rigidbody.angularVelocity = 0;
                _rigidbody.drag = 0;
            }
        }

        ClampVelocity();
	}

    void ClampVelocity()
    {
        var velocity = _rigidbody.velocity;
        float horizontalVelocity = velocity.x;
        float verticalVelocity = velocity.y;

        if ( Mathf.Abs(horizontalVelocity) > _maxVelocity * _xMultiplier)
        {
            horizontalVelocity = Mathf.Sign(velocity.x) * _maxVelocity * _xMultiplier;
        }

        if ( verticalVelocity > _maxVelocity * _yMultiplier )
        {
            verticalVelocity = _maxVelocity * _yMultiplier;
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
