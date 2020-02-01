using NUnit.Framework;
using UnityEngine.PrefabHandles.Tests;
using UnityEngine.PrefabHandles.Tests.Contexts;
using UnityEngine.SceneManagement;

namespace UnityEditor.PrefabHandles.Tests
{
    public sealed class EditorDummyContext : EditorHandleContext, ITestContext
    {
        public int handleCount
        {
            get { return handles.Count; }
        }
    }

    public sealed class EditorHandleLifecycleTests : HandleLifecycleTests<EditorDummyContext>
    {
        int m_SceneRootCount;
        
        [Test]
        public void CreatingHandle_DoesNotChangeActiveScene()
        {
            using (var template = new DummyHandleTemplate(DummyHandleTemplate.Template.BasicInteractiveHandle))
            {
                var prevRootCount = SceneManager.GetActiveScene().rootCount;
                var handle = context.CreateHandle(template.gameObject);

                Assert.AreEqual(prevRootCount, SceneManager.GetActiveScene().rootCount);

                context.DestroyHandle(handle);
            }
        }

        [Test]
        public void CreatingHandle_DoesNotDirtyActiveScene()
        {
            using (var template = new DummyHandleTemplate(DummyHandleTemplate.Template.BasicInteractiveHandle))
            {
                Assume.That(SceneManager.GetActiveScene().isDirty, Is.False);
                var handle = context.CreateHandle(template.gameObject);
               
                Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
                context.DestroyHandle(handle);
            }
        }

        [Test]
        public void DestroyingHandle_DoesNotChangeActiveScene()
        {
            using (var template = new DummyHandleTemplate(DummyHandleTemplate.Template.BasicInteractiveHandle))
            {
                var prevRootCount = SceneManager.GetActiveScene().rootCount;
                var handle = context.CreateHandle(template.gameObject);
                var newCount = SceneManager.GetActiveScene().rootCount;
                Assume.That(newCount == prevRootCount);

                context.DestroyHandle(handle);
                Assert.AreEqual(newCount, SceneManager.GetActiveScene().rootCount);
            }
        }

        [Test]
        public void DestroyingHandle_DoesNotDirtyActiveScene()
        {
            using (var template = new DummyHandleTemplate(DummyHandleTemplate.Template.BasicInteractiveHandle))
            {
                Assume.That(SceneManager.GetActiveScene().isDirty, Is.False);
                var handle = context.CreateHandle(template.gameObject);
                context.DestroyHandle(handle);

                Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);
            }
        }
    }
}