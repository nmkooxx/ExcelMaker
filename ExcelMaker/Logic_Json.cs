using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public partial class Logic {
    private void ExportJson(string dirPath, bool sync, char type) {
        int slot = 0;
        string csvName = null, csvDir = null, path = null;
        List<string> paths = new List<string>(8);
        m_LogBuilder.AppendLine($"导出Json type:{type} sync:{sync}");
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
                ++slot;
                if (m_Setting.exportDir && csvDir != null) {
                    path = Path.Combine(dirPath, csvDir + "/" + csvName + ".json");
                }
                else {
                    path = Path.Combine(dirPath, csvName + ".json");
                }
                if (!Directory.Exists(dirPath)) {
                    Directory.CreateDirectory(dirPath);
                }
            }
            else {
                //同步模式，检查表格是否已经存在
                if (m_Setting.exportDir) {
                    path = Path.Combine(dirPath, csvDir + "/" + csvName + ".json");
                }
                else {
                    path = Path.Combine(dirPath, csvName + ".json");
                }
                if (!File.Exists(path)) {
                    continue;
                }
            }

            paths.Clear();
            foreach (var item in list) {
                paths.Add(item.path);
            }
            bool need = ReadExcel(paths, type, InitJson, RowToJson, FinishJson);
            if (!need) {
                continue;
            }
            string text = JsonConvert.SerializeObject(m_JObject, m_JsonSettings);
            text = Regex.Replace(text, "(?<!\r)\n|\r\n", "\n");
            File.WriteAllText(path, text, CsvConfig.encoding);

            m_LogBuilder.AppendLine(path);
        }
        Debug.Log(m_LogBuilder.ToString());
    }

    private bool CheckJsonFormat(string cellType, string info, CsvHeader header) {
        char lastTypeChar = cellType[cellType.Length - 1];
        char lastInfoChar = info[info.Length - 1];
        if (lastTypeChar == ']') {
            if (lastInfoChar != ']') {
                LogError("Json数组[]格式错误, index：" + m_CurIndex + " header:" + header.name + " info:" + info);
                return true;
            }
            CheckJsonArray(cellType, info, header);
            return true;
        }
        else {
            if (lastInfoChar != '}') {
                if (lastInfoChar == ']') {
                    CheckJsonArray(cellType, info, header);
                    return true;
                }
                if (!IsEnum(cellType) && header.subs == null) {
                    //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError($"{m_FileName} 扩展格式错误,index:{m_CurIndex} header:{header.name} info:{info}");
                }
                return false;
            }

            CheckJsonClass(cellType, info, header);
            return true;
        }
    }

    private void CheckJsonArray(string cellType, string info, CsvHeader header) {
        header.InitJToken(cellType);

        //检查Json格式
        try {
            var array = JsonConvert.DeserializeObject(info) as JArray;
            if (array == null) {
                LogError($"{m_FileName} Json数组格式错误, index：{m_CurIndex} header:{header.name} info:{info}");
                return;
            }
            var token = array.First;
            for (int rank = header.arrayRank - 1; rank > 0; rank--) {
                if (!token.HasValues) {
                    LogError($"{m_FileName} Json数组维度错误, index：{m_CurIndex} header:{header.name} info:{info}");
                    return;
                }
                token = token.First;
            }
            if (!header.CheckJToken(token)) {
                LogError($"{m_FileName} Json数组内数据类型错误, index：{m_CurIndex} header:{header.name} type:{cellType} J:{token} info:{info}" );
            }
        }
        catch (Exception e) {
            LogError($"{m_FileName} Json数组解析错误, index：{m_CurIndex} header:{header.name} info:{info}\n{e}");
        }

    }

    private void CheckJsonClass(string cellType, string info, CsvHeader header) {
        //检查Json格式
        try {
            var obj = JsonConvert.DeserializeObject(info);
        }
        catch (Exception e) {
            LogError($"{m_FileName} Json对象解析错误, index:{m_CurIndex} header:{header.name} info:{info} \n{e}");
        }
    }

    private JsonSerializerSettings m_JsonSettings;
    private JObject m_JObject;
    private void InitJson() {
        if (m_JsonSettings == null) {
            m_JsonSettings = new JsonSerializerSettings();
            m_JsonSettings.Formatting = Formatting.Indented;
        }

        m_JObject = new JObject();
    }

    private void FinishJson() {
        
    }

    private void RowToJson(char type, IRow row) {
        object value;
        JToken token;
        string cellType;
        CsvHeader header;
        CsvNode node;
        string info;
        if (m_DefineStyle) {
            int rank = m_Headers.Count - 1;
            header = m_Headers[rank];

            var cell = row.GetCell(3);
            if (cell == null) {
                return;
            }
            value = GetCellValue(cell);
            if (value == null) {
                return;
            }

            cellType = m_CellTypes[rank].ToLower();
            switch (cellType) {
                case "bool":
                    token = JToken.FromObject(Convert.ToBoolean(value));
                    break;
                case "byte":
                    token = JToken.FromObject(Convert.ToByte(value));
                    break;
                case "ushort":
                    token = JToken.FromObject(Convert.ToUInt16(value));
                    break;
                case "uint":
                    token = JToken.FromObject(Convert.ToUInt32(value));
                    break;
                case "ulong":
                    token = JToken.FromObject(Convert.ToUInt64(value));
                    break;
                case "short":
                    token = JToken.FromObject(Convert.ToInt16(value));
                    break;
                case "int":
                    token = JToken.FromObject(Convert.ToInt32(value));
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
                    if (string.IsNullOrWhiteSpace(info)) {
                        return;
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
                            Debug.LogError($"格式错误 index:{m_CurIndex} header:{header.name}\ninfo:{value}");
                        }
                        token = JToken.FromObject(Convert.ToInt32(value));
                    }
                    break;
            }

            m_JObject.Add(header.name, token);
            return;
        }


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
                case "byte":
                    token = JToken.FromObject(Convert.ToByte(value));
                    break;
                case "ushort":
                    token = JToken.FromObject(Convert.ToUInt16(value));
                    break;
                case "uint":
                    token = JToken.FromObject(Convert.ToUInt32(value));
                    break;
                case "ulong":
                    token = JToken.FromObject(Convert.ToUInt64(value));
                    break;
                case "short":
                    token = JToken.FromObject(Convert.ToInt16(value));
                    break;
                case "int":
                    token = JToken.FromObject(Convert.ToInt32(value));
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
                        LogError("单元格类型错误 index：" + m_CurIndex + " header:" + header.name + " info:" + value);
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

        m_JObject.Add(csv["id"].ToString(), csv);
    }
}
