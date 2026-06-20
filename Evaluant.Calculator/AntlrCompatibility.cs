using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Antlr.Runtime.Tree;

namespace Antlr.Runtime
{
    /// <summary>
    /// Preserves ANTLR runtime type names that were embedded in the legacy NCalc
    /// package and were renamed or removed by the newer ANTLR runtime package.
    /// </summary>
    public class RuleReturnScope : ParserRuleReturnScope<IToken>
    {
        public new virtual object Start
        {
            get { return base.Start; }
            set { base.Start = (IToken)value; }
        }

        public new virtual object Stop
        {
            get { return base.Stop; }
            set { base.Stop = (IToken)value; }
        }

        public virtual object Tree { get; set; }

        public virtual object Template { get { return null; } }
    }

    public class ParserRuleReturnScope : RuleReturnScope
    {
    }

    public static class Token
    {
        public const int EOR_TOKEN_TYPE = TokenTypes.EndOfRule;
        public const int DOWN = TokenTypes.Down;
        public const int UP = TokenTypes.Up;
        public const int INVALID_TOKEN_TYPE = TokenTypes.Invalid;
        public const int DEFAULT_CHANNEL = TokenChannels.Default;
        public const int HIDDEN_CHANNEL = TokenChannels.Hidden;

        public static readonly int MIN_TOKEN_TYPE = TokenTypes.Min;
        public static readonly int EOF = TokenTypes.EndOfFile;
        public static readonly IToken EOF_TOKEN = new CommonToken(EOF);
        public static readonly IToken INVALID_TOKEN = new CommonToken(INVALID_TOKEN_TYPE);
        public static readonly IToken SKIP_TOKEN = Tokens.Skip;
    }

    public class CommonErrorNode : Tree.CommonErrorNode
    {
        public CommonErrorNode(ITokenStream input, IToken start, IToken stop, RecognitionException e)
            : base(input, start, stop, e)
        {
        }
    }

    public delegate void SynPredPointer();

#if NETSTANDARD2_0
    public class ANTLRFileStream : ANTLRStringStream
    {
        private string fileName;

        protected ANTLRFileStream()
        {
        }

        public ANTLRFileStream(string fileName)
        {
            this.fileName = fileName;
            Load(fileName, null);
        }

        public ANTLRFileStream(string fileName, Encoding encoding)
        {
            this.fileName = fileName;
            Load(fileName, encoding);
        }

        public override string SourceName { get { return fileName; } }

        public virtual void Load(string fileName, Encoding encoding)
        {
            string text = encoding == null ? File.ReadAllText(fileName) : File.ReadAllText(fileName, encoding);

            data = text.ToCharArray();
            n = data.Length;
            p = 0;
            Line = 1;
            CharPositionInLine = 0;
        }
    }
#endif
}

namespace Antlr.Runtime.Collections
{
    public class CollectionUtils
    {
        public static string DictionaryToString(IDictionary dict)
        {
            if (dict == null)
            {
                return "null";
            }

            List<string> entries = new List<string>();
            foreach (DictionaryEntry entry in dict)
            {
                entries.Add(entry.Key + "=" + entry.Value);
            }

            return "[" + string.Join(", ", entries.ToArray()) + "]";
        }

        public static string ListToString(IList coll)
        {
            if (coll == null)
            {
                return "null";
            }

            List<string> entries = new List<string>();
            foreach (object item in coll)
            {
                entries.Add(Convert.ToString(item));
            }

            return "[" + string.Join(", ", entries.ToArray()) + "]";
        }
    }

    public sealed class HashList : IDictionary
    {
        private readonly OrderedDictionary values;

        public HashList()
            : this(0)
        {
        }

        public HashList(int capacity)
        {
            values = new OrderedDictionary(capacity);
        }

        public object this[object key]
        {
            get { return values[key]; }
            set { values[key] = value; }
        }

        public ICollection Keys { get { return values.Keys; } }

        public ICollection Values { get { return values.Values; } }

        public bool IsReadOnly { get { return values.IsReadOnly; } }

        public bool IsFixedSize { get { return ((IDictionary)values).IsFixedSize; } }

        public int Count { get { return values.Count; } }

        public object SyncRoot { get { return ((ICollection)values).SyncRoot; } }

        public bool IsSynchronized { get { return ((ICollection)values).IsSynchronized; } }

        public void Add(object key, object value)
        {
            values.Add(key, value);
        }

        public void Clear()
        {
            values.Clear();
        }

        public bool Contains(object key)
        {
            return values.Contains(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public void Remove(object key)
        {
            values.Remove(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)values).CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }
    }

    public class StackList : List<object>
    {
        public void Push(object item)
        {
            Add(item);
        }

        public object Pop()
        {
            object item = Peek();
            RemoveAt(Count - 1);
            return item;
        }

        public object Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Stack is empty.");
            }

            return this[Count - 1];
        }
    }
}

namespace Antlr.Runtime.Misc
{
    public class ErrorManager
    {
        public static void InternalError(object error, Exception e)
        {
            throw new InvalidOperationException(Convert.ToString(error), e);
        }

        public static void InternalError(object error)
        {
            throw new InvalidOperationException(Convert.ToString(error));
        }

        public static void Error(object arg)
        {
            throw new InvalidOperationException(Convert.ToString(arg));
        }
    }

    public class Stats
    {
        public static double Avg(int[] values)
        {
            return values.Length == 0 ? 0 : (double)Sum(values) / values.Length;
        }

        public static int Max(int[] values)
        {
            if (values.Length == 0)
            {
                return 0;
            }

            int result = values[0];
            foreach (int value in values)
            {
                result = Math.Max(result, value);
            }

            return result;
        }

        public static int Min(int[] values)
        {
            if (values.Length == 0)
            {
                return 0;
            }

            int result = values[0];
            foreach (int value in values)
            {
                result = Math.Min(result, value);
            }

            return result;
        }

        public static int Sum(int[] values)
        {
            int result = 0;
            foreach (int value in values)
            {
                result += value;
            }

            return result;
        }

        public static double Stddev(int[] values)
        {
            if (values.Length <= 1)
            {
                return 0;
            }

            double average = Avg(values);
            double sumOfSquares = 0;
            foreach (int value in values)
            {
                double difference = value - average;
                sumOfSquares += difference * difference;
            }

            return Math.Sqrt(sumOfSquares / (values.Length - 1));
        }

        public static string GetAbsoluteFileName(string filename)
        {
            return Path.GetFullPath(filename);
        }

        public static void WriteReport(string filename, string data)
        {
            File.WriteAllText(GetAbsoluteFileName(filename), data);
        }
    }
}

namespace Antlr.Runtime.Tree
{
    public sealed class Tree
    {
        public static readonly ITree INVALID_NODE = new CommonTree(Antlr.Runtime.Token.INVALID_TOKEN);
    }

    public class TreeRuleReturnScope : Antlr.Runtime.RuleReturnScope
    {
        public override object Start { get; set; }

        public override object Stop { get; set; }
    }

    public class UnBufferedTreeNodeStream : BufferedTreeNodeStream
    {
        private readonly object legacyTreeSource;

        public UnBufferedTreeNodeStream(object tree)
            : base(tree)
        {
            legacyTreeSource = tree;
        }

        public UnBufferedTreeNodeStream(ITreeAdaptor adaptor, object tree)
            : base(adaptor, tree)
        {
            legacyTreeSource = tree;
        }

        public bool HasUniqueNavigationNodes
        {
            get { return UniqueNavigationNodes; }
            set { UniqueNavigationNodes = value; }
        }

        public int Size()
        {
            return CountTreeNodes(legacyTreeSource);
        }

        public new int Index()
        {
            return base.Index;
        }

        private static int CountTreeNodes(object node)
        {
            ITree tree = node as ITree;
            if (tree == null)
            {
                return node == null ? 0 : 1;
            }

            int count = 1;
            for (int childIndex = 0; childIndex < tree.ChildCount; childIndex++)
            {
                count += CountTreeNodes(tree.GetChild(childIndex));
            }

            return count;
        }
    }

    public abstract class RewriteRuleElementStream<T> : RewriteRuleElementStream
    {
        public RewriteRuleElementStream(ITreeAdaptor adaptor, string elementDescription)
            : base(adaptor, elementDescription)
        {
        }

        public RewriteRuleElementStream(ITreeAdaptor adaptor, string elementDescription, T oneElement)
            : base(adaptor, elementDescription, oneElement)
        {
        }

        public RewriteRuleElementStream(ITreeAdaptor adaptor, string elementDescription, IList<T> elements)
            : base(adaptor, elementDescription, new List<T>(elements))
        {
        }

        public RewriteRuleElementStream(ITreeAdaptor adaptor, string elementDescription, IList elements)
            : base(adaptor, elementDescription, elements)
        {
        }

        public void Add(T element)
        {
            base.Add(element);
        }

        public new bool HasNext()
        {
            return base.HasNext;
        }

        public int Size()
        {
            return Count;
        }

        protected object _Next()
        {
            return NextCore();
        }

        protected virtual object ToTree(T element)
        {
            return element;
        }

        protected override object ToTree(object element)
        {
            return ToTree((T)element);
        }
    }
}
