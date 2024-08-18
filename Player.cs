using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * The Player script is in charge of making any changes to the main player entity (Muna). If Muna is damaged, hit by a grenade, trys to loot, or consumes an item, then any animation triggers are sent here
 * and reflected onto her animation state. The Player's link to the Human script allows us to block certain behaviours of overhead mechanics such as menu item manipulation while reloading,
 * hanging on a cliff, or in a damaged state. Other environmental values are found here, such as slope angles that prevent the player from running up steep inclines or perpendicular walls that can be climbed.
 * Rotational changes are also made here based on the player's animation: by default the player faces the direction of camera-relative input, but other animations like slashing, throwing, or firing, require
 * the player face the same direction of the camera or the input. Certain animations can also flip the animation to oppose input, or slow rotational speed. This script also offers player bone transform positions
 * for instatiating throwables, bullets, or consumables. Links to HealthController and StaminaController have also been made to limit player movement when all stamina has been exhasuted or the player dies.
 */

public enum RotationMode{ oppositeVelocity = 1, forwardVelocity = 2, snapInput = 3, slopeForward = 4, ball = 5, snapPause = 6, attackRotation = 7, towardTarget = 8, towardWall = 9, towardInput = -1, lockRotation = -2}
[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Animator))]
public class Player : MonoBehaviour
{
    //core components for movement and animation
    private Rigidbody m_rb;
    private CapsuleCollider m_cc;
    private Animator m_anim;

    //TEMP public float for testing
    public float rotationSpeed;
    public float movementSpeed;
    //TEMP used to verify floor angle
    [SerializeField]
    private float contactNormal;
    [SerializeField]
    private Vector3 slopeDirection;

    private Vector3 m_lateralVelocity;
    private int m_rotMode;

    private Quaternion m_targetForward;

    [SerializeField]
    public bool isGrounded;

    [SerializeField]
    private bool onSlope;
    
    private ContactPoint[] collisionPoints;

    //TEMP: Debug purposes, checking what the polayer is touching on OnCollisionStay
    [SerializeField]
    private List<string> collisionNames;

    public float slopeLimit;

    private Ray groundRay;

    private Vector3 targetDirection;
    private Vector3 movementRelativeDirection;
    private Quaternion targetLateral;

    [SerializeField]
    private int gravityMultiplier;

    private float prevPressure, deltaPressure;

    [SerializeField]
    private Item currentThrowable;

    [SerializeField]
    private GameObject knife;

    private GameObject heldPrefab;

    private ExpressionController expressionContoller;
    private MeleeController meleeContoller;
    private GunController gunController;
    
    public delegate void EnterAnyState();
    public EnterAnyState enterAnyState;

    public delegate void OnActivity();
    public OnActivity onActivity;

    public delegate Item OnThrow();
    public OnThrow onThrow;

    private StaminaController staminaController;
    private HydrationController hydrationController;

    // Start is called before the first frame update
    void Start()
    {
        m_rb = GetComponent<Rigidbody>();
        m_cc = GetComponent<CapsuleCollider>();
        m_anim = GetComponent<Animator>();

        expressionContoller = GetComponent<ExpressionController>();
        meleeContoller = GetComponent<MeleeController>();
        staminaController = GetComponent<StaminaController>();
        hydrationController = GetComponent<HydrationController>();
        gunController = GetComponent<GunController>();

        if (this.TryGetComponent<HealthController>(out HealthController hc)) {
            hc.onDamage += Damage;
            hc.die += Die;
        }

        if (staminaController) {
            staminaController.onExhaustion += Exhaust;
            staminaController.onRecover += Recover;
        }

        //ReloadWeapon();
        //Reload(true);

        InitializePlayerStats();

        collisionNames = new List<string>();
    }

    private void Update()
    {
        //SOMEWHAT TEMP, just updating the ground ray used to check distance from ground (in-tandem with isGrounded - when this raycast hits, still grounded)
        groundRay.direction = Vector3.down;
        groundRay.origin = transform.position + (Vector3.up);

        DrawRay();
    }

    //Set physics-based values to animator every physics-based update
    private void FixedUpdate()
    {
        m_lateralVelocity = new Vector3(m_rb.velocity.x, 0.0f, m_rb.velocity.z);


        #region AnimValues
        m_anim.SetFloat("velocity", m_lateralVelocity.magnitude / 6.5f); //6.5f hard-coded, decided by run animation
        m_anim.SetFloat("vertical", m_rb.velocity.y);
        m_anim.SetFloat("slopeAngle", contactNormal);
        m_anim.SetFloat("inputSlope", FloorInputAngle());
        m_anim.SetBool("grounded", isGrounded);

        if (staminaController)
        {
            m_anim.SetFloat("stamina", GetStamina());
            m_anim.SetBool("exhaust", staminaController.exhausted);
        }
        else {
            m_anim.SetFloat("stamina", 1.0f); //IF NO STAMINA CONTROLLER, u get infinite stamina basically
        }
            

        //Slope should factor in if you are running into the hill
        //i guess if u can keep ur velocity above a certain threshold, than it's impossible to be sloped while velocity>threhsold
        //as long as there is a confirmed way to reduce that speed while on a slope and the player is trying to 
        m_anim.SetBool("slope", onSlope && isGrounded);
        #endregion AnimValues

        //NOTE: Semi-hard coded solution to rootmotion and grounded
        //Automatically makes root motion OFF when NOT grounded
        //m_anim.applyRootMotion = isGrounded;

        StaminaDepletion();
        HydrationDepletion();
        //CalculateDeltaPressure();

        ApplyPassiveForce();
        ApplySlopeForce();
        ApplyGravitationalForce();
        StickToGround();
        //ApplyStatus();
    }

    private void LateUpdate()
    {
        if (m_anim.GetBool("firing")) {
            m_anim.GetBoneTransform(HumanBodyBones.Spine).Rotate(targetLateral.eulerAngles);
        }
    }

    #region Collision


    private Vector3 wallNormal;
    private ContactPoint ledgeContactPoint;
    private Vector3 ledgePosition;

    private void OnCollisionEnter(Collision collision)
    {
        //we just have to check that at least one collision point is a slope
        //if it's a slope, then it doesn't matter if we're touching a wall - slope supercedes all

        //isGrounded = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("Colliding with: " + collision.collider.name + " - POC: " + collision.contactCount);
        //isGrounded = false;
        //onSlope = false;

        collisionPoints = new ContactPoint[collision.contactCount];
        collision.GetContacts(collisionPoints);

        collisionNames.Clear();

        wallNormal = Vector3.zero;
        //isGrounded = false;

        foreach (ContactPoint cp in collisionPoints)
        {

            collisionNames.Add(cp.otherCollider.name);

            //get the angle of the contact point relative to the sky/Vector3.up
            contactNormal = Vector3.Angle(Vector3.up, cp.normal);

            if (contactNormal < 90.0f)
            {
                isGrounded = true;

                //onSlope = contactNormal > slopeLimit;

                //if (onSlope)
                //    slopeDirection = Vector3.Cross(Vector3.Cross(Vector3.up, cp.normal), Vector3.up);
                //else
                //    slopeDirection = Vector3.zero;
            }

            //Debug.DrawRay(transform.position, cp.normal, Color.white);

            //if the angle is greater than 90, essentially a ceiling and is ignored

            //if perfectly 90, it's deemed as a grabbable wall
            if (contactNormal == 90.0f)
            {
                //m_rb.velocity = Vector3.up * m_rb.velocity.y;
                wallNormal = cp.normal; //only need to store one wall, it's fine to overwrite any other wall values
                ledgeContactPoint = cp;
                //m_rb.velocity = Vector3.up * m_rb.velocity.y;
                continue; //don't need to process this contact point anymore - move on to next contact point
            }

            if (contactNormal > 90.0f)
                continue;

        }


        //not grounded, we found a valid wall and we are falling
        //also check the height of the object right?
        //make sure it's only x amount higher than the player at the moment they grab

        if (!wallNormal.Equals(Vector3.zero))
        {
            //hit a wall

            if (!isGrounded)
            {
                //check to see if the player would like to grab the wall
                //are they holding in the direction they want to grab?
                bool grab = false;
                float wallAngle = -1.0f;

                if (movementRelativeDirection.Equals(Vector3.zero))
                {
                    //not holding a direction, use player forward and A
                    //when not holding direction, player model must be facing the wall 
                    wallAngle = Vector3.Angle(transform.forward, wallNormal);

                    grab = (wallAngle > 150.0f) && (wallAngle < 210.0f) && (m_anim.GetBool("jump"));
                }
                else
                {
                    //holding a direction, use direction angle and only when falling do you grab
                    wallAngle = Vector3.Angle(movementRelativeDirection, wallNormal);

                    //increased angle window for input grab
                    grab = ((wallAngle > 120.0f) && (wallAngle < 240.0f));
                }

                if (grab)
                {

                    //TODO: Height needs to be multiplied not always by 2, but by the percent difference between height and center
                    Vector3 highestLedgePoint = ledgeContactPoint.point - (ledgeContactPoint.normal.normalized * m_cc.radius * 2.0f) + (Vector3.up * m_cc.height * (m_cc.height/m_cc.center.y));

                    Debug.DrawRay(highestLedgePoint, Vector3.down * m_cc.height * 2.15f, Color.white, 2.0f);

                    if (Physics.Raycast(highestLedgePoint + (Vector3.up * 0.1f), Vector3.down, out RaycastHit hitInfo, m_cc.height * 2.15f))
                    {

                        Debug.Log("Hit: " + hitInfo.collider.name);
                        //if we've hit something AND the ledge is steppable (won't slide off)
                        if (Vector3.Angle(hitInfo.normal, Vector3.up) < slopeLimit)
                        {

                            float ledgeDifference = hitInfo.point.y - m_cc.bounds.max.y;

                            if ((ledgeDifference >= 0) && (ledgeDifference < 0.5f))
                            {

                                Vector3 predictedLedgePosition = highestLedgePoint;
                                predictedLedgePosition = new Vector3(predictedLedgePosition.x, hitInfo.point.y, predictedLedgePosition.z);

                                ledgePosition = predictedLedgePosition;
                                m_anim.SetBool("ledge", true);

                                //Debug.Break();

                            }
                        }

                    }
                }
            }
            else
            {
                //hit a wall AND grounded
                float wallMovementAngle = Vector3.Angle(wallNormal, movementRelativeDirection);

                m_anim.SetBool("wallPush", !wallNormal.Equals(Vector3.zero) && !movementRelativeDirection.Equals(Vector3.zero) && (wallMovementAngle >= 90.0f));
            }

            #region OldWallLogic
            //if (!isGrounded && (!wallNormal.Equals(Vector3.zero)))
            //{

            //    //check to see if the player would like to grab the wall
            //    //are they holding in the direction they want to grab?
            //    bool grab = false;
            //    float wallAngle = -1.0f;

            //    if (movementRelativeDirection.Equals(Vector3.zero))
            //    {
            //        //not holding a direction, use player forward and A
            //        //when not holding direction, player model must be facing the wall 
            //        wallAngle = Vector3.Angle(transform.forward, wallNormal);

            //        grab = (wallAngle > 150.0f) && (wallAngle < 210.0f) && (m_anim.GetBool("jump"));
            //    }
            //    else
            //    {
            //        //holding a direction, use direction angle and only when falling do you grab
            //        wallAngle = Vector3.Angle(movementRelativeDirection, wallNormal);

            //        //increased angle window for input grab
            //        grab = ((wallAngle > 120.0f) && (wallAngle < 240.0f));
            //    }

            //    if (grab)
            //    {

            //        Vector3 highestLedgePoint = ledgeContactPoint.point - (ledgeContactPoint.normal.normalized * m_cc.radius * 2.0f) + (Vector3.up * m_cc.height * 2.0f);

            //        Debug.DrawRay(highestLedgePoint, Vector3.down * m_cc.height * 2.15f, Color.white, 2.0f);

            //        if (Physics.Raycast(highestLedgePoint + (Vector3.up * 0.1f), Vector3.down, out RaycastHit hitInfo, m_cc.height * 2.15f))
            //        {

            //            Debug.Log("Hit: " + hitInfo.collider.name);
            //            //if we've hit something AND the ledge is steppable (won't slide off)
            //            if (Vector3.Angle(hitInfo.normal, Vector3.up) < slopeLimit)
            //            {

            //                float ledgeDifference = hitInfo.point.y - m_cc.bounds.max.y;

            //                if ((ledgeDifference >= 0) && (ledgeDifference < 0.5f))
            //                {

            //                    Vector3 predictedLedgePosition = highestLedgePoint;
            //                    predictedLedgePosition = new Vector3(predictedLedgePosition.x, hitInfo.point.y, predictedLedgePosition.z);

            //                    ledgePosition = predictedLedgePosition;
            //                    m_anim.SetBool("ledge", true);

            //                }
            //            }

            //        }

            //    }
            //} //if !grounded && wall
            #endregion OldWallLogic
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        //resets values for when there's no collision points left to run the grounded checks against
        if (collision.contactCount == 0)
        {
            //ledgeContactPoint = new ContactPoint();
            m_anim.SetBool("ledge", false);
            m_anim.SetBool("wallPush", false);
            //isGrounded = onSlope = false;
            isGrounded = false;
        }
        else
        {
            //check all exiting collision points
            //if you exit a grounded one while still touching other stuff, you're still grounded
            foreach (ContactPoint cp in collision.contacts) {
                float contactNormalAngle = Vector3.Angle(Vector3.up, cp.normal);

                if (contactNormalAngle < 90.0f) {
                    isGrounded = false;
                    return;
                }
            }
        }
    }

    #endregion Collision

    public void LedgeGrab()
    {
        //teleport player to top of ledge


        //float ledgeHeight = (ledgeContactPoint.otherCollider.bounds.ClosestPoint(ledgeContactPoint.otherCollider.transform.position + Vector3.up)).y;
        //Vector3 ledgePosition = new Vector3(ledgeContactPoint.point.x, ledgeHeightPoint, ledgeContactPoint.point.z) - (ledgeContactPoint.normal.normalized * (m_cc.radius * 2.0f));

        //Vector3 ledgePosition = ledgeContactPoint.otherCollider.bounds.ClosestPoint(ledgeContactPoint.otherCollider.transform.position + Vector3.up);
        //m_rb.MovePosition(ledgePosition);
        transform.position = ledgePosition;
        m_rb.velocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(-ledgeContactPoint.normal, Vector3.up);
    }

    private void StickToGround() {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, 0.125f))
        {

            //CHECK SLOPE HERE
            float floorAngle = Vector3.Angle(hitInfo.normal, Vector3.up);

            if (floorAngle >= slopeLimit)
            {
                slopeDirection = Vector3.Cross(Vector3.Cross(Vector3.up, hitInfo.normal), Vector3.up);
                onSlope = true;
            }
            else
            {
                if (m_anim.applyRootMotion)
                    m_rb.MovePosition(new Vector3(transform.position.x, hitInfo.point.y, transform.position.z));

                slopeDirection = Vector3.zero;
                onSlope = false;
            }
        }

        else
            onSlope = false;
    }

    #region ApplyForces
    //Always applys a force in the forward. Negative values can be used to apply force backwards
    public void ApplyForce(float force) {

        m_rb.velocity = Vector3.zero;
        //apply a force in a direction based on rotationDirection
        m_rb.AddForce(force * transform.forward * m_rb.mass, ForceMode.Impulse);

        //m_rb.AddForce(appliedForce * m_rb.mass, ForceMode.Impulse);
    }

    public void ApplyJumpForce(float jf)
    {
        m_rb.AddForce(Vector3.up * jf * m_rb.mass, ForceMode.Impulse);
    }

    private void ApplySlopeForce() {

        //only applied a counter force when running up a slope
        if (onSlope)
        {
            //ForceMode fm = ForceMode.Force;
            //float slopeStrength = (90 - contactNormal) / (90 - slopeLimit);
            //Debug.Log("SlopeStrength: " + slopeStrength);

            //if (!m_anim.applyRootMotion)
            //    m_rb.AddForce(slopeDirection.normalized * (5.0f + (slopeStrength * 2.5f)), fm);

            if (!m_anim.applyRootMotion) {
                //free falling on a slopegravity
                //apply a force in the direction of the slope
                m_rb.AddForce(slopeDirection.normalized * 10.0f, ForceMode.Acceleration);
            }

            if (FloorVelocityAngle() > 70.0f)
            {
                //NOTE: Uncommenting the line below will reduce stamina when running up a slope
                //Note needed since slopes automatically put you into a slide now
                //stamina -= staminaReductionRate * Time.fixedDeltaTime * 5.0f;
            }

        }
    }

    private void ApplyPassiveForce() {

        //m_rb.AddForce(movementRelativeDirection * m_rb.mass, ForceMode.Acceleration);

        //if (m_rb.velocity.magnitude > movementSpeed)
        //{
        //    m_rb.velocity = m_rb.velocity.normalized * movementSpeed;
        //}


        if (!isGrounded)
            m_rb.AddForce(movementRelativeDirection.normalized * m_rb.mass * 0.05f, ForceMode.Acceleration);

        //m_rb.velocity = Vector3.ClampMagnitude(m_rb.velocity, 5.0f);
    }

    private void ApplyGravitationalForce() {

        //m_rb.drag = 0.0f;
        //gravityMultiplier = 1;

        //GROUNDED >

        //if (!m_anim.applyRootMotion)
        //{
        //    //higher gravity
        //    //less drag
        //    m_rb.drag = 0.01f;

        //}
        //else {
        //    m_rb.drag = 2.0f;
        //    gravityMultiplier = 9;
        //}

        if (isGrounded && !onSlope)
        {
            gravityMultiplier = 10;
            m_rb.drag = 1.5f;
        }
        else
        {
            gravityMultiplier = 1;
            m_rb.drag = 0.01f;
        }

        m_rb.AddForce(Physics.gravity * gravityMultiplier * (1 + (1 - m_anim.GetFloat("sprinting"))), ForceMode.Acceleration); //applies additional 3x gravity to stick player to floor - less floatiness while not affecting general gravity

    }
    #endregion ApplyForces

    private void StaminaDepletion() {
        if (m_anim.GetFloat("sprinting") <= 0.5f) {
            if (staminaController)
                staminaController.DepleteStamina();
        }
    }

    public void UseStamina(float s) {
        if (staminaController) {
            staminaController.UpdateStamina(s);
        }
    }

    public void Exhaust() {
        m_anim.SetBool("exhaust", true);
    }

    public void Recover() {
        m_anim.SetBool("exhaust", false);
    }

    private void HydrationDepletion() {
        
    }

    public void UseHydration(float h) {
        if (hydrationController) {
            hydrationController.UpdateHydration(h);
        }
    }

    void InitializePlayerStats() {

        rotationSpeed = 5.0f;
        //movementSpeed = 0.5f;
        slopeLimit = 25.0f;

        //Note: this float is used to make rotating slower when sprinting. Setting to 1 means rotation speed is multiplied by 1, and thus, unchanged from basic behaviour
        m_anim.SetFloat("sprinting" , 1.0f);

        knife.SetActive(false);
    }

    //void ApplyStatus() {
    //    hydration -= (Time.fixedDeltaTime * 0.01f) + ((0.0001f * (m_lateralVelocity.magnitude / 6.5f)));
    //    hydration = Mathf.Clamp(hydration, 0.0f, MAX_HYDRATION);
    //}

    //pivots should lerp towards input direction, it feels better
    public void Rotate(Vector3 direction)
    {

        movementRelativeDirection = direction;

        //if (!m_rb.freezeRotation && m_anim.GetInteger("rotationMode") == (int)RotationMode.ball)
        //    return;
        //else
        //    m_rb.freezeRotation = true;

        if (m_anim.GetBool("firing")) {
            transform.rotation = Quaternion.LookRotation(targetDirection);

            return;
        }

        if (transform.rotation.eulerAngles.x != 0.0f || transform.rotation.eulerAngles.z != 0.0f)
            transform.rotation = Quaternion.Euler(0.0f, transform.rotation.eulerAngles.y, 0.0f);
        //NOTE: Animator Integer 'rotationMode' can be changed by animation states, allowing for the change of rotation function
        switch (m_anim.GetInteger("rotationMode"))
        {
            //m_rotMode just an integer, would be nice to replace with enum names instead
            case ((int)RotationMode.oppositeVelocity):
                if (m_lateralVelocity.magnitude > 0.5f)
                    transform.rotation = Quaternion.LookRotation(-m_lateralVelocity.normalized);
                break;
            case ((int)RotationMode.forwardVelocity):
                if (m_lateralVelocity.magnitude > 0.5f)
                    transform.rotation = Quaternion.LookRotation(m_lateralVelocity.normalized);
                break;
            case ((int)RotationMode.snapInput): //rotation gets set in one frame: here it snaps towards Input and immediately reverts to normal rotation

                if (!direction.Equals(Vector3.zero))
                    transform.rotation = Quaternion.LookRotation(direction);

                //after snapping rotation, automatically reverts rotationMode to normal
                m_anim.SetInteger("rotationMode", (int)RotationMode.towardInput);

                break;
            case ((int)RotationMode.snapPause):

                if (m_anim.GetBool("focus"))
                    return;

                //look towards camera and that's it...
                if (!direction.Equals(Vector3.zero))
                    transform.rotation = Quaternion.LookRotation(direction);

                m_anim.SetInteger("rotationMode", (int)RotationMode.attackRotation);
                break;
            case ((int)RotationMode.attackRotation):
                if (!direction.Equals(Vector3.zero))
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 0.25f);
                    //Debug.Log("Angle: " + Vector3.Angle(transform.forward, direction) + " vs. SignedAngle: " + Vector3.SignedAngle(transform.forward, direction, Vector3.up));
                }
                break;
            case ((int)RotationMode.slopeForward):
                if (!slopeDirection.Equals(Vector3.zero))
                    transform.rotation = Quaternion.LookRotation(new Vector3(slopeDirection.x, 0.0f, slopeDirection.z));
                break;
            case((int)RotationMode.ball):
                m_rb.freezeRotation = false;
                break;
            case ((int)RotationMode.towardTarget):
                //have an angle yeh? it always points in the direction of the target relative to you...
                //so if they are 
                transform.rotation = Quaternion.LookRotation(targetDirection, Vector3.up); //NOTE: rename camRelativeDirection to just targetDirection or something
                break;
            case ((int)RotationMode.towardInput):
                if (!direction.Equals(Vector3.zero))
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime * m_anim.GetFloat("sprinting"));
                    //Debug.Log("Angle: " + Vector3.Angle(transform.forward, direction) + " vs. SignedAngle: " + Vector3.SignedAngle(transform.forward, direction, Vector3.up));
                }
                break;
            case ((int)RotationMode.towardWall):

                if(!wallNormal.Equals(Vector3.zero))
                    transform.rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);

                break;
            default:
                
                break;
        }

        m_anim.SetFloat("directionIntention", Vector3.Angle(transform.forward, direction));
    }

    public void SnapRotate() {
        if (!movementRelativeDirection.Equals(Vector3.zero))
            transform.rotation = Quaternion.LookRotation(movementRelativeDirection, Vector3.up);
    }

    public void Jump(bool j) {

        m_anim.SetBool("jump", j);

        //if (j && m_anim.applyRootMotion && isGrounded)
        //    ApplyJumpForce(500.0f);
        //else
        //    m_anim.SetBool("jump", j);

    }

    public void Throw(bool t) {
        m_anim.SetBool("throws", t);
    }

    GameObject thrownObject = null;

    public void ThrowObject() {
        //USE currentThrowable TO FIND THE PROPER THROWABLEL FROM throwableList.throwables[currentThrowable]

        if (thrownObject != null)
            return;

        thrownObject = Instantiate(onThrow?.Invoke().GetPrefab(), m_anim.GetBoneTransform(HumanBodyBones.LeftHand).position + (m_anim.GetBoneTransform(HumanBodyBones.LeftHand).forward * 0.25f), Quaternion.identity);

        thrownObject.tag = this.tag;

        

        if (thrownObject.TryGetComponent<Grenade>(out Grenade gr))
        {
            //if (!movementRelativeDirection.Equals(Vector3.zero))
            //    gr.LaunchThrowable(Quaternion.Euler(m_targetForward.eulerAngles.x, 0.0f, 0.0f) * (movementRelativeDirection.normalized + (Vector3.up * 0.25f)));
            //else
            //    gr.LaunchThrowable(Quaternion.Euler(m_targetForward.eulerAngles.x, 0.0f, 0.0f) * (transform.forward + (Vector3.up * 0.25f)));

            gr.LaunchThrowable(m_targetForward * Vector3.forward);
        }


        thrownObject = null;
    }

    public void Sprint(bool s)
    {

        bool canSprint = s;

        if (staminaController) {
            //check to make sure stamina isn't blocked
            canSprint = s && !staminaController.exhausted;
        }

        m_anim.SetBool("sprint", canSprint);
    }

    public void Slash(bool s) {

        bool canSlash = s;

        if (staminaController)
        {
            //check to make sure stamina isn't blocked
            canSlash = s && !staminaController.exhausted;
        }

        if (canSlash)
            m_anim.SetTrigger("slash");

        m_anim.SetBool("slashs", canSlash);
    }

    public void Equip(bool e) {
        m_anim.SetBool("equip", e);
    }

    public void EquipPrimary(bool e)
    {
        m_anim.SetBool("equip1", e);
    }

    public void EquipSecondary(bool e)
    {
        m_anim.SetBool("equip2", e);
    }

    public void Focus(bool f) { m_anim.SetBool("focus", f); }
    public void Fire(bool f) {

        if (f)
            m_anim.SetTrigger("fire");

        m_anim.SetBool("fires", f);
    
    }

    public void FireWeapon() {
        if (gunController)
            gunController.Fire(targetPosition, m_anim.GetBool("focus"));
    }

    public void Reload(bool r) {
        if(gunController)
            m_anim.SetBool("reload", r && gunController.CanReload());
    }

    public void ReloadWeapon() {

        if (gunController)
            gunController.Reload();

    }

    public void Crouch(bool c) { 
        
        m_anim.SetBool("crouch", c);

        //if (c && (m_anim.GetCurrentAnimatorStateInfo(0).IsName("BaseMovement") || m_anim.GetCurrentAnimatorStateInfo(0).IsName("Idle") || m_anim.GetCurrentAnimatorStateInfo(0).IsName("FocusedSubBlend"))) {
        //    //in a crouchable state AND crouched
        //    if (m_anim.GetLayerWeight(1) >= 1.0f)
        //    {
        //        //crouched...
        //        m_anim.SetLayerWeight(1, 0.0f);
        //    }
        //    else {
        //        m_anim.SetLayerWeight(1, 1.0f);
        //    }
        //}
    }

    public void SetTargetForward(Quaternion tf) {
        m_targetForward = tf;
    }

    Vector3 targetPosition;

    public void SetTargetPosition(Vector3 tp) {
        targetPosition = tp;
    }

    //Note: Pressure refers to how hard player is pressing on joystick (keyboards inheritently lack this ability, defaults to 0 or 1
    public void SetPressure(float pressure) {

        //lerp to pressure
        m_anim.SetFloat("pressure", Mathf.Lerp(m_anim.GetFloat("pressure"), pressure, 0.4f));
    }

    private void CalculateDeltaPressure() {
        deltaPressure = Mathf.Abs(prevPressure - m_anim.GetFloat("pressure"));

        //basically, if you go below 6 AND the delta was greater than some value (ie the delta is greater than

        //NOTE: better to localize the fullSpeed check only while in this state as to not trigger full pressure when in other animations
        if (deltaPressure >= 0.25f)
            m_anim.SetTrigger("fullSpeed");
        else
            m_anim.ResetTrigger("fullSpeed");

        prevPressure = m_anim.GetFloat("pressure");
    }

    public void SetTargetDirection(Vector3 targDir) {
        targetDirection = targDir;
    }

    public void SetTargetLateral(Quaternion targLat) {

        targetLateral = targLat;
    }

    //Note: interactions are technically controlled via LootButton and, in general, button presses
    //That is to say, events attached to LootButton run a BlockInput function that set the interact boolean to true, but send a blend int to change interaction/menu animations
    //Kind of a weird design, and it puts the blunt of responsibility of setting and pausing player to buttons instead of making it the responsibility of menus
    //All that to say, menus can now be opened without necessarily letting the player animator know.

    //Perhaps it better to allow buttons change the blend float, and keep the pausing/interaction boolean setting to menus opening/closing
    public void SetInteract(bool i) {
        m_anim.SetBool("interact", i);
    }

    public void SetInteractBlend(float t) {
        m_anim.SetFloat("interactBlend", t);
    }

    public bool GetActing() {
        return m_anim.GetBool("reloading") || m_anim.GetBool("ledge") || m_anim.GetBool("dead");
    }

    public bool GetInActionState() { return m_anim.GetBool("acting"); }

    private void ReduceStamina() {
        //check velocity angle with floor angle,
        //anytime you are moving against slope (and on slope)
        
    }

    public float FloorInputAngle() {
        return Vector3.Angle(movementRelativeDirection, slopeDirection); //won't be 0 really ever but we'll see
    }

    private float FloorVelocityAngle() {
        return Vector3.Angle(m_lateralVelocity.normalized, slopeDirection);
    }

    public void Freeze() {
        this.m_rb.velocity = new Vector3(0.0f, m_rb.velocity.y, 0.0f);
    }

    private void DrawRay() {
        Debug.DrawRay(groundRay.origin, groundRay.direction, Color.red);
    }

    public float GetStamina()
    {
        if (staminaController)
            return staminaController.GetStamina();
        else
            return 1.0f;
    }

    public int GetHealth() {
        if (this.TryGetComponent<HealthController>(out HealthController hc))
        {
            return hc.health;
        }
        else
            return 1;
    }

    public void ConsumeItem(ConsumableItem item) {

        //grab consumable info if any

        //cast item as consumable??

        //ConsumableItem cItem = (ConsumableItem)item;

        m_anim.SetTrigger("consume");
        m_anim.SetInteger("consumeType", item.index);

        //FUCK
        item.ApplyEffects(this);

        GameObject consumePrefab = item.GetPrefab();

        if (consumePrefab)
        {
            heldPrefab = Instantiate(consumePrefab, m_anim.GetBoneTransform(HumanBodyBones.LeftHand));

            //if (heldPrefab.TryGetComponent<PrefabDestroyer>(out PrefabDestroyer pd))
            //    pd.SetDestroyTime(60 * Time.fixedDeltaTime);
        }
        else
            heldPrefab = null;
    }

    public Expression GetCurrentExpression() {

        if (expressionContoller)
            return expressionContoller.previousExpression;

        return Expression.normal;
    }

    public void ChangeExpression(Expression e) {
        if (expressionContoller)
            expressionContoller.ChangeExpression(e);
    }


    public void DestoryHeldItem() {
        if (heldPrefab)
            Destroy(heldPrefab.gameObject);
    }

    public void ConsumeItemState() { 
        
    }

    public void EnableSlash() {
        knife.GetComponent<TrailRenderer>().enabled = true;
    }

    public void EquipKnife(bool k) {
        knife.SetActive(k);
    }

    public void EquipGun(bool g) {
        gunController.ToggleGun(g);
    }

    public void AddHealth(int h) {
        //healthController.AddHealth(h);

    }

    public void SetDirectionAngle(float angle) {
        //well it's a direction, as well as an angle...
        m_anim.SetFloat("direction", angle);
    }

    public void Damage() {

        m_anim.SetTrigger("damage");
        //m_anim.applyRootMotion = false;
        m_anim.SetBool("damaged", true);
        StartCoroutine("DamageStun");
    }


    private IEnumerator DamageStun() {
        yield return new WaitForSeconds(3.0f);
        m_anim.SetBool("damaged", false);
    }

    public void Die() {

        m_anim.SetBool("dead", true);
        m_anim.SetTrigger("die");
    }

    //public float GetHydration() {
    //    return hydration;
    //}

    private float ActingHalt() {
        if (GetActing())
            return 0.0f;
        else
            return 1.0f;
    }

    public void SetStats(int health, int maxHealth, float maxStamina, float hydration) {
        if (this.TryGetComponent<HealthController>(out HealthController hc)) {
            hc.SetHealth(health);
            hc.SetMaxHealth(maxHealth);
        }

        if (staminaController) {
            staminaController.SetMaxStamina(maxStamina);
        }

        if (hydrationController) {
            hydrationController.SetHydration(hydration);
        }
    }

    public float GetHydration() {

        if (hydrationController)
            return hydrationController.hydration;
        else
            return 1.0f;

    }

    public Item GetCurrentThrowable() { return currentThrowable; }

    public bool IsActing() {

        return m_anim.GetBool("acting");
    }

}
