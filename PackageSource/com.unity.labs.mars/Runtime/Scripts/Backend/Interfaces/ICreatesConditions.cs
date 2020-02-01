using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public interface ICreatesConditions : ICreatesConditionsBase
    {
        /// <summary>
        /// Creates new conditions and adds them to the gameobject
        /// </summary>
        /// <param name="go"> The game object to which the conditions should be added. </param>
        void CreateIdealConditions(GameObject go);

        /// <summary>
        /// Modifies a given condition to make it pass, if possible
        /// </summary>
        /// <param name="condition"> The condition to be modified. </param>
        void ConformCondition(ICondition condition);
    }
}
