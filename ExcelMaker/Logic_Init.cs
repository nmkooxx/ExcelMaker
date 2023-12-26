using CsvHelper;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public partial class Logic {

    public Logic(CheckedListBox excelList) {
        m_ExcelList = excelList;
        ReadConfig();
        ReadSetting();
        ReadExtend();
    }

    private Config m_Config;
    public Config config {
        get {
            return m_Config;
        }
    }

    private DirectoryInfo m_RootInfo;

    private string m_ConfigPath = "ExcelMakerConfig.txt";
    private void ReadConfig() {
        if (m_ConfigPath == null) {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //将该路径传递给 System.IO.Path.GetDirectoryName(path)，获得执行程序集所在的目录
            string directory = System.IO.Path.GetDirectoryName(path);
            m_ConfigPath = directory + "/ExcelMakerConfig.txt";
        }

        if (File.Exists(m_ConfigPath)) {
            string text = File.ReadAllText(m_ConfigPath);
            m_Config = JsonConvert.DeserializeObject<Config>(text);
            m_RootInfo = new DirectoryInfo(m_Config.rootPath);
        }
        else {
            m_Config = new Config();
            WriteConfig();
        }
    }

    public void WriteConfig() {
        string text = JsonConvert.SerializeObject(m_Config, m_JsonSettings);
        File.WriteAllText(m_ConfigPath, text, CsvConfig.encoding);
        m_RootInfo = new DirectoryInfo(m_Config.rootPath);
    }

    private Setting m_Setting;
    public Setting setting {
        get {
            return m_Setting;
        }
    }

    private string m_SettingPath = "ExcelMakerSetting.txt";
    private void ReadSetting() {
        if (File.Exists(m_SettingPath)) {
            string text = File.ReadAllText(m_SettingPath);
            m_Setting = JsonConvert.DeserializeObject<Setting>(text);
        }
        else {
            m_Setting = new Setting();
        }
        CsvConfig.classPostfix = m_Setting.csvClassPostfix;
    }

    public void WriteSetting() {
        string text = JsonConvert.SerializeObject(m_Setting, m_JsonSettings);
        File.WriteAllText(m_SettingPath, text);
    }

    private string m_ExtendPath = "ExcelMakerExtend";
    //language -> place -> file -> info
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> m_Extends;
    private void ReadExtend() {
        if (!Directory.Exists(m_ExtendPath)) {
            return;
        }
        var root = new DirectoryInfo(m_ExtendPath);
        var dirs = root.GetDirectories();
        m_Extends = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(dirs.Length);
        for (int i = 0; i < dirs.Length; i++) {
            var dir = dirs[i];
            var subDirs = dir.GetDirectories();
            if (subDirs.Length == 0) {
                continue;
            }
            var languageExtends = new Dictionary<string, Dictionary<string, string>>(subDirs.Length);
            m_Extends[dir.Name] = languageExtends;
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
        if (m_Setting.combines == null) {
            combine = null;
            return false;
        }
        for (int i = 0; i < m_Setting.combines.Length; i++) {
            var cb = m_Setting.combines[i];
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
        if (m_Extends == null) {
            return false;
        }
        Dictionary<string, Dictionary<string, string>> languageExtends;
        if (!m_Extends.TryGetValue(language, out languageExtends)) {
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
        if (m_Setting.sparses == null) {
            return false;
        }
        for (int i = 0; i < m_Setting.sparses.Length; i++) {
            var sparse = m_Setting.sparses[i];
            if (sparse == name) {
                return true;
            }
        }

        return false;
    }

    private bool IsEnum(string name) {
        Header header;
        switch (s_ExportLanguage) {
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
                Debug.LogError("IsEnum error exportLanguage:" + s_ExportLanguage);
                return false;
        }
    }

    public class ExcelInfo {
        public string key { get; }
        public string path;
        public string folder { get; }
        public string name { get; }
        public string id { get; }
        public int index { get; }
        public bool selected;

        public string nameWithDir;

        public ExcelInfo(string key, string path, int index, DirectoryInfo root) {
            this.key = key;
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

    private List<ExcelInfo> m_ExcelInfos = new List<ExcelInfo>(50);
    public List<ExcelInfo> excelInfos {
        get {
            return m_ExcelInfos;
        }
    }

    private Dictionary<string, List<ExcelInfo>> m_ExcelMap = new Dictionary<string, List<ExcelInfo>>();
    public Dictionary<string, List<ExcelInfo>> excelMap {
        get {
            return m_ExcelMap;
        }
    }

    public void Search(string key) {

    }

    private CheckedListBox m_ExcelList;
    public void Scan(string search, List<string> selectNames) {
        if (!Directory.Exists(m_Config.rootPath)) {
            Debug.LogError("Excel路径错误：" + m_Config.rootPath);
            return;
        }

        m_ExcelInfos.Clear();
        m_ExcelMap.Clear();
        m_ExcelList.Items.Clear();
        int offset = m_Config.rootPath.Length + 1;
        //var paths = Directory.GetFiles(m_config.rootPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".xlsx") || s.EndsWith(".csv"));
        var paths = Directory.GetFiles(m_Config.rootPath, "*.xlsx", SearchOption.AllDirectories);
        Array.Sort(paths);
        foreach (var path in paths) {
            if (path.IndexOf('~') >= 0) {
                continue;
            }
            string str = path.Substring(offset);
            if (!string.IsNullOrEmpty(search)) {
                if (str.IndexOf(search) < 0) {
                    //不在搜索中就跳过
                    continue;
                }
            }
            bool selected = false;
            if (selectNames.IndexOf(str) >= 0) {
                selected = true;
            }
            int index = m_ExcelList.Items.Count;
            m_ExcelList.Items.Add(str);
            m_ExcelList.SetItemChecked(index, selected);
            var info = new ExcelInfo(str, path, index, m_RootInfo);
            if (info.folder == null) {
                info.nameWithDir = str;
            }
            else {
                info.nameWithDir = str.Substring(str.IndexOf(info.folder));
            }
            info.selected = selected;
            m_ExcelInfos.Add(info);
            if (!m_ExcelMap.TryGetValue(info.id, out var list)) {
                list = new List<ExcelInfo>();
                m_ExcelMap[info.id] = list;
            }
            list.Add(info);
        }

        Debug.Log("路径扫描完成，总计文件个数：" + m_ExcelInfos.Count);
    }

    /// <summary>
    /// 原件导出，不做同名合并
    /// Debug用
    /// </summary>
    public bool keepSplit = false;

    private static ExportLanguage s_ExportLanguage;
    public void Export(string dirPath, bool sync,
        char type, ExportType exportType, ExportLanguage language, string codePath, bool exportCode) {
        s_ExportLanguage = language;
        m_LogBuilder.Clear();
        switch (exportType) {
            case ExportType.Csv:
                ExportCsv(dirPath, sync, type, codePath, exportCode);
                break;
            case ExportType.Json:
                ExportJson(dirPath, sync, type);
                break;
            case ExportType.Lua:
                ExportLua(dirPath, sync, type, codePath);
                break;
            default:
                break;
        }
    }

    private string m_FileName;
    private string m_FilePath;
    private string m_SheetName;
    private List<CsvHeader> m_Headers = new List<CsvHeader>(64);
    private List<CsvNode> m_Nodes = new List<CsvNode>(64);
    private List<string> m_RawTypes = new List<string>(64);
    private List<string> m_CellTypes = new List<string>(64);
    /// <summary>
    /// 常量定义列序号
    /// </summary>
    private int m_DefineIndex;
    private string m_DefineName;
    /// <summary>
    /// 是否是需要竖着读取的定义配置
    /// </summary>
    private bool m_DefineStyle;
    /// <summary>
    /// 导出前后端列序号
    /// </summary>
    private int m_ExportIndex;
    private int m_CurIndex;
    private HashSet<string> m_RawIds = new HashSet<string>();
    private int m_ErrorIdNum = 0;
    private int m_ErrorNum = 0;
    private StringBuilder m_ErrorIdBuilder = new StringBuilder();
    private StringBuilder m_ErrorBuilder = new StringBuilder();
    private StringBuilder m_LogBuilder = new StringBuilder();

    /// <summary>
    /// 多语言配置按语言拆多个
    /// </summary>
    private StringBuilder[] m_LocalizeBuilders;
    private string[] m_LocalizeNames;


    private bool ReadExcel(List<string> paths, char type, Action headAction, Action<char, IRow> rowAction, Action finishAction) {
        bool result;
        for (int iPath = 0; iPath < paths.Count; iPath++) {
            m_FilePath = paths[iPath];
            m_ErrorBuilder.Clear();
            m_ErrorNum = 0;
            m_ErrorBuilder.AppendLine("路径：" + m_FilePath);
            using (FileStream fs = new FileStream(m_FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                XSSFWorkbook workbook = new XSSFWorkbook(fs);
                //只读取第一张表
                ISheet sheet = workbook.GetSheetAt(0);
                if (sheet == null) {
                    Debug.LogError("ReadExcel Error Sheet:" + m_FilePath);
                    return false;
                }

                //表头
                //IRow headRow = sheet.GetRow(1);

                //导出服务器客户端
                IRow exportRow = sheet.GetRow(3);
                ICell exportCell = exportRow.GetCell(0);
                if (exportCell == null || exportCell.StringCellValue == null) {
                    Debug.LogError("导出配置错误:" + m_FilePath);
                    return false;
                }
                if (exportCell.StringCellValue != "A" && !exportCell.StringCellValue.Contains(type)) {
                    Debug.LogWarning("跳过导出:" + m_FilePath);
                    return false;
                }

                if (iPath == 0) {
                    result = InitExcel(sheet, type, headAction);
                    if (!result) {
                        Debug.LogError("InitExcel Fail path:" + m_FilePath);
                        DOLogError();
                        return false;
                    }
                }

                m_ErrorIdBuilder.AppendLine("路径：" + m_FilePath);
                result = ReadExcel(sheet, type, rowAction);
                if (iPath == paths.Count - 1) {
                    finishAction?.Invoke();
                }
                if (!result) {
                    Debug.LogError("ReadExcel Fail path:" + m_FilePath);
                    DOLogError();
                    return false;
                }

                DOLogError();
            }
        }

        if (m_ErrorIdNum > 0) {
            Debug.LogError($"{m_FileName} 重复id数量：{m_ErrorIdNum}，详见ExcelMakerLog.txt");
            m_ErrorIdBuilder.Insert(0, $"{m_FileName} 重复id数量:{m_ErrorIdNum}  列表：\n");
            Debug.WriteError(m_ErrorIdBuilder.ToString());
            m_ErrorIdBuilder.Clear();
        }
        return true;
    }

    private bool InitExcel(ISheet sheet, char type, Action headAction) {
        m_SheetName = sheet.SheetName;

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

        m_DefineIndex = -1;
        m_ExportIndex = -1;
        m_DefineStyle = false;

        ICell headCell = headRow.GetCell(0);
        if (headCell == null || headCell.StringCellValue == null) {
            Debug.LogError("导出属性错误:" + m_FilePath);
        }
        else if (headCell.StringCellValue.Equals("key", StringComparison.OrdinalIgnoreCase)) {
            m_DefineStyle = true;
        }

        if (m_FileName == "Localize") {
            m_DefineIndex = 0;
            m_DefineName = "i18n";
        }

        m_RawTypes.Clear();
        m_CellTypes.Clear();
        m_Headers.Clear();
        m_Nodes.Clear();

        if (!m_DefineStyle) {
            //第一列为id，只支持int，string
            cell = typeRow.GetCell(0);
            string value = cell.StringCellValue;
            if (value[0] == CsvConfig.skipFlag) {
                m_RawTypes.Add(value);
                m_CellTypes.Add(value.Substring(1));
            }
            else {
                m_RawTypes.Add(CsvConfig.skipFlag + value);
                m_CellTypes.Add(value);
            }
            for (int i = 1; i < cellCount; i++) {
                cell = typeRow.GetCell(i);
                if (cell == null) {
                    m_RawTypes.Add(string.Empty);
                    m_CellTypes.Add(string.Empty);
                    continue;
                }
                value = cell.StringCellValue;
                m_RawTypes.Add(value);
                int pos = value.LastIndexOf(CsvConfig.classSeparator);
                if (pos > 0) {
                    m_CellTypes.Add(value.Substring(pos + 1));
                }
                else {
                    m_CellTypes.Add(value);
                }
            }

            for (int i = 0; i < cellCount; i++) {
                if (string.IsNullOrEmpty(exportSettings[i])) {
                    m_Headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }
                if (exportSettings[i] != "A" && !exportSettings[i].Contains(type)) {
                    m_Headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }

                cell = headRow.GetCell(i);
                if (cell == null) {
                    m_Headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }

                var cellType = m_CellTypes[i];
                if (cellType == "define") {
                    m_DefineIndex = i;
                    m_DefineName = cell.StringCellValue;
                    m_Headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }
                if (cellType == "export") {
                    m_ExportIndex = i;
                    m_Headers.Add(CsvHeader.Pop(string.Empty));
                    continue;
                }

                m_Headers.Add(CsvHeader.Pop(cell.StringCellValue));
                if (m_Headers[i].type == eFieldType.Array && cellType.Length > 2
                    && cellType.EndsWith("[]", StringComparison.OrdinalIgnoreCase)
                    && m_Headers[i].name == m_Headers[i].baseName) {
                    //去除结尾的[]
                    m_CellTypes[i] = cellType.Substring(0, cellType.Length - 2);
                }
            }
            int slot;
            List<string> nodeSlots = new List<string>(1);
            for (int i = 0; i < m_Headers.Count; i++) {
                CsvHeader header = m_Headers[i];
                if (header.type == eFieldType.Primitive) {
                    header.SetSlot(-1);
                    continue;
                }
                slot = nodeSlots.IndexOf(header.baseName);
                if (slot < 0) {
                    nodeSlots.Add(header.baseName);
                    header.SetSlot(nodeSlots.Count - 1);
                    var node = CsvNode.Pop(header.baseName, header.type);
                    var subTypes = m_RawTypes[i].Split(CsvConfig.arrayChars, CsvConfig.classSeparator);
                    node.cellType = subTypes[0];
                    m_Nodes.Add(node);
                }
                else {
                    header.SetSlot(slot);
                }
            }
        }
        else {
            m_ExportIndex = 2;
        }

        headAction();

        m_RawIds.Clear();
        m_ErrorIdNum = 0;
        m_ErrorIdBuilder.Clear();

        return true;
    }

    /// <summary>
    /// 用于竖行读取Define
    /// </summary>
    /// <param name="name"></param>
    /// <param name="type"></param>
    private void AppendHeader(string name, string type) {
        m_RawTypes.Add(type);
        int pos = type.LastIndexOf(CsvConfig.classSeparator);
        if (pos > 0) {
            m_CellTypes.Add(type.Substring(pos + 1));
        }
        else {
            m_CellTypes.Add(type);
        }

        m_Headers.Add(CsvHeader.Pop(name));
    }

    private bool ReadExcel(ISheet sheet, char type, Action<char, IRow> rowAction) {
        //         if (m_sheetName != sheet.SheetName) {
        //             m_sheetName = sheet.SheetName;
        //             Debug.LogError($"{m_filePath} sheet name error：{m_sheetName} / {sheet.SheetName}");
        //             return false;
        //         }

        IRow nameRow = sheet.GetRow(0);
        if (nameRow == null) {
            Debug.LogError($"{m_FilePath} 表数据为空");
            return false;
        }
        int cellCount = nameRow.LastCellNum;
        if (!m_DefineStyle && m_CellTypes.Count != cellCount) {
            Debug.LogError($"{m_FilePath} 表头数量错误：{m_CellTypes.Count} / {cellCount}");
            return false;
        }

        int startIndex = 4;// sheet.FirstRowNum;
        int lastIndex = sheet.LastRowNum;
        for (int index = startIndex; index <= lastIndex; index++) {
            m_CurIndex = index;
            IRow row = sheet.GetRow(index);
            if (row == null) {
                continue;
            }

            //跳过id为空，或者#号开头的行
            var cell = row.GetCell(0);
            if (cell == null) {
                continue;
            }
            var obj = GetCellValue(cell);
            if (obj == null) {
                continue;
            }
            string id = obj.ToString();
            if (string.IsNullOrEmpty(id) || id[0] == CsvConfig.skipFlag) {
                continue;
            }

            if (m_RawIds.Contains(id)) {
                m_ErrorIdBuilder.AppendLine($"index：{index} id:{id}");
                ++m_ErrorIdNum;
                //Debug.LogError(m_filePath + " 重复id, index：" + index + " id:" + id);
                continue;
            }
            m_RawIds.Add(id);

            if (m_ExportIndex > 0) {
                var exportCell = row.GetCell(m_ExportIndex);
                if (exportCell == null) {
                    continue;
                }
                obj = GetCellValue(exportCell);
                if (obj == null) {
                    continue;
                }
                var exportStr = obj.ToString();
                if (exportStr != "A" && !exportStr.Contains(type)) {
                    continue;
                }
            }

            if (m_DefineStyle) {
                var typeCell = row.GetCell(1);
                obj = GetCellValue(typeCell);
                if (obj == null) {
                    continue;
                }
                AppendHeader(id, obj.ToString());
            }

            try {
                rowAction(type, row);
            }
            catch (Exception e) {
                ++m_ErrorNum;
                LogError("转换错误, index：" + index + " info:" + obj + " \n" + e);
                //Debug.LogError(m_filePath + " 转换错误, index：" + index + " info:" + obj + " \n" + e);
            }
        }

        return true;
    }

    private void LogError(string info) {
        ++m_ErrorNum;
        m_ErrorBuilder.AppendLine(info);
    }

    private void DOLogError() {
        if (m_ErrorNum <= 0) {
            return;
        }
        Debug.LogError(m_ErrorBuilder.ToString());
    }


    private object GetCellValue(ICell cell) {
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
}
