using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CsvHelper {
    /// <summary>
    /// 用于存储多列映射数组或者类的信息
    /// </summary>
    public class CsvNode {
        #region reader
        public string name { get; protected set; }
        public eFieldType type { get; protected set; }
        public List<CsvNode> arrayInfos { get; protected set; }
        public Dictionary<string, CsvNode> classInfos { get; protected set; }
        /// <summary>
        /// 实际内容
        /// </summary>
        public JToken text { get; protected set; }
        /// <summary>
        /// 是否第一层
        /// </summary>
        public bool frist { get; protected set; }

        public string cellType;

        public CsvNode() { }

        protected void Init(string name, eFieldType type) {
            this.name = name;
            this.type = type;
            switch (type) {
                case eFieldType.Primitive:

                    break;
                case eFieldType.Array:
                    //arrayInfos = new List<CsvNode>(3);
                    arrayInfos = m_listPool.Pop();
                    break;
                case eFieldType.Class:
                    //classInfos = new Dictionary<string, CsvNode>(3);
                    classInfos = m_dictPool.Pop();
                    break;
                default:
                    Debug.LogError("CsvNode unsupport type:" + type);
                    break;
            }
        }

        /// <summary>
        /// 添加内容
        /// </summary>
        /// <param name="fields">字段名分解</param>
        /// <param name="layer">处理到第几个字段</param>
        /// <param name="text"></param>
        protected void Add(CsvHeader header, int layer, JToken text) {
            CsvHeader subHeader;
            CsvHeader nextHeader;
            CsvNode subNode;
            switch (type) {
                case eFieldType.Primitive:
                    //Debug.LogError(name + " can't add, type:" + type);
                    this.text = text;
                    break;
                case eFieldType.Array:
                    if (header.type == eFieldType.Primitive) {
                        Debug.LogError(name + " can't add Array, header:" + header.name);
                        return;
                    }
                    subHeader = header.subs[layer];
                    if (subHeader.type != eFieldType.Array || layer + 1 >= header.subs.Length) {
                        Debug.LogError(name + " can't add Array, subHeader:" + subHeader.name + " " + subHeader.type                            + " header" + header.name + " " + layer);
                        return;
                    }
                    if (arrayInfos.Count <= subHeader.index) {
                        nextHeader = header.subs[layer + 1];
                        subNode = Pop(nextHeader.name, nextHeader.type);
                        arrayInfos.Add(subNode);
                    } else {
                        subNode = arrayInfos[subHeader.index];
                    }
                    subNode.Add(header, layer + 1, text);
                    break;
                case eFieldType.Class:
                    if (header.type == eFieldType.Primitive) {
                        Debug.LogError(name + " can't add Class, header:" + header.name);
                        return;
                    }
                    subHeader = header.subs[layer];
                    if (subHeader.type != eFieldType.Class || layer + 1 >= header.subs.Length) {
                        Debug.LogError(name + " can't add Class, subHeader:" + subHeader.name + " " + subHeader.type                            + " header:" + header.name + " " + layer);
                        return;
                    }
                    nextHeader = header.subs[layer + 1];
                    if (!classInfos.TryGetValue(nextHeader.baseName, out subNode)) {
                        subNode = Pop(nextHeader.name, nextHeader.type);
                        classInfos[nextHeader.baseName] = subNode;
                    }
                    subNode.Add(header, layer + 1, text);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 添加内容
        /// </summary>
        /// <param name="fields">字段名分解</param>
        /// <param name="index">处理到第几个字段</param>
        /// <param name="text"></param>
        public void Add(CsvHeader header, JToken text) {
            Add(header, 0, text);
        }

        protected void Clear() {
            name = string.Empty;
            text = null;
            frist = false;
            type = eFieldType.Primitive;
            if (arrayInfos != null) {
                foreach (var item in arrayInfos) {
                    Push(item);
                }
                arrayInfos.Clear();
                m_listPool.Push(arrayInfos);
                arrayInfos = null;
            }
            if (classInfos != null) {
                foreach (var item in classInfos.Values) {
                    Push(item);
                }
                classInfos.Clear();
                m_dictPool.Push(classInfos);
                classInfos = null;
            }
        }

        /// <summary>
        /// 重置内容方便下次使用
        /// </summary>
        public void Reset() {
            text = null;
            if (arrayInfos != null) {
                foreach (var item in arrayInfos) {
                    Push(item);
                }
                arrayInfos.Clear();
            }
            if (classInfos != null) {
                foreach (var item in classInfos.Values) {
                    Push(item);
                }
                classInfos.Clear();
            }
        }

        public JToken ToJToken() {
            switch (type) {
                case eFieldType.Primitive:
                    return text;
                case eFieldType.Array:
                    JArray jArray = new JArray();
                    for (int i = 0; i < arrayInfos.Count; i++) {
                        jArray.Add(arrayInfos[i].ToJToken());
                    }
                    return jArray;
                case eFieldType.Class:
                    JObject jObject = new JObject();
                    foreach (var pair in classInfos) {
                        jObject.Add(pair.Key, pair.Value.ToJToken());
                    }
                    return jObject;
                default:
                    Debug.LogError("CsvNode unsupport type:" + type);
                    return null;
            }

            
        }

        #endregion reader

        #region pool
        private static ObjectPool<List<CsvNode>> m_listPool = new ObjectPool<List<CsvNode>>();
        private static ObjectPool<Dictionary<string, CsvNode>> m_dictPool = new ObjectPool<Dictionary<string, CsvNode>>();
        private static ObjectPool<CsvNode> m_pool = new ObjectPool<CsvNode>();
        public static CsvNode Pop(string title, eFieldType type) {
            CsvNode node = m_pool.Pop();
            node.Init(title, type);
            node.frist = true;
            return node;
        }

        public static void Push(CsvNode node) {
            node.Clear();
            m_pool.Push(node);
        }

        public static void ClearPool() {
            m_pool.Clear();
        }
        #endregion pool

        public static void JsonSplit(string text, List<string> subTexts) {
            //括号深度
            int bracketDepth = 0;
            //引号数量
            int quotesNum = 0;
            //字符分割开始位置
            int startIndex = 1;

            string subText;
            for (int i = 1; i < text.Length - 1; i++) {
                char c = text[i];
                if (c == '{' || c == '[') {
                    ++bracketDepth;
                } else if (c == '}' || c == ']') {
                    --bracketDepth;
                } else if (c == '"' || c == '\'') {
                    ++quotesNum;
                } else if (c == ',') {
                    //不在括号内也不在引号内则表示一段数据
                    if (bracketDepth == 0 && quotesNum % 2 == 0) {
                        subText = text.Substring(startIndex, i - startIndex);
                        subTexts.Add(subText);
                        startIndex = i + 1;
                    }
                }
            }
            if (startIndex < text.Length - 1) {
                subText = text.Substring(startIndex, text.Length - 1 - startIndex);
                subTexts.Add(subText);
            }
        }
    }

}