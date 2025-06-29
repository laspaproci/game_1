using UnityEngine;
using WebSocketSharp;
using System;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;

    void Start()
    {
        // Adres ws://adres-serwera:port
        ws = new WebSocket("ws://127.0.0.1:8080");

       
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("Połączono z serwerem WebSocket");
            ws.Send("Hello from Unity!");
        };

        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Otrzymano wiadomość: " + e.Data);
            
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError("Błąd WebSocket: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Połączenie zamknięte: " + e.Reason);
        };

       
        ws.ConnectAsync();
    }

    void Update()
    {
        
        if (Time.frameCount % 60 == 0 && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send("Ping at " + Time.time);
        }
    }

    void OnDestroy()
    {
      
        if (ws != null && ws.ReadyState == WebSocketState.Open)
            ws.Close();
    }
}