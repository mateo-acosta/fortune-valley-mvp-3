using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Shared color palette for QuestionMaster answer buttons and overlays.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestionUITheme", menuName = "Fortune Valley/Question UI Theme")]
    public class QuestionUITheme : ScriptableObject
    {
        [SerializeField] private Color _idleColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color _correctColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color _wrongColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        public Color IdleColor => _idleColor;
        public Color CorrectColor => _correctColor;
        public Color WrongColor => _wrongColor;
    }
}
