using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Temporary NetworkManager stub for mobile-only builds
    /// Replace with Mirror.NetworkManager when networking is needed
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        [Header("Network Configuration")]
        public string networkAddress = "localhost";
        public int maxConnections = 100;
        public bool autoStartServerBuild = false;
        public bool dontDestroyOnLoad = true;

        // Stub properties to match Mirror.NetworkManager
        public static NetworkManager singleton { get; private set; }
        public bool isNetworkActive { get; private set; }
        public bool isServer { get; private set; }
        public bool isClient { get; private set; }

        // Events to match Mirror's interface
        public static event Action OnServerReady;
        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;

        void Awake()
        {
            if (singleton == null)
            {
                singleton = this;
                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
            }
            else if (singleton != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Stub methods to match Mirror's interface
        public virtual void StartHost() 
        { 
            Debug.Log("NetworkManager: StartHost (stub)");
            isServer = true;
            isClient = true;
            isNetworkActive = true;
        }

        public virtual void StartServer() 
        { 
            Debug.Log("NetworkManager: StartServer (stub)");
            isServer = true;
            isNetworkActive = true;
        }

        public virtual void StartClient() 
        { 
            Debug.Log("NetworkManager: StartClient (stub)");
            isClient = true;
            isNetworkActive = true;
        }

        public virtual void StopHost() 
        { 
            Debug.Log("NetworkManager: StopHost (stub)");
            isServer = false;
            isClient = false;
            isNetworkActive = false;
        }

        public virtual void StopServer() 
        { 
            Debug.Log("NetworkManager: StopServer (stub)");
            isServer = false;
            if (!isClient) isNetworkActive = false;
        }

        public virtual void StopClient() 
        { 
            Debug.Log("NetworkManager: StopClient (stub)");
            isClient = false;
            if (!isServer) isNetworkActive = false;
        }

        // Virtual methods for overriding in derived classes
        public virtual void OnStartHost() { }
        public virtual void OnStartServer() { }
        public virtual void OnStartClient() { }
        public virtual void OnStopHost() { }
        public virtual void OnStopServer() { }
        public virtual void OnStopClient() { }
        public virtual void OnServerConnect(object conn) { }
        public virtual void OnServerDisconnect(object conn) { }
        public virtual void OnServerReady(object conn) { }
        public virtual void OnServerAddPlayer(object conn) { }
        public virtual void OnClientConnect() { }
        public virtual void OnClientDisconnect() { }
        public virtual void OnClientError(Exception exception) { }
    }
}

// Stub NetworkBehaviour for compatibility
namespace Mirror
{
    public class NetworkBehaviour : MonoBehaviour
    {
        public uint netId { get; protected set; }
        public bool isServer => NetworkManager.singleton?.isServer ?? false;
        public bool isClient => NetworkManager.singleton?.isClient ?? false;
        public bool hasAuthority { get; protected set; } = true;

        protected virtual void Awake() { }
        protected virtual void Start() { }

        // Stub RPC methods
        [System.AttributeUsage(System.AttributeTargets.Method)]
        public class ClientRpcAttribute : System.Attribute { }

        [System.AttributeUsage(System.AttributeTargets.Method)]
        public class ServerAttribute : System.Attribute { }

        [System.AttributeUsage(System.AttributeTargets.Field)]
        public class SyncVarAttribute : System.Attribute
        {
            public string hook { get; set; }
        }
    }

    public static class NetworkServer
    {
        public static bool active => NetworkManager.singleton?.isServer ?? false;
        public static void Spawn(GameObject obj) { }
        public static void Destroy(GameObject obj) { }
    }

    public static class NetworkClient
    {
        public static bool active => NetworkManager.singleton?.isClient ?? false;
    }
}
