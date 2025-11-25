using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Minimal dialogue panel for tutorial instructions.
    /// Displays text, an optional sprite, and a Continue button.
    /// </summary>
    public class TutorialDialoguePanel : MonoBehaviour
    {
        public GameObject root;
        public TMP_Text bodyText;
        public Image displayImage;
        public TMP_Text hintText;
        public string hintMessage = "Press Enter to continue";

        public bool IsVisible => root != null && root.activeSelf;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            SetVisible(false);
            SetContinueHintVisible(false);
        }

        public void ShowStep(string text, Sprite sprite)
        {
            SetVisible(true);

            if (bodyText != null)
            {
                bodyText.text = text;
            }

            if (displayImage != null)
            {
                if (sprite != null)
                {
                    displayImage.sprite = sprite;
                    displayImage.enabled = true;
                }
                else
                {
                    displayImage.sprite = null;
                    displayImage.enabled = false;
                }
            }

            SetContinueHintVisible(false);
        }

        public void SetContinueHintVisible(bool show)
        {
            if (hintText == null) return;
            hintText.text = hintMessage;
            hintText.gameObject.SetActive(show);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }
    }
}

