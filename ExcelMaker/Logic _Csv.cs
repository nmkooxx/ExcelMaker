using CsvHelper;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public partial class Logic {
    private void ExportCsv(string dirPath, bool sync, char type, string codePaths, bool exportCode) {
        string localPath;
        if (type == 'C') {
            localPath = "client";
        }
        else {
            localPath = "server";
        }
        if (!Directory.Exists(localPath)) {
            Directory.CreateDirectory(localPath);
        }
        if (exportCode && s_ExportLanguage == ExportLanguage.TypeScript) {
            CsvMaker_TypeScript.InitCatalog();
        }
        m_LogBuilder.AppendLine($"导出Csv type:{type} sync:{sync}");
        string headExtend;
        string csvExtend;
        string readerExtend;
        int offset = m_Config.rootPath.Length + 1;
        string csvName = null, csvDir = null, path = null;
        string localCsvPath = null;
        List<string> paths = new List<string>(8);
        foreach (var list in m_ExcelMap.Values) {
            var info = list[0];
            csvDir = info.folder;
            csvName = info.name;
            m_FileName = info.name;
            if (!sync) {
                //选择模式
                if (!info.selected) {
                    continue;
                }
                if (m_Setting.exportDir && csvDir != null) {
                    path = dirPath + "/" + csvDir + "/" + csvName + ".csv";
                    localCsvPath = localPath + "/" + csvDir + "/" + csvName + ".csv";
                    if (!Directory.Exists(dirPath + "/" + csvDir)) {
                        Directory.CreateDirectory(dirPath + "/" + csvDir);
                    }
                    if (!Directory.Exists(localPath + "/" + csvDir)) {
                        Directory.CreateDirectory(localPath + "/" + csvDir);
                    }
                }
                else {
                    path = dirPath + "/" + csvName + ".csv";
                    localCsvPath = localPath + "/" + csvName + ".csv";
                }
            }
            else {
                //同步模式，检查表格是否已经存在
                if (m_Setting.exportDir && csvDir != null) {
                    path = dirPath + "/" + csvDir + "/" + csvName + ".csv";
                    localCsvPath = localPath + "/" + csvDir + "/" + csvName + ".csv";
                }
                else {
                    path = dirPath + "/" + csvName + ".csv";
                    localCsvPath = localPath + "/" + csvName + ".csv";
                }
                if (!File.Exists(path)) {
                    continue;
                }
            }

            bool need;
            string csvText;
            if (keepSplit) {
                m_LogBuilder.AppendLine(path);
                foreach (var item in list) {
                    paths.Clear();
                    paths.Add(item.path);
                    need = ReadExcel(paths, type, InitCsv, RowToCsv);
                    if (!need) {
                        continue;
                    }
                    if (m_FileName == "Localize" || m_FileName == "Text") {
                        for (int l = 0; l < m_LocalizeNames.Length; l++) {
                            var localizeName = m_LocalizeNames[l];
                            csvText = m_LocalizeBuilders[l].ToString();
                            csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                            localCsvPath = $"{localPath}/{localizeName}/{csvName}.csv";
                            if (!Directory.Exists(localCsvPath)) {
                                Directory.CreateDirectory(localCsvPath);
                            }
                            File.WriteAllText(localCsvPath, csvText, CsvConfig.encoding);
                        }
                    }
                    else {
                        csvText = m_CsvBuilder.ToString();
                        csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                        localCsvPath = localPath + "/" + item.nameWithDir.Replace(".xlsx", ".csv");
                        File.WriteAllText(localCsvPath, csvText, CsvConfig.encoding);
                    }

                }

                continue;
            }

            paths.Clear();
            foreach (var item in list) {
                paths.Add(item.path);
            }            
            need = ReadExcel(paths, type, InitCsv, RowToCsv);
            if (!need) {
                continue;
            }
            if (m_FileName == "Localize" || m_FileName == "Text") {
                for (int l = 0; l < m_LocalizeNames.Length; l++) {
                    var localizeName = m_LocalizeNames[l];
                    csvText = m_LocalizeBuilders[l].ToString();
                    csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                    if (!Directory.Exists($"{dirPath}/{localizeName}/")) {
                        Directory.CreateDirectory($"{dirPath}/{localizeName}/");
                    }
                    path = $"{dirPath}/{localizeName}/{csvName}.csv";
                    File.WriteAllText(path, csvText, CsvConfig.encoding);
                    m_LogBuilder.AppendLine(path);

                    if (!Directory.Exists($"{localPath}/{localizeName}/")) {
                        Directory.CreateDirectory($"{localPath}/{localizeName}/");
                    }
                    localCsvPath = $"{localPath}/{localizeName}/{csvName}.csv";
                    File.WriteAllText(localCsvPath, csvText, CsvConfig.encoding);
                }
            }
            else {
                csvText = m_CsvBuilder.ToString();
                csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                File.WriteAllText(path, csvText, CsvConfig.encoding);
                //在本地同时保留一份，方便提交svn对比存档
                File.WriteAllText(localCsvPath, csvText, CsvConfig.encoding);
                m_LogBuilder.AppendLine(path);
            }


            if (exportCode) {
                switch (s_ExportLanguage) {
                    case ExportLanguage.CSharp:
                        if (m_DefineStyle) {
                            DefineMaker_CSharp.MakeClass(codePaths, csvName, m_Headers, m_RawTypes);
                        }
                        else {
                            CsvMaker_CSharp.MakeCsvClass(codePaths, csvName, m_Headers, m_RawTypes, IsSparse(csvName));
                        }
                        break;
                    case ExportLanguage.Java:
                        CsvMaker_Java.MakeCsvClass(codePaths, csvName, m_Headers, m_RawTypes);
                        break;
                    case ExportLanguage.TypeScript:
                        CsvMaker_TypeScript.AddCatalog(csvName);
                        TryGetExtend("TypeScript", "extendHead", csvName, out headExtend);
                        TryGetExtend("TypeScript", "extendCsv", csvName, out csvExtend);
                        TryGetExtend("TypeScript", "extendReader", csvName, out readerExtend);
                        CsvMaker_TypeScript.MakeCsvClass(codePaths, csvName, m_Headers, m_RawTypes, headExtend, csvExtend, readerExtend);
                        break;
                    default:
                        break;
                }
            }
            if (m_DefineIndex >= 0) {
                string[] codePathArrray = codePaths.Split(';');
                foreach (string codePath in codePathArrray) {
                    if (!Directory.Exists(codePath)) {
                        Directory.CreateDirectory(codePath);
                    }
                    switch (s_ExportLanguage) {
                        case ExportLanguage.CSharp:
                            CsvMaker_CSharp.MakeCsvDefine(codePath, m_DefineName);
                            break;
                        case ExportLanguage.Java:
                            CsvMaker_Java.MakeCsvDefine(codePath, m_DefineName);
                            break;
                        case ExportLanguage.TypeScript:
                            CsvMaker_TypeScript.MakeCsvDefine(codePath, m_DefineName);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        if (exportCode && s_ExportLanguage == ExportLanguage.TypeScript) {
            string[] codePathArrray = codePaths.Split(';');
            foreach (string codePath in codePathArrray) {
                if (!Directory.Exists(codePath)) {
                    Directory.CreateDirectory(codePath);
                }
                CsvMaker_TypeScript.MakeCatalog(codePath);
            }
        }
        Debug.Log(m_LogBuilder.ToString());

//         if (m_LocalizeKeys.Count > 0) {
//             StringBuilder keyBuilder = new StringBuilder();
//             foreach (var item in m_LocalizeKeys) {
//                 keyBuilder.Append(item.Key);
//                 keyBuilder.Append(CsvConfig.delimiter);
//                 keyBuilder.AppendLine(item.Value);
//             }
//             string keyPath = "LocalizeKey.txt";
//             File.WriteAllText(keyPath, keyBuilder.ToString());
//         }

//         if (m_PathKeys.Count > 0) {
//             StringBuilder keyBuilder = new StringBuilder();
//             keyBuilder.AppendLine("id,value");
//             foreach (var item in m_PathKeys) {
//                 keyBuilder.Append(item.Key);
//                 keyBuilder.Append(CsvConfig.delimiter);
//                 keyBuilder.AppendLine(item.Value);
//             }
//             string text = keyBuilder.ToString();
//             path = dirPath + "/PathKey.csv";
//             localCsvPath = localPath + "/PathKey.csv";
//             File.WriteAllText(path, text);
//             File.WriteAllText(localCsvPath, text);
//         }
    }

    /// <summary>
    /// 将含有特殊符号的字符串包裹起来
    /// </summary>
    /// <param name="rawString"></param>
    /// <returns></returns>
    private string PackString(string rawString, bool force) {
        string newString = rawString;
        if (force || rawString.IndexOf(CsvConfig.delimiter) > 0) {
            //引号要替换成双引号
            newString = newString.Replace("\"", "\"\"");
            //出现逗号分隔符，需要包裹
            newString = string.Format("\"{0}\"", newString);
        }

        if (newString.IndexOf("\n") > 0
        || newString.IndexOf("\t") > 0
        || newString.IndexOf("\\") > 0
        ) {
            LogError("文本含特殊符号, index：" + m_CurIndex + " info:" + rawString);
        }

        return newString;
    }

    private object m_LocalizeId;
    private Dictionary<string, string>[] m_LocalizeReplaces;
    private const string kLocalizeRefTag = "[id=";
    private string PackLocalizeString(string rawString, bool force, int slot) {
        string newString = rawString;
        if (force || rawString.IndexOf(CsvConfig.delimiter) > 0) {
            //引号要替换成双引号
            newString = newString.Replace("\"", "\"\"");
            //出现逗号分隔符，需要包裹
            newString = string.Format("\"{0}\"", newString);
        }

        //多语言表才可能出现复杂的格式
        newString = newString.Replace("\n\r", "\\n")
            .Replace("\r\n", "\\n")
            .Replace("\r", "\\n")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            ;

        var replace = m_LocalizeReplaces[slot];
        int startIdx = newString.IndexOf(kLocalizeRefTag);
        if (startIdx >= 0) {
            while (startIdx >= 0) {
                int keyStartIdx = startIdx + kLocalizeRefTag.Length;
                int endIdx = newString.IndexOf("]", startIdx);
                if (endIdx <= keyStartIdx) {
                    //找不到就报错，跳出替换
                    LogError($"多语言找不到替换内容, index：{m_CurIndex} startIdx:{startIdx} info:{rawString}");
                    startIdx = -1;
                    break;
                }
                string key = newString.Substring(keyStartIdx, endIdx - keyStartIdx);
                if (!replace.TryGetValue(key, out var value)) {
                    //找不到就报错，跳出替换
                    LogError($"多语言找不到替换内容, index：{m_CurIndex} key:{key} info:{rawString}");
                    startIdx = -1;
                    break;
                }
                newString = newString.Replace(kLocalizeRefTag + key + "]", value);
                startIdx = newString.IndexOf(kLocalizeRefTag, startIdx);
            }
        }
        else {
            //不同的多语言会覆盖之前的，简单无需解析的id要填前面
            replace[m_LocalizeId.ToString()] = newString;
        }

        if (newString.IndexOf("\n") > 0
        || newString.IndexOf("\t") > 0
        || newString.IndexOf("\\") > 0
        ) {
            newString += ",1";
        }
        else {
            newString += ",";
        }

        return newString;
    }

    private StringBuilder m_CsvBuilder;
    /// <summary>
    /// 多语言配置按语言拆多个
    /// </summary>
    private StringBuilder[] m_LocalizeBuilders;
    private string[] m_LocalizeNames;
    private void InitCsv() {
        switch (s_ExportLanguage) {
            case ExportLanguage.CSharp:
                CsvMaker_CSharp.InitCsvDefine();
                break;
            case ExportLanguage.Java:
                CsvMaker_Java.InitCsvDefine();
                break;
            case ExportLanguage.TypeScript:
                CsvMaker_TypeScript.InitCsvDefine();
                break;
            default:
                break;
        }

        if (m_FileName == "Localize" || m_FileName == "Text") {
            var cnt = m_Headers.Count - 1;
            m_LocalizeBuilders = new StringBuilder[cnt];
            m_LocalizeNames = new string[cnt];
            m_LocalizeReplaces = new Dictionary<string, string>[cnt];
            for (int i = 1; i < m_Headers.Count; i++) {
                var header = m_Headers[i];
                m_LocalizeNames[i - 1] = header.name;
                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("id,value,needParse");
                m_LocalizeBuilders[i - 1] = csvBuilder;
                m_LocalizeReplaces[i - 1] = new Dictionary<string, string>();
            }
            m_CsvBuilder = null;
            return;
        }

        m_CsvBuilder = new StringBuilder();
        if (m_DefineStyle) {
            return;
        }
        m_CsvBuilder.Append(m_Headers[0].name);
        for (int i = 1; i < m_Headers.Count; i++) {
            var header = m_Headers[i];
            if (header.skip) {
                continue;
            }
            m_CsvBuilder.Append(CsvConfig.delimiter);
            m_CsvBuilder.Append(header.name);
        }
        m_CsvBuilder.Append("\n");

        //读取代码导出由Unity改为本工具后，不再需要类型信息
        //         m_csvBuilder.Append(m_rawTypes[0]);
        //         for (int i = 1; i < m_rawTypes.Length; i++) {
        //             var header = m_headers[i];
        //             if (header.skip) {
        //                 continue;
        //             }
        //             var rawType = m_rawTypes[i];
        //             m_csvBuilder.Append(CsvConfig.delimiter);
        //             m_csvBuilder.Append(rawType);
        //         }
        //         m_csvBuilder.Append("\n");
    }

    private void RowToCsv(char type, IRow row) {
        object value;
        string cellType;
        string info;
        CsvHeader header;
        if (m_DefineStyle) {
            ICell cell = row.GetCell(0);
            if (cell == null) {
                return;
            }
            value = GetCellValue(cell);
            if (value == null) {
                return;
            }

            m_CsvBuilder.Append(value.ToString());
            m_CsvBuilder.Append(CsvConfig.delimiter);

            int rank = m_Headers.Count - 1;
            header = m_Headers[rank];
            cellType = m_CellTypes[rank];

            cell = row.GetCell(3);
            if (cell == null) {
                m_CsvBuilder.Append("\n");
                return;
            }
            value = GetCellValue(cell);
            if (value == null) {
                m_CsvBuilder.Append("\n");
                return;
            }

            bool isSimple = CheckSimpleFormat(cellType, value, header, rank);
            if (!isSimple) {
                //对象结构需要引号包起来
                info = value.ToString();
                if (info.Length <= 0) {
                    //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError($"{m_FileName} 扩展格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                    return;
                }
                if (cell.CellType == CellType.String) {
                    bool isJson = CheckJsonFormat(cellType, info, header);
                    m_CsvBuilder.Append(PackString(info, isJson));
                }
                else {
                    if (!IsEnum(cellType) && header.subs == null) {
                        //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError($"{m_FileName} 扩展格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                    }
                    m_CsvBuilder.Append(PackString(info, false));
                }
            }

            m_CsvBuilder.Append("\n");
            return;
        }

        if (m_FileName == "Localize" || m_FileName == "Text") {
            ICell cell = row.GetCell(0);
            value = GetCellValue(cell);
            m_LocalizeId = value;

            for (int i = 0; i < m_LocalizeBuilders.Length; i++) {
                var csvBuilder = m_LocalizeBuilders[i];
                csvBuilder.Append(PackString(value.ToString(), false));
                csvBuilder.Append(CsvConfig.delimiter);
            }

            for (int rank = 1; rank < m_Headers.Count; rank++) {
                var csvBuilder = m_LocalizeBuilders[rank -1];

                cell = row.GetCell(rank);
                if (cell == null) {
                    csvBuilder.AppendLine(CsvConfig.delimiter);
                    continue;
                }
                value = GetCellValue(cell);
                if (value == null) {
                    csvBuilder.AppendLine(CsvConfig.delimiter);
                    continue;
                }

                csvBuilder.AppendLine(PackLocalizeString(value.ToString(), false, rank - 1));
            }

            return;
        }

        for (int rank = 0; rank < m_Headers.Count; rank++) {
            header = m_Headers[rank];
            if (header.skip) {
                continue;
            }

            if (rank != 0) {
                m_CsvBuilder.Append(CsvConfig.delimiter);
            }
            ICell cell = row.GetCell(rank);
            if (cell == null) {
                continue;
            }
            value = GetCellValue(cell);
            if (value == null) {
                continue;
            }
            cellType = m_CellTypes[rank];
            if (string.IsNullOrEmpty(cellType)) {
                continue;
            }

            if (header.subs != null) {
                int subIndex = header.subs.Length - 1;
                if (header.subs[subIndex].type == eFieldType.Primitive) {
                    --subIndex;
                    while (subIndex >= 0 && header.subs[subIndex].type == eFieldType.Array) {
                        if (cellType.Length > 2) {
                            cellType = cellType.Substring(0, cellType.Length - 2);
                        }
                        else {
                            if (m_CurIndex < 10) {
                                LogError($"{m_FileName} 表头过小, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                                break;
                            }
                        }
                        --subIndex;
                    }
                }
            }

            bool isSimple = CheckSimpleFormat(cellType, value, header, rank);
            if (!isSimple) {
                //对象结构需要引号包起来
                info = value.ToString();
                if (string.IsNullOrWhiteSpace(info)) {
                    //可能是公式填写
                    continue;
                }
                if (cell.CellType == CellType.String) {
                    bool isJson = CheckJsonFormat(cellType, info, header);
                    m_CsvBuilder.Append(PackString(info, isJson));
                }
                else {
                    if (!IsEnum(cellType) && header.subs == null) {
                        LogError($"{m_FileName} 未定义扩展格式, index：{m_CurIndex} header:{header.name} type:{cellType} info:{value}");
                    }
                    m_CsvBuilder.Append(PackString(info, false));
                }
            }
        }
        m_CsvBuilder.Append("\n");

        RowToDefine(row);
    }

    private void RowToDefine(IRow row) {
        if (m_DefineIndex <= 0) {
            return;
        }
        ICell cell = row.GetCell(m_DefineIndex);
        if (cell == null) {
            return;
        }
        object value = GetCellValue(cell);
        if (value == null) {
            return;
        }

        string valueType = m_CellTypes[0];
        switch (s_ExportLanguage) {
            case ExportLanguage.CSharp:
                CsvMaker_CSharp.AddCsvDefine(valueType, value, GetCellValue(row.GetCell(0)));
                break;
            case ExportLanguage.Java:
                CsvMaker_Java.AddCsvDefine(valueType, value, GetCellValue(row.GetCell(0)));
                break;
            case ExportLanguage.TypeScript:
                CsvMaker_TypeScript.AddCsvDefine(valueType, value, GetCellValue(row.GetCell(0)));
                break;
            default:
                break;
        }
    }

    private bool CheckSimpleFormat(string cellType, object value, CsvHeader header, int slot) {
        string key;
        string str;
        switch (cellType.ToLower()) {
            case "bool":
                if (value is string strb) {
                    if (!string.IsNullOrWhiteSpace(strb)) {
                        str = strb;
                        if (strb.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                            str = "1";
                        }
                        else if (strb.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                            str = "0";
                        }
                        else if (str == "1") {

                        }
                        else if (str == "0") {
                            
                        }
                        else if (string.IsNullOrWhiteSpace(str)) {
                            str = string.Empty;
                        }
                        else {
                            LogError($"{m_FileName} bool格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                        }
                    }
                    else {
                        str = string.Empty;
                    }
                }
                else {
                    str = value.ToString();
                    if (str == "1") {
                        
                    }
                    else if (str == "0") {
                        
                    }
                    else if (string.IsNullOrWhiteSpace(str)) {
                        str = string.Empty;
                    }
                    else {
                        LogError($"{m_FileName} bool格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                    }
                }
                m_CsvBuilder.Append(str);
                break;
            case "uint":
            case "ulong":
                if (value is string stru) {
                    if (!string.IsNullOrWhiteSpace(stru)) {
                        str = stru;
                        //检查是否字符串格式的数字
                        if (ulong.TryParse(stru, out ulong ul)) {

                        }
                        else {
                            LogError($"{m_FileName} 正整数格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                        }
                    }
                    else {
                        str = string.Empty;
                    }
                }
                else {
                    str = value.ToString();
                    if (str.IndexOf('.') >= 0) {
                        LogError($"{m_FileName} 正整数格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + str);
                    }
                    else if (str[0] == '-') {
                        LogError($"{m_FileName} 正整数填写了负数, index：" + m_CurIndex + " header:" + header.name + " info:" + str);
                    }
                    else if (string.IsNullOrWhiteSpace(str)) {
                        str = string.Empty;
                    }
                }
                m_CsvBuilder.Append(str);
                break;
            case "int":
            case "long":
            case "fixed":
                //Debug.Log("value:" + value + " type:" + value.GetType());
                if (value is string stri) {
                    if (!string.IsNullOrWhiteSpace(stri)) {
                        str = stri;
                        //检查是否字符串格式的数字
                        if (ulong.TryParse(stri, out var l)) {

                        }
                        else {
                            LogError($"{m_FileName} 整数格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                        }
                    }
                    else {
                        str = string.Empty;
                    }
                }
                else {
                    str = value.ToString();
                    if (str.IndexOf('.') >= 0) {
                        LogError($"{m_FileName} 整数格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + str);
                    }
                    else if (string.IsNullOrWhiteSpace(str)) {
                        str = string.Empty;
                    }
                }
                m_CsvBuilder.Append(str);
                break;
            case "float":
            case "double":
                if (value is string strf) {
                    if (!string.IsNullOrWhiteSpace(strf)) {
                        str = strf;
                        //检查是否字符串格式的数字
                        if (double.TryParse(strf, out var d)) {

                        }
                        else {
                            LogError($"{m_FileName} 小数格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                        }
                    }
                    else {
                        str = string.Empty;
                    }
                }
                else {
                    str = value.ToString();
                    if (string.IsNullOrWhiteSpace(str)) {
                        str = string.Empty;
                    }
                }
                m_CsvBuilder.Append(str);
                break;
            case "string":
                str = PackString(value.ToString(), false);
                m_CsvBuilder.Append(str);
                break;
            case "localizekey": //LocalizeKey
                key = PackString(value.ToString(), false);
                m_CsvBuilder.Append(key);
                break;
            case "pathkey": //PathKey
                key = PackString(value.ToString(), false);
                m_CsvBuilder.Append(key);
                break;
            default:
                return false;
        }
        return true;
    }
}
