using System;
using System.Collections;
using System.IO;
using System.Text;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NCalc.Tests
{
    [TestClass]
    public class AntlrCompatibilityTests
    {
        [TestMethod]
        public void ParserReturnScope_WithLegacyObjectProperties_DoesStoreTokensAndTree()
        {
            CommonToken start = new CommonToken(1, "start");
            CommonToken stop = new CommonToken(2, "stop");
            CommonTree tree = new CommonTree(start);
            RuleReturnScope ruleReturnScope = new RuleReturnScope();
            ParserRuleReturnScope parserRuleReturnScope = new ParserRuleReturnScope();
            TreeRuleReturnScope treeRuleReturnScope = new TreeRuleReturnScope();

            ruleReturnScope.Start = start;
            ruleReturnScope.Stop = stop;
            ruleReturnScope.Tree = tree;
            parserRuleReturnScope.Start = start;
            treeRuleReturnScope.Start = tree;

            Assert.AreSame(start, ruleReturnScope.Start);
            Assert.AreSame(stop, ruleReturnScope.Stop);
            Assert.AreSame(tree, ruleReturnScope.Tree);
            Assert.IsNull(ruleReturnScope.Template);
            Assert.AreSame(start, parserRuleReturnScope.Start);
            Assert.AreSame(tree, treeRuleReturnScope.Start);
        }

        [TestMethod]
        public void Token_WithLegacyConstants_DoesExposeCurrentRuntimeValues()
        {
            AssertLegacyTokenValue(-1, Token.EOF);
            AssertLegacyTokenValue(0, Token.INVALID_TOKEN_TYPE);
            AssertLegacyTokenValue(1, Token.EOR_TOKEN_TYPE);
            AssertLegacyTokenValue(2, Token.DOWN);
            AssertLegacyTokenValue(3, Token.UP);
            AssertLegacyTokenValue(0, Token.DEFAULT_CHANNEL);
            AssertLegacyTokenValue(99, Token.HIDDEN_CHANNEL);
            AssertLegacyTokenValue(4, Token.MIN_TOKEN_TYPE);
            Assert.IsNotNull(Token.EOF_TOKEN);
            Assert.IsNotNull(Token.INVALID_TOKEN);
            Assert.IsNotNull(Token.SKIP_TOKEN);
        }

        [TestMethod]
        public void AntlrFileStream_WithUtf8File_DoesLoadTextAndSourceName()
        {
            string filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

            try
            {
                File.WriteAllText(filePath, "abc", Encoding.UTF8);

                ANTLRFileStream stream = new ANTLRFileStream(filePath, Encoding.UTF8);

                Assert.AreEqual(filePath, stream.SourceName);
                Assert.AreEqual(3, stream.Count);
                Assert.AreEqual("abc", stream.Substring(0, 3));
                LegacyAntlrFileStream derivedStream = new LegacyAntlrFileStream();

                derivedStream.Load(filePath, Encoding.UTF8);
                Assert.IsNull(derivedStream.SourceName);
                Assert.AreEqual(3, derivedStream.Count);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [TestMethod]
        public void CollectionUtils_WithLegacyInputs_DoesFormatCollections()
        {
            Hashtable dictionary = new Hashtable();
            dictionary["one"] = 1;
            ArrayList list = new ArrayList();
            list.Add("a");
            list.Add("b");

            string dictionaryText = Antlr.Runtime.Collections.CollectionUtils.DictionaryToString(dictionary);
            string listText = Antlr.Runtime.Collections.CollectionUtils.ListToString(list);

            StringAssert.Contains(dictionaryText, "one=1");
            Assert.AreEqual("[a, b]", listText);
            Assert.AreEqual("null", Antlr.Runtime.Collections.CollectionUtils.DictionaryToString(null));
            Assert.AreEqual("null", Antlr.Runtime.Collections.CollectionUtils.ListToString(null));
        }

        [TestMethod]
        public void HashList_WithDictionaryOperations_DoesPreserveInsertionAndLookup()
        {
            Antlr.Runtime.Collections.HashList hashList = new Antlr.Runtime.Collections.HashList(2);
            hashList.Add("first", 1);
            hashList["second"] = 2;

            Assert.AreEqual(2, hashList.Count);
            Assert.IsTrue(hashList.Contains("first"));
            Assert.AreEqual(2, hashList["second"]);
            Assert.IsFalse(hashList.IsReadOnly);
            Assert.IsFalse(hashList.IsFixedSize);
            Assert.IsFalse(hashList.IsSynchronized);
            Assert.IsNotNull(hashList.SyncRoot);
            CollectionAssert.Contains((ICollection)hashList.Keys, "first");
            CollectionAssert.Contains((ICollection)hashList.Values, 2);

            DictionaryEntry[] entries = new DictionaryEntry[2];
            hashList.CopyTo(entries, 0);
            CollectionAssert.Contains(new[] { entries[0].Key, entries[1].Key }, "first");
            CollectionAssert.Contains(new[] { entries[0].Key, entries[1].Key }, "second");

            IDictionaryEnumerator enumerator = hashList.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            hashList.Remove("first");
            Assert.IsFalse(hashList.Contains("first"));
            hashList.Clear();
            Assert.AreEqual(0, hashList.Count);
        }

        [TestMethod]
        public void StackList_WithPushPopOperations_DoesBehaveLikeStack()
        {
            Antlr.Runtime.Collections.StackList stackList = new Antlr.Runtime.Collections.StackList();
            stackList.Push("first");
            stackList.Push("second");

            Assert.AreEqual("second", stackList.Peek());
            Assert.AreEqual("second", stackList.Pop());
            Assert.AreEqual("first", stackList.Pop());
            Assert.Throws<InvalidOperationException>(delegate { stackList.Peek(); });
        }

        [TestMethod]
        public void Stats_WithLegacyHelpers_DoesCalculateValuesAndWriteReport()
        {
            int[] values = { 1, 2, 3 };
            string reportPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

            try
            {
                Assert.AreEqual(2d, Antlr.Runtime.Misc.Stats.Avg(values));
                Assert.AreEqual(3, Antlr.Runtime.Misc.Stats.Max(values));
                Assert.AreEqual(1, Antlr.Runtime.Misc.Stats.Min(values));
                Assert.AreEqual(6, Antlr.Runtime.Misc.Stats.Sum(values));
                Assert.AreEqual(1d, Antlr.Runtime.Misc.Stats.Stddev(values));
                Assert.AreEqual(0d, Antlr.Runtime.Misc.Stats.Avg(new int[0]));
                Assert.AreEqual(0, Antlr.Runtime.Misc.Stats.Max(new int[0]));
                Assert.AreEqual(0, Antlr.Runtime.Misc.Stats.Min(new int[0]));
                Assert.AreEqual(0d, Antlr.Runtime.Misc.Stats.Stddev(new[] { 1 }));

                Antlr.Runtime.Misc.Stats.WriteReport(reportPath, "report");
                Assert.AreEqual("report", File.ReadAllText(Antlr.Runtime.Misc.Stats.GetAbsoluteFileName(reportPath)));
            }
            finally
            {
                if (File.Exists(reportPath))
                {
                    File.Delete(reportPath);
                }
            }
        }

        [TestMethod]
        public void ErrorManager_WithLegacyErrors_DoesThrowInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(delegate { Antlr.Runtime.Misc.ErrorManager.Error("error"); });
            Assert.Throws<InvalidOperationException>(delegate { Antlr.Runtime.Misc.ErrorManager.InternalError("error"); });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                delegate { Antlr.Runtime.Misc.ErrorManager.InternalError("error", new ApplicationException("inner")); });

            Assert.IsInstanceOfType<ApplicationException>(exception.InnerException);
        }

        [TestMethod]
        public void TreeCompatibility_WithLegacyTypes_DoesExposeTreeHelpers()
        {
            CommonTree tree = new CommonTree(new CommonToken(1, "root"));
            CommonTokenStream tokens = new CommonTokenStream(new NCalcLexer(new ANTLRStringStream("1 + 2")));
            IToken start = new CommonToken(1, "start");
            IToken stop = new CommonToken(2, "stop");
            Antlr.Runtime.CommonErrorNode errorNode = new Antlr.Runtime.CommonErrorNode(
                tokens,
                start,
                stop,
                new RecognitionException(tokens));
            Antlr.Runtime.Tree.UnBufferedTreeNodeStream stream = new Antlr.Runtime.Tree.UnBufferedTreeNodeStream(tree);

            stream.HasUniqueNavigationNodes = true;

            Assert.IsNotNull(Antlr.Runtime.Tree.Tree.INVALID_NODE);
            Assert.IsNotNull(errorNode);
            Assert.IsTrue(stream.HasUniqueNavigationNodes);
            Assert.IsGreaterThanOrEqualTo(0, stream.Size());
            Assert.AreEqual(-1, stream.Index());
        }

        [TestMethod]
        public void RewriteRuleElementStream_WithLegacyGenericType_DoesExposeStreamMembers()
        {
            LegacyRewriteRuleElementStream stream = new LegacyRewriteRuleElementStream(new CommonTreeAdaptor());

            stream.Add("value");

            Assert.IsTrue(stream.HasNext());
            Assert.AreEqual(1, stream.Size());
            Assert.AreEqual("value", stream.ReadNext());
        }

        [TestMethod]
        public void SynPredPointer_WithLegacyDelegate_DoesInvoke()
        {
            bool invoked = false;
            SynPredPointer synPredPointer = delegate { invoked = true; };

            synPredPointer();

            Assert.IsTrue(invoked);
        }

        private sealed class LegacyRewriteRuleElementStream : RewriteRuleElementStream<object>
        {
            public LegacyRewriteRuleElementStream(ITreeAdaptor adaptor)
                : base(adaptor, "legacy")
            {
            }

            public object ReadNext()
            {
                return _Next();
            }

            protected override object Dup(object element)
            {
                return element;
            }
        }

        private sealed class LegacyAntlrFileStream : ANTLRFileStream
        {
        }

        private static void AssertLegacyTokenValue(int expectedValue, int actualValue)
        {
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}
