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
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.CheckedListBox;

public class Logic {

    public Logic() {
        readConfig();
        readSetting();
        readExtend();
    }

    private Config m_config;
    public Config config {
        get {
            return m_config;
        }
    }

    private DirectoryInfo m_rootInfo;

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
            m_rootInfo = new DirectoryInfo(m_config.rootPath);
        }
        else {
            m_config = new Config();
            WriteConfig();
        }
    }

    public void WriteConfig() {
        string text = JsonConvert.SerializeObject(m_config, m_jsonSettings);
        File.WriteAllText(m_configPath, text);
        m_rootInfo = new DirectoryInfo(m_config.rootPath);
    }

    private Setting m_setting;
    public Setting setting {
        get {
            return m_setting;
        }
    }

    private string m_settingPath = "ExcelMakerSetting.txt";
    private void readSetting() {
        if (File.Exists(m_settingPath)) {
            string text = File.ReadAllText(m_settingPath);
            m_setting = JsonConvert.DeserializeObject<Setting>(text);
        }
        else {
            m_setting = new Setting();
        }
        CsvConfig.classPostfix = m_setting.classPostfix;
    }

    public void WriteSetting() {
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

    /// <summary>
    /// 是否稀疏表
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private bool IsSparse(string name) {
        if (m_setting.sparses == null) {
            return false;
        }
        for (int i = 0; i < m_setting.sparses.Length; i++) {
            var sparse = m_setting.sparses[i];
            if (sparse == name) {
                return true;
            }
        }

        return false;
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

    public class ExcelInfo {
        public string path;
        public string folder { get; }
        public string name { get; }
        public string id { get; }
        public int index { get; }
        public bool selected;

        public string nameWithDir;

        public ExcelInfo(string path, int index, DirectoryInfo root) {
            this.path = path;
            this.index = index;

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (root.Name == directoryInfo.Parent.Name || root.Name == directoryInfo.Parent.Parent?.Name) {
                //没有文件夹
                folder = null;
            }
            else {
                //只保留最后一级文件夹名称
                folder = directoryInfo.Parent.Name;
            }

            string dirName = directoryInfo.Name;
            string[] keys = dirName.Substring(0, dirName.Length - directoryInfo.Extension.Length).Split('_');
            if (keys.Length >= 2) {
                name = keys[0];
            }
            else {
                name = dirName;
                Debug.LogError($"Excel error name:{dirName}, need format [name]_[desc]");
            }

            id = folder + "_" + name;
        }
    }

    private List<ExcelInfo> m_excelInfos = new List<ExcelInfo>(50);
    public List<ExcelInfo> excelInfos {
        get {
            return m_excelInfos;
        }
    }

    private Dictionary<string, List<ExcelInfo>> m_excelMap = new Dictionary<string, List<ExcelInfo>>();
    public Dictionary<string, List<ExcelInfo>> excelMap {
        get {
            return m_excelMap;
        }
    }

    public void Scan(CheckedListBox excelList) {
        if (!Directory.Exists(m_config.rootPath)) {
            Debug.LogError("Excel路径错误：" + m_config.rootPath);
            return;
        }

        m_excelInfos.Clear();
        excelList.Items.Clear();
        int offset = m_config.rootPath.Length + 1;
        //var paths = Directory.GetFiles(m_config.rootPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".xlsx") || s.EndsWith(".csv"));
        var paths = Directory.GetFiles(m_config.rootPath, "*.xlsx", SearchOption.AllDirectories);
        foreach (var path in paths) {
            if (path.IndexOf('~') >= 0) {
                continue;
            }
            int index = excelList.Items.Count;
            string str = path.Substring(offset);
            excelList.Items.Add(str);
            //默认选中
            excelList.SetItemChecked(index, true);
            var info = new ExcelInfo(path, index, m_rootInfo);
            if (info.folder == null) {
                info.nameWithDir = str;
            }
            else {
                info.nameWithDir = str.Substring(str.IndexOf(info.folder));
            }
            info.selected = true;
            m_excelInfos.Add(info);
            if (!m_excelMap.TryGetValue(info.id, out var list)) {
                list = new List<ExcelInfo>();
                m_excelMap[info.id] = list;
            }
            list.Add(info);
        }

        Debug.Log("路径扫描完成，总计文件个数：" + m_excelInfos.Count);
    }

    public bool keep = false;

    private static ExportLanguage s_exportLanguage;
    public void Export(string dirPath, bool sync,
        char type, ExportType exportType, ExportLanguage language, string codePath, bool exportCode) {
        s_exportLanguage = language;
        m_logBuilder.Clear();
        m_PathKeys.Clear();
        switch (exportType) {
            case ExportType.Csv:
                exportCsv(dirPath, sync, type, codePath, exportCode);
                break;
            case ExportType.Json:
                exportJson(dirPath, sync, type);
                break;
            default:
                break;
        }
    }

    private void exportCsv(string dirPath, bool sync, char type, string codePaths, bool exportCode) {
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
        m_logBuilder.AppendLine($"导出Csv type:{type} sync:{sync}");
        string headExtend;
        string csvExtend;
        string readerExtend;
        int offset = m_config.rootPath.Length + 1;
        string csvName = null, csvDir = null, path = null;
        string localCsvPath = null;
        List<string> paths = new List<string>(8);
        foreach (var list in m_excelMap.Values) {
            var info = list[0];
            csvDir = info.folder;
            csvName = info.name;
            m_fileName = info.name;
            if (!sync) {
                //选择模式
                if (!info.selected) {
                    continue;
                }
                if (m_setting.exportDir && csvDir != null) {
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
                if (m_setting.exportDir && csvDir != null) {
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
            if (keep) {
                m_logBuilder.AppendLine(path);
                foreach (var item in list) {
                    paths.Clear();
                    paths.Add(item.path);
                    need = ReadExcel(paths, type, initCsv, rowToCsv);
                    if (!need) {
                        continue;
                    }
                    csvText = m_csvBuilder.ToString();
                    csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                    localCsvPath = localPath + "/" + item.nameWithDir.Replace(".xlsx", ".csv");
                    File.WriteAllText(localCsvPath, csvText);
                }

                continue;
            }

            paths.Clear();
            foreach (var item in list) {
                paths.Add(item.path);
            }            
            need = ReadExcel(paths, type, initCsv, rowToCsv);
            if (!need) {
                continue;
            }
            csvText = m_csvBuilder.ToString();
            csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

            File.WriteAllText(path, csvText);
            File.WriteAllText(localCsvPath, csvText);
            m_logBuilder.AppendLine(path);

            if (exportCode) {
                switch (s_exportLanguage) {
                    case ExportLanguage.CSharp:
                        CsvMaker_CSharp.MakeCsvClass(codePaths, csvName, m_headers, m_rawTypes, IsSparse(csvName));
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
            if (m_defineIndex >= 0) {
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
        Debug.Log(m_logBuilder.ToString());

        if (m_LocalizeKeys.Count > 0) {
            StringBuilder keyBuilder = new StringBuilder();
            foreach (var item in m_LocalizeKeys) {
                keyBuilder.Append(item.Key);
                keyBuilder.Append(CsvConfig.delimiter);
                keyBuilder.AppendLine(item.Value);
            }
            string keyPath = "LocalizeKey.txt";
            File.WriteAllText(keyPath, keyBuilder.ToString());
        }

        if (m_PathKeys.Count > 0) {
            StringBuilder keyBuilder = new StringBuilder();
            keyBuilder.AppendLine("id,value");
            foreach (var item in m_PathKeys) {
                keyBuilder.Append(item.Key);
                keyBuilder.Append(CsvConfig.delimiter);
                keyBuilder.AppendLine(item.Value);
            }
            string text = keyBuilder.ToString();
            path = dirPath + "/PathKey.csv";
            localCsvPath = localPath + "/PathKey.csv";
            File.WriteAllText(path, text);
            File.WriteAllText(localCsvPath, text);
        }
    }

    private void exportJson(string dirPath, bool sync, char type) {
        int slot = 0;
        string csvName = null, csvDir = null, path = null;
        List<string> paths = new List<string>(8);
        m_logBuilder.Append($"导出Json type:{type} sync:{sync}");
        foreach (var list in m_excelMap.Values) {
            var info = list[0];
            csvDir = info.folder;
            csvName = info.name;
            m_fileName = info.name;
            if (!sync) {
                //选择模式
                if (!info.selected) {
                    continue;
                }
                ++slot;
                if (m_setting.exportDir && csvDir != null) {
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
                if (m_setting.exportDir) {
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
            bool need = ReadExcel(paths, type, initJson, rowToJson);
            if (!need) {
                continue;
            }
            string text = JsonConvert.SerializeObject(m_jObject, m_jsonSettings);
            text = Regex.Replace(text, "(?<!\r)\n|\r\n", "\n");
            File.WriteAllText(path, text);

            m_logBuilder.AppendLine(path);
        }
        Debug.Log(m_logBuilder.ToString());
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
                //Debug.LogWarning("导出配置错误:" + m_filePath + " 未定义单元格类型：" + cell.CellType + " "  + cell.StringCellValue + " row:" + cell.RowIndex);
                LogError("未定义单元格类型：" + cell.CellType + " " + cell.StringCellValue + " row:" + cell.RowIndex);
                return cell.StringCellValue;
        }
    }

    private string m_fileName;
    private string m_filePath;
    private string m_sheetName;
    private CsvHeader[] m_headers;
    private CsvNode[] m_nodes;
    private string[] m_rawTypes;
    private string[] m_cellTypes;
    /// <summary>
    /// 常量定义列序号
    /// </summary>
    private int m_defineIndex;
    private string m_defineName;
    /// <summary>
    /// 导出前后端列序号
    /// </summary>
    private int m_exportIndex;
    private int m_curIndex;
    private HashSet<string> m_rawIds = new HashSet<string>();
    private int m_errorIdNum = 0;
    private int m_errorNum = 0;
    private StringBuilder m_errorIdBuilder = new StringBuilder();
    private StringBuilder m_errorBuilder = new StringBuilder();
    private StringBuilder m_logBuilder = new StringBuilder();

    private bool ReadExcel(List<string> paths, char type, Action headAction, Action<char, IRow> rowAction) {
        bool result;
        for (int iPath = 0; iPath < paths.Count; iPath++) {
            m_filePath = paths[iPath];
            m_errorBuilder.Clear();
            m_errorNum = 0;
            m_errorBuilder.AppendLine("路径：" + m_filePath);
            using (FileStream fs = new FileStream(m_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                XSSFWorkbook workbook = new XSSFWorkbook(fs);
                //只读取第一张表
                ISheet sheet = workbook.GetSheetAt(0);
                if (sheet == null) {
                    Debug.LogError("ReadExcel Error Sheet:" + m_filePath);
                    return false;
                }

                //导出服务器客户端
                IRow exportRow = sheet.GetRow(3);
                ICell cell = exportRow.GetCell(0);
                if (cell == null || cell.StringCellValue == null) {
                    Debug.LogError("导出配置错误:" + m_filePath);
                    return false;
                }
                if (cell.StringCellValue != "A" && !cell.StringCellValue.Contains(type)) {
                    Debug.LogWarning("跳过导出:" + m_filePath);
                    return false;
                }

                if (iPath == 0) {
                    result = InitExcel(sheet, type, headAction);
                    if (!result) {
                        Debug.LogError("InitExcel Fail path:" + m_filePath);
                        DOLogError();
                        return false;
                    }
                }

                m_errorIdBuilder.AppendLine("路径：" + m_filePath);
                result = ReadExcel(sheet, type, rowAction);
                if (!result) {
                    Debug.LogError("ReadExcel Fail path:" + m_filePath);
                    DOLogError();
                    return false;
                }

                DOLogError();
            }
        }

        m_Localizes.Clear();

        if (m_errorIdNum > 0) {
            Debug.LogError($"{m_fileName} 重复id数量：{m_errorIdNum}，详见ExcelMakerLog.txt");
            m_errorIdBuilder.Insert(0, $"{m_fileName} 重复id数量:{m_errorIdNum}  列表：\n");
            Debug.WriteError(m_errorIdBuilder.ToString());
            m_errorIdBuilder.Clear();
        }
        return true;
    }

    private bool InitExcel(ISheet sheet, char type, Action headAction) {
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

        ICell cell;
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
        m_exportIndex = -1;

        if (m_fileName == "Localize") {
            m_defineIndex = 0;
            m_defineName = "i18n";
        }

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
            if (cellType == "export") {
                m_exportIndex = i;
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
        m_errorIdNum = 0;
        m_errorIdBuilder.Clear();

        return true;
    }

    private bool ReadExcel(ISheet sheet, char type, Action<char, IRow> rowAction) {
        //         if (m_sheetName != sheet.SheetName) {
        //             m_sheetName = sheet.SheetName;
        //             Debug.LogError($"{m_filePath} sheet name error：{m_sheetName} / {sheet.SheetName}");
        //             return false;
        //         }

        IRow nameRow = sheet.GetRow(0);
        if (nameRow == null) {
            Debug.LogError($"{m_filePath} 表数据为空");
            return false;
        }
        int cellCount = nameRow.LastCellNum;
        if (m_cellTypes.Length != cellCount) {
            Debug.LogError($"{m_filePath} 表头数量错误：{m_cellTypes.Length} / {cellCount}");
            return false;
        }

        int startIndex = 4;// sheet.FirstRowNum;
        int lastIndex = sheet.LastRowNum;
        for (int index = startIndex; index <= lastIndex; index++) {
            m_curIndex = index;
            IRow row = sheet.GetRow(index);
            if (row == null) {
                continue;
            }

            //跳过id为空，或者#号开头的行
            var cell = row.GetCell(0);
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

            if (m_rawIds.Contains(id)) {
                m_errorIdBuilder.AppendLine("index：" + index + " id:" + id);
                ++m_errorIdNum;
                //Debug.LogError(m_filePath + " 重复id, index：" + index + " id:" + id);                
                continue;
            }
            m_rawIds.Add(id);

            if (m_exportIndex > 0) {
                var exportCell = row.GetCell(m_exportIndex);
                if (exportCell == null) {
                    continue;
                }
                obj = getCellValue(exportCell);
                if (obj == null) {
                    continue;
                }
                var exportStr = obj.ToString();
                if (exportStr != "A" && !exportStr.Contains(type)) {
                    continue;
                }
            }

            try {
                rowAction(type, row);
            }
            catch (Exception e) {
                ++m_errorNum;
                LogError("转换错误, index：" + index + " info:" + obj + " \n" + e);
                //Debug.LogError(m_filePath + " 转换错误, index：" + index + " info:" + obj + " \n" + e);
            }
        }        
        
        return true;
    }

    private void LogError(string info) {
        ++m_errorNum;
        m_errorBuilder.AppendLine(info);
    }

    private void DOLogError() {
        if (m_errorNum <= 0) {
            return;
        }
        Debug.LogError(m_errorBuilder.ToString());
    }

    private object m_curId;
    private Dictionary<string, string> m_Localizes = new Dictionary<string, string>();
    private const string kLocalizeRefTag = "[id=";

    private Dictionary<int, string> m_LocalizeKeys = new Dictionary<int, string>();
    private Dictionary<int, string> m_PathKeys = new Dictionary<int, string>();

    /// <summary>
    /// 将含有特殊符号的字符串包裹起来
    /// 不支持Json格式的String[]
    /// </summary>
    /// <param name="rawString"></param>
    /// <returns></returns>
    private string packString(string rawString, bool force, int slot) {
        string newString = rawString;
        if (force || rawString.IndexOf(CsvConfig.delimiter) > 0) {
            if (rawString.IndexOf(CsvConfig.quote) > 0) {
                //在非第一个字符串中出现引号，则需要替换
                newString = newString.Replace("\"", "\"\"");
            }
            //出现逗号分隔符，需要包裹
            newString = string.Format("\"{0}\"", newString);
        }

        //多语言表才可能出现复杂的格式
        if (m_fileName == "Localize") {
            newString = newString.Replace("\n\r", "\\n")
                .Replace("\r\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                ;

            int startIdx = newString.IndexOf(kLocalizeRefTag);
            if (startIdx >= 0) {
                while (startIdx >= 0) {
                    int keyStartIdx = startIdx + kLocalizeRefTag.Length;
                    int endIdx = newString.IndexOf("]", startIdx);
                    if (endIdx <= keyStartIdx) {
                        //找不到就报错，跳出替换
                        LogError($"多语言找不到替换内容, index：{m_curIndex} startIdx:{startIdx} info:{rawString}");
                        startIdx = -1;
                        break;
                    }
                    string key = newString.Substring(keyStartIdx, endIdx - keyStartIdx);
                    if (!m_Localizes.TryGetValue(key, out var value)) {
                        //找不到就报错，跳出替换
                        LogError($"多语言找不到替换内容, index：{m_curIndex} key:{key} info:{rawString}");
                        startIdx = -1;
                        break;
                    }
                    newString = newString.Replace(kLocalizeRefTag + key + "]", value);
                    startIdx = newString.IndexOf(kLocalizeRefTag, startIdx);
                }
            }
            else {
                //不同的多语言会覆盖之前的，简单无需解析的id要填前面
                m_Localizes[m_curId.ToString()] = newString;
            }

            if (newString.IndexOf("\n") > 0
            || newString.IndexOf("\t") > 0
            || newString.IndexOf("\\") > 0
            ) {
                newString += ",1";
            }
            else {
                if (slot > 0) {
                    newString += ",";
                }
            }
        }
        else {
            if (newString.IndexOf("\n") > 0
            || newString.IndexOf("\t") > 0
            || newString.IndexOf("\\") > 0
            ) {
                LogError("文本含特殊符号, index：" + m_curIndex + " info:" + rawString);
            }
        }

        return newString;
    }

    private StringBuilder m_csvBuilder;
    private void initCsv() {
        m_csvBuilder = new StringBuilder();
        m_csvBuilder.Append(m_headers[0].name);
        if (m_fileName == "Localize") {
            m_csvBuilder.Append(CsvConfig.delimiter);
            m_csvBuilder.Append("key");
        }
        for (int i = 1; i < m_headers.Length; i++) {
            var header = m_headers[i];
            if (header.skip) {
                continue;
            }
            m_csvBuilder.Append(CsvConfig.delimiter);
            m_csvBuilder.Append(header.name);
        }
        if (m_fileName == "Localize") {
            m_csvBuilder.Append(CsvConfig.delimiter);
            m_csvBuilder.Append("needParse");
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

            bool isSpreadArray = false;
            if (header.subs != null) {
                int subLen = header.subs.Length;
                if (header.subs[subLen - 2].type == eFieldType.Array && header.subs[subLen - 1].type == eFieldType.Primitive) {
                    isSpreadArray = true;
                }
            }
            if (isSpreadArray) {
                if (cellType.Length > 2) {
                    cellType = cellType.Substring(0, cellType.Length - 2);
                }
                else {
                    if (m_curIndex < 10) {
                        LogError("表头过小, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                }
            }
            if (rank == 0) {
                m_curId = value;
            }
            bool isSimple = CheckSimpleFormat(cellType, value, header, rank);
            if (!isSimple) {
                //对象结构需要引号包起来
                info = value.ToString();
                if (info.Length <= 0) {
                    //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError("扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    break;
                }
                if (cell.CellType == CellType.String) {
                    bool isJson = CheckJsonFormat(cellType, info, header);
                    m_csvBuilder.Append(packString(info, isJson, rank));
                }
                else {
                    if (!IsEnum(cellType) && header.subs == null) {
                        //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError("扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                    m_csvBuilder.Append(packString(info, false, rank));
                }
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

    private bool CheckSimpleFormat(string cellType, object value, CsvHeader header, int slot) {
        string key, existKey;
        int hashcode;
        switch (cellType.ToLower()) {
            case "bool":
                if (value is string) {
                    //Debug.LogError(m_filePath + " 数字格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError("bool格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                }
                else {
                    var info = value.ToString();
                    if (info != "1" && info != "0") {
                        //Debug.LogError(m_filePath + " 整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError("bool格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                }
                m_csvBuilder.Append(value);
                break;
            case "uint":
            case "ulong":
                if (value is string) {
                    //Debug.LogError(m_filePath + " 数字格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError("正整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                }
                else {
                    var info = value.ToString();
                    if (info.IndexOf('.') >= 0) {
                        //Debug.LogError(m_filePath + " 整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError("正整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                    else if (info[0] == '-') {
                        LogError("正整数填写了负数, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                }
                m_csvBuilder.Append(value);
                break;
            case "int":
            case "long":
            case "fixed":
                //Debug.Log("value:" + value + " type:" + value.GetType());
                if (value is string) {
                    //Debug.LogError(m_filePath + " 数字格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError("整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                }
                else if (value.ToString().IndexOf('.') >= 0) {
                    //Debug.LogError(m_filePath + " 整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError("整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                }
                m_csvBuilder.Append(value);
                break;
            case "float":
            case "double":
                if (value is string) {
                    //Debug.LogError(m_filePath + " 小数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError("小数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                }
                m_csvBuilder.Append(value);
                break;
            case "string":
                m_csvBuilder.Append(packString(value.ToString(), false, slot));
                break;
            case "localizekey": //LocalizeKey
                key = packString(value.ToString(), false, slot);
                hashcode = key.GetHashCode();
                m_csvBuilder.Append(hashcode);

                if (m_LocalizeKeys.TryGetValue(hashcode, out existKey)) {
                    //找不到就报错，跳出替换
                    if (existKey != key) {
                        LogError($"多语言hash冲突, hashcode：{hashcode} key:{key} existKey:{existKey}");
                    }
                }
                else {
                    m_LocalizeKeys[hashcode] = key;
                }

                if (slot == 0 && m_fileName == "Localize") {
                    m_csvBuilder.Append(CsvConfig.delimiter);
                    m_csvBuilder.Append(key);

                    //还是手动生成比较合理，很多不需要导出define
                    //switch (s_exportLanguage) {
                    //    case ExportLanguage.CSharp:
                    //        CsvMaker_CSharp.AddCsvDefine("int", key, hashcode);
                    //        break;
                    //    case ExportLanguage.Java:
                    //        CsvMaker_Java.AddCsvDefine("Int32", key, hashcode);
                    //        break;
                    //    case ExportLanguage.TypeScript:
                    //        CsvMaker_TypeScript.AddCsvDefine("Int32", key, hashcode);
                    //        break;
                    //    default:
                    //        break;
                    //}
                }
                break;
            case "pathkey": //PathKey
                key = packString(value.ToString(), false, slot);
                hashcode = key.GetHashCode();
                m_csvBuilder.Append(hashcode);

                if (m_PathKeys.TryGetValue(hashcode, out existKey)) {
                    //找不到就报错，跳出替换
                    if (existKey != key) {
                        LogError($"PathKey hash冲突, hashcode：{hashcode} key:{key} existKey:{existKey}");
                    }
                }
                else {
                    m_PathKeys[hashcode] = key;
                }
                break;
            default:
                return false;
        }
        return true;
    }

    private bool CheckJsonFormat(string cellType, string info, CsvHeader header) {
        char lastTypeChar = cellType[cellType.Length - 1];
        char lastInfoChar = info[info.Length - 1];
        if (lastTypeChar == ']') {
            if (lastInfoChar != ']') {
                LogError("Json数组[]格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
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
                    LogError("扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
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
                LogError("Json数组格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
                return;
            }
            var token = array.First;
            for (int rank = header.arrayRank - 2; rank >= 0; rank--) {
                if (!token.HasValues) {
                    LogError("Json数组维度错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
                    return;
                }
                token = token.First;
            }
            if (!header.CheckJToken(token)) {
                LogError("Json数组内数据类型错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
            }
        }
        catch (Exception e) {
            //Debug.LogError(m_filePath + " Json数组格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
            LogError("Json数组解析错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
        }

    }

    private void CheckJsonClass(string cellType, string info, CsvHeader header) {
        //检查Json格式
        try {
            var obj = JsonConvert.DeserializeObject(info);
        }
        catch (Exception e) {
            //Debug.LogError(m_filePath + " Json格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
            LogError("Json对象解析错误, index：" + m_curIndex + " header:" + header.name + " info:" + info);
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
                        //Debug.LogError(m_filePath + " 格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError("格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                                Debug.LogError("格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                            LogError("格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
