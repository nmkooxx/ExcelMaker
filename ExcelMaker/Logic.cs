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
using System.Text.RegularExpressions;
using System.Windows.Forms;

public partial class MainForm {

    private void init() {
        readConfig();
        readSetting();
        readExtend();

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

    private void writeSetting() {
        string text = JsonConvert.SerializeObject(m_setting, m_jsonSettings);
        File.WriteAllText(m_settingPath, text);
    }

    private string m_extendPath = "ExcelMakerExtend";
    //language -> place -> file -> info
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> m_extends;
    private void readExtend() {
        if (!Directory.Exists(m_extendPath)) {
            return;
        }
        var root = new DirectoryInfo(m_extendPath);
        var dirs = root.GetDirectories();
        m_extends = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(dirs.Length);
        for (int i = 0; i < dirs.Length; i++) {
            var dir = dirs[i];
            var subDirs = dir.GetDirectories();
            if (subDirs.Length == 0) {
                continue;
            }
            var languageExtends = new Dictionary<string, Dictionary<string, string>>(subDirs.Length);
            m_extends[dir.Name] = languageExtends;
            for (int j = 0; j < subDirs.Length; j++) {
                var subDir = subDirs[j];
                var files = subDir.GetFiles();
                if (files.Length == 0) {
                    continue;
                }
                var keyExtends = new Dictionary<string, string>(files.Length);
                languageExtends[subDir.Name] = keyExtends;
                for (int k = 0; k < files.Length; k++) {
                    var file = files[k];
                    var info = File.ReadAllText(file.FullName);
                    string fileName = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
                    keyExtends[fileName] = info;
                }
            }
        }
    }

    private bool TryGetCombine(string name, out Combine combine) {
        if (m_setting.combines == null) {
            combine = null;
            return false;
        }
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

    private bool TryGetExtend(string language, string key, string fileName, out string extend) {
        extend = null;
        if (m_extends == null) {
            return false;
        }
        Dictionary<string, Dictionary<string, string>> languageExtends;
        if (!m_extends.TryGetValue(language, out languageExtends)) {
            return false;
        }
        Dictionary<string, string> keyExtends;
        if (!languageExtends.TryGetValue(key, out keyExtends)) {
            return false;
        }

        return keyExtends.TryGetValue(fileName, out extend);
    }

    private bool IsEnum(string name) {
        Header header;
        switch (s_exportLanguage) {
            case ExportLanguage.CSharp:
                if (CsvMaker_CSharp.TryGetHeader(name, out header)) {
                    return header.isEnum;
                }
                return false;
            case ExportLanguage.Java:
                if (CsvMaker_Java.TryGetHeader(name, out header)) {
                    return header.isEnum;
                }
                return false;
            case ExportLanguage.TypeScript:
                if (CsvMaker_TypeScript.TryGetHeader(name, out header)) {
                    return header.isEnum;
                }
                return false;
            default:
                Debug.LogError("IsEnum error exportLanguage:" + s_exportLanguage);
                return false;
        }
    }

    private void initUI() {
        foreach (var item in group_serverExportType.Controls) {
            var radioButton = item as RadioButton;
            if (radioButton.TabIndex == m_setting.serverExportType) {
                radioButton.Checked = true;
                break;
            }
        }

        foreach (var item in group_clientExportType.Controls) {
            var radioButton = item as RadioButton;
            if (radioButton.TabIndex == m_setting.clientExportType) {
                radioButton.Checked = true;
                break;
            }
        }

        input_root.Text = m_config.rootPath;
        input_server.Text = m_config.serverPath;
        input_serverCode.Text = m_config.serverCodePath;
        input_client.Text = m_config.clientPath;
        input_clientCode.Text = m_config.clientCodePath;

        check_useSheetName.Checked = m_setting.nameSource == 1;
    }

    private List<string> m_excelPaths = new List<string>(50);
    private void scan() {
        if (!Directory.Exists(m_config.rootPath)) {
            Debug.LogError("Excel路径错误：" + m_config.rootPath);
            return;
        }

        m_excelPaths.Clear();
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

    private static ExportLanguage s_exportLanguage;
    private void export(string dirPath, char type, ExportType exportType, ExportLanguage language, string codePath, bool exportCode) {
        s_exportLanguage = language;
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
        if (m_setting.nameSource == 1) {
            return m_sheetName;
        }
        DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
        string fileName = directoryInfo.Name;
        string[] keys = fileName.Substring(0, fileName.Length - directoryInfo.Extension.Length).Split('_');
        if (keys.Length == 2) {
            return keys[0];
        }
        else {
            return keys[keys.Length - 1];
        }
    }

    private void exportCsv(string dirPaths, char type, string codePaths, bool exportCode) {
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
        if (exportCode && s_exportLanguage == ExportLanguage.TypeScript) {
            CsvMaker_TypeScript.InitCatalog();
        }
        string headExtend;
        string csvExtend;
        string readerExtend;
        foreach (int index in excelList.CheckedIndices) {
            m_filePath = m_excelPaths[index];
            bool need = ReadExcel(m_filePath, type, initCsv, rowToCsv);
            if (!need) {
                continue;
            }
            string csvText = m_csvBuilder.ToString();
            csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");
            string csvName = getFileName(m_filePath);
            string[] dirPathArrray = dirPaths.Split(';');
            foreach (string dirPath in dirPathArrray) {
                if (!Directory.Exists(dirPath)) {
                    Directory.CreateDirectory(dirPath);
                }
                string csvPath = Path.Combine(dirPath, csvName + ".csv");
                File.WriteAllText(csvPath, csvText);
                Debug.Log("导出Csv:" + csvPath);
            }

            string localCsvPath = Path.Combine(localPath, csvName + ".csv");
            File.WriteAllText(localCsvPath, csvText);

            if (exportCode) {
                switch (s_exportLanguage) {
                    case ExportLanguage.CSharp:
                        CsvMaker_CSharp.MakeCsvClass(codePaths, csvName, m_headers, m_rawTypes);
                        break;
                    case ExportLanguage.Java:
                        CsvMaker_Java.MakeCsvClass(codePaths, csvName, m_headers, m_rawTypes);
                        break;
                    case ExportLanguage.TypeScript:
                        CsvMaker_TypeScript.AddCatalog(csvName);
                        TryGetExtend("TypeScript", "extendHead", csvName, out headExtend);
                        TryGetExtend("TypeScript", "extendCsv", csvName, out csvExtend);
                        TryGetExtend("TypeScript", "extendReader", csvName, out readerExtend);
                        CsvMaker_TypeScript.MakeCsvClass(codePaths, csvName, m_headers, m_rawTypes, headExtend, csvExtend, readerExtend);
                        break;
                    default:
                        break;
                }
            }
            if (m_defineIndex > 0) {
                string[] codePathArrray = codePaths.Split(';');
                foreach (string codePath in codePathArrray) {
                    if (!Directory.Exists(codePath)) {
                        Directory.CreateDirectory(codePath);
                    }
                    switch (s_exportLanguage) {
                        case ExportLanguage.CSharp:
                            CsvMaker_CSharp.MakeCsvDefine(codePath, m_defineName);
                            break;
                        case ExportLanguage.Java:
                            CsvMaker_Java.MakeCsvDefine(codePath, m_defineName);
                            break;
                        case ExportLanguage.TypeScript:
                            CsvMaker_TypeScript.MakeCsvDefine(codePath, m_defineName);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        if (exportCode && s_exportLanguage == ExportLanguage.TypeScript) {
            string[] codePathArrray = codePaths.Split(';');
            foreach (string codePath in codePathArrray) {
                if (!Directory.Exists(codePath)) {
                    Directory.CreateDirectory(codePath);
                }
                CsvMaker_TypeScript.MakeCatalog(codePath);
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
            string text = JsonConvert.SerializeObject(m_jObject, m_jsonSettings);
            text = Regex.Replace(text, "(?<!\r)\n|\r\n", "\n");
            File.WriteAllText(path, text);

            Debug.Log("导出Json:" + path);
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
    private string m_sheetName;
    private CsvHeader[] m_headers;
    private CsvNode[] m_nodes;
    private string[] m_rawTypes;
    private string[] m_cellTypes;
    private int m_defineIndex;
    private string m_defineName;
    private int m_curIndex;
    private List<string> m_rawIds = new List<string>(1000);

    private bool ReadExcel(string filePath, char type, Action headAction, Action<char, IRow> rowAction) {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
            XSSFWorkbook workbook = new XSSFWorkbook(fs);
            //只读取第一张表
            ISheet sheet = workbook.GetSheetAt(0);
            if (sheet == null) {
                Debug.LogError("ReadExcel Error Sheet:" + filePath);
                return false;
            }
            m_sheetName = sheet.SheetName;

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

            //Debug.Log("导出:" + filePath);

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
                if (m_headers[i].type == eFieldType.Array && cellType.Length > 2
                    && cellType.EndsWith("[]", StringComparison.OrdinalIgnoreCase)
                    && m_headers[i].name == m_headers[i].baseName) {
                    //去除结尾的[]
                    m_cellTypes[i] = cellType.Substring(0, cellType.Length - 2);
                }
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

            m_rawIds.Clear();
            int startIndex = 4;// sheet.FirstRowNum;
            int lastIndex = sheet.LastRowNum;
            for (int index = startIndex; index <= lastIndex; index++) {
                m_curIndex = index;
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
                string id = obj.ToString();
                if (string.IsNullOrEmpty(id) || id[0] == CsvConfig.skipFlag) {
                    continue;
                }

                if (m_rawIds.IndexOf(id) >= 0) {
                    Debug.LogError(m_filePath + " 重复id, index：" + index + " id:" + id);
                    continue;
                }
                m_rawIds.Add(id);

                try {
                    rowAction(type, row);
                }
                catch (Exception e) {
                    Debug.LogError(m_filePath + " 转换错误, index：" + index + " info:" + obj + " \n" + e);
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

        if (force || rawString.IndexOf(CsvConfig.delimiter) > 0) {
            //出现逗号分隔符，需要包裹
            newString = string.Format("\"{0}\"", newString);
        }

        return newString;
    }

    private StringBuilder m_csvBuilder;
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

        switch (s_exportLanguage) {
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
    }

    private void rowToCsv(char type, IRow row) {
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
            cellType = m_cellTypes[rank];
            if (string.IsNullOrEmpty(cellType)) {
                continue;
            }
            switch (cellType.ToLower()) {
                case "bool":
                case "uint":
                case "int":
                case "ulong":
                case "long":
                case "float":
                case "double":
                case "fixed":
                    if (value is string) {
                        Debug.LogError(m_filePath + " 基础格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                    m_csvBuilder.Append(value);
                    break;
                case "string":
                    m_csvBuilder.Append(packString(value.ToString(), false));
                    break;
                default:
                    //对象结构需要引号包起来
                    info = value.ToString();
                    if (info.Length <= 0) {
                        Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        break;
                    }
                    if (cell.CellType == CellType.String) {
                        bool isJson = false;
                        char frist = info[0];
                        char last = info[info.Length - 1];
                        if ((frist == '[' && last == ']') || (frist == '{' && last == '}')) {
                            isJson = true;
                            //检查Json格式
                            try {
                                var obj = JsonConvert.DeserializeObject(info);
                            }
                            catch (Exception e) {
                                Debug.LogError(m_filePath + " Json格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                            }
                        }
                        else {
                            if (!IsEnum(cellType) && header.subs == null) {
                                Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                            }
                        }
                        m_csvBuilder.Append(packString(info, isJson));
                    }
                    else {
                        if (!IsEnum(cellType) && header.subs == null) {
                            Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        }
                        m_csvBuilder.Append(packString(info, false));
                    }
                    break;
            }

        }
        m_csvBuilder.Append("\n");

        rowToDefine(row);
    }

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

        string valueType = m_cellTypes[0];
        switch (s_exportLanguage) {
            case ExportLanguage.CSharp:
                CsvMaker_CSharp.AddCsvDefine(valueType, value, getCellValue(row.GetCell(0)));
                break;
            case ExportLanguage.Java:
                CsvMaker_Java.AddCsvDefine(valueType, value, getCellValue(row.GetCell(0)));
                break;
            case ExportLanguage.TypeScript:
                CsvMaker_TypeScript.AddCsvDefine(valueType, value, getCellValue(row.GetCell(0)));
                break;
            default:
                break;
        }
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

    private void rowToJson(char type, IRow row) {
        object value;
        JToken token;
        string cellType;
        CsvHeader header;
        CsvNode node;
        string info;
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
                    info = value.ToString();
                    if (info.Length <= 0) {
                        Debug.LogError(m_filePath + " 格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                                Debug.LogError(m_filePath + " 格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                            Debug.LogError(m_filePath + " 格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                csv.Add(node.name, token);
            }

            //重置node，以便下一条使用
            node.Reset();
        }

        m_jObject.Add(csv["id"].ToString(), csv);
    }
}
