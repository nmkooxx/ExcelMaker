using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class MainForm {

    private void init() {
        readConfig();
        readSetting();

        initUI();

        scan();
    }


    private Config m_config;
    //private string m_configPath;
    private string m_configPath = "ExcelMakerConfig.txt";
    private void readConfig() {
        if (m_configPath == null) {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //将该路径传递给 System.IO.Path.GetDirectoryName(path)，获得执行程序集所在的目录
            string directory = System.IO.Path.GetDirectoryName(path);
            m_configPath = directory + "/ExcelMakerConfig.txt";
        }

        if (File.Exists(m_configPath)) {
            string text = File.ReadAllText(m_configPath);
            m_config = JsonConvert.DeserializeObject<Config>(text);
        }
        else {
            m_config = new Config();
            writeConfig();
        }
    }

    private void writeConfig() {
        string text = JsonConvert.SerializeObject(m_config, m_jsonSettings);
        File.WriteAllText(m_configPath, text);
    }

    private Setting m_setting;
    //private string m_configPath;
    private string m_settingPath = "ExcelMakerSetting.txt";
    private void readSetting() {
        if (File.Exists(m_settingPath)) {
            string text = File.ReadAllText(m_settingPath);
            m_setting = JsonConvert.DeserializeObject<Setting>(text);
        }
        else {
            m_setting = new Setting();            
        }
    }

    private bool TryGetCombine(string name, out Combine combine) {
        for (int i = 0; i < m_setting.combines.Length; i++) {
            var cb = m_setting.combines[i];
            if (cb.name == name) {
                combine = cb;
                return true;
            }
        }

        combine = null;
        return false;
    }

    private void initUI() {
        foreach (var item in group_serverExportType.Controls) {
            var radioButton = item as RadioButton;
            if (radioButton.TabIndex == m_config.serverExportType) {
                radioButton.Checked = true;
                break;
            }
        }

        foreach (var item in group_clientExportType.Controls) {
            var radioButton = item as RadioButton;
            if (radioButton.TabIndex == m_config.clientExportType) {
                radioButton.Checked = true;
                break;
            }
        }

        input_root.Text = m_config.rootPath;
        input_server.Text = m_config.serverPath;
        input_serverCode.Text = m_config.serverCodePath;
        input_client.Text = m_config.clientPath;
        input_clientCode.Text = m_config.clientCodePath;
    }

    private List<string> m_excelPaths = new List<string>(50);
    private void scan() {
        if (!Directory.Exists(m_config.rootPath)) {
            Debug.LogError("Excel路径错误：" + m_config.rootPath);
            return;
        }

        //m_excelPaths.Clear();
        //var rootInfo = new DirectoryInfo(m_config.rootPath);

        excelList.Items.Clear();
        //m_excelPaths = Directory.GetFiles(m_config.rootPath, "*.xls|*.xlsx", SearchOption.AllDirectories);
        int offset = m_config.rootPath.Length + 1;
        string[] paths = Directory.GetFiles(m_config.rootPath, "*.xlsx", SearchOption.AllDirectories);
        for (int i = 0; i < paths.Length; i++) {
            string path = paths[i];
            if (path.IndexOf('~') >= 0) {
                continue;
            }
            string str = paths[i].Substring(offset);
            excelList.Items.Add(str);
            //默认选中
            excelList.SetItemChecked(excelList.Items.Count - 1, true);
            m_excelPaths.Add(path);
        }

        Debug.Log("路径扫描完成，总计文件个数：" + m_excelPaths.Count);
    }

    private void export(string dirPath, char type, ExportType exportType, string codePath, bool exportCode) {
        switch (exportType) {
            case ExportType.Csv:
                exportCsv(dirPath, type, codePath, exportCode);
                break;
            case ExportType.Json:
                exportJson(dirPath, type);
                break;
            default:
                break;
        }
    }

    private string getFileName(string filePath) {
        int startPos = filePath.LastIndexOf('_');
        int length = filePath.LastIndexOf('.') - startPos - 1;
        return filePath.Substring(startPos + 1, length);
    }

    private void exportCsv(string dirPath, char type, string codePath, bool exportCode) {
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        if (!Directory.Exists(codePath)) {
            Directory.CreateDirectory(codePath);
        }
        foreach (int index in excelList.CheckedIndices) {
            m_filePath = m_excelPaths[index];
            bool need = ReadExcel(m_filePath, type, initCsv, rowToCsv);
            if (!need) {
                continue;
            }

            string csvName = getFileName(m_filePath);
            string csvPath = Path.Combine(dirPath, csvName + ".csv");
            File.WriteAllText(csvPath, m_csvBuilder.ToString());

            if (exportCode) {
                if (type == 'C')
                {
                    CsvMaker.MakeCsvClass(codePath, csvName, m_headers, m_rawTypes, m_setting.importHeads);
                }
                else if(type == 'S')
                {
                    ServerCsvMaker.MakeCsvClass(codePath, csvName, m_headers, m_rawTypes, m_setting.importHeads);
                }
            }

            if (m_defineIndex > 0 && m_defineBuilder.Length > 0) {
                string className = m_defineName;
                string definePath = Path.Combine(codePath, className + ".cs");
                string defineClassStr = TemplateDefineClass.Replace("@className", className)
                    .Replace("#property#", m_defineBuilder.ToString());
                File.WriteAllText(definePath, defineClassStr);
            }
        }
        Debug.Log("导出Csv完成:" + type);
    }

    private void exportJson(string dirPath, char type) {
        foreach (int index in excelList.CheckedIndices) {
            m_filePath = m_excelPaths[index];
            bool need = ReadExcel(m_filePath, type, initJson, rowToJson);
            if (!need) {
                continue;
            }

            string path = Path.Combine(dirPath, getFileName(m_filePath) + ".json");
            if (!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(m_jObject, m_jsonSettings));
        }
        Debug.Log("导出Json完成:" + type);
    }

    private object getCellValue(ICell cell) {
        short format = cell.CellStyle.DataFormat;
        switch (cell.CellType) {
            case CellType.Blank:
                return null;
            case CellType.Numeric:
                //对时间格式（2015.12.5、2015/12/5、2015-12-5等）的处理 
                if (format == 14 || format == 31 || format == 57 || format == 58) {
                    return cell.DateCellValue;
                }
                else if (format == 177 || format == 178 || format == 188) {
                    return cell.NumericCellValue.ToString("#0.00");
                }
                else {
                    return cell.NumericCellValue;
                }
            case CellType.String:
                return cell.StringCellValue;
            case CellType.Formula:
                try {
                    if (format == 177 || format == 178 || format == 188) {
                        return cell.NumericCellValue.ToString("#0.00");
                    }
                    else {
                        return cell.NumericCellValue;
                    }
                }
                catch {
                    return cell.StringCellValue;
                }
            default:
                Debug.LogWarning("导出配置错误:" + m_filePath + " 未定义单元格类型：" + cell.CellType);
                return cell.StringCellValue;
        }
    }

    private string m_filePath;
    private CsvHeader[] m_headers;
    private CsvNode[] m_nodes;
    private string[] m_rawTypes;
    private string[] m_cellTypes;
    private int m_defineIndex;
    private string m_defineName;

    private bool ReadExcel(string filePath, char type, Action headAction, Action<IRow> rowAction) {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
            XSSFWorkbook workbook = new XSSFWorkbook(fs);
            //只读取第一张表
            ISheet sheet = workbook.GetSheetAt(0);
            if (sheet == null) {
                Debug.LogError("ReadExcel Error Sheet:" + filePath);
                return false;
            }

            //第一行为表头，必定是最大列数
            IRow nameRow = sheet.GetRow(0);
            int cellCount = nameRow.LastCellNum;
            //表头
            IRow headRow = sheet.GetRow(1);
            //导出类型
            IRow typeRow = sheet.GetRow(2);
            //导出服务器客户端
            IRow exportRow = sheet.GetRow(3);

            ICell cell = exportRow.GetCell(0);
            if (cell == null || cell.StringCellValue == null) {
                Debug.LogError("导出配置错误:" + filePath);
                return false;
            }
            if (cell.StringCellValue != "A" && !cell.StringCellValue.Contains(type)) {
                Debug.LogWarning("跳过导出:" + filePath);
                return false;
            }

            Debug.Log("导出:" + filePath);

            string[] exportSettings = new string[cellCount];
            for (int i = 0; i < cellCount; i++) {
                cell = exportRow.GetCell(i);
                if (cell == null) {
                    exportSettings[i] = string.Empty;
                    continue;
                }
                exportSettings[i] = cell.StringCellValue;
            }

            m_defineIndex = -1;
            m_rawTypes = new string[cellCount];
            m_cellTypes = new string[cellCount];
            //第一列为id，只支持int，string
            cell = typeRow.GetCell(0);
            string value = cell.StringCellValue;
            if (value[0] == CsvConfig.skipFlag) {
                m_rawTypes[0] = value;
                m_cellTypes[0] = value.Substring(1);
            }
            else {
                m_rawTypes[0] = CsvConfig.skipFlag + value;
                m_cellTypes[0] = value;
            }
            for (int i = 1; i < cellCount; i++) {
                cell = typeRow.GetCell(i);
                if (cell == null) {
                    continue;
                }
                value = cell.StringCellValue;
                m_rawTypes[i] = value;
                int pos = value.LastIndexOf(CsvConfig.classSeparator);
                if (pos > 0) {
                    m_cellTypes[i] = value.Substring(pos + 1);
                }
                else {
                    m_cellTypes[i] = value;
                }
            }

            m_headers = new CsvHeader[cellCount];
            for (int i = 0; i < cellCount; i++) {
                if (string.IsNullOrEmpty(exportSettings[i])) {
                    m_headers[i] = CsvHeader.Pop(string.Empty);
                    continue;
                }
                if (exportSettings[i] != "A" && !exportSettings[i].Contains(type)) {
                    m_headers[i] = CsvHeader.Pop(string.Empty);
                    continue;
                }

                cell = headRow.GetCell(i);
                if (cell == null) {
                    m_headers[i] = CsvHeader.Pop(string.Empty);
                    continue;
                }

                var cellType = m_cellTypes[i];
                if (cellType == "define") {
                    m_defineIndex = i;
                    m_defineName = cell.StringCellValue;
                    m_headers[i] = CsvHeader.Pop(string.Empty);
                    continue;
                }

                m_headers[i] = CsvHeader.Pop(cell.StringCellValue);
            }
            int slot;
            List<string> nodeSlots = new List<string>(1);
            List<CsvNode> nodes = new List<CsvNode>(1);
            for (int i = 0; i < m_headers.Length; i++) {
                CsvHeader header = m_headers[i];
                if (header.type == eFieldType.Primitive) {
                    header.SetSlot(-1);                    
                    continue;
                }
                slot = nodeSlots.IndexOf(header.baseName);
                if (slot < 0) {
                    nodeSlots.Add(header.baseName);
                    header.SetSlot(nodeSlots.Count - 1);
                    var node = CsvNode.Pop(header.baseName, header.type);
                    var subTypes = m_rawTypes[i].Split(CsvConfig.arrayChars, CsvConfig.classSeparator);
                    node.cellType = subTypes[0];
                    nodes.Add(node);
                }
                else {
                    header.SetSlot(slot);
                }
            }
            if (nodeSlots.Count > 0) {
                m_nodes = nodes.ToArray();
            }
            else {
                m_nodes = new CsvNode[0];
            }

            headAction();

            int startIndex = 4;// sheet.FirstRowNum;
            int lastIndex = sheet.LastRowNum;
            for (int index = startIndex; index <= lastIndex; index++) {
                IRow row = sheet.GetRow(index);
                if (row == null) {
                    continue;
                }

                //跳过id为空，或者#号开头的行
                cell = row.GetCell(0);
                if (cell == null) {
                    continue;
                }
                var obj = getCellValue(cell);
                if (obj == null) {
                    continue;
                }
                if (obj.ToString()[0] == CsvConfig.skipFlag) {
                    continue;
                }

                try {
                    rowAction(row);
                }
                catch (Exception e) {
                    Debug.LogError(m_filePath + " 转换错误, id：" + obj + " \n" + e);
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 将含有特殊符号的字符串包裹起来
    /// 不支持Json格式的String[]
    /// </summary>
    /// <param name="rawString"></param>
    /// <returns></returns>
    private string packString(string rawString, bool force) {
        string newString = rawString;
        if (rawString.IndexOf(CsvConfig.quote) > 0) {
            //在非第一个字符串中出现引号，则需要替换
            newString = newString.Replace("\"", "\"\"");
        }

        if (force || rawString.IndexOf(CsvConfig.delimiter) > 0 ) {
            //出现逗号分隔符，需要包裹
            newString = string.Format("\"{0}\"", newString);
        }

        return newString;
    }

    private StringBuilder m_csvBuilder;
    private StringBuilder m_defineBuilder;
    private void initCsv() {
        m_csvBuilder = new StringBuilder();
        m_csvBuilder.Append(m_headers[0].name);
        for (int i = 1; i < m_headers.Length; i++) {
            var header = m_headers[i];
            if (header.skip) {
                continue;
            }
            m_csvBuilder.Append(CsvConfig.delimiter);
            m_csvBuilder.Append(header.name);
        }
        m_csvBuilder.Append("\n");

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

        if (m_defineIndex > 0) {
            m_defineBuilder = new StringBuilder();
        }
        else {
            m_defineBuilder = null;
        }
    }

    private void rowToCsv(IRow row) {
        object value;
        string cellType;
        string info;
        CsvHeader header;
        for (int rank = 0; rank < m_headers.Length; rank++) {
            header = m_headers[rank];
            if (header.skip) {
                continue;
            }

            if (rank != 0) {
                m_csvBuilder.Append(CsvConfig.delimiter);
            }
            ICell cell = row.GetCell(rank);
            if (cell == null) {
                continue;
            }
            value = getCellValue(cell);
            if (value == null) {
                continue;
            }
            cellType = m_cellTypes[rank].ToLower();
            switch (cellType) {
                case "bool":
                case "uint":
                case "int":
                case "ulong":
                case "long":
                case "float":
                case "double":
                    m_csvBuilder.Append(value);
                    break;
                case "string":
                    m_csvBuilder.Append(packString(value.ToString(), false));
                    break;
                default:
                    //对象结构需要引号包起来
                    info = value.ToString();
                    if (cell.CellType == CellType.String) {
                        bool isJson = false;
                        if (info.Length > 0 && (info[0] == '[' || info[0] == '{')) {
                            isJson = true;
                        }
                        m_csvBuilder.Append(packString(info, isJson));
                    }
                    else {
                        m_csvBuilder.Append(packString(info, false));
                    }
                    break;
            }
        }
        m_csvBuilder.Append("\n");

        rowToDefine(row);
    }

    static string TemplateDefineClass = @"public partial class @className {
#property#
}
";

    static string TemplateDefineField = @"
    public const @type @name = @value;";

    private void rowToDefine(IRow row) {
        if (m_defineIndex <= 0) {
            return;
        }
        ICell cell = row.GetCell(m_defineIndex);
        if (cell == null) {
            return;
        }
        object value = getCellValue(cell);
        if (value == null) {
            return;
        }

        string type = m_cellTypes[0];
        string template = TemplateDefineField.Replace("@type", type)
                    .Replace("@name", value.ToString());
        if (type == "string") {
            template = template.Replace("@value", '"' + getCellValue(row.GetCell(0)).ToString() + '"');
        }
        else {
            template = template.Replace("@value", getCellValue(row.GetCell(0)).ToString());
        }
        m_defineBuilder.Append(template);
    }


    private JsonSerializerSettings m_jsonSettings;
    private JObject m_jObject;
    private void initJson() {
        if (m_jsonSettings == null) {
            m_jsonSettings = new JsonSerializerSettings();
            m_jsonSettings.Formatting = Formatting.Indented;
        }

        m_jObject = new JObject();
    }

    private void rowToJson(IRow row) {
        object value;
        JToken token;
        string cellType;
        CsvHeader header;
        CsvNode node;
        JObject csv = new JObject();
        for (int rank = 0; rank < m_headers.Length; rank++) {
            header = m_headers[rank];
            if (header.skip) {
                continue;
            }

            ICell cell = row.GetCell(rank);
            if (cell == null || cell.CellType == CellType.Blank) {
                continue;
            }
            value = getCellValue(cell);
            cellType = m_cellTypes[rank].ToLower();
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
                    token = JToken.FromObject(Convert.ToInt64(value));
                    break;
                case "float":
                case "double":
                    token = JToken.FromObject(value);
                    break;
                case "string":
                    token = JToken.FromObject(value);
                    break;
                default:
                    token = JToken.Parse((string)value);
                    break;
            }

            if (header.type == eFieldType.Primitive) {
                csv.Add(header.name, token);
            }
            else {
                if (header.slot < 0) {
                    Debug.LogError(m_filePath + " lineToCsv map error:" + csv["id"] + " field:" + header.name + " " + header.baseName);
                    continue;
                }
                node = m_nodes[header.slot];
                node.Add(header, token);
            }
        }

        //最后统一处理多列合并一列的情况，以兼容字段分散的情况
        for (int i = 0; i < m_nodes.Length; i++) {
            node = m_nodes[i];

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
                csv.Add(node.name, node.ToJToken());
            }

            //重置node，以便下一条使用
            node.Reset();
        }

        m_jObject.Add(csv["id"].ToString(), csv);
    }
}
