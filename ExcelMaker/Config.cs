using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum ExportType {
    Csv,
    Json,
}

public class Config {
    public string rootPath = "excel";
    public string serverPath = "server";
    public string serverCodePath = "serverCode";
    public int serverExportType = 1;
    public string clientPath = "client";
    public string clientCodePath = "clientCode";
    public int clientExportType;
    /// <summary>
    /// 表名来源
    /// 0 文件名中_分割的最后一个
    /// 1 SheetName 
    /// </summary>
    public int nameSource = 0;
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
    public Combine[] combines;
    /// <summary>
    /// 枚举保存为字符串
    /// </summary>
    public string[] enumNames;
    /// <summary>
    /// 导入头文件
    /// </summary>
    public string importHeads;
}