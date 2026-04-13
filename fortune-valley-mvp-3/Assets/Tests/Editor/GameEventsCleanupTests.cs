using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Guardrail for Issue 15A: every public static event on GameEvents MUST be nulled
    /// by ClearAllSubscriptions(). Forgotten entries leak subscriptions across scene loads.
    /// </summary>
    [TestFixture]
    public class GameEventsCleanupTests
    {
        [Test]
        public void ClearAllSubscriptions_NullsEveryPublicStaticEventField()
        {
            Type t = typeof(GameEvents);

            // NOTE: C# events compile to a private delegate field + public add/remove accessors.
            // The reflection target is the backing field (non-public, static).
            var fields = new List<FieldInfo>();
            foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public))
            {
                if (typeof(Delegate).IsAssignableFrom(f.FieldType))
                {
                    fields.Add(f);
                }
            }

            Assert.That(fields.Count, Is.GreaterThan(0), "No delegate fields found on GameEvents -- test setup is broken");

            // Seed every field with a non-null handler by subscribing via the event accessor.
            foreach (var e in t.GetEvents(BindingFlags.Public | BindingFlags.Static))
            {
                var addMethod = e.GetAddMethod();
                if (addMethod == null) continue;
                Delegate handler = BuildNoopHandler(e.EventHandlerType);
                addMethod.Invoke(null, new object[] { handler });
            }

            // Act.
            GameEvents.ClearAllSubscriptions();

            // Assert.
            var leaked = new List<string>();
            foreach (var f in fields)
            {
                var v = f.GetValue(null);
                if (v != null) leaked.Add(f.Name);
            }

            Assert.IsEmpty(leaked,
                "These GameEvents fields were not nulled by ClearAllSubscriptions: " + string.Join(", ", leaked));
        }

        // Construct a no-op delegate of the specified type. Works for Action and Action<...> variants.
        private static Delegate BuildNoopHandler(Type handlerType)
        {
            // DynamicMethod-free approach: match parameter list and return null.
            var invokeMethod = handlerType.GetMethod("Invoke");
            var paramTypes = Array.ConvertAll(invokeMethod.GetParameters(), p => p.ParameterType);

            // Use a tiny helper via MethodInfo.CreateDelegate. We need a static method that matches
            // the signature; the simplest general path is a DynamicMethod. Keep it in-test.
            var dm = new System.Reflection.Emit.DynamicMethod(
                "noop_" + Guid.NewGuid().ToString("N"),
                invokeMethod.ReturnType,
                paramTypes);
            var il = dm.GetILGenerator();
            if (invokeMethod.ReturnType != typeof(void))
            {
                // Should not happen for event handlers, but be safe.
                il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
            }
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return dm.CreateDelegate(handlerType);
        }
    }
}
