using System;

namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// A class that implements IUsesMarsSceneEvaluation gains the ability to request evaluation of the query scene
    /// </summary>
    public interface IUsesMarsSceneEvaluation{ }

    public delegate MarsSceneEvaluationRequestResponse RequestSceneEvaluationDelegate(Action onEvaluationComplete = null);

    static class IUsesMarsSceneEvaluationMethods
    {
        public static RequestSceneEvaluationDelegate RequestSceneEvaluation { get; internal set; }
        public static Action<MarsSceneEvaluationMode> SetEvaluationMode { get; internal set; }
    }

    public static class IUsesMarsSceneEvaluationExtensionMethods
    {
        /// <summary>
        /// Request that the results of all active queries be recalculated.
        /// </summary>
        /// <param name="onEvaluationComplete">
        /// A callback executed when the evaluation triggered by the request has completed
        /// </param>
        /// <returns>An enum describing the system response to the request</returns>
        public static MarsSceneEvaluationRequestResponse RequestSceneEvaluation(this IUsesMarsSceneEvaluation caller,
            Action onEvaluationComplete = null)
        {
            return IUsesMarsSceneEvaluationMethods.RequestSceneEvaluation(onEvaluationComplete);
        }

        /// <summary>
        /// Set the scheduling mode for evaluating the MARS scene.
        /// Changing the mode to EvaluateOnInterval will queue an evaluation.
        /// </summary>
        /// <param name="mode">The mode to set</param>
        public static void SetEvaluationMode(this IUsesMarsSceneEvaluation caller, MarsSceneEvaluationMode mode)
        {
            IUsesMarsSceneEvaluationMethods.SetEvaluationMode(mode);
        }
    }
}
