namespace Unity.Labs.MARS
{
    /// <summary>Options for when to evaluate the scene</summary>
    public enum MarsSceneEvaluationMode : byte
    {
        /// <summary>Don't evaluate until a request to do so is received</summary>
        WaitForRequest,
        /// <summary>Evaluate the scene regularly regardless of evaluation requests</summary>
        EvaluateOnInterval
    }
}
