using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum ExportType {
    Csv,
    Json,
}

public enum ExportLanguage {
    CSharp,
    Java,
    TypeScript,
}

public class Config {
    public string rootPath = "_Excel";
    public string serverPath = "Export/Server";
    public string serverCodePath = "Export/ServerCode";
    public string clientPath = "Export/Client";
    public string clientCodePath = "Export/ClientCode";
}

public class Combine {
    /// <summary>
    /// 需要合并类的名称
    /// </summary>
    public string name;
    /// <summary>
    /// 作为key的属性名称
    /// </summary>
    public string key;
    /// <summary>
    /// 作为值的属性名称
    /// </summary>
    public string[] values;
}

public class Setting {
    public int serverExportType = 1;
    public int serverLanguage = 1;
    public int clientExportType;
    public int clientLanguage = 0;

    /// <summary>
    /// 是否导出文件夹
    /// </summary>
    public bool exportDir = true;

    public Combine[] combines;

    public string classPostfix = "Cfg";

    /// <summary>
    /// 稀疏表名称
    /// </summary>
    public string[] sparses;
}

public class Header {
    public string name;
    public string path;
    public bool isEnum;
}