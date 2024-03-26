using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectionUI : MonoBehaviour
{
    [SerializeField] private Button[] mapBtns;
    [SerializeField] private Transform GameOptions;
    private void Awake() {
        foreach (Button btn in mapBtns)
        {
            btn.onClick.AddListener(() => {
                gameObject.SetActive(false);
                GameOptions.gameObject.SetActive(true);
                ServerManager.Instance.SetMap("Scenes/Tracks/" + btn.name);
            });
        }
    }
}
