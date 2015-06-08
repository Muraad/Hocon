using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Nulands.HOCON
{
    using OGS.HOCON.Impl;
    using System.IO;

    // Forked from https://github.com/4agenda/OGS.HOCON

    public static partial class Hocon
    {
        public const string DefaultPathDelimeter = ".";

        public static HoconConfig ParseString(string configContent)
        {
            HoconConfig config = new HoconConfig();
            config.ReadFromString(configContent);
            return config;
        }

        public static HoconConfig ParseStream(Stream streamContent)
        {
            HoconConfig config = new HoconConfig();
            config.ReadFromStream(streamContent);
            return config;
        }
    }

    public class HoconConfig
    {
        private sealed class ConfigurationNode
        {
        }

        protected Dictionary<string, object> ConfigurationData = new Dictionary<string, object>();

        public delegate string ResolveConfigSourceHandler(string configSource);
        public event ResolveConfigSourceHandler ResolveConfigSource;

        public string PathDelimeter { get; set; }

        public HoconConfig()
        {
            PathDelimeter = Hocon.DefaultPathDelimeter;
        }
        public HoconConfig(ResolveConfigSourceHandler resolveConfigSource)
        {
            ResolveConfigSource += resolveConfigSource;
            PathDelimeter = Hocon.DefaultPathDelimeter;
        }

        public void Read(string configSource)
        {
            var reader = Createreader();
            reader.Read(configSource);
        }

        public void ReadFromString(string configContent)
        {
            var reader = Createreader();
            reader.ReadFromString(configContent);
        }

        public void ReadFromStream(Stream configStream)
        {
            var reader = Createreader();
            reader.ReadFromStream(configStream);
        }

        private Reader<ConfigurationNode> Createreader()
        {
            var reader = new Reader<ConfigurationNode>();
            reader.CreateOrUpdateValue += (path, value) => ConfigurationData[path] = value;
            reader.CreateOrUpdateNode += (path, node) => ConfigurationData[path] = node;
            reader.RemoveNode += path => ConfigurationData.Remove(path);
            reader.GetNodeOrValue += path => ConfigurationData.ContainsKey(path) ? ConfigurationData[path] : null;
            reader.GetNodesOrValues += path => ConfigurationData.Where(item => path == item.Key || item.Key.StartsWith(path + PathDelimeter)).ToArray();

            reader.ResolveSource += RiseResolveConfigSource;

            return reader;
        }

        public bool HasPath(string path)
        {
            return ConfigurationData.ContainsKey(path);
        }

        public bool HasValue(string path)
        {
            object value;

            return
                ConfigurationData.TryGetValue(path, out value) &&
                value != null &&
                value as ConfigurationNode == null;
        }

        public string GetString(string path, string defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToValue<string>(value);
        }

        public int GetInt(string path, int defaultValue = 0)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToValue<int>(value);
        }

        public decimal GetDecimal(string path, decimal defaultValue = 0.0m)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToValue<decimal>(value);
        }

        public bool GetBool(string path, bool defaultValue = false)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToValue<bool>(value);
        }

        public object GetValue(string path, object defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return value;
        }

        public T GetValue<T>(string path, T defaultValue = default(T))
        {
            Type type = typeof(T);
            if (!type.GetTypeInfo().IsValueType)
                throw new HoconConfigException("GetValue<T>: Invalid type, expected a value type, but: '{0}'", type.Name);

            return CastToValue<T>(GetValue(path, (object)defaultValue));
        }

        public List<object> GetValueList(string path, List<object> defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            var list = value as List<object>;
            if (list == null && value != null)
                throw new HoconConfigException("Invalid type, expected '{0}', but: '{1}'", typeof(List<object>), value.GetType());

            return list == null ? defaultValue : list.ToList();
        }

        public List<string> GetStringList(string path, List<string> defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToList(value, defaultValue);
        }

        public List<int> GetIntList(string path, List<int> defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToList(value, defaultValue);
        }

        public List<decimal> GetDecimalList(string path, List<decimal> defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToList(value, defaultValue);
        }

        public List<bool> GetBoolList(string path, List<bool> defaultValue = null)
        {
            object value;
            if (ConfigurationData.TryGetValue(path, out value) == false)
                return defaultValue;

            return CastToList(value, defaultValue);
        }

        private List<T> CastToList<T>(object source, List<T> defaultValue)
        {
            var list = source as List<object>;
            if (list == null && source != null)
                throw new HoconConfigException("Invalid type, expected '{0}', but: '{1}'", typeof(List<T>), source.GetType());

            return (list == null) ? (defaultValue ?? new List<T>()) : list.Cast<T>().ToList();
        }

        private T CastToValue<T>(object source)
        {
            if (source is T == false)
                throw new HoconConfigException("Invalid type, expected '{0}', but: '{1}'", typeof(T), source.GetType());

            return (T)source;
        }

        protected virtual string RiseResolveConfigSource(string configsource)
        {
            var handler = ResolveConfigSource;
            return (handler != null) ? handler(configsource) : string.Empty;
        }
    }

    public class HoconConfigException : Exception
    {
        public HoconConfigException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }
    }


    public class DictionaryReaderNode
    {
    }

    public class DictionaryReader : Reader<DictionaryReaderNode>
    {
        public IDictionary<string, object> Source { get; private set; }

        public DictionaryReader(ResolveSourceHandler resolveSource)
        {
            Source = new Dictionary<string, object>();

            ResolveSource += resolveSource;

            CreateOrUpdateValue += (path, value) => Source[path] = value;
            CreateOrUpdateNode += (path, node) => Source[path] = node;
            RemoveNode += path => Source.Remove(path);
            GetNodeOrValue += path => Source.ContainsKey(path) ? Source[path] : null;
            GetNodesOrValues += path => Source.Where(item => path == item.Key || item.Key.StartsWith(path + PathDelimeter)).ToArray();
        }
    }

    #region Reader 

    public class Reader<TNode>
        where TNode : class, new()
    {
        public delegate void CreateOrUpdateValueHandler(string path, object value);
        public delegate void CreateOrUpdateNodeHandler(string path, TNode value);
        public delegate void RemoveNodeHandler(string path);
        public delegate KeyValuePair<string, object>[] GetNodesOrValuesHandler(string startPath);
        public delegate object GetNodeOrValueHandler(string path);
        public delegate string ResolveSourceHandler(string source);

        public event ResolveSourceHandler ResolveSource;
        public event CreateOrUpdateNodeHandler CreateOrUpdateNode;
        public event CreateOrUpdateValueHandler CreateOrUpdateValue;
        public event RemoveNodeHandler RemoveNode;

        public event GetNodesOrValuesHandler GetNodesOrValues;
        public event GetNodeOrValueHandler GetNodeOrValue;

        public string PathDelimeter { get; set; }

        public void Read(string sourcePath)
        {
            PathDelimeter = Hocon.DefaultPathDelimeter;

            var startSource = WrapIncludeName(sourcePath);

            var tokenizer = new Tokenizer(RiseResolveSource(sourcePath), TokenLibrary.Tokens);
            ReadKey(tokenizer, string.Empty, ref startSource);
        }

        public void ReadFromString(string content)
        {
            PathDelimeter = Hocon.DefaultPathDelimeter;
            var startSource = string.Empty;

            var tokenizer = new Tokenizer(content, TokenLibrary.Tokens);
            ReadKey(tokenizer, string.Empty, ref startSource);
        }

        public void ReadFromStream(Stream stream)
        {
            ReadFromString((new StreamReader(stream)).ReadToEnd());
        }

        #region Event Helpers

        protected virtual object RiseGetNodeOrValue(string path)
        {
            var handler = GetNodeOrValue;
            return handler != null ? handler(path) : null;
        }

        protected virtual void RiseCreateOrUpdateValue(string path, object value)
        {
            var handler = CreateOrUpdateValue;
            if (handler != null) handler(path, value);
        }

        protected virtual void RiseCreateOrUpdateNode(string path, TNode value)
        {
            var handler = CreateOrUpdateNode;
            if (handler != null) handler(path, value);
        }

        protected virtual void RiseRemoveNode(string path)
        {
            var handler = RemoveNode;
            if (handler != null) handler(path);
        }

        protected virtual KeyValuePair<string, object>[] RiseGetNodesOrValues(string startpath)
        {
            var handler = GetNodesOrValues;
            return handler != null ? handler(startpath) : new KeyValuePair<string, object>[0];
        }

        protected virtual string RiseResolveSource(string source)
        {
            var handler = ResolveSource;
            return handler != null ? handler(source) : string.Empty;
        }

        #endregion

        #region Reader Implementaion

        private string WrapIncludeName(string path)
        {
            return string.Format("^{0}$", path);
        }

        private void ReadInclude(Tokenizer tokenizer, string path, ref string alreadyIncluded)
        {
            var include = WrapIncludeName(path);

            if (alreadyIncluded.Contains(include))
                throw new ReaderException("Already included: {0}", path);

            alreadyIncluded += include;

            tokenizer.Include(RiseResolveSource(path));
        }

        private void ReadKey(Tokenizer tokenizer, string originalPath, ref string alreadyIncluded)
        {
            TokenType token;
            string value;

            while (tokenizer.ReadNext(out token, out value,
                                      new[] { TokenType.Include, TokenType.Key },
                                      new[] { TokenType.Comment, TokenType.Space }))
            {
                tokenizer.Consume();

                // Read include
                if (token == TokenType.Include)
                {
                    ReadInclude(tokenizer, value, ref alreadyIncluded);
                    continue;
                }


                // Update path
                var currentPath = originalPath;
                if (string.IsNullOrEmpty(currentPath) == false)
                    currentPath += PathDelimeter;

                RiseCreateOrUpdateNode((currentPath += value), new TNode());

                // Read assign or scope
                if (tokenizer.ReadNext(out token, out value,
                    new[] { TokenType.BeginScope, TokenType.Assign },
                    new[] { TokenType.Comment, TokenType.Space }) == false)
                    throw new ReaderException("Expected assign or begin scope, but: {0}, offset: {1}", token, tokenizer.Offset);

                tokenizer.Consume();

                if (token == TokenType.Assign)
                {
                    if (tokenizer.ReadNext(out token, out value,
                        new[]
                            {
                                TokenType.StringValue, 
                                TokenType.NumericValue, 
                                TokenType.DeciamlValue, 
                                TokenType.DoubleValue,
                                TokenType.BooleanValue, 
                                TokenType.Substitution,
                                TokenType.SafeSubstitution,
                                TokenType.BeginArray
                            },
                        new[] { TokenType.Comment, TokenType.Space }) == false)
                        throw new ReaderException("Expected arra/string/numeric/bool/substitution, but: {0}, offset: {1}", token, tokenizer.Offset);

                    tokenizer.Consume();

                    // Read extends
                    if (token == TokenType.Substitution && RiseGetNodeOrValue(value) is TNode)
                    {
                        foreach (var item in RiseGetNodesOrValues(value))
                        {
                            var newPath = item.Key.Replace(value, currentPath);
                            var node = item.Value as TNode;

                            if (node != null)
                                RiseCreateOrUpdateNode(newPath, node);
                            else
                            {
                                RiseCreateOrUpdateValue(newPath, item.Value);
                            }
                        }

                        // Continue with scope
                        if (tokenizer.ReadNext(out token, out value,
                                               new[] { TokenType.BeginScope },
                                               new[] { TokenType.Comment, TokenType.Space }))
                        {
                            tokenizer.Consume();

                            ReadBeginScope(tokenizer, currentPath, ref alreadyIncluded);
                        }
                    }
                    else if (token == TokenType.BeginArray)
                        ReadAray(tokenizer, currentPath);
                    else
                    {
                        object simpleValue;
                        if (ReadValue(token, value, out simpleValue))
                            RiseCreateOrUpdateValue(currentPath, simpleValue);
                        else
                            RiseRemoveNode(currentPath);
                    }
                }
                else if (token == TokenType.BeginScope)
                    ReadBeginScope(tokenizer, currentPath, ref alreadyIncluded);
            }
        }

        private void ReadAray(Tokenizer tokenizer, string currentPath)
        {
            var array = new List<object>();
            TokenType token;
            string value;

            while (tokenizer.ReadNext(out token, out value,
                new[]
                    {
                        TokenType.StringValue,
                        TokenType.NumericValue,
                        TokenType.DeciamlValue,
                        TokenType.DoubleValue,
                        TokenType.BooleanValue,
                        TokenType.Substitution,
                        TokenType.SafeSubstitution,
                        TokenType.ArraySeparator,
                        TokenType.EndArray
                    },
                new[] { TokenType.Comment, TokenType.Space }))
            {
                tokenizer.Consume();

                if (token == TokenType.EndArray)
                    break;

                if (token == TokenType.ArraySeparator)
                    continue;

                object arrayValue;
                if (ReadValue(token, value, out arrayValue))
                    array.Add(arrayValue);
            }

            RiseCreateOrUpdateValue(currentPath, array);
        }

        private void ReadBeginScope(Tokenizer tokenizer, string currentPath, ref string alreadyIncluded)
        {
            while (true)
            {
                TokenType token;
                string value;

                if (tokenizer.ReadNext(out token, out value,
                                       new[] { TokenType.Key },
                                       new[] { TokenType.Comment, TokenType.Space }))
                {
                    ReadKey(tokenizer, currentPath, ref alreadyIncluded);
                }
                else
                    if (tokenizer.ReadNext(out token, out value,
                                           new[] { TokenType.EndScope },
                                           new[] { TokenType.Comment, TokenType.Space }))
                    {
                        tokenizer.Consume();
                        break;
                    }
                    else
                        throw new ReaderException("Expected begin end scope '}}' or a property, but: {0}, offset: {1}", token, tokenizer.Offset);
            }
        }

        private bool ReadValue(TokenType token, string content, out object value)
        {
            value = null;

            switch (token)
            {
                case TokenType.Substitution:
                    {
                        var data = RiseGetNodeOrValue(content);
                        if (data == null)
                            throw new ReaderException("Substitution not found: '{0}'", content);

                        value = data;
                    }
                    return true;

                case TokenType.SafeSubstitution:
                    {
                        var data = RiseGetNodeOrValue(content);
                        if (data == null) return false;

                        value = data;
                    }
                    return true;

                case TokenType.NumericValue:
                    value = int.Parse(content);
                    return true;

                case TokenType.DeciamlValue:
                    value = decimal.Parse(content);
                    return true;

                case TokenType.DoubleValue:
                    value = (decimal)double.Parse(content);
                    return true;

                case TokenType.BooleanValue:
                    value =
                        (
                            content.Equals("on", StringComparison.CurrentCultureIgnoreCase) ||
                            content.Equals("true", StringComparison.CurrentCultureIgnoreCase) ||
                            content.Equals("yes", StringComparison.CurrentCultureIgnoreCase) ||
                            content.Equals("enabled", StringComparison.CurrentCultureIgnoreCase)
                        );
                    return true;

                //case TokenType.StringValue:
                default:
                    value = content;
                    return true;
            }
        }

        #endregion
    }

    public class ReaderException : Exception
    {
        public ReaderException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }
    }

    #endregion

    #region Writer

    public class Writer<TNode>
        where TNode : class, new()
    {
        public string PathDelimeter { get; set; }

        public void WriteStream(Stream stream, IEnumerable<KeyValuePair<string, object>> data, string headline = null)
        {
            PathDelimeter = Hocon.DefaultPathDelimeter;
            var writter = new StreamWriter(stream);
            writter.Write(WriteString(data, headline));
            writter.Flush();
        }

        public string WriteString(IEnumerable<KeyValuePair<string, object>> data, string headline = null)
        {
            var builder = new StringBuilder();

            if (string.IsNullOrEmpty(headline) == false)
            {
                foreach (var line in headline.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    builder.Append("# ");
                    builder.AppendLine(line);
                }
            }

            var blocks = new Stack<string>();

            foreach (var entry in data.OrderBy(item => item.Key))
            {
                if (entry.Value is TNode)
                {
                    if (blocks.Count == 0)
                    {
                        builder.AppendLine();
                        builder.AppendFormat("{0} {{", entry.Key);
                        builder.AppendLine();

                        blocks.Push(entry.Key);
                    }
                    else if (entry.Key.StartsWith(blocks.Peek() + PathDelimeter))
                    {
                        builder.AppendLine();
                        builder.AppendFormat("{0}{1} {{",
                            new string('\t', blocks.Count),
                            entry.Key.Replace(blocks.Peek() + PathDelimeter, string.Empty));
                        builder.AppendLine();

                        blocks.Push(entry.Key);
                    }
                    else
                    {
                        while (blocks.Count > 0)
                        {
                            blocks.Pop();

                            builder.AppendFormat("{0}}}", new string('\t', blocks.Count));
                            builder.AppendLine();
                        }

                        blocks.Push(entry.Key);

                        builder.AppendLine();
                        builder.AppendFormat("{0} {{", entry.Key);
                        builder.AppendLine();
                    }
                }
                else
                {
                    if (blocks.Count == 0 || entry.Key.StartsWith(blocks.Peek() + PathDelimeter) == false)
                    {
                        while (blocks.Count > 0)
                        {
                            blocks.Pop();
                            builder.Append(new string('\t', blocks.Count));
                            builder.AppendLine("}");
                        }
                        builder.AppendLine();
                    }

                    builder.Append(new string('\t', blocks.Count));

                    builder.AppendFormat("{0} : ",
                        (blocks.Count) > 0 ? entry.Key.Replace(blocks.Peek() + PathDelimeter, string.Empty) : entry.Key);

                    WriteValue(builder, entry.Value);

                    builder.AppendLine();
                }
            }

            while (blocks.Count > 0)
            {
                blocks.Pop();
                builder.Append(new string('\t', blocks.Count));
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private void WriteValue(StringBuilder builder, object value)
        {
            var array = value as List<object>;
            if (array != null)
            {
                builder.Append("[");

                var count = array.Count;
                foreach (var item in array)
                {
                    WriteValue(builder, item);
                    if (--count > 0) builder.Append(", ");
                }
                builder.Append("]");
            }
            else if (value is string)
            {
                builder.AppendFormat("\"{0}\"", value);
            }
            else if (value is bool)
            {
                builder.Append(((bool)value) ? "true" : "false");
            }
            else
                builder.AppendFormat("{0}", value);
        }
    }

#endregion

    namespace OGS.HOCON.Impl
    {
        internal enum TokenType
        {
            Unknown,
            Comment,
            Space,

            Include,

            Key,
            Assign,
            BeginScope,
            EndScope,

            StringValue,
            NumericValue,
            DeciamlValue,
            BooleanValue,
            Substitution,
            SafeSubstitution,

            BeginArray,
            EndArray,
            ArraySeparator,
            DoubleValue
        }

        internal class TokenLibrary
        {
            public static readonly IEnumerable<Token> Tokens;

            static TokenLibrary()
            {
                Tokens = new List<Token>
                {
                    new Token(TokenType.Comment, @"([#]|(//)).*([\n\r])?"),
                    new Token(TokenType.Include, @"include[ ]*[""](?<value>.+)[""]"),

                    new Token(TokenType.Key, @"[a-zA-Z0-9_-]+([.][a-zA-Z0-9_]+)*"),
                    new Token(TokenType.Space, @"[ \r\n\t]+"),

                    new Token(TokenType.Assign, @"[(:)|(=)]"),

                    new Token(TokenType.BeginArray, @"[[]"),
                    new Token(TokenType.EndArray, @"[]]"),
                    new Token(TokenType.ArraySeparator, @"[,]"),

                    new Token(TokenType.Substitution, @"[$][(](?<value>(\w|[._-])+)[)]"),
                    new Token(TokenType.Substitution, @"[$][{](?<value>(\w|[._-])+)[}]"),
                    new Token(TokenType.SafeSubstitution, @"[$][(][?](?<value>\w*)[)]"),
                    new Token(TokenType.SafeSubstitution, @"[$][{][?](?<value>\w*)[}]"),
                    new Token(TokenType.BooleanValue, @"(?i:(on|off|true|false|yes|no|enabled|disabled))", new Stop(@"\Z|[ \],\r\n\t]")),
                    new Token(TokenType.DeciamlValue, @"(?<value>[-]?[0-9]+[.][0-9]+)", new Stop(@"\Z|[ \],\r\n\t]")),
                    new Token(TokenType.DoubleValue, @"(?<value>[-]?[0-9]+[Ee][0-9]+)", new Stop(@"\Z|[ \],\r\n\t]")),
                    new Token(TokenType.NumericValue, @"(?<value>[-]?[0-9]+)", new Stop(@"\Z|[ \],\r\n\t]")),
                    new Token(TokenType.StringValue, @"[""](?<value>([""][""]|[^""])*)[""]", new Stop(@"\Z|[ \],\r\n\t]")),
                    new Token(TokenType.StringValue, @"([^""${}\[\]:=,+#'^?!@*& \r\n\t])+", new Stop(@"\Z|[ \],\r\n\t]")),

                    new Token(TokenType.BeginScope, @"[{]"),
                    new Token(TokenType.EndScope, @"[}]")
                };
            }
        }

        internal class Tokenizer
        {
            private readonly List<Token> _tokens;
            private TokenHandle _handle;
            private string _content;
            private int _offset;

            public int Offset
            {
                get { return _offset; }
            }

            public Tokenizer(string content, IEnumerable<Token> tokens)
            {
                _tokens = new List<Token>(tokens);
                _content = content;
            }

            public void Include(string content)
            {
                _content = _content.Insert(_offset, Environment.NewLine + content + Environment.NewLine);
            }

            public bool ReadNext(out TokenType token, out string value, TokenType[] requestedTokens, TokenType[] ignoreTokens)
            {
                token = TokenType.Unknown;
                value = string.Empty;

                if (IsEOF()) return false;

                bool tokenFound;

                // Read ignorable tokens
                do
                {
                    tokenFound = false;
                    foreach (var nextToken in _tokens.Where(item => ignoreTokens.Any(t => t == item.Type)))
                    {
                        tokenFound = nextToken.Match(_content, _offset, out _handle);
                        if (tokenFound)
                        {
                            Consume();
                            break;
                        }
                    }

                    if (IsEOF()) return false;
                } while (tokenFound);

                // Read requested token
                foreach (var nextToken in _tokens.Where(item => requestedTokens.Any(t => t == item.Type)))
                {
                    tokenFound = nextToken.Match(_content, _offset, out _handle);
                    if (tokenFound)
                    {
                        token = nextToken.Type;
                        value = _handle.Value;
                        break;
                    }
                }

                return tokenFound;
            }

            public void Consume()
            {
                if (_handle != null) _offset = _handle.Consume();
            }

            private bool IsEOF()
            {
                return _offset >= _content.Length;
            }
        }

        internal class TokenHandle
        {
            public string Value { get; private set; }
            private readonly int _nextOffset;

            public TokenHandle(string value, int nextOffset)
            {
                Value = value;
                _nextOffset = nextOffset;
            }

            public int Consume()
            {
                return _nextOffset;
            }
        }

        internal class Stop
        {
            public Regex Parser { get; private set; }

            public Stop(string parser)
            {
                Parser = new Regex(parser);
            }

            public bool Match(string content, int offset)
            {
                var match = Parser.Match(content, offset);
                return match.Success && match.Groups[0].Index == offset;
            }
        }

        internal class Token
        {
            public Regex Parser { get; private set; }
            public TokenType Type { get; private set; }
            public Stop Stop { get; private set; }

            public Token(TokenType type, string parser, Stop stop = null)
            {
                Parser = new Regex(parser);
                Type = type;
                Stop = stop;
            }

            public bool Match(string content, int offset, out TokenHandle handle)
            {
                handle = null;

                var match = Parser.Match(content, offset);
                if (match.Success == false)
                    return false;

                var capture = match.Captures[0];
                if (capture.Index != offset)
                    return false;

                if (Stop != null && Stop.Match(content, match.Groups[0].Index + match.Groups[0].Length) == false)
                    return false;

                if (match.Groups["value"].Success)
                    handle = new TokenHandle(match.Groups["value"].Value, offset + capture.Length);
                else
                    handle = new TokenHandle(capture.Value, offset + capture.Length);

                return true;
            }
        }
    }
}
