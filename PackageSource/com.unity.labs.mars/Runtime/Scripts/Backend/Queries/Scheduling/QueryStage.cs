using UnityEngine.Profiling;

namespace Unity.Labs.MARS.Query
{
    abstract class QueryStage
    {
        public string Label;

        protected int m_FrameBudget;

        internal virtual int FrameBudget { get; set; }

        public bool IsComplete { get; internal set; }


        protected QueryStage(string label)
        {
            Label = label;
        }

        /// <summary>
        /// Perform some or all of the work for the stage.
        /// If all work is completed, Tick() must mark IsComplete true.
        /// </summary>
        public abstract void Tick();

        public abstract void Complete();
    }

    class QueryStage<T> : QueryStage
        where T : DataTransform
    {
        public readonly T Transformation;

        internal override int FrameBudget
        {
            get => m_FrameBudget;
            set
            {
                m_FrameBudget = value;
                Transformation.FrameBudget = value;
            }
        }

        protected QueryStage(string label, T transformation)
            : base(label)
        {
            Transformation = transformation;
        }

        public override void Tick()
        {
            Profiler.BeginSample(Label);
            Transformation.Tick();
            IsComplete = Transformation.IsComplete;
            Profiler.EndSample();
        }

        public override void Complete()
        {
            Profiler.BeginSample(Label);
            Transformation.Complete();
            IsComplete = true;
            Profiler.EndSample();
        }

        internal void CycleStart()
        {
            IsComplete = false;
            Transformation.OnCycleStart();
            Transformation.IsComplete = false;
        }
    }

    class QueryStage<T1, T2> : QueryStage
        where T1 : DataTransform
        where T2 : DataTransform
    {
        public readonly T1 Transformation1;
        public readonly T2 Transformation2;

        internal override int FrameBudget
        {
            get => m_FrameBudget;
            set
            {
                m_FrameBudget = value;
                Transformation1.FrameBudget = value;
                Transformation2.FrameBudget = value;
            }
        }

        protected QueryStage(string label, T1 transformation1, T2 transformation2)
            : base(label)
        {
            Transformation1 = transformation1;
            Transformation2 = transformation2;
        }

        public override void Tick()
        {
            Profiler.BeginSample(Label);

            if(!Transformation1.IsComplete)
                Transformation1.Tick();
            if(!Transformation2.IsComplete)
                Transformation2.Tick();

            IsComplete = Transformation1.IsComplete && Transformation2.IsComplete;
            Profiler.EndSample();
        }

        public override void Complete()
        {
            Profiler.BeginSample(Label);
            Transformation1.Complete();
            Transformation2.Complete();
            IsComplete = true;
            Profiler.EndSample();
        }

        internal void CycleStart()
        {
            IsComplete = false;
            Transformation1.IsComplete = false;
            Transformation2.IsComplete = false;
            Transformation1.OnCycleStart();
            Transformation2.OnCycleStart();
        }
    }

    class QueryStage<T1, T2, T3> : QueryStage
        where T1 : DataTransform
        where T2 : DataTransform
        where T3 : DataTransform
    {
        public readonly T1 Transformation1;
        public readonly T2 Transformation2;
        public readonly T3 Transformation3;

        internal override int FrameBudget
        {
            get => m_FrameBudget;
            set
            {
                m_FrameBudget = value;
                Transformation1.FrameBudget = value;
                Transformation2.FrameBudget = value;
                Transformation3.FrameBudget = value;
            }
        }

        protected QueryStage(string label, T1 transformation1, T2 transformation2, T3 transformation3)
            : base(label)
        {
            Transformation1 = transformation1;
            Transformation2 = transformation2;
            Transformation3 = transformation3;
        }

        public override void Tick()
        {
            Profiler.BeginSample(Label);

            if(!Transformation1.IsComplete)
                Transformation1.Tick();
            if(!Transformation2.IsComplete)
                Transformation2.Tick();
            if(!Transformation3.IsComplete)
                Transformation3.Tick();

            IsComplete = Transformation1.IsComplete && Transformation2.IsComplete && Transformation3.IsComplete;
            Profiler.EndSample();
        }

        public override void Complete()
        {
            Profiler.BeginSample(Label);
            Transformation1.Complete();
            Transformation2.Complete();
            Transformation3.Complete();
            IsComplete = true;
            Profiler.EndSample();
        }

        internal void CycleStart()
        {
            IsComplete = false;
            Transformation1.IsComplete = false;
            Transformation2.IsComplete = false;
            Transformation3.IsComplete = false;
            Transformation1.OnCycleStart();
            Transformation2.OnCycleStart();
            Transformation3.OnCycleStart();
        }
    }
}
