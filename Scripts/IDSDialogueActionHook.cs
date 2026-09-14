using DialogueSystem.Enums;

namespace DialogueSystem
{
    /// <summary>
    /// Lets a game decide how dialogue choice actions are applied without the dialogue system knowing about game types.
    /// </summary>
    public interface IDSDialogueActionHook
    {
        /// <returns>True if the game handled the action, which skips the built-in fallback behaviour.</returns>
        bool TryHandleAction(DSActionType actionType, string actionParameter);
    }
}
