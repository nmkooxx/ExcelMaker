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
        static char m_skipFlag = '#';
        public static char skipFlag {
            get { return m_skipFlag; }
            set { m_skipFlag = value; }
        }

#if UseParseFlag
        /// <summary>
        /// 需要解析标志
        /// </summary>
        static char m_parseFlag = '*';
        public static char parseFlag
        {
            get { return m_parseFlag; }
            set { m_parseFlag = value; }
        }
#endif

        /// <summary>
        /// 类分隔符
        /// </summary>
        static char m_classSeparator = '.';
        public static char classSeparator {
            get { return m_classSeparator; }
            set { m_classSeparator = value; }
        }

        /// <summary>
        /// 数组分隔符
        /// </summary>
        static char m_arraySeparator = '_';
        public static char arraySeparator {
            get { return m_arraySeparator; }
            set { m_arraySeparator = value; }
        }

        static char[] m_arrayChars = new[] { '[', ']' };
        public static char[] arrayChars {
            get { return m_arrayChars; }
            set { m_arrayChars = value; }
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
        static int m_digits = 2;
        public static int digits {
            get { return m_digits; }
            set { m_digits = value; }
        }

        /// <summary>
        /// 读取转换缓冲大小
        /// </summary>
        static int m_bufferSize = 2048;
        public static int bufferSize {
            get { return m_bufferSize; }
            set { m_bufferSize = value; }
        }

        static bool m_throwOnBadData = false;
        public static bool throwOnBadData {
            get { return m_throwOnBadData; }
            set { m_throwOnBadData = value; }
        }

        public static bool countBytes { get; set; }

        static Encoding m_encoding = Encoding.UTF8;
        public static Encoding encoding {
            get { return m_encoding; }
            set { m_encoding = value; }
        }

        static char m_quote = '"';
        static string m_quoteString = "\"";
        static string m_doubleQuoteString = "\"\"";
        static char[] m_quoteRequiredChars = new[] { '\r', '\n' };

        public static char quote {
            get { return m_quote; }
            set {
//                 if (value == '\n') {
//                     Debug.LogError("Newline is not a valid quote.");
//                 }
// 
//                 if (value == '\r') {
//                     Debug.LogError("Carriage return is not a valid quote.");
//                 }
// 
//                 if (value == '\0') {
//                     Debug.LogError("Null is not a valid quote.");
//                 }
// 
//                 if (Convert.ToString(value) == m_delimiter) {
//                     Debug.LogError("You can not use the delimiter as a quote.");
//                 }

                m_quote = value;

                m_quoteString = Convert.ToString(value);
                m_doubleQuoteString = m_quoteString + m_quoteString;
            }
        }

        public static string quoteString {
            get { return m_quoteString; }
        }

        /// <summary>
        /// 用于写回文本时替换单引号
        /// </summary>
        public static string doubleQuoteString {
            get { return m_doubleQuoteString; }
        }

        public static char[] quoteRequiredChars {
            get { return m_quoteRequiredChars; }
        }

        static bool m_ignoreQuotes = false;
        public static bool ignoreQuotes {
            get { return m_ignoreQuotes; }
            set { m_ignoreQuotes = value; }
        }

        static string m_delimiter = ",";
        /// <summary>
        /// Gets or sets the delimiter used to separate fields.
        /// Default is ',';
        /// </summary>
        public static string delimiter {
            get { return m_delimiter; }
            set {
//                 if (value == "\n") {
//                     Debug.LogError("Newline is not a valid delimiter.");
//                 }
// 
//                 if (value == "\r") {
//                     Debug.LogError("Carriage return is not a valid delimiter.");
//                 }
// 
//                 if (value == "\0") {
//                     Debug.LogError("Null is not a valid delimiter.");
//                 }
// 
//                 if (value == Convert.ToString(m_quote)) {
//                     Debug.LogError("You can not use the quote as a delimiter.");
//                 }

                m_delimiter = value;

                BuildRequiredQuoteChars();
            }
        }

        private static void BuildRequiredQuoteChars() {
            m_quoteRequiredChars = m_delimiter.Length > 1 ?
                new[] { '\r', '\n' } :
                new[] { '\r', '\n', m_delimiter[0] };
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
        static bool m_ignoreBlankLines = true;
        public static bool ignoreBlankLines {
            get { return m_ignoreBlankLines; }
            set { m_ignoreBlankLines = value; }
        }

        static char m_comment = '#';
        public static char comment {
            get { return m_comment; }
            set { m_comment = value; }
        }

        /// <summary>
        /// 是否读取注释
        /// </summary>
        public static bool allowComments { get; set; }

        /// <summary>
        /// 检查重复行
        /// </summary>
        static bool m_checkRepeatLines = false;
        public static bool checkRepeatLines {
            get { return m_checkRepeatLines; }
            set { m_checkRepeatLines = value; }
        }

    }

}