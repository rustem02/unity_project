using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRTraining.Lobby
{
    /// <summary>
    /// Lobby menu: start training. Wired to a UI Button.
    /// </summary>
    public class LobbyMenuController : MonoBehaviour
    {
        [SerializeField] private Button startTrainingButton;
        [SerializeField] private string trainingSceneName = "Training";

        private void OnEnable()
        {
            if (startTrainingButton != null)
                startTrainingButton.onClick.AddListener(StartTraining);
        }

        private void OnDisable()
        {
            if (startTrainingButton != null)
                startTrainingButton.onClick.RemoveListener(StartTraining);
        }

        public void StartTraining()
        {
            SceneManager.LoadScene(trainingSceneName);
        }
    }
}
