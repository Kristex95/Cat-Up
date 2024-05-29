using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegsRigController : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [Range(0, 3f)][SerializeField] private float raycastDistance;
    [SerializeField] private LayerMask groundLayermask;
    [Range(0, 5f)][SerializeField] private float distanceToStep;
    [Range(0, 5f)][SerializeField] private float distanceBetweenLegs;
    [SerializeField] private float IKSpeed = 1f;

    private Leg movingLeg = Leg.left;

    [Header("Left")]
    [SerializeField] private Transform IKTargetL;
    [SerializeField] private Transform raycastStartL;
    private Vector3 finalPosL;
    private Quaternion finalRotL;


    [Header("Right")]
    [SerializeField] private Transform IKTargetR;
    [SerializeField] private Transform raycastStartR;
    private Vector3 finalPosR;
    private Quaternion finalRotR;

    [Space(10)]

    [Header("Audio")]
    [SerializeField] private List<AudioClip> stepSounds;
    private AudioSource stepSoundsSource;

    [Space(10)]

    [Header("Others")]
    private PlayerController playerController;
    private Rigidbody rb;
    private Vector2 inputVector;

    private Vector3 leftLegStartPos;
    private Vector3 rightLegStartPos;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        rb = playerController._rb;

        stepSoundsSource = GetComponent<AudioSource>();

        finalPosL = IKTargetL.position;
        finalPosR = IKTargetR.position;

        leftLegStartPos = IKTargetL.localPosition;
        rightLegStartPos = IKTargetR.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();
        if (playerController._isGrounded)
        {
            IKTargetL.position = Vector3.Slerp(IKTargetL.position, finalPosL, IKSpeed * Time.deltaTime);
            IKTargetR.position = Vector3.Slerp(IKTargetR.position, finalPosR, IKSpeed * Time.deltaTime);
        }
        /*IKTargetL.rotation = finalRotL;
        IKTargetR.rotation = finalRotR;*/
        
    }

    private void FixedUpdate()
    {

        if (!playerController._isGrounded)
        {
            /*IKTargetL.localPosition = Vector3.Slerp(IKTargetL.localPosition, leftLegStartPos, IKSpeed * Time.fixedDeltaTime);
            IKTargetR.localPosition = Vector3.Slerp(IKTargetR.localPosition, rightLegStartPos, IKSpeed * Time.fixedDeltaTime);*/
            IKTargetL.localPosition = leftLegStartPos;
            IKTargetR.localPosition = rightLegStartPos;
            return;
        }

        RaycastHit hit;


        float velocity = Mathf.Clamp(rb.velocity.magnitude, 0f, 1f);
        float velocityHighClamp = Mathf.Clamp(rb.velocity.magnitude, .5f, 2f);

        velocity = velocity > .5f ? velocity : 0f;
        //Left
        if (movingLeg == Leg.left)
        {
            Ray rayL = new Ray(raycastStartL.position + rb.velocity.normalized * velocity, Vector3.down);
            if (Physics.Raycast(rayL, out hit, raycastDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Distance(finalPosR, hit.point) > distanceBetweenLegs * velocityHighClamp)
                {
                    Debug.DrawLine(raycastStartL.position + playerBody.forward, hit.point);
                    finalPosL = hit.point;
                    finalRotL = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    ChangeLeg();
                }
            }
        }

        //Right
        if (movingLeg == Leg.right)
        {
            Ray rayR = new Ray(raycastStartR.position + rb.velocity.normalized * velocity, Vector3.down);
            if (Physics.Raycast(rayR, out hit, raycastDistance, groundLayermask, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Distance(finalPosL, hit.point) > distanceBetweenLegs * velocityHighClamp)
                {
                    Debug.DrawLine(raycastStartR.position + playerBody.forward, hit.point);
                    finalPosR = hit.point;
                    finalRotR = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    ChangeLeg();
                }
            }
           
        }
        
    }

    private void ChangeLeg()
    {
        PlayStepSound();
        switch (movingLeg)
        {
            case Leg.left:
                movingLeg = Leg.right;
                break;
            case Leg.right:
                movingLeg = Leg.left;
                break;
        }
    }

    private void GetInput()
    {
        inputVector = playerController._inputVector;
    }

    private enum Leg { 
        right,
        left
    }

    private void PlayStepSound()
    {
        int randVal = Random.Range(0, stepSounds.Count);
        stepSoundsSource.clip = stepSounds[randVal];
        stepSoundsSource.Play();
    }
}
