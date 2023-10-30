using System;
using System.Collections.Generic;
using System.Text;

namespace CsvHelper {
    public enum eFieldType {
        Primitive,
        Array,
        Class,
    }

    /// <summary>
    /// Csv配置信息
    /// </summary>
    public class CsvConfig {
        /// <summary>
        /// 忽略标志
        /// </summary>
        static char m_SkipFlag = '#';
        public static char skipFlag {
            get { return m_SkipFlag; }
            set { m_SkipFlag = value; }
        }

#if UseParseFlag
        /// <summary>
        /// 需要解析标志
        /// </summary>
        static char m_ParseFlag = '*';
        public static char parseFlag
        {
            get { return m_ParseFlag; }
            set { m_ParseFlag = value; }
        }
#endif

        /// <summary>
        /// 类分隔符
        /// </summary>
        static char m_ClassSeparator = '.';
        public static char classSeparator {
            get { return m_ClassSeparator; }
            set { m_ClassSeparator = value; }
        }

        /// <summary>
        /// 数组分隔符
        /// </summary>
        static char m_ArraySeparator = '_';
        public static char arraySeparator {
            get { return m_ArraySeparator; }
            set { m_ArraySeparator = value; }
        }

        static char[] m_ArrayChars = new[] { '[', ']' };
        public static char[] arrayChars {
            get { return m_ArrayChars; }
            set { m_ArrayChars = value; }
        }

        /// <summary>
        /// 每个csv对象的主键
        /// </summary>
        public static string primaryKey = "id";
        /// <summary>
        /// csv表名对应添加的后缀
        /// </summary>
        public static string classPostfix = "Csv";

        /// <summary>
        /// 小数位数
        /// </summary>
        static int m_Digits = 2;
        public static int digits {
            get { return m_Digits; }
            set { m_Digits = value; }
        }

        /// <summary>
        /// 读取转换缓冲大小
        /// </summary>
        static int m_BufferSize = 2048;
        public static int bufferSize {
            get { return m_BufferSize; }
            set { m_BufferSize = value; }
        }

        static bool m_ThrowOnBadData = false;
        public static bool throwOnBadData {
            get { return m_ThrowOnBadData; }
            set { m_ThrowOnBadData = value; }
        }

        public static bool countBytes { get; set; }

        //static Encoding m_Encoding = Encoding.UTF8;
        static Encoding m_Encoding = new UTF8Encoding(false);
        public static Encoding encoding {
            get { return m_Encoding; }
            set { m_Encoding = value; }
        }

        static char m_Quote = '"';
        static string m_quoteString = "\"";
        public static char quote {
            get { return m_Quote; }
            set {
                m_Quote = value;

                m_quoteString = Convert.ToString(value);
            }
        }

        static bool m_IgnoreQuotes = false;
        public static bool ignoreQuotes {
            get { return m_IgnoreQuotes; }
            set { m_IgnoreQuotes = value; }
        }

        static string m_Delimiter = ",";
        /// <summary>
        /// Gets or sets the delimiter used to separate fields.
        /// Default is ',';
        /// </summary>
        public static string delimiter {
            get { return m_Delimiter; }
            set {
                m_Delimiter = value;
            }
        }

        /// <summary>
        /// 单独一格的需要双引号括起来
        /// </summary>
        /// <param name="str"></param>
        /// <param name="solo"></param>
        public static void DealQuote(ref string str, bool solo) {
            if (!solo) {
                return;
            }
            str = string.Format("\"{0}\"", str);
        }

        /// <summary>
        /// 忽略空白行
        /// </summary>
        static bool m_IgnoreBlankLines = true;
        public static bool ignoreBlankLines {
            get { return m_IgnoreBlankLines; }
            set { m_IgnoreBlankLines = value; }
        }

        static char m_Comment = '#';
        public static char comment {
            get { return m_Comment; }
            set { m_Comment = value; }
        }

        /// <summary>
        /// 是否读取注释
        /// </summary>
        public static bool allowComments { get; set; }

        /// <summary>
        /// 检查重复行
        /// </summary>
        static bool m_CheckRepeatLines = false;
        public static bool checkRepeatLines {
            get { return m_CheckRepeatLines; }
            set { m_CheckRepeatLines = value; }
        }

    }

}