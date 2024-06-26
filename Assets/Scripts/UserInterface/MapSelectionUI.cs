using UnityEngine;
using UnityEngine.UI;

public class MapSelectionUI : MonoBehaviour
{
    [SerializeField] private Button[] MapBtns;
    [SerializeField] private Transform GameOptions;
    private void Awake() {
        foreach (Button btn in MapBtns)
        {
            btn.onClick.AddListener(() => {
                gameObject.SetActive(false);
                GameOptions.gameObject.SetActive(true);
                ServerManager.Instance.SetMap("Scenes/Tracks/" + btn.name);
            });
        }
    }
}
