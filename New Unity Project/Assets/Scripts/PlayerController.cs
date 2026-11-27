using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode; 
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    [Header("Aiming")]
    [SerializeField] private Transform playerVisual;
    [SerializeField] private float aimAngleOffset = -90f;

    private PlayerNetworkData networkData;

    private CameraFollow cameraFollow;
    [SerializeField] Camera playerCam;
    private Vector2 aimInput;

    private float moveSpeed = 64f;
    [SerializeField] private GameObject localPlayerMarker;
    //[SerializeField] TMP_InputField playerNameInput;
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;

    private PlayerInputs controls;

    private ChatManager chat;
    private Vector2 moveInput;
    private float shiftInput;
    private Rigidbody2D rb;

    [SerializeField] SpawnManager spawn;

    private int playerBeingSpectated;

    private void Awake()
    {
        controls = new PlayerInputs();
        rb = GetComponent<Rigidbody2D>();
        networkData = GetComponent<PlayerNetworkData>();
        playerCam = FindFirstObjectByType<Camera>();
        spawn = FindFirstObjectByType<SpawnManager>();
        chat = FindFirstObjectByType<ChatManager>();
        cameraFollow = GameObject.FindFirstObjectByType<Camera>().GetComponent<CameraFollow>();
    }

    public override void OnNetworkSpawn() 
    {
        if (!IsOwner)
        {
            playerVisual.GetComponent<SpriteRenderer>().color = Color.pink;
            return;
        }

        transform.position = spawn.GetRandomSpawnPoint().position;

        GameObject playerIcon1ButtonObj = GameObject.FindWithTag("Player Icon 1");
        GameObject playerIcon2ButtonObj = GameObject.FindWithTag("Player Icon 2");
        GameObject playerIcon3ButtonObj = GameObject.FindWithTag("Player Icon 3");

        playerIcon1ButtonObj.SetActive(false);
        playerIcon2ButtonObj.SetActive(false);
        playerIcon3ButtonObj.SetActive(false);

        playerVisual.GetComponent<SpriteRenderer>().color = Color.red;

        localPlayerMarker.SetActive(true);

        //playerNameInput = GameObject.Find("Player name").GetComponent<TMP_InputField>();


        GameObject hostButtonObj = GameObject.FindWithTag("Host");
        GameObject joinButtonObj = GameObject.FindWithTag("Join");
        GameObject playerNameInput = GameObject.FindWithTag("Player name");

        if (hostButtonObj != null)
        {
            hostButton = hostButtonObj.GetComponent<Button>();
            hostButtonObj.SetActive(false);
        }

        else
        {
            Debug.LogError("Host Button is null for some reason");
        }

        if (joinButtonObj != null)
        {
            joinButton = joinButtonObj.GetComponent<Button>();
            joinButtonObj.SetActive(false);
        }

        else
        {
            Debug.LogError("Join Button is null for some reason");
        }

        if (playerNameInput != null)
        {
            playerNameInput.gameObject.SetActive(false);
        }

        else
        {
            Debug.LogError("player name input is null for some reason");
        }

        if (networkData.IsAlive.Value == true)
        {
            cameraFollow.SetTarget(transform);
        }
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

        if (!networkData.IsAlive.Value)
        {
            moveInput = Vector2.zero;

            List<PlayerNetworkData> livingPlayers = new List<PlayerNetworkData>();

            PlayerNetworkData[] players = FindObjectsByType<PlayerNetworkData>(FindObjectsSortMode.None);

            for (int i = 0; i < players.Length; i++)
            {
                //Only adds a player to the list if they are ready 
                if (players[i].IsAlive.Value == true)
                {
                    livingPlayers.Add(players[i]);
                }

                //Ignores players that aren't alive 
                else
                {
                    continue;
                }
            }

            if(controls.Player.nextPlayer.WasPressedThisFrame() && playerBeingSpectated < livingPlayers.Count)
            {
                playerBeingSpectated++;
            }

            else
            {
                Debug.Log("No more players to spectate");
            }

            if(controls.Player.previousPlayer.WasPressedThisFrame() && playerBeingSpectated > 0)
            {
                playerBeingSpectated--;
            }

            else
            {
                Debug.Log("Spectating first living player in list");
            }

            cameraFollow.SetTarget(livingPlayers[playerBeingSpectated].gameObject.transform);

            return;
        }

        else
        {
            cameraFollow.SetTarget(transform);
            playerBeingSpectated = 0;
        }

        moveInput = controls.Player.Move.ReadValue<Vector2>();

        aimInput = controls.Player.Aim.ReadValue<Vector2>();

        shiftInput = controls.Player.Sprint.ReadValue<float>();

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

        if (shiftInput > 0)
        {
            moveSpeed = 78;
            //UnityEngine.Debug.Log("Sprinting " + moveSpeed);
        }

        else
        {
            moveSpeed = 64;
        }

        if(controls.Player.ShowHideChat.WasPressedThisFrame())
        {
            chat.HideOrShowChat();
        }

        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f);
        transform.position += movement * moveSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        if (!networkData.IsAlive.Value)
            return;

        Vector2 nextPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }

}