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
            string dirName = directoryInfo.Name;
            string[] keys = dirName.Substring(0, dirName.Length - directoryInfo.Extension.Length).Split('_');
            if (keys.Length >= 2) {
                name = keys[0];
            }
            else {
                name = dirName;
                Debug.LogError($"Excel error name:{dirName}, need format [name]_[desc]");
            }

            if (root.Name == directoryInfo.Parent.Name || root.Name == directoryInfo.Parent.Parent?.Name) {
                //没有文件夹
                folder = null;

                id = name;
            }
            else {
                //只保留最后一级文件夹名称
                folder = directoryInfo.Parent.Name;

                id = folder + "_" + name;
            }
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
        m_excelMap.Clear();
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

    /// <summary>
    /// 原件导出，不做同名合并
    /// Debug用
    /// </summary>
    public bool keepSplit = false;

    private static ExportLanguage s_exportLanguage;
    public void Export(string dirPath, bool sync,
        char type, ExportType exportType, ExportLanguage language, string codePath, bool exportCode) {
        s_exportLanguage = language;
        m_logBuilder.Clear();
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
            if (keepSplit) {
                m_logBuilder.AppendLine(path);
                foreach (var item in list) {
                    paths.Clear();
                    paths.Add(item.path);
                    need = ReadExcel(paths, type, initCsv, rowToCsv);
                    if (!need) {
                        continue;
                    }
                    if (m_fileName == "Localize") {
                        for (int l = 0; l < m_localizeNames.Length; l++) {
                            var localizeName = m_localizeNames[l];
                            csvText = m_localizeBuilders[l].ToString();
                            csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                            localCsvPath = $"{localPath}/{localizeName}/{csvName}.csv";
                            if (!Directory.Exists(localCsvPath)) {
                                Directory.CreateDirectory(localCsvPath);
                            }
                            File.WriteAllText(localCsvPath, csvText);
                        }
                    }
                    else {
                        csvText = m_csvBuilder.ToString();
                        csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                        localCsvPath = localPath + "/" + item.nameWithDir.Replace(".xlsx", ".csv");
                        File.WriteAllText(localCsvPath, csvText);
                    }

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
            if (m_fileName == "Localize") {
                for (int l = 0; l < m_localizeNames.Length; l++) {
                    var localizeName = m_localizeNames[l];
                    csvText = m_localizeBuilders[l].ToString();
                    csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                    if (!Directory.Exists($"{dirPath}/{localizeName}/")) {
                        Directory.CreateDirectory($"{dirPath}/{localizeName}/");
                    }
                    path = $"{dirPath}/{localizeName}/{csvName}.csv";
                    File.WriteAllText(path, csvText);
                    m_logBuilder.AppendLine(path);

                    if (!Directory.Exists($"{localPath}/{localizeName}/")) {
                        Directory.CreateDirectory($"{localPath}/{localizeName}/");
                    }
                    localCsvPath = $"{localPath}/{localizeName}/{csvName}.csv";
                    File.WriteAllText(localCsvPath, csvText);
                }
            }
            else {
                csvText = m_csvBuilder.ToString();
                csvText = Regex.Replace(csvText, "(?<!\r)\n|\r\n", "\n");

                File.WriteAllText(path, csvText);
                //在本地同时保留一份，方便提交svn对比存档
                File.WriteAllText(localCsvPath, csvText);
                m_logBuilder.AppendLine(path);
            }


            if (exportCode) {
                switch (s_exportLanguage) {
                    case ExportLanguage.CSharp:
                        if (m_defineStyle) {
                            DefineMaker_CSharp.MakeClass(codePaths, csvName, m_headers, m_rawTypes);
                        }
                        else {
                            CsvMaker_CSharp.MakeCsvClass(codePaths, csvName, m_headers, m_rawTypes, IsSparse(csvName));
                        }
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
    private List<CsvHeader> m_headers = new List<CsvHeader>(64);
    private List<CsvNode> m_nodes = new List<CsvNode>(64);
    private List<string> m_rawTypes = new List<string>(64);
    private List<string> m_cellTypes = new List<string>(64);
    /// <summary>
    /// 常量定义列序号
    /// </summary>
    private int m_defineIndex;
    private string m_defineName;
    /// <summary>
    /// 是否是需要竖着读取的定义配置
    /// </summary>
    private bool m_defineStyle;
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

                //表头
                //IRow headRow = sheet.GetRow(1);

                //导出服务器客户端
                IRow exportRow = sheet.GetRow(3);
                ICell exportCell = exportRow.GetCell(0);
                if (exportCell == null || exportCell.StringCellValue == null) {
                    Debug.LogError("导出配置错误:" + m_filePath);
                    return false;
                }
                if (exportCell.StringCellValue != "A" && !exportCell.StringCellValue.Contains(type)) {
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
        m_defineStyle = false;

        ICell headCell = headRow.GetCell(0);
        if (headCell == null || headCell.StringCellValue == null) {
            Debug.LogError("导出属性错误:" + m_filePath);
        }
        else if (headCell.StringCellValue.Equals("key", StringComparison.OrdinalIgnoreCase)) {
            m_defineStyle = true;
        }

        if (m_fileName == "Localize") {
            m_defineIndex = 0;
            m_defineName = "i18n";
        }

        m_rawTypes.Clear();
        m_cellTypes.Clear();
        m_headers.Clear();
        m_nodes.Clear();

        if (!m_defineStyle) {
            //第一列为id，只支持int，string
            cell = typeRow.GetCell(0);
            string value = cell.StringCellValue;
            if (value[0] == CsvConfig.skipFlag) {
                m_rawTypes.Add(value);
                m_cellTypes.Add(value.Substring(1));
            }
            else {
                m_rawTypes.Add(CsvConfig.skipFlag + value);
                m_cellTypes.Add(value);
            }
            for (int i = 1; i < cellCount; i++) {
                cell = typeRow.GetCell(i);
                if (cell == null) {
                    m_rawTypes.Add(string.Empty);
                    m_cellTypes.Add(string.Empty);
                    continue;
                }
                value = cell.StringCellValue;
                m_rawTypes.Add(value);
                int pos = value.LastIndexOf(CsvConfig.classSeparator);
                if (pos > 0) {
                    m_cellTypes.Add(value.Substring(pos + 1));
                }
                else {
                    m_cellTypes.Add(value);
                }
            }

            for (int i = 0; i < cellCount; i++) {
                if (string.IsNullOrEmpty(exportSettings[i])) {
                    m_headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }
                if (exportSettings[i] != "A" && !exportSettings[i].Contains(type)) {
                    m_headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }

                cell = headRow.GetCell(i);
                if (cell == null) {
                    m_headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }

                var cellType = m_cellTypes[i];
                if (cellType == "define") {
                    m_defineIndex = i;
                    m_defineName = cell.StringCellValue;
                    m_headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }
                if (cellType == "export") {
                    m_exportIndex = i;
                    m_headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }

                m_headers.Add(CsvHeader.Pop(cell.StringCellValue));
                if (m_headers[i].type == eFieldType.Array && cellType.Length > 2
                    && cellType.EndsWith("[]", StringComparison.OrdinalIgnoreCase)
                    && m_headers[i].name == m_headers[i].baseName) {
                    //去除结尾的[]
                    m_cellTypes[i] = cellType.Substring(0, cellType.Length - 2);
                }
            }
            int slot;
            List<string> nodeSlots = new List<string>(1);
            for (int i = 0; i < m_headers.Count; i++) {
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
                    m_nodes.Add(node);
                }
                else {
                    header.SetSlot(slot);
                }
            }
        }
        else {
            m_exportIndex = 2;
        }

        headAction();

        m_rawIds.Clear();
        m_errorIdNum = 0;
        m_errorIdBuilder.Clear();

        return true;
    }

    /// <summary>
    /// 用于竖行读取Define
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    private void AppendHeader(string name, string type) {
        m_rawTypes.Add(type);
        int pos = type.LastIndexOf(CsvConfig.classSeparator);
        if (pos > 0) {
            m_cellTypes.Add(type.Substring(pos + 1));
        }
        else {
            m_cellTypes.Add(type);
        }

        m_headers.Add(CsvHeader.Pop(name));
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
        if (!m_defineStyle && m_cellTypes.Count != cellCount) {
            Debug.LogError($"{m_filePath} 表头数量错误：{m_cellTypes.Count} / {cellCount}");
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
                m_errorIdBuilder.AppendLine($"index：{index} id:{id}");
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

            if (m_defineStyle) {
                var typeCell = row.GetCell(1);
                obj = getCellValue(typeCell);
                if (obj == null) {
                    continue;
                }
                AppendHeader(id, obj.ToString());
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

    /// <summary>
    /// 将含有特殊符号的字符串包裹起来
    /// </summary>
    /// <param name="rawString"></param>
    /// <returns></returns>
    private string packString(string rawString, bool force) {
        string newString = rawString;
        if (force || rawString.IndexOf(CsvConfig.delimiter) > 0) {
            if (rawString.IndexOf(CsvConfig.quote) > 0) {
                //在非第一个字符串中出现引号，则需要替换
                newString = newString.Replace("\"", "\"\"");
            }
            //出现逗号分隔符，需要包裹
            newString = string.Format("\"{0}\"", newString);
        }

        if (newString.IndexOf("\n") > 0
        || newString.IndexOf("\t") > 0
        || newString.IndexOf("\\") > 0
        ) {
            LogError("文本含特殊符号, index：" + m_curIndex + " info:" + rawString);
        }

        return newString;
    }

    private object m_localizeId;
    private Dictionary<string, string>[] m_LocalizeReplaces;
    private const string kLocalizeRefTag = "[id=";
    private string packLocalizeString(string rawString, bool force, int slot) {
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
                    LogError($"多语言找不到替换内容, index：{m_curIndex} startIdx:{startIdx} info:{rawString}");
                    startIdx = -1;
                    break;
                }
                string key = newString.Substring(keyStartIdx, endIdx - keyStartIdx);
                if (!replace.TryGetValue(key, out var value)) {
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
            replace[m_localizeId.ToString()] = newString;
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

    private StringBuilder m_csvBuilder;
    /// <summary>
    /// 多语言配置按语言拆多个
    /// </summary>
    private StringBuilder[] m_localizeBuilders;
    private string[] m_localizeNames;
    private void initCsv() {
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

        if (m_fileName == "Localize") {
            var cnt = m_headers.Count - 1;
            m_localizeBuilders = new StringBuilder[cnt];
            m_localizeNames = new string[cnt];
            m_LocalizeReplaces = new Dictionary<string, string>[cnt];
            for (int i = 1; i < m_headers.Count; i++) {
                var header = m_headers[i];
                m_localizeNames[i - 1] = header.name;
                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("id,value,needParse");
                m_localizeBuilders[i - 1] = csvBuilder;
                m_LocalizeReplaces[i - 1] = new Dictionary<string, string>();
            }
            m_csvBuilder = null;
            return;
        }

        m_csvBuilder = new StringBuilder();
        if (m_defineStyle) {
            return;
        }
        m_csvBuilder.Append(m_headers[0].name);
        for (int i = 1; i < m_headers.Count; i++) {
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
    }

    private void rowToCsv(char type, IRow row) {
        object value;
        string cellType;
        string info;
        CsvHeader header;
        if (m_defineStyle) {
            ICell cell = row.GetCell(0);
            if (cell == null) {
                return;
            }
            value = getCellValue(cell);
            if (value == null) {
                return;
            }

            m_csvBuilder.Append(value.ToString());
            m_csvBuilder.Append(CsvConfig.delimiter);

            int rank = m_headers.Count - 1;
            header = m_headers[rank];
            cellType = m_cellTypes[rank];

            cell = row.GetCell(3);
            if (cell == null) {
                m_csvBuilder.Append("\n");
                return;
            }
            value = getCellValue(cell);
            if (value == null) {
                m_csvBuilder.Append("\n");
                return;
            }

            bool isSimple = CheckSimpleFormat(cellType, value, header, rank);
            if (!isSimple) {
                //对象结构需要引号包起来
                info = value.ToString();
                if (info.Length <= 0) {
                    //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    LogError($"{m_fileName} 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    return;
                }
                if (cell.CellType == CellType.String) {
                    bool isJson = CheckJsonFormat(cellType, info, header);
                    m_csvBuilder.Append(packString(info, isJson));
                }
                else {
                    if (!IsEnum(cellType) && header.subs == null) {
                        //Debug.LogError(m_filePath + " 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        LogError($"{m_fileName} 扩展格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                    m_csvBuilder.Append(packString(info, false));
                }
            }

            m_csvBuilder.Append("\n");
            return;
        }

        if (m_fileName == "Localize") {
            ICell cell = row.GetCell(0);
            value = getCellValue(cell);
            m_localizeId = value;

            for (int i = 0; i < m_localizeBuilders.Length; i++) {
                var csvBuilder = m_localizeBuilders[i];
                csvBuilder.Append(packString(value.ToString(), false));
                csvBuilder.Append(CsvConfig.delimiter);
            }

            for (int rank = 1; rank < m_headers.Count; rank++) {
                var csvBuilder = m_localizeBuilders[rank -1];

                cell = row.GetCell(rank);
                if (cell == null) {
                    csvBuilder.AppendLine(CsvConfig.delimiter);
                    continue;
                }
                value = getCellValue(cell);
                if (value == null) {
                    csvBuilder.AppendLine(CsvConfig.delimiter);
                    continue;
                }

                csvBuilder.AppendLine(packLocalizeString(value.ToString(), false, rank - 1));
            }

            return;
        }

        for (int rank = 0; rank < m_headers.Count; rank++) {
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

            if (header.subs != null) {
                int subIndex = header.subs.Length - 1;
                if (header.subs[subIndex].type == eFieldType.Primitive) {
                    --subIndex;
                    while (subIndex >= 0 && header.subs[subIndex].type == eFieldType.Array) {
                        if (cellType.Length > 2) {
                            cellType = cellType.Substring(0, cellType.Length - 2);
                        }
                        else {
                            if (m_curIndex < 10) {
                                LogError($"{m_fileName} 表头过小, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                    m_csvBuilder.Append(packString(info, isJson));
                }
                else {
                    if (!IsEnum(cellType) && header.subs == null) {
                        LogError($"{m_fileName} 未定义扩展格式, index：{m_curIndex} header:{header.name} type:{cellType} info:{value}");
                    }
                    m_csvBuilder.Append(packString(info, false));
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
                            LogError($"{m_fileName} bool格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                        LogError($"{m_fileName} bool格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                    }
                }
                m_csvBuilder.Append(str);
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
                            LogError($"{m_fileName} 正整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        }
                    }
                    else {
                        str = string.Empty;
                    }
                }
                else {
                    str = value.ToString();
                    if (str.IndexOf('.') >= 0) {
                        LogError($"{m_fileName} 正整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + str);
                    }
                    else if (str[0] == '-') {
                        LogError($"{m_fileName} 正整数填写了负数, index：" + m_curIndex + " header:" + header.name + " info:" + str);
                    }
                    else if (string.IsNullOrWhiteSpace(str)) {
                        str = string.Empty;
                    }
                }
                m_csvBuilder.Append(str);
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
                            LogError($"{m_fileName} 整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
                        }
                    }
                    else {
                        str = string.Empty;
                    }
                }
                else {
                    str = value.ToString();
                    if (str.IndexOf('.') >= 0) {
                        LogError($"{m_fileName} 整数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + str);
                    }
                    else if (string.IsNullOrWhiteSpace(str)) {
                        str = string.Empty;
                    }
                }
                m_csvBuilder.Append(str);
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
                            LogError($"{m_fileName} 小数格式错误, index：" + m_curIndex + " header:" + header.name + " info:" + value);
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
                m_csvBuilder.Append(str);
                break;
            case "string":
                str = packString(value.ToString(), false);
                m_csvBuilder.Append(str);
                break;
            case "localizekey": //LocalizeKey
                key = packString(value.ToString(), false);
                m_csvBuilder.Append(key);
                break;
            case "pathkey": //PathKey
                key = packString(value.ToString(), false);
                m_csvBuilder.Append(key);
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
                    LogError($"{m_fileName} 扩展格式错误,index:{m_curIndex} header:{header.name} info:{info}");
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
                LogError($"{m_fileName} Json数组格式错误, index：{m_curIndex} header:{header.name} info:{info}");
                return;
            }
            var token = array.First;
            for (int rank = header.arrayRank - 1; rank > 0; rank--) {
                if (!token.HasValues) {
                    LogError($"{m_fileName} Json数组维度错误, index：{m_curIndex} header:{header.name} info:{info}");
                    return;
                }
                token = token.First;
            }
            if (!header.CheckJToken(token)) {
                LogError($"{m_fileName} Json数组内数据类型错误, index：{m_curIndex} header:{header.name} type:{cellType} info:{info}" );
            }
        }
        catch (Exception e) {
            LogError($"{m_fileName} Json数组解析错误, index：{m_curIndex} header:{header.name} info:{info}\n{e}");
        }

    }

    private void CheckJsonClass(string cellType, string info, CsvHeader header) {
        //检查Json格式
        try {
            var obj = JsonConvert.DeserializeObject(info);
        }
        catch (Exception e) {
            LogError($"{m_fileName} Json对象解析错误, index:{m_curIndex} header:{header.name} info:{info} \n{e}");
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
        if (m_defineStyle) {
            int rank = m_headers.Count - 1;
            header = m_headers[rank];

            var cell = row.GetCell(3);
            if (cell == null) {
                return;
            }
            value = getCellValue(cell);
            if (value == null) {
                return;
            }

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

            m_jObject.Add(header.name, token);
            return;
        }


        JObject csv = new JObject();
        for (int rank = 0; rank < m_headers.Count; rank++) {
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
        for (int i = 0; i < m_nodes.Count; i++) {
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
