
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HP Bar Prefab")]
    [Tooltip("Przypisz prefab z komponentem Slider, który będzie używany jako pasek HP")]  
    public Slider hpBarPrefab;

    [Header("UI Parent")]
    [Tooltip("RectTransform, w którym będą umieszczane paski HP")]  
    public RectTransform hpBarContainer;

    private Dictionary<ulong, Slider> bars = new Dictionary<ulong, Slider>();

 
private void Awake()
{
    // root-em scen check
    if (transform.parent != null)
    {
        // odłaczenie 
        transform.SetParent(null);
    }

    // Zachowanie miedzy scenami
    DontDestroyOnLoad(gameObject);

    // singleton 
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
}
    
    //pasek HP dla gracza 
    //
    public void RegisterPlayer(ulong clientId)
    {
        if (bars.ContainsKey(clientId)) return;
        Slider bar = Instantiate(hpBarPrefab, hpBarContainer);
        bar.maxValue = 100;
        bar.value = 100;
        bars.Add(clientId, bar);
    }

    
    /// Aktualizuje wartość paska HP
   
    public void UpdateHpDisplay(ulong clientId, int newHp)
    {
        if (bars.TryGetValue(clientId, out Slider bar))
        {
            bar.value = newHp;
        }
    }


    ///usunięcie paska 
    public void UnregisterPlayer(ulong clientId)
    {
        if (bars.TryGetValue(clientId, out Slider bar))
        {
            Destroy(bar.gameObject);
            bars.Remove(clientId);
        }
    }
}