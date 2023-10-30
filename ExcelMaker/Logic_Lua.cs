using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public partial class Logic {
    private void ExportLua(string dirPath, bool sync, char type, string codePaths) {
        string localPath;
        if (type == 'C') {
            localPath = setting.clientPath;
        }
        else {
            localPath = setting.serverPath;
        }
        if (!Directory.Exists(localPath)) {
            Directory.CreateDirectory(localPath);
        }
        m_LogBuilder.AppendLine($"导出Lua type:{type} sync:{sync}");
        int offset = m_Config.rootPath.Length + 1;
        string luaName = null, luaDir = null, path = null;
        string localLuaPath = null;
        List<string> paths = new List<string>(8);
        foreach (var list in m_ExcelMap.Values) {
            var info = list[0];
            luaDir = info.folder;
            luaName = info.name.ToLower() + "productdata";
            m_FileName = info.name;
            if (!sync) {
                //选择模式
                if (!info.selected) {
                    continue;
                }
                if (m_Setting.exportDir && luaDir != null) {
                    path = dirPath + "/" + luaDir + "/" + luaName + ".lua";
                    localLuaPath = localPath + "/" + luaDir + "/" + luaName + ".lua";
                    if (!Directory.Exists(dirPath + "/" + luaDir)) {
                        Directory.CreateDirectory(dirPath + "/" + luaDir);
                    }
                    if (!Directory.Exists(localPath + "/" + luaDir)) {
                        Directory.CreateDirectory(localPath + "/" + luaDir);
                    }
                }
                else {
                    path = dirPath + "/" + luaName + ".lua";
                    localLuaPath = localPath + "/" + luaName + ".lua";
                }
            }
            else {
                //同步模式，检查表格是否已经存在
                if (m_Setting.exportDir && luaDir != null) {
                    path = dirPath + "/" + luaDir + "/" + luaName + ".lua";
                    localLuaPath = localPath + "/" + luaDir + "/" + luaName + ".lua";
                }
                else {
                    path = dirPath + "/" + luaName + ".lua";
                    localLuaPath = localPath + "/" + luaName + ".lua";
                }
                if (!File.Exists(path)) {
                    continue;
                }
            }

            bool need;
            string luaText;
            if (keepSplit) {
                m_LogBuilder.AppendLine(path);
                foreach (var item in list) {
                    paths.Clear();
                    paths.Add(item.path);
                    need = ReadExcel(paths, type, InitLua, RowToLua, FinishLua);
                    if (!need) {
                        continue;
                    }
                    if (m_FileName == "Localize" || m_FileName == "Text") {
                        for (int l = 0; l < m_LocalizeNames.Length; l++) {
                            var localizeName = m_LocalizeNames[l];
                            luaText = m_LocalizeBuilders[l].ToString();
                            luaText = Regex.Replace(luaText, "(?<!\r)\n|\r\n", "\n");

                            localLuaPath = $"{localPath}/{localizeName}/{luaName}.lua";
                            if (!Directory.Exists(localLuaPath)) {
                                Directory.CreateDirectory(localLuaPath);
                            }
                            File.WriteAllText(localLuaPath, luaText, CsvConfig.encoding);
                        }
                    }
                    else {
                        luaText = m_LuaBuilder.ToString();
                        luaText = Regex.Replace(luaText, "(?<!\r)\n|\r\n", "\n");

                        localLuaPath = localPath + "/" + item.nameWithDir.Replace(".xlsx", ".lua");
                        File.WriteAllText(localLuaPath, luaText, CsvConfig.encoding);
                    }

                }

                continue;
            }

            paths.Clear();
            foreach (var item in list) {
                paths.Add(item.path);
            }
            need = ReadExcel(paths, type, InitLua, RowToLua, FinishLua);
            if (!need) {
                continue;
            }
            if (m_FileName == "Localize" || m_FileName == "Text") {
                for (int l = 0; l < m_LocalizeNames.Length; l++) {
                    var localizeName = m_LocalizeNames[l];
                    luaText = m_LocalizeBuilders[l].ToString();
                    luaText = Regex.Replace(luaText, "(?<!\r)\n|\r\n", "\n");

                    if (!Directory.Exists($"{dirPath}/{localizeName}/")) {
                        Directory.CreateDirectory($"{dirPath}/{localizeName}/");
                    }
                    path = $"{dirPath}/{localizeName}/{luaName}.lua";
                    File.WriteAllText(path, luaText, CsvConfig.encoding);
                    m_LogBuilder.AppendLine(path);

                    if (!Directory.Exists($"{localPath}/{localizeName}/")) {
                        Directory.CreateDirectory($"{localPath}/{localizeName}/");
                    }
                    localLuaPath = $"{localPath}/{localizeName}/{luaName}.lua";
                    File.WriteAllText(localLuaPath, luaText, CsvConfig.encoding);
                }
            }
            else {
                luaText = m_LuaBuilder.ToString();
                luaText = Regex.Replace(luaText, "(?<!\r)\n|\r\n", "\n");

                File.WriteAllText(path, luaText, CsvConfig.encoding);
                //在本地同时保留一份，方便提交svn对比存档
                File.WriteAllText(localLuaPath, luaText, CsvConfig.encoding);
                m_LogBuilder.AppendLine(path);
            }

            if (m_DefineIndex >= 0) {
                string[] codePathArrray = codePaths.Split(';');
                string defineClassStr = m_LuaDefineBuilder.ToString();
                defineClassStr = Regex.Replace(defineClassStr, "(?<!\r)\n|\r\n", "\n");
                foreach (string codePath in codePathArrray) {
                    if (!Directory.Exists(codePath)) {
                        Directory.CreateDirectory(codePath);
                    }

                    m_LuaDefineBuilder.AppendLine("}");
                    string definePath = Path.Combine(codePath, m_DefineName + ".lua");
                    File.WriteAllText(definePath, defineClassStr, CsvConfig.encoding);
                }
            }
        }
        Debug.Log(m_LogBuilder.ToString());
    }

    private StringBuilder m_LuaBuilder;
    private StringBuilder m_LuaDefineBuilder;

    private void InitLua() {
        if (m_FileName == "Localize" || m_FileName == "Text") {
            var cnt = m_Headers.Count - 1;
            m_LocalizeBuilders = new StringBuilder[cnt];
            m_LocalizeNames = new string[cnt];
            m_LocalizeReplaces = new Dictionary<string, string>[cnt];
            for (int i = 1; i < m_Headers.Count; i++) {
                var header = m_Headers[i];
                m_LocalizeNames[i - 1] = header.name;
                var builder = new StringBuilder();
                builder.AppendLine("local data = {");
                m_LocalizeBuilders[i - 1] = builder;
                m_LocalizeReplaces[i - 1] = new Dictionary<string, string>();
            }
            m_LuaBuilder = null;
            return;
        }

        m_LuaBuilder = new StringBuilder();
        if (m_DefineStyle) {
            //采用有全局名称
            m_LuaBuilder.AppendLine($"{m_FileName} = {{");
            return;
        }
        m_LuaBuilder.AppendLine("local data = {");

        m_LuaDefineBuilder = new StringBuilder();
        //全局变量
        m_LuaDefineBuilder.AppendLine($"{m_DefineName} = {{");
    }

    private void FinishLua() {
        if (m_FileName == "Localize" || m_FileName == "Text") {
            for (int i = 1; i < m_Headers.Count; i++) {
                var builder = m_LocalizeBuilders[i-1];
                builder.AppendLine("}\nreturn data");
            }
            return;
        }

        if (m_DefineStyle) {
            m_LuaBuilder.AppendLine("}");
            return;
        }
        m_LuaBuilder.AppendLine("}\nreturn data");
    }

    private void RowToLua(char type, IRow row) {
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

            m_LuaBuilder.Append($"\t{value.ToString()} = " );

            int rank = m_Headers.Count - 1;
            header = m_Headers[rank];
            cellType = m_CellTypes[rank];

            cell = row.GetCell(3);
            if (cell == null) {
                m_LuaBuilder.AppendLine("nil,");
                return;
            }
            value = GetCellValue(cell);
            if (value == null) {
                m_LuaBuilder.AppendLine("nil,");
                return;
            }

            bool isSimple = LuaCheckSimpleFormat(cellType, value, header, rank);
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
                    //将Json转为Lua Table
                    if (isJson) {
                        m_LuaBuilder.Append(info.Replace('[', '{').Replace(']', '}'));
                    }
                    else {
                        m_LuaBuilder.Append(info);
                    }
                    m_LuaBuilder.AppendLine(",");
                }
                else {
                    if (!IsEnum(cellType) && header.subs == null) {
                        //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError($"{m_FileName} 扩展格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                    }
                    //m_LuaBuilder.Append(PackString(info, false));
                    //TODO 这里需要转为Lua Table对象

                    m_LuaBuilder.AppendLine(",");
                }
            }

            return;
        }

        if (m_FileName == "Localize" || m_FileName == "Text") {
            ICell cell = row.GetCell(0);
            value = GetCellValue(cell);
            m_LocalizeId = value;

            for (int i = 0; i < m_LocalizeBuilders.Length; i++) {
                var builder = m_LocalizeBuilders[i];
                builder.AppendLine($"\t{PackString(value.ToString(), false)} = {{");
            }

            bool needParse = false;
            for (int rank = 1; rank < m_Headers.Count; rank++) {
                var builder = m_LocalizeBuilders[rank -1];

                cell = row.GetCell(rank);
                if (cell == null) {
                    continue;
                }
                value = GetCellValue(cell);
                if (value == null) {
                    continue;
                }

                builder.AppendLine($"\t\tvalue = {PackLocalizeString(value.ToString(), true, rank - 1, out needParse)},");
                builder.AppendLine($"\t\tneedParse = {needParse.ToString()}");
                builder.AppendLine("\t},");
            }

            return;
        }

        JToken token;
        CsvNode node;
        JObject csv = new JObject();
        for (int rank = 0; rank < m_Headers.Count; rank++) {
            header = m_Headers[rank];
            if (header.skip) {
                continue;
            }

            ICell cell = row.GetCell(rank);
            if (cell == null || cell.CellType == CellType.Blank) {
                continue;
            }
            value = GetCellValue(cell);
            cellType = m_CellTypes[rank].ToLower();
            switch (cellType) {
                case "bool":
                    token = JToken.FromObject(Convert.ToBoolean(value));
                    break;
                case "uint":
                    token = JToken.FromObject(Convert.ToUInt32(value));
                    break;
                case "int":
                    token = JToken.FromObject(Convert.ToInt32(value));
                    break;
                case "ulong":
                    token = JToken.FromObject(Convert.ToUInt64(value));
                    break;
                case "long":
                case "fixed":
                    token = JToken.FromObject(Convert.ToInt64(value));
                    break;
                case "float":
                case "double":
                    token = JToken.FromObject(value);
                    break;
                case "string":
                //json不特殊处理key类型
                case "localizekey":
                case "pathkey":
                    token = JToken.FromObject(value);
                    break;
                default:
                    info = value.ToString();
                    if (info.Length <= 0) {
                        LogError($"单元格类型错误, index:{m_CurIndex} header:{header.name} info:{value}");
                        continue;
                    }
                    if (cell.CellType == CellType.String) {
                        bool isJson = false;
                        char frist = info[0];
                        char last = info[info.Length - 1];
                        if ((frist == '[' && last == ']') || (frist == '{' && last == '}')) {
                            isJson = true;
                        }
                        else {
                            if (!IsEnum(cellType) && header.subs == null) {
                                //Debug.LogError(m_filePath + " 格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                                Debug.LogError($"Json格式错误 index:{m_CurIndex} header:{header.name}\n头:{frist} 尾:{last}\ninfo:{value}");
                            }
                        }
                        if (isJson) {
                            token = JToken.Parse(info);
                        }
                        else {
                            token = JToken.FromObject(value);
                        }
                    }
                    else {
                        if (!IsEnum(cellType) && header.subs == null) {
                            //Debug.LogError(m_filePath + " 格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                            LogError("格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                        }
                        token = JToken.FromObject(Convert.ToInt32(value));
                    }
                    break;
            }

            if (header.type == eFieldType.Primitive) {
                csv.Add(header.name, token);
            }
            else {
                if (header.slot < 0) {
                    //Debug.LogError(m_filePath + " lineToCsv map error:" + csv["id"] + " field:" + header.name + " " + header.baseName);
                    LogError("rowToJson map error:" + csv["id"] + " field:" + header.name + " " + header.baseName);
                    continue;
                }
                node = m_Nodes[header.slot];
                node.Add(header, token);
            }
        }

        //最后统一处理多列合并一列的情况，以兼容字段分散的情况
        for (int i = 0; i < m_Nodes.Count; i++) {
            node = m_Nodes[i];

            token = node.ToJToken();
            if (token is JArray array && TryGetCombine(node.cellType, out Combine combine)) {
                var subObj = new JObject();
                for (int j = 0; j < array.Count; j++) {
                    var obj = array[j];
                    if (combine.values.Length > 1) {
                        subObj.Add(obj[combine.key].ToString(), obj);
                    }
                    else {
                        subObj.Add(obj[combine.key].ToString(), obj[combine.values[0]]);
                    }
                }
                csv.Add(node.name, subObj);
            }
            else {
                csv.Add(node.name, token);
            }

            //重置node，以便下一条使用
            node.Reset();
        }

        if (m_JsonSettings == null) {
            m_JsonSettings = new JsonSerializerSettings();
            m_JsonSettings.Formatting = Formatting.Indented;
        }
        string text = JsonConvert.SerializeObject(csv, m_JsonSettings);
        text = Regex.Replace(text, "(?<!\r)\n|\r\n", "\n")
                .Replace("\":", " =")
                .Replace("\"", "")
                .Replace("[", "{")
                .Replace("]", "}")
                .Replace("        ", "\t\t\t\t\t")
                .Replace("      ",   "\t\t\t\t")
                .Replace("    ",     "\t\t\t")
                .Replace("  ",       "\t\t")
                .Replace("\n}", "\n\t}")
                //.Replace("}\n}", "}\n\t}")
                ;
        m_LuaBuilder.AppendLine($"\t[{csv["id"].ToString()}] = {text},");

        RowToLuaDefine(row);
    }

    private void RowToLuaDefine(IRow row) {
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

        object id = GetCellValue(row.GetCell(0));
        m_LuaDefineBuilder.AppendLine($"\t{value} = {id},");
    }

    private bool LuaCheckSimpleFormat(string cellType, object value, CsvHeader header, int slot) {
        string key;
        string str;
        switch (cellType.ToLower()) {
            case "bool":
                if (value is string strb) {
                    if (!string.IsNullOrWhiteSpace(strb)) {
                        str = strb;
                        if (strb.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                            str = "true";
                        }
                        else if (strb.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                            str = "false";
                        }
                        else if (str == "1") {
                            str = "true";
                        }
                        else if (str == "0") {
                            str = "false";
                        }
                        else if (string.IsNullOrWhiteSpace(str)) {
                            str = "false";
                        }
                        else {
                            LogError($"{m_FileName} bool格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                            str = "false";
                        }
                    }
                    else {
                        str = "false";
                    }
                }
                else {
                    str = value.ToString();
                    if (str == "1") {
                        str = "true";
                    }
                    else if (str == "0") {
                        str = "false";
                    }
                    else if (string.IsNullOrWhiteSpace(str)) {
                        str = "false";
                    }
                    else {
                        LogError($"{m_FileName} bool格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + value);
                        str = "false";
                    }
                }
                m_LuaBuilder.Append(str);
                m_LuaBuilder.AppendLine(",");
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
                m_LuaBuilder.Append(str);
                m_LuaBuilder.AppendLine(",");
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
                m_LuaBuilder.Append(str);
                m_LuaBuilder.AppendLine(",");
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
                m_LuaBuilder.Append(str);
                m_LuaBuilder.AppendLine(",");
                break;
            case "string":
                str = PackString(value.ToString(), false);
                m_LuaBuilder.Append(str);
                m_LuaBuilder.AppendLine(",");
                break;
            case "localizekey": //LocalizeKey
                key = PackString(value.ToString(), false);
                m_LuaBuilder.Append(key);
                m_LuaBuilder.AppendLine(",");
                break;
            case "pathkey": //PathKey
                key = PackString(value.ToString(), false);
                m_LuaBuilder.Append(key);
                m_LuaBuilder.AppendLine(",");
                break;
            default:
                return false;
        }
        return true;
    }
}
