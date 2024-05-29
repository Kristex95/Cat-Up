using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    [SerializeField] private Transform playerBody;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Rigidbody torsoRB;
    [SerializeField] private CapsuleCollider spineCollider;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private ConfigurableJoint cj;
    [SerializeField] private Transform orientation;
    private PlayerInputActions playerInputActions;
    private Vector2 inputVector;
    private bool jumpInput = false;
    private Camera cam;


    [Header("Settings")]
    [SerializeField] private float speed;
    [SerializeField] private float airialSpeed;
    [SerializeField] private float climbingSpeed;
    [SerializeField] private float topSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float rotationSpeed;

    [Space(5)]
    [SerializeField][Range(1, 5f)] private float groundRaycastDistance = 1f;


    [Space(10)]
    [Header("States")]
    [SerializeField] private bool isLeftHandGrabbing = false;
    [SerializeField] private bool isRightHandGrabbing = false;
    [SerializeField] public bool isClimbing { private set; get; } = false;
    [SerializeField] private bool isGrounded = false;
    private bool isGroundedPrevFrame = false;




    [Header("Collider")]
    [SerializeField] private Vector3 collStartCenter;
    [SerializeField] private Vector3 collClimbCenter;
    [Space(5)]
    [SerializeField] private float collStartHeight;
    [SerializeField] private float collClimbHeight;
    [Space(5)]
    [SerializeField][Range(1, 20f)] private float collGrowSpeed = 1f;




    [Space(10)]
    [Header("Hands")]
    [Header("Left")]

    [SerializeField] private Transform leftShoulder;

    [SerializeField] private Transform leftArm;

    [SerializeField] private Transform leftArmTarget;

    [SerializeField] private TwoBoneIKConstraint leftIKConstraint;

    [SerializeField] private GameObject leftVisualTargetTransform;
    [SerializeField] private SpriteRenderer leftVisualTargetSprite;

    [Space(5)]
    [Header("Right")]

    [SerializeField] private Transform rightShoulder;

    [SerializeField] private Transform rightArm;

    [SerializeField] private Transform rightArmTarget;

    [SerializeField] private TwoBoneIKConstraint rightIKConstraint;

    [SerializeField] private GameObject rightVisualTargetTransform;
    [SerializeField] private SpriteRenderer rightVisualTargetSprite;

    [Space(5)]
    [SerializeField]
    [Range(0, 20f)]
    private float IKTargetSpeed;

    [SerializeField]
    [Range(0, 5f)]
    private float shoulderRaycastDistance;

    [Space(10)]

    //Others
    [Header("Others")]
    [SerializeField] private Transform chestPoint;
    [SerializeField] private GameObject grabPointPrefab;

    private GameObject leftGrabPoint;
    private GameObject rightGrabPoint;

    private Vector3 leftArmClimbPos;
    private Vector3 rightArmClimbPos;

    private Vector3 leftArmStartPos;
    private Vector3 rightArmStartPos;

    private float cjAngularXDriveSpring;
    private float cjAngularYZDriveSpring;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();

        cam = Camera.main;

        if(_rb == null)
        {
            _rb = rb;
        }


    }

    private void OnEnable()
    {
        playerInputActions.Player.Jump.performed += OnJumpPerformed;
        playerInputActions.Player.Enable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        leftArmStartPos = leftArmTarget.localPosition;
        rightArmStartPos = rightArmTarget.localPosition;

        cjAngularXDriveSpring = cj.angularXDrive.positionSpring;
        cjAngularYZDriveSpring = cj.angularYZDrive.positionSpring;
    }

    void Update()
    {
        GetInput();
        

        if (!isClimbing)
            Rotation();

        isGrounded = IsGrounded();

        /*if (isGrounded)
        {

            if (!isGroundedPrevFrame)
            {
                //fix spine position spring
                JointDrive jdX = cj.angularXDrive;
                jdX.positionSpring = cjAngularXDriveSpring;
                cj.angularXDrive = jdX;

                JointDrive jdYZ = cj.angularYZDrive;
                jdYZ.positionSpring = cjAngularXDriveSpring;
                cj.angularYZDrive = jdYZ;
            }



            isGroundedPrevFrame = true;
        }
        else
        {
            if (isGroundedPrevFrame)
            {
                //fix spine position spring
                JointDrive jdX = cj.angularXDrive;
                jdX.positionSpring = 0f;
                cj.angularXDrive = jdX;

                JointDrive jdYZ = cj.angularYZDrive;
                jdYZ.positionSpring = 0f;
                cj.angularYZDrive = jdYZ;
            }


            isGroundedPrevFrame = false;
        }*/
    }

    private void FixedUpdate()
    {
        isClimbing = (isLeftHandGrabbing || isRightHandGrabbing) ? true : false;
        MoveHands();

        

        if (isClimbing)
        {
            Climbing();
        }
        else
        {
            Movement();
            GrowCollider();

        }
    }

    private void LateUpdate()
    {
        
    }

    private void GetInput()
    {
        inputVector = playerInputActions.Player.Movement.ReadValue<Vector2>();
    }

    #region Basic Movement

    private void Movement()
    {
        Vector3 forwardDirection = orientation.forward; // Get the forward direction of the player's orientation

        Vector3 forceDir = orientation.forward * inputVector.y + orientation.right * inputVector.x;
        if (isGrounded)
        {
            rb.AddForce(forceDir * speed, ForceMode.Force);
            torsoRB.AddForce(forceDir * airialSpeed, ForceMode.Force);
        }
        else
        {
            rb.AddForce(forceDir * airialSpeed, ForceMode.Force);
        }


        if (rb.velocity.magnitude > topSpeed && isGrounded)
        {
            rb.velocity = rb.velocity.normalized * topSpeed;
        }
    }

    private void Rotation()
    {
        Vector3 viewDir = playerBody.position - new Vector3(cam.transform.position.x, playerBody.position.y, cam.transform.position.z); //getting direction camera faces
        Debug.DrawLine(orientation.position, viewDir + orientation.position, new UnityEngine.Color(255, 9, 0));

        orientation.forward = viewDir.normalized;
        Vector3 inputDir = orientation.forward * inputVector.y + orientation.right * inputVector.x; //which way to rotate player
        inputDir = inputDir.normalized;

        //if player pressing movement keys
        if (inputDir != Vector3.zero)
        {
            // Create a quaternion from the input direction
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);
            // Adjust the y-axis rotation to match the input direction
            cj.targetRotation = Quaternion.Euler(new Vector3(0f, Mathf.Abs(targetRotation.eulerAngles.y - 360f), 0f));
        }
        //if player NOT pressing movement keys and moves the camera
        else if (Vector3.Angle(orientation.forward, playerBody.forward) > .5f)
        {
            cj.targetRotation = Quaternion.Euler(0f, Mathf.Abs(orientation.rotation.eulerAngles.y - 360f), 0f);
        }

    }

    private bool IsGrounded()
    {
        RaycastHit hit;
        Ray ray = new Ray(playerBody.position, Vector3.down);
        if(Physics.Raycast(ray, out hit, groundRaycastDistance, groundLayerMask)) { 
            return true;
        }
        return false;
    }

    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!isClimbing && isGrounded && context.performed)
        {
            Jump();
        }
    }


    #endregion

    #region Climbing
    private void Climbing()
    {
        Vector3 forceDir = playerBody.up * inputVector.y + playerBody.right * inputVector.x;
        rb.AddForce(forceDir * climbingSpeed, ForceMode.Force);
        torsoRB.AddForce(playerBody.forward * climbingSpeed/2);

        spineCollider.center = Vector3.Lerp(spineCollider.center, collClimbCenter, Time.fixedDeltaTime * collGrowSpeed);
        spineCollider.height = Mathf.Lerp(spineCollider.height, collClimbHeight, Time.fixedDeltaTime * collGrowSpeed);
    }


    private void ClimbingRotation()
    {
        RaycastHit hit;
        Ray ray = new Ray(chestPoint.position, cam.transform.forward);
        if (Physics.Raycast(ray, out hit, shoulderRaycastDistance, groundLayerMask))
        {
            transform.forward = Vector3.Slerp(transform.forward, -hit.normal, Time.deltaTime * rotationSpeed);
        }


    }


    private Vector3 findNextArmPosition(Vector3 shoulderPos, out Vector3 normal)
    {
        RaycastHit hit;
        Ray ray = new Ray(shoulderPos, cam.transform.forward);
        Physics.Raycast(ray, out hit, shoulderRaycastDistance, groundLayerMask);
        Debug.DrawLine(shoulderPos, shoulderPos+cam.transform.forward*shoulderRaycastDistance);
        normal = hit.normal;
        return hit.point;
    }

    private GameObject CreateGrabPoint(Vector3 grabPointPos, Transform IKTargetTransform)
    {
        GameObject grabPoint = Instantiate(grabPointPrefab, grabPointPos, IKTargetTransform.transform.rotation);
        SpringJoint springJoint = grabPoint.GetComponent<SpringJoint>();
        springJoint.connectedBody = rb;

        return grabPoint;
    }


    private Vector3 leftHandHitNormal = Vector3.zero;
    private Vector3 rightHandHitNormal = Vector3.zero;
    private void MoveHands()
    {
        //getting position for arms to grab
        if (!isLeftHandGrabbing)
        {
            if (isClimbing)
            {
                leftArmClimbPos = findNextArmPosition(leftShoulder.position + (leftShoulder.transform.up * inputVector.y + leftShoulder.transform.right * inputVector.x) / 2, out leftHandHitNormal);
            }
            else
            {
                leftArmClimbPos = findNextArmPosition(leftShoulder.position, out leftHandHitNormal);
            }
        }
        if (!isRightHandGrabbing)
        {
            if (isClimbing)
                rightArmClimbPos = findNextArmPosition(rightShoulder.position + (rightShoulder.transform.up * inputVector.y + rightShoulder.transform.right * inputVector.x) / 2, out rightHandHitNormal);
            else
                rightArmClimbPos = findNextArmPosition(leftShoulder.position, out rightHandHitNormal);
        }

        bool leftMouseInput = playerInputActions.Player.LeftGrab.ReadValue<float>() > 0 ? true : false;
        bool rightMouseInput = playerInputActions.Player.RightGrab.ReadValue<float>() > 0 ? true : false;

        if(leftArmClimbPos != Vector3.zero) { 
            leftArmTarget.position = Vector3.Slerp(leftArmTarget.position, leftArmClimbPos, IKTargetSpeed * Time.fixedDeltaTime);
            leftIKConstraint.weight = 1f;

            leftVisualTargetTransform.SetActive(true);
            leftVisualTargetTransform.transform.position = leftArmClimbPos;
            leftVisualTargetTransform.transform.rotation = Quaternion.LookRotation(leftHandHitNormal);
        }
        else
        {
            leftVisualTargetTransform.SetActive(false);
            leftIKConstraint.weight = 0f;
        }


        if(rightArmClimbPos != Vector3.zero)
        {
            rightArmTarget.position = Vector3.Slerp(rightArmTarget.position, rightArmClimbPos, IKTargetSpeed * Time.fixedDeltaTime);
            rightIKConstraint.weight = 1f;

            rightVisualTargetTransform.SetActive(true);
            rightVisualTargetTransform.transform.position = rightArmClimbPos;
            rightVisualTargetTransform.transform.rotation = Quaternion.LookRotation(rightHandHitNormal);
        }
        else
        {
            rightVisualTargetTransform.SetActive(false);
            rightIKConstraint.weight = 0f;
        }


        if (leftMouseInput)
        {
            if (!isLeftHandGrabbing)
            {

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
            
            Destroy(leftGrabPoint);
        }

        if (rightMouseInput)
        {
            if (!isRightHandGrabbing)
            {

                float distanceToClimbPos = Vector3.Distance(rightArm.position, rightArmClimbPos);

                if (distanceToClimbPos < .4f)
                {
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
            
            Destroy(rightGrabPoint);
        }
    }
    #endregion

    #region Others
    private void GrowCollider()
    {
        spineCollider.center = Vector3.Lerp(spineCollider.center, collStartCenter, Time.fixedDeltaTime * collGrowSpeed);
        spineCollider.height = Mathf.Lerp(spineCollider.height, collStartHeight, Time.fixedDeltaTime * collGrowSpeed);
    }



    public Rigidbody _rb
    {
        get { return rb; }
        private set { rb = value; }
    }

    public Vector2 _inputVector
    {
        get { return inputVector; }
        private set { inputVector = value; }
    }

    public bool _isGrounded
    {
        get { return isGrounded; }
        private set { isGrounded = value; }
    }

    public float _topSpeed
    {
        get { return topSpeed; } 
        private set { topSpeed = value; }
    }
    #endregion
}
