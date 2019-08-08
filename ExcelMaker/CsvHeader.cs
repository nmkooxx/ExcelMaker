using System.Collections.Generic;

namespace CsvHelper {
    /// <summary>
    /// 记录Csv头文件的配置信息，映射情况等等
    /// </summary>
    public class CsvHeader {
        public CsvHeader() { }

        public bool skip { get; private set; }
        public eFieldType type { get; private set; }
        public string name { get; private set; }
        public int index { get; private set; }
        public string baseName { get; private set; }
        public CsvHeader[] subs { get; private set; }
        public int slot { get; private set; }

        public void SetSlot(int slot) {
            this.slot = slot;
        }

        private bool NeedParse(ref string title) {
#if UseParseFlag
            if (title[0] == CsvConfig.ParseFlag)
            {
                title = title.Substring(1);
                return true;    
            }
            else
            {
                return false;
            }
#else
            int length = title.Length;
            //字段名必须大于1个字符，加上特殊符号与序号就是3个
            if (length < 3) {
                return false;
            }
            //分隔符靠后，从倒数第2个向前判断
            int max = length - 2;
            //字段名必须大于1个字符
            int min = 1;
            for (int i = max; i > min; i--) {
                char c = title[i];
                if (c == CsvConfig.classSeparator || c == CsvConfig.arraySeparator) {
                    return true;
                }
            }
            return false;
#endif
        }

        private void Init(string title) {
            if (string.IsNullOrEmpty(title) || title[0] == CsvConfig.skipFlag) {
                skip = true;
                return;
            }
            name = title;
            baseName = title;
            if (!NeedParse(ref title)) {
                return;
            }
            //UnityEngine.Debug.Log("title init:" + title);
            string[] subTitles = title.Split(CsvConfig.classSeparator);
            CsvHeader header;
            List<CsvHeader> subList = new List<CsvHeader>(subTitles.Length);
            for (int i = 0; i < subTitles.Length; i++) {
                string[] itemTitles = subTitles[i].Split(CsvConfig.arraySeparator);
                if (itemTitles.Length > 1) {
                    if (i == 0) {
                        type = eFieldType.Array;
                        baseName = itemTitles[0];
                    }

                    int subindex = 0;
                    //解析数组结构
                    for (int j = 1; j < itemTitles.Length; j++) {
                        if (!int.TryParse(itemTitles[j], out subindex)) {
                            Debug.LogError("CsvHeader.Init TryParse error:" + title + " " + itemTitles[j]);
                            continue;
                        }
                        subindex -= 1;
                        header = Pop();
                        header.Set(itemTitles[0], subTitles[i], eFieldType.Array);
                        header.index = subindex;
                        subList.Add(header);
                    }
                } else {
                    if (i == 0) {
                        type = eFieldType.Class;
                        baseName = itemTitles[0];
                    }
                }

                eFieldType subtype = eFieldType.Class;
                if (i == subTitles.Length - 1) {
                    subtype = eFieldType.Primitive;
                }
                header = Pop();
                header.Set(itemTitles[0], subTitles[i], subtype);
                subList.Add(header);
            }
            subs = subList.ToArray();
        }

        private void Set(string baseName, string name, eFieldType type) {
            //Debug.Log("subtitle init:" + baseName + " " + name + " " + type);
            this.baseName = baseName;
            this.name = name;
            this.type = type;
        }

        private static ObjectPool<CsvHeader> m_pool = new ObjectPool<CsvHeader>();
        private static CsvHeader Pop() {
            CsvHeader header = m_pool.Pop();
            return header;
        }

        public static CsvHeader Pop(string title) {
            CsvHeader header = m_pool.Pop();
            header.Init(title);
            return header;
        }

        public static void Push(CsvHeader header) {
            header.skip = false;
            header.type = eFieldType.Primitive;
            header.name = string.Empty;
            header.index = 0;
            header.baseName = string.Empty;
            if (header.subs != null) {
                foreach (var item in header.subs) {
                    Push(item);
                }
                header.subs = null;
            }
            m_pool.Push(header);
        }

        public static void Clear() {
            m_pool.Clear();
        }

    }

}