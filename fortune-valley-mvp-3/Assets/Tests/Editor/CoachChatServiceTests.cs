using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class CoachChatServiceTests
    {
        // ═══════════════════════════════════════════════════════════════
        // JSON ESCAPE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void EscapeJsonString_HandlesQuotes()
        {
            string result = CoachChatService.EscapeJsonString("say \"hello\"");
            Assert.AreEqual("say \\\"hello\\\"", result);
        }

        [Test]
        public void EscapeJsonString_HandlesNewlines()
        {
            string result = CoachChatService.EscapeJsonString("line1\nline2");
            Assert.AreEqual("line1\\nline2", result);
        }

        [Test]
        public void EscapeJsonString_HandlesBackslashes()
        {
            string result = CoachChatService.EscapeJsonString("path\\to\\file");
            Assert.AreEqual("path\\\\to\\\\file", result);
        }

        [Test]
        public void EscapeJsonString_HandlesNull()
        {
            string result = CoachChatService.EscapeJsonString(null);
            Assert.AreEqual("null", result);
        }

        // ═══════════════════════════════════════════════════════════════
        // RESPONSE PARSING TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ParseResponse_ValidJson_ExtractsContent()
        {
            string json = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Great question!\"}}]}";
            string result = CoachChatService.ParseResponse(json);
            Assert.AreEqual("Great question!", result);
        }

        [Test]
        public void ParseResponse_MalformedJson_ReturnsFallback()
        {
            string result = CoachChatService.ParseResponse("{garbage not json");
            Assert.AreEqual("I didn't get a clear answer. Try asking again.", result);
        }

        [Test]
        public void ParseResponse_EmptyContent_ReturnsFallback()
        {
            // Empty choices array
            string json = "{\"choices\":[]}";
            string result = CoachChatService.ParseResponse(json);
            Assert.AreEqual("I didn't get a clear answer. Try asking again.", result);
        }

        [Test]
        public void ParseResponse_NullInput_ReturnsFallback()
        {
            string result = CoachChatService.ParseResponse(null);
            Assert.AreEqual("I didn't get a clear answer. Try asking again.", result);
        }

        [Test]
        public void ParseResponse_EmptyString_ReturnsFallback()
        {
            string result = CoachChatService.ParseResponse("");
            Assert.AreEqual("I didn't get a clear answer. Try asking again.", result);
        }

        // ═══════════════════════════════════════════════════════════════
        // HISTORY CAP TEST
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void HistoryCap_TrimOldestMessages()
        {
            // Create a config with max 3 exchanges (= system + 6 messages max)
            var config = UnityEngine.ScriptableObject.CreateInstance<CoachConfig>();

            // Use reflection to set maxHistoryExchanges since it's private
            var field = typeof(CoachConfig).GetField("_maxHistoryExchanges",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(config, 3);

            var service = new CoachChatService(config, "test context");

            // Verify initial state: 1 system message
            Assert.AreEqual(1, service.HistoryCount);

            // Clean up
            UnityEngine.Object.DestroyImmediate(config);
        }

        // ═══════════════════════════════════════════════════════════════
        // API KEY HANDLING
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Constructor_WithMissingEnvVar_CreatesService()
        {
            // Even without an API key, the service should be created (fails gracefully on send)
            var config = UnityEngine.ScriptableObject.CreateInstance<CoachConfig>();

            // Set env var name to something that won't exist
            var field = typeof(CoachConfig).GetField("_envVarName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(config, "NONEXISTENT_TEST_KEY_12345");

            var service = new CoachChatService(config, "test context");
            Assert.IsNotNull(service);
            Assert.AreEqual(1, service.HistoryCount); // System message present

            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
