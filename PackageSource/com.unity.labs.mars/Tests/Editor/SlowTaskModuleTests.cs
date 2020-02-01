#if UNITY_EDITOR
using NUnit.Framework;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Labs.MARS.Tests
{
    public class SlowTaskModuleTests
    {
        SlowTaskModule m_Module;
        Action m_DefaultAction;

        const float k_DefaultInterval = 0.5f;
        const int k_DesiredFrameRate = 60;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Module = SlowTaskModule.instance;
            m_Module.LoadModule();
            m_Module.ClearTasks();
            m_DefaultAction = () => { };
            m_Module.taskFrameRate = k_DesiredFrameRate;
        }

        [SetUp]
        public void BeforeEach()
        {
            m_Module.ClearTasks();
            m_Module.OnBehaviorUpdate();
            m_Module.OnMarsUpdate();
        }

        void RegisterMultipleDummyTasks(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var dummyNumber = i + 1;
                Action action = () => { DummyAction(dummyNumber); };
                Assert.True(m_Module.AddSlowTask(action, k_DefaultInterval));
            }
            m_Module.OnBehaviorUpdate();
        }

        void RegisterMultipleDummyMarsTimeTasks(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var dummyNumber = i + 1;
                Action action = () => { DummyAction(dummyNumber); };
                Assert.True(m_Module.AddMarsTimeSlowTask(action, k_DefaultInterval));
            }

            m_Module.OnMarsUpdate();
        }

        public int DummyAction(int i)
        {
            return i + 1;
        }

        [Test]
        public void RegisterSingleTask()
        {
            Assert.True(m_Module.AddSlowTask(m_DefaultAction, k_DefaultInterval));
            Assert.AreEqual(0, m_Module.tasks.Count); // Actual add happens in OnBehaviorUpdate
            m_Module.OnBehaviorUpdate();

            var tasks = m_Module.tasks;
            Assert.AreEqual(1, tasks.Count);
            SlowTaskModule.SlowTask task;
            Assert.True(tasks.TryGetValue(m_DefaultAction, out task));
            Assert.AreEqual(k_DefaultInterval, task.sleepTime);
        }

        [Test]
        public void RegisterSingleMarsTimeTask()
        {
            Assert.True(m_Module.AddMarsTimeSlowTask(m_DefaultAction, k_DefaultInterval));
            Assert.AreEqual(0, m_Module.MarsTimeTasks.Count); // Actual add happens in OnMarsUpdate
            m_Module.OnMarsUpdate();

            var tasks = m_Module.MarsTimeTasks;
            Assert.AreEqual(1, tasks.Count);
            SlowTaskModule.SlowTask task;
            Assert.True(tasks.TryGetValue(m_DefaultAction, out task));
            Assert.AreEqual(k_DefaultInterval, task.sleepTime);
        }

        [Test]
        public void ReregisteringIdenticalTaskDoesNothing()
        {
            Assert.True(m_Module.AddSlowTask(m_DefaultAction, k_DefaultInterval));
            m_Module.OnBehaviorUpdate();
            Assert.False(m_Module.AddSlowTask(m_DefaultAction, k_DefaultInterval));
            Assert.AreEqual(1, m_Module.tasks.Count);
        }

        [Test]
        public void ReregisteringIdenticalMarsTimeTaskDoesNothing()
        {
            Assert.True(m_Module.AddMarsTimeSlowTask(m_DefaultAction, k_DefaultInterval));
            m_Module.OnMarsUpdate();
            Assert.False(m_Module.AddMarsTimeSlowTask(m_DefaultAction, k_DefaultInterval));
            Assert.AreEqual(1, m_Module.MarsTimeTasks.Count);
        }

        [Test]
        public void RemoveTask()
        {
            m_Module.AddSlowTask(m_DefaultAction, k_DefaultInterval);
            m_Module.OnBehaviorUpdate();
            Assert.True(m_Module.RemoveSlowTask(m_DefaultAction));
            Assert.AreEqual(1, m_Module.tasks.Count); // We don't remove immediately, so it should still be there
            m_Module.OnBehaviorUpdate();
            Assert.AreEqual(0, m_Module.tasks.Count); // OnBehaviorUpdate cleans out tasks, so it should be gone now
        }

        [Test]
        public void RemoveMarsTimeTask()
        {
            m_Module.AddMarsTimeSlowTask(m_DefaultAction, k_DefaultInterval);
            m_Module.OnMarsUpdate();
            Assert.True(m_Module.RemoveMarsTimeSlowTask(m_DefaultAction));
            Assert.AreEqual(1, m_Module.MarsTimeTasks.Count); // We don't remove immediately, so it should still be there
            m_Module.OnMarsUpdate();
            Assert.AreEqual(0, m_Module.MarsTimeTasks.Count); // OnMarsUpdate cleans out tasks, so it should be gone now
        }

        [Test]
        public void ReplaceTask()
        {
            const float newInterval = 2f;
            Assert.True(m_Module.AddSlowTask(m_DefaultAction, k_DefaultInterval));
            m_Module.OnBehaviorUpdate();
            Assert.AreEqual(1, m_Module.tasks.Count);
            Assert.True(m_Module.AddSlowTask(m_DefaultAction, newInterval, true));
            m_Module.OnBehaviorUpdate();
            var tasks = m_Module.tasks;
            Assert.AreEqual(1, tasks.Count);
            Assert.AreEqual(newInterval, tasks.First().Value.sleepTime);
        }

        [Test]
        public void ReplaceMarsTimeTask()
        {
            const float newInterval = 2f;
            Assert.True(m_Module.AddMarsTimeSlowTask(m_DefaultAction, k_DefaultInterval));
            m_Module.OnMarsUpdate();
            Assert.AreEqual(1, m_Module.MarsTimeTasks.Count);
            Assert.True(m_Module.AddMarsTimeSlowTask(m_DefaultAction, newInterval, true));
            m_Module.OnMarsUpdate();
            var tasks = m_Module.MarsTimeTasks;
            Assert.AreEqual(1, tasks.Count);
            Assert.AreEqual(newInterval, tasks.First().Value.sleepTime);
        }

        [Test]
        public void ClearTasksRemovesAllTasks()
        {
            const int taskCount = 5;
            m_Module.ClearTasks();
            RegisterMultipleDummyTasks(taskCount);
            RegisterMultipleDummyMarsTimeTasks(taskCount);
            Assert.AreEqual(taskCount, m_Module.tasks.Count);
            Assert.AreEqual(taskCount, m_Module.MarsTimeTasks.Count);
            m_Module.ClearTasks();
            Assert.AreEqual(0, m_Module.tasks.Count);
            Assert.AreEqual(0, m_Module.MarsTimeTasks.Count);
        }

        [Test]
        public void RegisteringManyTasks_DelaysFirstExecutionAfterThreshold()
        {
            int taskCount = m_Module.incrementInterval + 1;
            RegisterMultipleDummyTasks(taskCount);

            var tasks = m_Module.tasks;
            Assert.AreEqual(taskCount, tasks.Count);
            var firstTask = tasks.First().Value;
            var lastTask = tasks.Last().Value;

            // Make sure a task past the threshold is registered with a start time later than the first
            Assert.Greater(lastTask.lastExecutionTime, firstTask.lastExecutionTime);
        }

        [Test]
        public void RegisteringManyMarsTimeTasks_DelaysFirstExecutionAfterThreshold()
        {
            int taskCount = m_Module.incrementInterval + 1;
            RegisterMultipleDummyMarsTimeTasks(taskCount);

            var tasks = m_Module.MarsTimeTasks;
            Assert.AreEqual(taskCount, tasks.Count);
            var firstTask = tasks.First().Value;
            var lastTask = tasks.Last().Value;

            // Make sure a task past the threshold is registered with a start time later than the first
            Assert.Greater(lastTask.lastExecutionTime, firstTask.lastExecutionTime);
        }

        [UnityTest]
        public IEnumerator ForceRunAllTasks_ResetsAllExecutionTimes()
        {
            const int taskCount = 10;
            RegisterMultipleDummyTasks(taskCount);
            Assert.AreEqual(taskCount,  m_Module.tasks.Count);
            yield return null; // Allow a frame to pass so time changes

            m_Module.ForceRunAllTasks();

            foreach (var kvp in  m_Module.tasks)
                Assert.AreEqual(kvp.Value.lastExecutionTime, Time.time);
        }
    }
}
#endif
