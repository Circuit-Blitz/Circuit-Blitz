using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private Transform NetworkOptionsUI;
    private void Awake() {
        playBtn.onClick.AddListener(() => {
            gameObject.SetActive(false);
            NetworkOptionsUI.gameObject.SetActive(true);
        });
        quitBtn.onClick.AddListener(() => {
            QuitGame();
        });
    }
    private void QuitGame() {
        Application.Quit();
    }
}
