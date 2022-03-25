using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks // changed from MonoBehaviour to MonoBehaviourPunCallbacks to connect and use some of the functions over the photon network or for controlling players over the network
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStore;
    private Vector2 mouseInput; //vector2 is a datatype having 2 values x and y

    public bool invertLook;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement; //vector3 is datatype having 3 values x, y and z

    public CharacterController charCon;

    public float jumpForce = 12f, gravityMod = 2.5f;

    private Camera cam;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    //public float timeBetweenShots = .1f;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 10f, /*heatPerShot = 1f, */ coolRate = 4f, overHeatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim; // animation variavle to initialize animator variable
    public GameObject playerModel; // to turn off or on player model for my self view
    public Transform modelGunPoint, gunHolder;

    public Material[] allSkins;

    public float adsSpeed=5f;
    public Transform adsOutPoint, adsInPoint;

    public AudioSource footstepLow, footstepFast;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;
        UIController.instance.weaponTempSlider.maxValue = maxHeat;

        //previously before it was set for default gun till gun switch mechanism not synchronise over the network, so if every player start the game with different gun then this default gun should not be their
        //SwitchGun(); // commented because of above reason, every where will be comented out where ever SwitchGun() is called
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        // we no need of this when the game is going to connect with photon network, not from there all the player will spawn automaticaly, previously below 3 lines of code spawn the player individually
        // the working of below code has been coded in PlayerSpawner.cs
        // Transform newTrans = SpawnManager.instance.GetSpawnPoint();
        // transform.position = newTrans.position;
        // transform.rotation = newTrans.rotation;

        currentHealth = maxHealth;

        // below 2 lines commented after we are initializing player model with animation. so that helath moniter and gun temperature will not mis behave over the network
        //UIController.instance.healthSlider.maxValue = maxHealth; // maximum range of slider i.e 100
        //UIController.instance.healthSlider.value = currentHealth; // current status of slider i.e current=max

        if (photonView.IsMine)
        {
            playerModel.SetActive(false);

            // delared here because it will check only the single and current instance of the player and initialize it
            UIController.instance.healthSlider.maxValue = maxHealth; // maximum range of slider i.e 100
            UIController.instance.healthSlider.value = currentHealth; // current status of slider i.e current=max
        }
        else
        {
            // to set the gun positio exact same as the parent game object
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {

            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            //moving camera sidewise
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            //moving camera up and down
            verticalRotStore += mouseInput.y;
            verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);
            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            //moving the player
            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;

                if(!footstepFast.isPlaying && moveDir != Vector3.zero)
                {
                    footstepFast.Play();
                    footstepLow.Stop();
                }
            }
            else
            {
                activeMoveSpeed = moveSpeed;

                if (!footstepFast.isPlaying && moveDir != Vector3.zero)
                {
                    footstepFast.Stop();
                    footstepLow.Play();
                }
            }

            if(moveDir==Vector3.zero || !isGrounded)
            {
                footstepFast.Stop();
                footstepLow.Stop();
            }

            float yVel = movement.y;

            movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
            movement.y = yVel;
            if (charCon.isGrounded)
            {
                movement.y = 0f;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }
            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charCon.Move(movement * Time.deltaTime);

            //deactivating muzzle flash
            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;
                if (muzzleCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }
            }
            if (!overHeated)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;
                    if (shotCounter <= 0)
                    {
                        shoot();
                    }
                }
                heatCounter -= coolRate * Time.deltaTime;
            }
            else
            {
                heatCounter -= overHeatCoolRate * Time.deltaTime;
                if (heatCounter <= 0)
                {
                    overHeated = false;
                    UIController.instance.oveHeatedImage.gameObject.SetActive(false);
                }
            }

            if (heatCounter < 0)
            {
                heatCounter = 0f;
            }
            UIController.instance.weaponTempSlider.value = heatCounter;

            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;
                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;
                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

            //selecting weapon with number keys
            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    //SwitchGun();
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
                }
            }

            // declering the animator variable according to the script, "grounded" and "speed" are two animation variable which will do accordingly to the animator
            anim.SetBool("grounded", isGrounded);
            anim.SetFloat("speed", moveDir.magnitude); // magnitude is used because to play the character in any one direction(magnitude) whereever it is moving, it always a +ve value


            if (Input.GetMouseButton(1))
            {
                cam.fieldOfView = Mathf.Lerp (cam.fieldOfView, allGuns[selectedGun].adsZoom, adsSpeed*Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 40f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);
            }


            //to make the cursor appear after clicking esc
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0) && !UIController.instance.optionsScreen.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }

    private void shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
           // Debug.Log("We hit " + hit.collider.gameObject.name);

            //if player find the gameobject with tag Player than it will show the hited player
            if (hit.collider.gameObject.tag == "Player")
            {
              //  Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);

                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                //added another argument as gun damage amount to the DealDamage function
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shotDamage,PhotonNetwork.LocalPlayer.ActorNumber); // this will run for every version or joined player in the game whoever is hit by any player
            }
            else
            {
                //.002f is like jugad so that unity will not confuse between the texture of two objects
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 5f);
            }
        }
        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if(heatCounter>=maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;

            UIController.instance.oveHeatedImage.gameObject.SetActive(true);
        }
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();
    }

    [PunRPC] // this PunRPC helps the function to run in every system for every different individual players to do whatever scripting have been done in below function
    public void DealDamage(string damager,int damageAmount,int actor)//actor is added while adding events
    {
        TakeDamage(damager,damageAmount,actor);
    }

    public void TakeDamage(string damager, int damageAmount,int actor)
    {
        if (photonView.IsMine)
        {
            // Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);
            // these two statements is before IsMine
            // gameObject.SetActive(false);

            currentHealth -= damageAmount;

            if (currentHealth <= 0)
            {
                currentHealth = 0;

                PlayerSpawner.instance.Die(damager);

                MatchManager.instance.UpdateStatSend(actor, 0, 1);//added while adding events
            }

            UIController.instance.healthSlider.value = currentHealth;
        }
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                cam.transform.position = viewPoint.position;
                cam.transform.rotation = viewPoint.rotation;
            }
            else
            {
                cam.transform.position = MatchManager.instance.mapCamPoint.position;
                cam.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }
        }
    }

    void SwitchGun()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC] // this will help to syncronize the individual gun switch machanism to every system over the network whoever have joined the room
    public void SetGun(int gunToSwitchTo)
    {
        if (gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }
}
