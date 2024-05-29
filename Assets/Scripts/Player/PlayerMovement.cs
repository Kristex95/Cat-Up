using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using Unity.VisualScripting;

public class PlayerMovement : MonoBehaviour
{
    #region Variables

    private Rigidbody rb;
    private PlayerInputActions playerInputActions;
    private Camera cam;
    private Vector2 inputVector;
    private Animator animator;

    [SerializeField]
    private float accelerationSpeed = 1f;
    [SerializeField]
    private float topSpeed = 10f;
    [SerializeField]
    private float rotationSpeed = 20f;
    [SerializeField]
    private Transform orientation;

    [SerializeField]
    [Range(0, 1f)]
    private float footDistanceToGround;

    [SerializeField]
    private LayerMask groundLayerMask;

    [Space(10)]

    [Header("States")]
    [SerializeField]
    private bool isClimbing = false;
    [SerializeField] private bool isLeftHandGrabbing = false;
    [SerializeField] private bool isRightHandGrabbing = false;


    [Space(10)]
    [Header("Hands")]
    [Header("Left")]

    [SerializeField] private Transform leftShoulder;

    [SerializeField] private Transform leftArm;

    [SerializeField] private Transform leftArmTarget;

    [SerializeField] private TwoBoneIKConstraint leftIKConstraint;

    [Space(5)]
    [Header("Right")]

    [SerializeField] private Transform rightShoulder;

    [SerializeField] private Transform rightArm;

    [SerializeField] private Transform rightArmTarget;

    [SerializeField] private TwoBoneIKConstraint rightIKConstraint;

    [Space(5)]
    [SerializeField]
    [Range(0, 20f)]
    private float IKTargetSpeed;

    [SerializeField]
    [Range(0, 5f)]
    private float shoulderRaycastDistance;

    //Others
    [Header("Others")]
    [SerializeField] private Transform chestPoint;
    [SerializeField] private GameObject grabPointPrefab;

    private GameObject leftGrabPoint;
    private GameObject rightGrabPoint;

    private Vector3 leftArmClimbPos;
    private Vector3 rightArmClimbPos;



    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cam = Camera.main;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();

        leftArmClimbPos = Vector3.zero;
        rightArmClimbPos = Vector3.zero;

        leftIKConstraint.weight = 0f;
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        
    }

    // Update is called once per frame
    void Update()
    {
        //get input 
        inputVector = playerInputActions.Player.Movement.ReadValue<Vector2>();

        HandleAnimation();
    }

    private void LateUpdate()
    {

        
    }

    

    private void FixedUpdate()
    {
        isClimbing = (isLeftHandGrabbing || isRightHandGrabbing) ? true : false;
        MoveHands();

        if (!isClimbing)
            Rotation();

        if (isClimbing)
        {
            rb.freezeRotation = false;
            Climbing();
        }
        else
        {
            rb.freezeRotation = true;
            Movement();
        }
    }


    


    #region Basic Movement
    private void Movement()
    {
        Vector3 forwardDirection = orientation.forward; // Get the forward direction of the player's orientation

        Vector3 forceDir = orientation.forward * inputVector.y + orientation.right * inputVector.x;
        rb.AddForce(forceDir * accelerationSpeed, ForceMode.Force);

        if(rb.velocity.magnitude > topSpeed)
        {
            rb.velocity = rb.velocity.normalized * topSpeed;
        }

        if(rb.velocity.magnitude > 2f) {
            animator.speed = rb.velocity.magnitude / topSpeed;
        }
        else
        {
            animator.speed = 1f;
        }

    }


    private void Rotation()
    {
        Vector3 viewDir = transform.position - new Vector3(cam.transform.position.x, transform.position.y, cam.transform.position.z); //getting direction camera faces
        Debug.DrawLine(transform.position, viewDir + transform.position, new UnityEngine.Color(255, 9, 0));

        orientation.forward = viewDir.normalized;
        Vector3 inputDir = orientation.forward * inputVector.y + orientation.right * inputVector.x; //which way to rotate player

        //if player pressing movement keys
        if (inputDir != Vector3.zero)
        {
            transform.forward = Vector3.Slerp(transform.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
        }
        //if player NOT pressing movement keys and moves the camera
        else if (Vector3.Angle(orientation.forward, transform.forward) > .5f)
        {
            transform.forward = Vector3.Slerp(transform.forward, orientation.forward, Time.deltaTime * rotationSpeed);
        }

        //transform.up = Vector3.Slerp(transform.up, Vector3.up, rotationSpeed * Time.deltaTime);
    }

    #endregion
    #region Animation Control
    private void HandleAnimation()
    {
        if(rb.velocity.magnitude > .5 && inputVector != Vector2.zero)
        {
            animator.SetBool("isRunning", true);
        }
        else if(rb.velocity.magnitude < .5)
        {
            animator.SetBool("isRunning", false);
        }
    }
    #endregion

    #region Climbing

    private void Climbing()
    {
        Vector3 forceDir = transform.up * inputVector.y * 2 + transform.right * inputVector.x;
        rb.AddForce(forceDir * accelerationSpeed, ForceMode.Force);
    }


    private void ClimbingRotation()
    {
        RaycastHit hit;
        Ray ray = new Ray(chestPoint.position, cam.transform.forward);
        if(Physics.Raycast(ray, out hit, shoulderRaycastDistance, groundLayerMask))
        {
            transform.forward = Vector3.Slerp(transform.forward, -hit.normal, Time.deltaTime * rotationSpeed);
        }


    }


    private Vector3 findNextArmPosition(Vector3 shoulderPos)
    {
        RaycastHit hit;
        Ray ray = new Ray(shoulderPos, cam.transform.forward);
        Physics.Raycast(ray, out hit, shoulderRaycastDistance, groundLayerMask);

        return hit.point;
    }

    private GameObject CreateGrabPoint(Vector3 grabPointPos, Transform IKTargetTransform)
    {
        GameObject grabPoint = Instantiate(grabPointPrefab, grabPointPos, IKTargetTransform.transform.rotation);
        SpringJoint springJoint = grabPoint.GetComponent<SpringJoint>();
        springJoint.connectedBody = rb;

        return grabPoint;
    }

    private void MoveHands()
    {
        //getting position for arms to grab
        if (!isLeftHandGrabbing) {
            if (isClimbing)
            {
                leftArmClimbPos = findNextArmPosition(leftShoulder.position + new Vector3(inputVector.x, inputVector.y, 0) / 2);
            }
            else
            {
                leftArmClimbPos = findNextArmPosition(leftShoulder.position);
            }
        }
        if (!isRightHandGrabbing) {
            if (isClimbing)
                rightArmClimbPos = findNextArmPosition(rightShoulder.position + new Vector3(inputVector.x, inputVector.y, 0) / 2);
            else
                rightArmClimbPos = findNextArmPosition(leftShoulder.position);
        }

        bool leftMouseInput = playerInputActions.Player.LeftGrab.ReadValue<float>() > 0 ? true : false;
        bool rightMouseInput = playerInputActions.Player.RightGrab.ReadValue<float>() > 0 ? true : false;


        if (leftMouseInput)
        {
            if (leftArmClimbPos != Vector3.zero && !isLeftHandGrabbing)
            {
                leftArmTarget.position = Vector3.Slerp(leftArmTarget.position, leftArmClimbPos, IKTargetSpeed * Time.fixedDeltaTime);
                leftIKConstraint.weight = 1f;

                float distanceToClimbPos = Vector3.Distance(leftArm.position, leftArmClimbPos);

                if (distanceToClimbPos < .4f)
                {
                    //TODO: grab
                    leftGrabPoint = CreateGrabPoint(leftArmClimbPos, leftArmTarget);
                    isLeftHandGrabbing = true;
                }
            }
            else if (isLeftHandGrabbing)
            {
                leftArmTarget.position = Vector3.Slerp(leftArmTarget.position, leftArmClimbPos, IKTargetSpeed * Time.fixedDeltaTime);
            }

        }
        else
        {
            isLeftHandGrabbing = false;
            leftIKConstraint.weight = 0f;
            Destroy(leftGrabPoint);
        }

        if (rightMouseInput)
        {
            if (rightArmClimbPos != Vector3.zero && !isRightHandGrabbing)
            {
                rightArmTarget.position = Vector3.Slerp(rightArmTarget.position, rightArmClimbPos, IKTargetSpeed * Time.fixedDeltaTime);
                rightIKConstraint.weight = 1f;

                float distanceToClimbPos = Vector3.Distance(rightArm.position, rightArmClimbPos);

                if (distanceToClimbPos < .4f)
                {
                    //TODO: grab
                    rightGrabPoint = CreateGrabPoint(rightArmClimbPos, rightArmTarget);
                    isRightHandGrabbing = true;
                }
            }
            else if (isRightHandGrabbing)
            {
                rightArmTarget.position = Vector3.Slerp(rightArmTarget.position, rightArmClimbPos, IKTargetSpeed * Time.fixedDeltaTime);
            }

        }
        else
        {
            isRightHandGrabbing = false;
            rightIKConstraint.weight = 0f;
            Destroy(rightGrabPoint);
        }
    }

    #endregion

    private void OnAnimatorIK(int layerIndex)
    {
        if (!animator)
            return;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("IKLeftFootWeight"));
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat("IKLeftFootWeight"));
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animator.GetFloat("IKRightFootWeight"));
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat("IKRightFootWeight"));

        //Left foot
        RaycastHit hit;
        Ray ray = new Ray(animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out hit, footDistanceToGround + 1f, groundLayerMask))
        {
            
            if(hit.transform.tag == "Walkable") //TODO: change to Layer
            {
                Vector3 footPosition = hit.point;
                footPosition.y += footDistanceToGround;
                animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
        }

        //Right foot
        ray = new Ray(animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out hit, footDistanceToGround + 1f, groundLayerMask))
        {

            if (hit.transform.tag == "Walkable") //TODO: change to Layer
            {
                Vector3 footPosition = hit.point;
                footPosition.y += footDistanceToGround;
                animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
        }
    }
}
