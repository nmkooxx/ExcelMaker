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
    public string rootPath = "excel";
    public string serverPath = "server";
    public string serverCodePath = "serverCode";
    public string clientPath = "client";
    public string clientCodePath = "clientCode";
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
    /// 表名来源
    /// 0 文件名中_分割的最后一个
    /// 1 SheetName
    /// </summary>
    public int nameSource = 0;

    public Combine[] combines;
}

public class Header {
    public string name;
    public string path;
    public bool isEnum;
}