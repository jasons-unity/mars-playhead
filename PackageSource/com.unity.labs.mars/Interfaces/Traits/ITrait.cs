namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// Describes different kinds of data applied on primitives in the MARS Backend
    /// </summary>
    public interface ITrait
    {
        string name { get; }
    }

    /// <summary>
    /// The actual typed trait interface for data - When needing more than just the name of a trait,
    /// users should be casting to this type, so that the value can be pulled out.
    /// </summary>
    /// <typeparam name="T">The type of data this trait contains</typeparam>
    public interface ITrait<out T> : ITrait
    {
        T value { get; }
    }
}
