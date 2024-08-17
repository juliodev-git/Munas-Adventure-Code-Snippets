using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cinemachine;

public class Human : MonoBehaviour
{

    [SerializeField]
    public Player m_player;

    [SerializeField]
    private RaidStatusScriptableObject rso; //

    [SerializeField]
    private ApplicationSettingsScriptableObject aso;

    Camera m_camera;
    Vector2 m_moveInput;

    Vector3 m_moveForward;
    Quaternion m_camForward;
    Quaternion m_camRight;

    public Image staminaBar;
    public Image healthBar;
    private Vector3 initialSlopeSizeDelta;

    public AmmoUI ammoUI;

    private PlayerControls m_input;

    [SerializeField]
    private CinemachineFreeLook playerCam;

    [SerializeField]
    private CinemachineVirtualCamera targetCam;

    private CinemachineInputProvider inputCamera;

    [SerializeField]
    private RaidItemController raidItemController;

    [SerializeField]
    private BagRaidController bagRaidController;

    [SerializeField]
    private GameObject targetUI;

    [SerializeField]
    private bool m_interact;

    [SerializeField]
    private bool blockInput;

    private float angle;

    [SerializeField]
    private float targetAngle;

    private Transform dTarget;

    [SerializeField]
    private float verticalAngle;

    //TEMP: FOR TESTING
    [SerializeField]
    private float cameraAngle;

    [SerializeField]
    LayerMask targetLayerMask;

    private Vector2 gamepadDamper;


    //NOTE: This is becoming too much for Human script, may be better to move this to some TargetCameraController and just send input to it from here
    //private float initialCameraSpeed;

    #region InputInitialize

    private void Awake()
    {
        m_input = new PlayerControls();
    }

    
    private void OnEnable()
    {
        m_input.Enable();
    }

    private void OnDisable()
    {
        m_input.Disable();
    }
    #endregion InputInitialize

    private void Start()
    {
        //InitializeHumanValues();
        raidItemController.onConsume += ConsumeCheck;
        //bagRaidController.useThrowable += ThrowableCheck;
        //GameController.instance.useThrowable += SendPlayerItem;

        Target.onTargetRemove += UpdateDesiredTarget;

        aso.onChange += UpdateCameraSense;

        gamepadDamper = Vector2.one;

        //if(m_player)
        //    m_player.takeDamage += UpdateHealthBar;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_player)
            return;

        if (m_interact)
            return;

        //check if on mouse/keyboard input
        //maybe even, just check if a gamepad input was pressed to auto-change the acceltime of the camera

        if (inputCamera) {

            if (inputCamera.XYAxis.action.activeControl != null)
            {
                //Debug.Log(inputCamera.XYAxis.action.activeControl.ToString());
                if (inputCamera.XYAxis.action.activeControl.ToString().Contains("Mouse"))
                {

                    playerCam.m_XAxis.m_AccelTime = 1.0f;
                    playerCam.m_YAxis.m_AccelTime = 1.0f; //basically some


                    playerCam.m_XAxis.m_DecelTime = 0.05f;
                    playerCam.m_YAxis.m_DecelTime = 0.05f;

                    //gamepadDamper = (Vector2.right * 2.0f) + (Vector2.up * 0.5f);

                    //gamepadDamper = Vector2.one;
                    gamepadDamper = (Vector2.right * 0.5f) + (Vector2.up * 0.3f);
                }
                else
                {
                    playerCam.m_XAxis.m_AccelTime = (0.25f);
                    playerCam.m_YAxis.m_AccelTime = (0.3f); //basically some

                    playerCam.m_XAxis.m_DecelTime = 0.025f;
                    playerCam.m_YAxis.m_DecelTime = 0.025f;

                    gamepadDamper = Vector2.one;
                }
            }
            else
            {

                playerCam.m_XAxis.m_AccelTime = (0.25f);
                playerCam.m_YAxis.m_AccelTime = (0.3f); //basically some

                playerCam.m_XAxis.m_DecelTime = 0.025f;
                playerCam.m_YAxis.m_DecelTime = 0.025f;

                gamepadDamper = Vector2.one;
            }

            //UpdateCameraSense();

        }
        ////a stick forward can be created using the Move Vector 2...
        //m_moveInput = m_input.Player.Move.ReadValue<Vector2>();
        //m_moveForward = new Vector3(m_moveInput.x, 0.0f, m_moveInput.y);

        //m_camForward = Quaternion.Euler(0.0f, m_camera.rotation.eulerAngles.y, 0.0f);

        ////TODO: Move some of this logic to fixedUpdate, higher framerate means faster rotations (which is bad)
        //m_player.Rotate(m_camForward * m_moveForward);
        //m_player.SetCameraRelativeDirection(m_camForward * m_moveForward);
        //m_player.SetPressure(m_input.Player.Move.ReadValue<Vector2>().magnitude);

        //Checks for input as well as verifies if a throwable is in inventory


        m_player.Jump(m_input.Player.Jump.WasPressedThisFrame() && !m_interact); //NOTE: A little hidden, but a boolean 'interact' disables jumping. That way you can't jump while grounded
        
        m_player.Sprint(m_input.Player.Sprint.WasPressedThisFrame());

        m_player.Slash(m_input.Player.Slash.WasPressedThisFrame() && !m_interact);

        m_player.Equip(m_input.Player.Equip.WasPressedThisFrame() && !m_interact && !blockInput); //blockInput makes it so you cant equip ur weapon AND open a door
        m_player.EquipPrimary(m_input.Player.EquipPrimary.WasPressedThisFrame() && !m_interact && !blockInput); //blockInput makes it so you cant equip ur weapon AND open a door
        m_player.EquipSecondary(m_input.Player.EquipSecondary.WasPressedThisFrame() && !m_interact && !blockInput); //blockInput makes it so you cant equip ur weapon AND open a door

        m_player.Focus(m_input.Player.Focus.IsPressed());

        m_player.Fire(m_input.Player.Fire.IsPressed());

        m_player.Crouch(m_input.Player.Crouch.WasPressedThisFrame());

        m_player.Throw(m_input.Player.Throw.WasPressedThisFrame() && GameController.instance.ThrowableExists());

        m_player.Reload(m_input.Player.Reload.WasPressedThisFrame());

        //m_player.SetHydration(rso.hydration);

        //ThrowInputCheck();

        //a stick forward can be created using the Move Vector 2...
        m_moveInput = m_input.Player.Move.ReadValue<Vector2>();
        m_moveForward = new Vector3(m_moveInput.x, 0.0f, m_moveInput.y);

        m_camForward = Quaternion.Euler(0.0f, m_camera.transform.rotation.eulerAngles.y, 0.0f);
        m_camRight = Quaternion.Euler(m_camera.transform.rotation.eulerAngles.x, 0.0f, 0.0f);

        //TODO: Move some of this logic to fixedUpdate, higher framerate means faster rotations (which is bad)
        m_player.Rotate(m_camForward * m_moveForward);
        m_player.SetTargetDirection(m_camForward * Vector3.forward);
        m_player.SetTargetLateral(m_camRight);
        
        m_player.SetTargetForward(Quaternion.LookRotation(m_camera.transform.forward));
        //m_player.SetTargetForward(RaycastTarget());

        m_player.SetTargetPosition(RaycastTarget());

        angle = Vector3.SignedAngle(Vector3.forward, m_moveForward, Vector3.up);

        m_player.SetDirectionAngle(angle);

        //
        //TargetFind();

        FocusCamera();

    }

    private void FixedUpdate()
    {
        if (!m_player)
            return;

        UpdateStaminaBar();

        if (m_interact) {
            m_player.SetPressure(0.0f);
            return;
        }

        UpdateHydrationStatus();

        m_player.SetPressure(m_input.Player.Move.ReadValue<Vector2>().magnitude);

        ////a stick forward can be created using the Move Vector 2...
        //m_moveInput = m_input.Player.Move.ReadValue<Vector2>();
        //m_moveForward = new Vector3(m_moveInput.x, 0.0f, m_moveInput.y);

        //m_camForward = Quaternion.Euler(0.0f, m_camera.rotation.eulerAngles.y, 0.0f);

        ////TODO: Move some of this logic to fixedUpdate, higher framerate means faster rotations (which is bad)
        //m_player.Rotate(m_camForward * m_moveForward);
        //m_player.SetTargetDirection(m_camForward * Vector3.forward);
        //m_player.SetPressure(m_input.Player.Move.ReadValue<Vector2>().magnitude);
        //m_player.SetTargetForward(m_camForward);

        //angle = Vector3.SignedAngle(Vector3.forward, m_moveForward, Vector3.up);

        //m_player.SetDirectionAngle(angle);

        //angle between camera forward and input...



    }

    private void LateUpdate()
    {
        //AdjustCameraToTarget();
    }

    public void InitializeHumanValues() {
        //initialSlopeSizeDelta = slopeStaminaBar.rectTransform.localScale ;

        if (!m_player) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            if(player)
                m_player = player.GetComponent<Player>();
        }

        m_camera = Camera.main;

        m_interact = false;
    }

    public void UpdateCameraSense()
    {

        if (playerCam && aso)
        {

            //take the aso.Sense value and use it to apply
            playerCam.m_XAxis.m_MaxSpeed = aso.GetSensitivity() * 200.0f * gamepadDamper.x; //200f at 1, 500 at
            playerCam.m_YAxis.m_MaxSpeed = aso.GetSensitivity() * 1.0f * gamepadDamper.y; //at 1, 2.0f... for mouse > 2.5f
        }

    }

    public Vector3 GetPlayerPosition() {
        return m_player.transform.position + Vector3.up; //Vector3 up is an offset to player position so that it doesn't target the player's feet
    }

    public void BlockInput() {
        blockInput = true;

        //if (m_player)
        //    m_player.SetInteract(true);
    }

    public void UnblockInput() {
        blockInput = false;
    }

    public void SetInteractAnimatorBlend(int i) {

        if (m_player)
            m_player.SetInteract(true);

        if (m_player)
            m_player.SetInteractBlend((float)i);
    }

    public void EnterInteraction(int i) {
        if (m_player)
            m_player.SetInteract(true);

        if (m_player)
            m_player.SetInteractBlend((float)i);
    }

    //UHM ONE fucking method to block input, one for making the player interact
    //
    public void ExitInteraction() {
        if (m_player)
            m_player.SetInteract(false);

        if (m_player)
            m_player.SetInteractBlend(0.0f);
    }
    
    public void ReleaseBlock() {
        m_interact = false;
        blockInput = false;
    }

    public bool IsPlayerGrounded() {
        return m_player.isGrounded;
    }

    public bool IsPlayerActing() {
        return m_player.GetActing();
    }

    //used by UI to send item info
    private void ConsumeCheck(ConsumableItem item) {
        //woawy zowy delegates can transfer arguments to listeners wow
        //ok also, essentially passed the item into player as well as the consumable
        //lets player attempt to eat the food (ie it plays the animation)

        //can immediately give the effects and just lock the player into the consuming state, that way to callbacks have to happen
        m_player.ConsumeItem(item);
        
    }

    //Sends item to player, removes item from bag
    public Item GetThrowable() {
        ItemPair toThrow = GameController.instance.GetThrowableItem();

        GameController.instance.RemoveItemFromBag(toThrow.Value);

        return toThrow.Key;
    }

    public void ReleaseItem() {
        //if (thrownItem != null) {
        //    GameController.instance.RemoveItemFromBag(thrownItem.Value);
        //}
    }

    //NOTE: This is called once per Start on a new scene. Human.cs is static so its own Start is only called once, need a soft 'Start' to get scene-specific objects
    public void SubscribeUI() {
        //uhm link human player to rso...
        //so whenever the health gets updated, or rather, when player gets damaged than make sure to update the rso...

        if (m_player) {
            if (m_player.TryGetComponent<HealthController>(out HealthController hc)) {
                //subscribe to player's health changes and update to rso
                hc.onHealthUpdate += UpdateHealthStatus;
                hc.die += GameController.instance.PlayerDeath;
            }

            m_player.onThrow += GetThrowable;

            m_player.GetComponent<GunController>().onAmmoChange += ammoUI.UpdateAmmo;

            //if(m_player.TryGetComponent<Hea>)
        }

        UpdateHealthBar();

        //NOTE: Finds PlayerCamera upon loading new scene
        //This isn't really the place for this but this function is called on Start of a new scene that has the
        //GameControllerLink script which, itself, is a soft Start call for GameController singleton stuff
        if (!playerCam)
            playerCam = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CinemachineFreeLook>();

        if (playerCam) {

            playerCam.m_XAxis.m_AccelTime = (0.25f);
            playerCam.m_YAxis.m_AccelTime = (0.3f); //basically some
            

            inputCamera = playerCam.GetComponent<CinemachineInputProvider>();
        }

        UpdateCameraSense();

        //OK SO ALSO, we can use applicationSettings.sensitivity to alter the actual camera value
        //

    }

    private void UpdateHealthStatus() {
        //whenever player's health gets updated, pass that change into rso....
        //get player's health and store it into rso
        if (rso) {
            rso.SetHealth(m_player.GetHealth());
        }

        UpdateHealthBar();
    }

    private void UpdateHydrationStatus() {
        if (m_player) {
            rso.SetHydration(m_player.GetHydration());
        }
    }

    public void SubscribeToDeath() { 
        
    }

    public void SetPlayerStats() {
        if (m_player) {
            m_player.SetStats(rso.health, rso.maxHealth, rso.maxStamina, rso.hydration); //also hydration hehe
        }
    }

    public void DeathHealth() {
        if (rso) {
            rso.SetHealth(rso.maxHealth);
        }
    }

    private void UpdateHealthBar() {
        //set the image to match with the percentage of health etc.
        if (healthBar && rso)
            healthBar.transform.localScale = new Vector3(rso.GetHealth() * 0.98f, healthBar.transform.localScale.y, healthBar.transform.localScale.z);
    }

    private void UpdateStaminaBar()
    {
        //set the image to match with the percentage of health etc.
        if (staminaBar && rso)
            staminaBar.transform.localScale = new Vector3(m_player.GetStamina() * 0.98f, staminaBar.transform.localScale.y, staminaBar.transform.localScale.z);
    }

    public Transform GetPlayerTransform() {
        if (m_player)
            return m_player.transform;

        Debug.Log("Human not found");

        return null;
    }

    private Transform DesiredTarget() {

        Transform nearestTarget = null;
        float nearestDistance = 100.0f;
        float furthestDistance = 100.0f;

        if (Target.targetPool != null)
        {

            foreach (Transform t in Target.targetPool)
            {
                //check angle between camera forward and 

                //i mean, really ur looking for closest target to center, followed by closest to range and/or in range
                Vector3 viewPortPoint = m_camera.WorldToViewportPoint(t.position);

                Vector2 viewPortLateral = new Vector2(viewPortPoint.x, viewPortPoint.y);

                float viewPortDistance = viewPortPoint.z; //this is how far away they are from the frustrum

                float targetViewDistance = Vector2.Distance((Vector2.one * 0.5f), viewPortLateral);

                //additional logic required for when a target is closer to player but further from center...
                //IDK YET, A DOUBLE MIN CHECK??
                //LIKE HERE ARE two values: closest to center AND closest to player
                //whoever is the min in both...
                //no idk, like track the closest and if it's within a certain angle from camera than it becomes the desired target

                if (targetViewDistance < nearestDistance) {
                    nearestDistance = targetViewDistance;
                    nearestTarget = t;
                }

                //this tracks the closest target yeh? and this should supersede the above set as long as the target is within a certain range
                //you can have a target closer but have it be not the center object so as long as the viewPortPoint is within this range, set it...
                if (viewPortDistance < furthestDistance) {
                    furthestDistance = viewPortDistance;

                    //Gonna focus on fixing the camera Lock before testing this
                    //if (targetViewDistance < 0.1f) //this says that this object is the closest to player, but it only becomes the desired Target if it's within a certain range from the center
                    //    nearestTarget = t;
                }
            }


        }

        return nearestTarget;

    }

    private Vector3 RaycastTarget() {
        if (Physics.Raycast(m_camera.transform.position, m_camera.transform.forward, out RaycastHit hitInfo, Mathf.Infinity))
        {
            //Debug.Log(hitInfo.collider.name);
            Debug.DrawRay(m_camera.transform.position, m_camera.transform.forward * 20000.0f, Color.cyan);
            return hitInfo.point;
        }
        else {
            //Debug.Log("Not hitting");
            return m_camera.transform.forward * 200000.0f;
        }
            
    }

    public bool IsActing() { return m_player.IsActing(); }

    //UHM sort them into new pool or return nearest upon calculation
    //
    private void ReturnNearestCenter(List<Transform> targets) {
        
    }

    private Transform GetDesiredTarget() {

        //so this is called the moment you try and focus
        //this is where we sort and return middle index...
        //scrolling through targets is done after this too, where the targets are sorted, but since it pulls from the same static target pool, that means targets no longer in view are not accessible...
        //new targets entering are added to the end though, and are not sorted..
        //so before a flick, re-sort OK



        if (Target.targetPool != null) {


            Target.targetPool.Sort(Target.dc);

            return DesiredTarget();
        }

        return null;
    }

    private Transform GetNextTarget(int n) {

        if (Target.targetPool != null)
        {

            Target.targetPool.Sort(Target.dc);

            //find the index of the current target...
            int desiredIndex = Target.targetPool.IndexOf(dTarget) + n;
            desiredIndex = Mathf.Clamp(desiredIndex, 0, Target.targetPool.Count - 1);


            return Target.targetPool[desiredIndex];


        }

        return null;
    }

    //NOTE: TaregetFind should always return the first target in line of targetPoolSorted
    //A seperate function checking for input will cycle through targetPoolSorted
    private void TargetFind() {

        if(playerCam)
            cameraAngle = playerCam.m_YAxis.Value;

        //grab a target
        if (m_input.Player.Focus.WasPressedThisFrame())
            dTarget = GetDesiredTarget();

        //While focus button is pressed...
        if (m_input.Player.Focus.IsPressed()) {

            //find angle of target relative to character and use those angles to auto-adjust target to center of camera

            //check if the current target is in the target pool

            //if it's not, or it gets removed, then dTarget becomes null

            Transform prevTarget = dTarget;

            //Scroll through targets LEFT/-1 and RIGHT/+1
            if (m_input.UI.ScrollLeft.WasPressedThisFrame()) {
                dTarget = GetNextTarget(-1);

                if (!dTarget.Equals(prevTarget))
                {
                    //succeful, play sound
                    if (targetUI.TryGetComponent<SoundController>(out SoundController sc))
                    {
                        sc.PlaySound(0);
                    }
                }
            }

            if (m_input.UI.ScrollRight.WasPressedThisFrame())
            {
                dTarget = GetNextTarget(1);

                if (!dTarget.Equals(prevTarget))
                {
                    //succeful, play sound
                    if (targetUI.TryGetComponent<SoundController>(out SoundController sc))
                    {
                        sc.PlaySound(0);
                    }
                }
            }

            //TEMP?: Enabled Target UI
            if (dTarget)
            {
                TargetUIEnabler(true);
            }
            else {
                TargetUIEnabler(false);

                if (playerCam)
                {
                    playerCam.m_XAxis.m_MaxSpeed = 150.0f;
                    playerCam.m_YAxis.m_MaxSpeed = 1.0f;
                }
            }

        }
        else {
            //not focusing, just make sure target is off...

            //TEMP: Hard-code axis 'sensitivity'/'speed' values to restore user control (zero-ing these out makes it so camera can't be moved by player so it stays locked to target/target angle
            if (playerCam) {
                playerCam.m_XAxis.m_MaxSpeed = 150.0f;
                playerCam.m_YAxis.m_MaxSpeed = 1.0f;
            }
            //ENDTEMP

            dTarget = null;

            TargetUIEnabler(false);
        }

    }

    [SerializeField]
    float cameraActualAngle;

    [SerializeField]
    Vector2 camMinMax;

    private void AdjustCameraToTarget() {
        if (dTarget)
        {

            Vector2 lateralDesiredTarget = new Vector2(dTarget.position.x, dTarget.position.z);
            Vector2 lateralPlayerPos = new Vector2(m_player.transform.position.x, m_player.transform.position.z);

            targetAngle = Vector2.SignedAngle(lateralDesiredTarget - lateralPlayerPos, Vector2.up);

            //as you approach the 'back' of an object, it goes from 179 to -179 depending on the side
            //the total difference is 2 * 180 - the amount from 179 to 0 and then from 0 to -179
            //as you approach 180, find some way to keep the directionality instead of going around the circle??

            //verticalAngle = Mathf.Cos(Vector3.SignedAngle(m_player.transform.position - dTarget.position, Vector3.up, Vector3.up) * Mathf.Deg2Rad);

            //verticalAngle = Vector3.Angle(m_camera.transform.position - dTarget.position, m_camera.transform.forward);

            //Vector3 camPosition = m_player.transform.position + (Vector3.up * 2.0f) - (m_player.transform.forward * 0.5f);
            Vector3 camPosition = m_player.transform.position + Vector3.up;
            Debug.DrawRay(camPosition, dTarget.position - camPosition, Color.cyan);


            verticalAngle = Vector3.Angle(dTarget.position - camPosition, Vector3.up);

            float minAngle = 75.0f;
            float maxAngle = 140.0f;
            
            //take the actual angle and clamp right?
            cameraActualAngle = Mathf.Clamp01(((verticalAngle - minAngle) / (maxAngle - minAngle)) + 0.085f);

            if (playerCam)
            {

                //playerCam.m_XAxis.m_MaxSpeed = 0.0f;
                //playerCam.m_YAxis.m_MaxSpeed = 0.0f;
                
                //TEMP: Just checking min max
                camMinMax = new Vector2(playerCam.m_YAxis.m_MinValue, playerCam.m_YAxis.m_MinValue);
                Debug.Log(camMinMax);
                //playerCam.m_YAxis.m_MaxSpeed = 0.0f;

                //playerCam.m_Orbits[0].;

                //playerCam.m_XAxis.Value = targetAngle;

                playerCam.m_XAxis.Value = Mathf.LerpAngle(playerCam.m_XAxis.Value, targetAngle, Time.deltaTime * 15.0f);
                //m_camera.transform.LookAt(dTarget, Vector3.up);
                //playerCam.m_YAxis.Value = cameraActualAngle;
                playerCam.m_YAxis.Value = Mathf.LerpAngle(playerCam.m_YAxis.Value, cameraActualAngle, Time.deltaTime * 15.0f);

                //is there a way to say hey, here is the world position required to get an object to be center on the screen?
                //screenToWorldPoint
                //returns a point in the world that aligns with a value on the screen.

            }

        }
    }

    private void UpdateDesiredTarget() {
        //SOOO check if dTarget is in the pool
        //if it's not, remove dTarget too

        if (dTarget) {
            if (!Target.targetPool.Contains(dTarget)) {
                dTarget = null;
            }
        }
    }

    private void FocusCamera() {
        if (m_input.Player.Focus.IsPressed())
        {
            TargetUIEnabler(true);
            if (playerCam) {
                Vector3 originalOffset = playerCam.GetComponent<CinemachineCameraOffset>().m_Offset;

                playerCam.GetComponent<CinemachineCameraOffset>().m_Offset = new Vector3(originalOffset.x, originalOffset.y, 1.5f);
            }
        }
        else {
            TargetUIEnabler(false);
            if (playerCam) {
                Vector3 originalOffset = playerCam.GetComponent<CinemachineCameraOffset>().m_Offset;
                playerCam.GetComponent<CinemachineCameraOffset>().m_Offset = new Vector3(originalOffset.x, originalOffset.y, 0.0f);
            }
        }
    }

    private void TargetUIEnabler(bool t) {

        //this just sets the targetting boolean
        //only need to enable when off
        if (targetUI)
        {
            if (targetUI.TryGetComponent<Animator>(out Animator anim))
            {
                anim.SetBool("targetting", t);
            }

            if (targetUI.TryGetComponent<RectTransform>(out RectTransform rect))
            {
                //set rect position to match world position to screen of target...
                //if(dTarget) //NOTE: this code here sets the ui position to meet the target
                //    rect.position = m_camera.WorldToScreenPoint(dTarget.position);
            }
        }

        //if()

    }

    private void EnableTargetCamera() {
        //only enable target camera is there is a target. no target, disable
        if (dTarget){
            if (targetCam)
            {
                targetCam.LookAt = dTarget;

                targetCam.gameObject.SetActive(true);
            }
        }
        else {
            if (targetCam)
                targetCam.gameObject.SetActive(false);
        }
    }

    private void DisableTargetUI() {
        if (targetUI.activeInHierarchy)
        {
            if (targetUI.TryGetComponent<Animator>(out Animator anim))
            {
                anim.SetTrigger("exit");
            }
            else
                targetUI.SetActive(false);
        }
        //else, already inactive
    }
}
