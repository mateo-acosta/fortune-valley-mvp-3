using UnityEngine;

namespace FortuneValley.UI
{
    /// <summary>
    /// Inspector-tunable framing for a character model inside a render texture.
    /// Adjust position, facing, and scale in the Inspector — changes apply
    /// immediately in the Editor (no Play Mode needed).
    /// </summary>
    [ExecuteAlways]
    public class CharacterFraming : MonoBehaviour
    {
        [Header("Position in View")]
        [Tooltip("Horizontal offset (negative = left, positive = right)")]
        [SerializeField] private float _horizontalOffset = 0f;

        [Tooltip("Vertical offset (lower values move character down in frame)")]
        [SerializeField] private float _verticalOffset = -0.35f;

        [Tooltip("Depth offset (closer to / further from camera)")]
        [SerializeField] private float _depthOffset = 0f;

        [Header("Rotation")]
        [Tooltip("Y-axis facing angle (negative = face right, positive = face left)")]
        [Range(-180f, 180f)]
        [SerializeField] private float _facing = -20f;

        [Header("Scale")]
        [Tooltip("Uniform scale of the character model")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _uniformScale = 1f;

        private void OnEnable()
        {
            ApplyFraming();
        }

        // Called when any inspector value changes — gives live preview in Editor
        private void OnValidate()
        {
            ApplyFraming();
        }

        private void ApplyFraming()
        {
            transform.localPosition = new Vector3(_horizontalOffset, _verticalOffset, _depthOffset);
            transform.localEulerAngles = new Vector3(0f, _facing, 0f);
            transform.localScale = Vector3.one * _uniformScale;
        }
    }
}
