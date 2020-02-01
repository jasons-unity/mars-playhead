// This file is automatically generated - DO NOT EDIT MANUALLY!
using System.Collections.Generic;

namespace Unity.Labs.MARS.Query
{
    public partial class QueryResult
    {
        ///<summary>Values for all matched queries' results of type Boolean</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, System.Boolean>> SemanticTagValues = 
            new Dictionary<QueryResult, Dictionary<string, System.Boolean>>();

        ///<summary>Values for all matched queries' results of type Int32</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, System.Int32>> IntValues = 
            new Dictionary<QueryResult, Dictionary<string, System.Int32>>();

        ///<summary>Values for all matched queries' results of type Single</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, System.Single>> FloatValues = 
            new Dictionary<QueryResult, Dictionary<string, System.Single>>();

        ///<summary>Values for all matched queries' results of type String</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, System.String>> StringValues = 
            new Dictionary<QueryResult, Dictionary<string, System.String>>();

        ///<summary>Values for all matched queries' results of type Pose</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, UnityEngine.Pose>> PoseValues = 
            new Dictionary<QueryResult, Dictionary<string, UnityEngine.Pose>>();

        ///<summary>Values for all matched queries' results of type Vector2</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, UnityEngine.Vector2>> Vector2Values = 
            new Dictionary<QueryResult, Dictionary<string, UnityEngine.Vector2>>();

        ///<summary>Values for all matched queries' results of type Vector3</summary>
        internal static readonly Dictionary<QueryResult, Dictionary<string, UnityEngine.Vector3>> Vector3Values = 
            new Dictionary<QueryResult, Dictionary<string, UnityEngine.Vector3>>();

        ///<summary>Get the value for a trait of type System.Boolean in this query</summary>
        public bool TryGetTrait(string traitName, out System.Boolean value)
        {
            if(!SemanticTagValues.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        ///<summary>Get the value for a trait of type System.Int32 in this query</summary>
        public bool TryGetTrait(string traitName, out System.Int32 value)
        {
            if(!IntValues.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        ///<summary>Get the value for a trait of type System.Single in this query</summary>
        public bool TryGetTrait(string traitName, out System.Single value)
        {
            if(!FloatValues.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        ///<summary>Get the value for a trait of type System.String in this query</summary>
        public bool TryGetTrait(string traitName, out System.String value)
        {
            if(!StringValues.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        ///<summary>Get the value for a trait of type UnityEngine.Pose in this query</summary>
        public bool TryGetTrait(string traitName, out UnityEngine.Pose value)
        {
            if(!PoseValues.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        ///<summary>Get the value for a trait of type UnityEngine.Vector2 in this query</summary>
        public bool TryGetTrait(string traitName, out UnityEngine.Vector2 value)
        {
            if(!Vector2Values.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        ///<summary>Get the value for a trait of type UnityEngine.Vector3 in this query</summary>
        public bool TryGetTrait(string traitName, out UnityEngine.Vector3 value)
        {
            if(!Vector3Values.TryGetValue(this, out var typeValues))
            {
                value = default;
                return false;
            }

            if(!typeValues.TryGetValue(traitName, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

        public void SetTrait(string traitName, System.Boolean value)
        {
            if(!SemanticTagValues.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.SemanticTagResults.Get();
                SemanticTagValues[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        public void SetTrait(string traitName, System.Int32 value)
        {
            if(!IntValues.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.IntResults.Get();
                IntValues[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        public void SetTrait(string traitName, System.Single value)
        {
            if(!FloatValues.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.FloatResults.Get();
                FloatValues[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        public void SetTrait(string traitName, System.String value)
        {
            if(!StringValues.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.StringResults.Get();
                StringValues[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        public void SetTrait(string traitName, UnityEngine.Pose value)
        {
            if(!PoseValues.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.PoseResults.Get();
                PoseValues[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        public void SetTrait(string traitName, UnityEngine.Vector2 value)
        {
            if(!Vector2Values.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.Vector2Results.Get();
                Vector2Values[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        public void SetTrait(string traitName, UnityEngine.Vector3 value)
        {
            if(!Vector3Values.TryGetValue(this, out var typeValues))
            {
                typeValues = Pools.Vector3Results.Get();
                Vector3Values[this] = typeValues;
            }

            typeValues[traitName] = value;
        }

        static void Clear(QueryResult result)
        {
            if(SemanticTagValues.ContainsKey(result))
                SemanticTagValues.Remove(result);

            if(IntValues.ContainsKey(result))
                IntValues.Remove(result);

            if(FloatValues.ContainsKey(result))
                FloatValues.Remove(result);

            if(StringValues.ContainsKey(result))
                StringValues.Remove(result);

            if(PoseValues.ContainsKey(result))
                PoseValues.Remove(result);

            if(Vector2Values.ContainsKey(result))
                Vector2Values.Remove(result);

            if(Vector3Values.ContainsKey(result))
                Vector3Values.Remove(result);
        }
    }
}
