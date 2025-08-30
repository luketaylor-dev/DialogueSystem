using DialogueSystem.Enums;
using UnityEngine;

namespace DialogueSystem
{
    public class DSDialogueActionHandler : MonoBehaviour
    {
        public static DSDialogueActionHandler Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void HandleDialogueAction(DSActionType actionType, string actionParameter)
        {
            switch (actionType)
            {
                case DSActionType.None:
                    break;
                case DSActionType.OpenShop:
                    OpenShop(actionParameter);
                    break;
                case DSActionType.GiveItem:
                    GiveItem(actionParameter);
                    break;
                case DSActionType.StartQuest:
                    StartQuest(actionParameter);
                    break;
                case DSActionType.EndQuest:
                    EndQuest(actionParameter);
                    break;
                case DSActionType.ChangeScene:
                    ChangeScene(actionParameter);
                    break;
                case DSActionType.PlaySound:
                    PlaySound(actionParameter);
                    break;
                case DSActionType.TriggerEvent:
                    TriggerEvent(actionParameter);
                    break;
                case DSActionType.Custom:
                    HandleCustomAction(actionParameter);
                    break;
                default:
                    Debug.LogWarning($"Unknown action type: {actionType}");
                    break;
            }
        }

        private void OpenShop(string shopId)
        {
            Debug.Log($"Opening shop: {shopId}");
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OpenShop();
            }
            else
            {
                Debug.LogWarning("ShopManager not found! Make sure it's in the scene.");
            }
        }

        private void GiveItem(string itemId)
        {
            Debug.Log($"Giving item: {itemId}");
        }

        private void StartQuest(string questId)
        {
            Debug.Log($"Starting quest: {questId}");
        }

        private void EndQuest(string questId)
        {
            Debug.Log($"Ending quest: {questId}");
        }

        private void ChangeScene(string sceneName)
        {
            Debug.Log($"Changing to scene: {sceneName}");
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene '{sceneName}': {e.Message}");
            }
        }

        private void PlaySound(string soundId)
        {
            Debug.Log($"Playing sound: {soundId}");
        }

        private void TriggerEvent(string eventId)
        {
            Debug.Log($"Triggering event: {eventId}");
        }

        private void HandleCustomAction(string customAction)
        {
            Debug.Log($"Handling custom action: {customAction}");
        }
    }
}
