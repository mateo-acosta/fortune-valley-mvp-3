using FortuneValley.Core;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Pure C# walker over an <see cref="IntroScriptSO"/>. Tracks the
    /// current step index without caring how steps advance; the controller
    /// decides when to call <see cref="Advance"/> based on the step kind
    /// (tap for Dialog, matching GameEvent for WaitForX, Skip button).
    /// </summary>
    public class TutorialSequenceMachine
    {
        private readonly IntroScriptSO _script;
        private int _index = -1;

        public TutorialSequenceMachine(IntroScriptSO script)
        {
            _script = script;
        }

        public int CurrentIndex => _index;
        public TutorialStepSO CurrentStep => _script != null ? _script.GetStep(_index) : null;
        public int StepCount => _script != null ? _script.StepCount : 0;
        public bool IsStarted => _index >= 0;
        public bool IsComplete => _script != null && _index >= _script.StepCount;

        /// <summary>
        /// Move to step 0. Safe to call multiple times; resets progress.
        /// </summary>
        public void Start() => _index = 0;

        /// <summary>
        /// Advance one step. Clamps at StepCount so repeated Advance calls
        /// past the end do not push the index into negative territory on
        /// subsequent Start invocations.
        /// </summary>
        public void Advance()
        {
            if (!IsStarted) return;
            if (IsComplete) return;
            _index++;
        }

        /// <summary>
        /// Skip directly to the end. Used by the Skip Tutorial button.
        /// </summary>
        public void JumpToEnd() => _index = StepCount;

        /// <summary>
        /// Reset to pre-start state.
        /// </summary>
        public void Reset() => _index = -1;
    }
}
