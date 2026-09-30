using System.Diagnostics;
using Unity.Netcode; 
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerController : NetworkBehaviour
{
    [Header("Aiming")]
    [SerializeField] private Transform playerVisual;
    [SerializeField] private float aimAngleOffset = -90f;

    private PlayerNetworkData networkData;
    [SerializeField] Camera playerCam;
    private Vector2 aimInput;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private GameObject localPlayerMarker;
    [SerializeField] TMP_InputField playerNameInput;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;

    private Vector3 Player1Spawn = new Vector3(246, 57, 0);
    private Vector3 Player2Spawn = new Vector3(790.1f, 508, 0);

    private PlayerInputs controls;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    private void Awake()
    {
        controls = new PlayerInputs();
        rb = GetComponent<Rigidbody2D>();
        //localPlayerMarker = this.transform.GetChild(1).gameObject;
        networkData = GetComponent<PlayerNetworkData>();
        playerCam = FindFirstObjectByType<Camera>();
    }

    public override void OnNetworkSpawn() 
    {
        UnityEngine.Debug.Log($"Local={NetworkManager.Singleton.LocalClientId}, " + $"Owner={OwnerClientId}, IsOwner={IsOwner}");

        if (!IsOwner)
        {
            playerVisual.GetComponent<SpriteRenderer>().color = Color.pink;
            return;
        }

        playerVisual.GetComponent<SpriteRenderer>().color = Color.red;

        UnityEngine.Debug.Log($"Before SetActive: {localPlayerMarker.activeSelf}");

        localPlayerMarker.SetActive(true);

        UnityEngine.Debug.Log(localPlayerMarker);

        UnityEngine.Debug.Log($"After SetActive: {localPlayerMarker.activeSelf}");

        UnityEngine.Debug.Log($"[{OwnerClientId}] Marker active: {localPlayerMarker.activeSelf}");

        playerNameInput = GameObject.FindFirstObjectByType<TMP_InputField>();

        if (OwnerClientId == 0)
        {
            transform.position = Player1Spawn;
        }
        else
        {
            transform.position = Player2Spawn;
        }

        GameObject hostButtonObj = GameObject.FindWithTag("Host");
        GameObject joinButtonObj = GameObject.FindWithTag("Join");

        if (hostButtonObj != null)
        {
            UnityEngine.Debug.Log(hostButtonObj.name);
            hostButton = hostButtonObj.GetComponent<Button>();
            hostButtonObj.SetActive(false);
        }

        else
        {
            UnityEngine.Debug.LogError("Host Button is null for some reason");
        }

        if (joinButtonObj != null)
        {
            UnityEngine.Debug.Log(joinButtonObj.name);
            joinButton = joinButtonObj.GetComponent<Button>();
            joinButtonObj.SetActive(false);
        }

        else
        {
            UnityEngine.Debug.LogError("Join Button is null for some reason");
        }

        playerNameInput.gameObject.SetActive(false);

        CameraFollow cameraFollow = GameObject.FindFirstObjectByType<Camera>().GetComponent<CameraFollow>(); 
        cameraFollow.SetTarget(transform);
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        moveInput = controls.Player.Move.ReadValue<Vector2>();

        aimInput = controls.Player.Aim.ReadValue<Vector2>();

        float distanceFromCamera = Mathf.Abs(playerCam.transform.position.z - transform.position.z);
        Vector3 mouseWorld = playerCam.ScreenToWorldPoint(new Vector3(aimInput.x, aimInput.y, distanceFromCamera));

        Vector2 direction = (Vector2)mouseWorld - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + aimAngleOffset;

        playerVisual.localRotation = Quaternion.Euler(0f, 0f, angle);

        networkData.SetFacingAngleRpc(angle);

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        //if (moveInput.sqrMagnitude > 0.01f) 
        //{ 
        //    float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg; 
        //    transform.rotation = Quaternion.Euler(0f, 0f, angle); 
        //}

        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f);
        transform.position += movement * moveSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        Vector2 nextPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }

}