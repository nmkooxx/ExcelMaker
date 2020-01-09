using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CsvMaker {
    static string TemplateClass = @"using System;
using UnityEngine;
//using CsvHelper;
//热更工程因为范型继承支持不完善，拷贝一份自己的基类
using CsvHelper.HotFix;
#headerfile#
public partial class @className : CsvTemplate<@classKey> {
#property#    

    public void SetField(string field, string text) {
        switch (field) {
        #BaseCase#            
			default:
				break;
        }
    }

    public void SetFieldByNode(string field, CsvNode node) {
        switch (field) {
        #NodeCase#            
            default:
                break;
        }
    }
}

public partial class @classNameReader : CsvReader<@classKey, @className> {
}

public partial class Csv {
    private static @classNameReader m_@class = null;
    public static @classNameReader @class {
        get {
            if (null == m_@class) {
                m_@class = new @classNameReader();
                m_@class.csvName = ""@fileName"";
            }
            return m_@class;
        }
    }
}
";
    static string TemplateSimpeCase = @"		
			case ""@name"":
				c_@name = @typeConverter.Inst.@funNameFrom(@paramName);
				break;";

    static string TemplateEnumCase = @"
			case ""@name"":
				c_@name = EnumConverter.@funNameFrom<@type>(@paramName);
				break;";

    static string TemplateProperty = @"
    private @type c_@name;
    public @type @name { 
        get { return c_@name; } 
        #if UNITY_EDITOR 
        set { c_@name = value; } 
        #endif 
    }";

    public static string TemplateDefineClass = @"public partial class @className {
#property#
}
";

    public static string TemplateDefineField = @"
    public const @type @name = @value;";

    static string mSuffixName = ".cs";

    static List<string> m_enumList = new List<string>();
    public static void RegisterEnums(string[] types) {
        m_enumList = new List<string>(types);
    }

    private static bool IsEnumByName(string typeName) {
        return m_enumList.Contains(typeName);
    }

    private static string GetSysType(string typeName) {
        string retTypeName = "";
        string typeNameLower = typeName.ToLower();
        switch (typeNameLower) {
            case "int":
            case "int32":
                retTypeName = typeof(int).Name;
                break;
            case "string":
                retTypeName = typeof(string).Name;
                break;
            case "uint":
                retTypeName = typeof(uint).Name;
                break;
            case "float":
            case "single":
                retTypeName = typeof(float).Name;
                break;
            case "bool":
                retTypeName = typeof(bool).Name;
                break;
            case "int16":
                retTypeName = typeof(Int16).Name;
                break;
            case "uint16":
                retTypeName = typeof(UInt16).Name;
                break;
            case "double":
                retTypeName = typeof(double).Name;
                break;
            default:
                retTypeName = typeName;
                break;
        }
        return retTypeName;
    }
    
    public static void MakeCsvClass(string outPaths, string fileCsv, 
        CsvHeader[] headers, string[] typeStrs, string headerfile) {
        //string fileCsv = Path.GetFileNameWithoutExtension(file);
        string classStr = TemplateClass;

        string templateSimpe = TemplateSimpeCase;
        string templateEnum = TemplateEnumCase;
        string templateProperty = TemplateProperty;

        StringBuilder propertyBuilder = new StringBuilder();
        StringBuilder simpleBuilder = new StringBuilder();
        StringBuilder nodeBuilder = new StringBuilder();

        string classKey = "";
        List<string> flagKeys = new List<string>();
        int defineFieldIndex = -1;

        for (int i = 0; i < headers.Length; i++) {
            CsvHeader header = headers[i];
            if (header.skip) {
                continue;
            }
            bool isSimple = false;
            string paramName = string.Empty;
            string fieldName = string.Empty;
            switch (header.type) {
                case eFieldType.Primitive:
                    fieldName = header.name;
                    paramName = "text";
                    isSimple = true;
                    break;
                case eFieldType.Array:
                    if (flagKeys.Contains(header.baseName)) {
                        //Debug.Log(file + " Array skip:" + header.name);
                        continue;
                    }
                    flagKeys.Add(header.baseName);

                    fieldName = header.baseName;
                    paramName = "node";
                    break;
                case eFieldType.Class:
                    if (flagKeys.Contains(header.baseName)) {
                        //Debug.Log(file + " Class skip:" + header.name);
                        continue;
                    }
                    flagKeys.Add(header.baseName);

                    fieldName = header.baseName;
                    paramName = "node";
                    break;
                default:
                    break;
            }

            string typeStr = typeStrs[i];
            if (string.IsNullOrEmpty(typeStr)) {
                continue;
            }
            if (typeStr == "define") {
                //define文件生成标志
                defineFieldIndex = i;
                continue;
            }
            if (typeStr[0] == CsvConfig.comment) {
                typeStr = typeStr.Substring(1);
            }
            string[] subTypes = typeStr.Split(CsvConfig.classSeparator);
            string typeName = GetSysType(subTypes[0]);
            subTypes = typeName.Split(CsvConfig.arrayChars);
            string baseTypeName = GetSysType(subTypes[0]);
            bool isEnum = IsEnumByName(baseTypeName);
            string funcName = string.Empty;
            if (subTypes.Length == 1) {
                funcName = "Base";
            }
            else {
                int arrayNum = (subTypes.Length - 1) / 2;
                for (int num = 0; num < arrayNum; num++) {
                    funcName += "Array";
                }
            }

            string template;
            if (isEnum) {
                template = templateEnum.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", baseTypeName).Replace("@paramName", paramName);
            }
            else {
                template = templateSimpe.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", baseTypeName).Replace("@paramName", paramName);
            }

            if (isSimple) {
                simpleBuilder.Append(template);
            }
            else {
                nodeBuilder.Append(template);
            }

            if (header.name == CsvConfig.primaryKey) {
                classKey = typeName;
            }

            template = templateProperty.Replace("@type", typeName).Replace("@name", fieldName);
            propertyBuilder.Append(template);
        }

        string fileCsvUpper = fileCsv.Substring(0, 1).ToUpper() + fileCsv.Substring(1);
        string className = fileCsvUpper + CsvConfig.classPostfix;
        classStr = classStr.Replace("@className", className);
        classStr = classStr.Replace("@classKey", classKey);
        classStr = classStr.Replace("@class", fileCsvUpper);
        classStr = classStr.Replace("@fileName", fileCsv);
        classStr = classStr.Replace("#headerfile#", headerfile);
        classStr = classStr.Replace("#property#", propertyBuilder.ToString());
        classStr = classStr.Replace("#BaseCase#", simpleBuilder.ToString());
        classStr = classStr.Replace("#NodeCase#", nodeBuilder.ToString());

        classStr = Regex.Replace(classStr, "(?<!\r)\n|\r\n", "\n");

        string fileName = className + mSuffixName;
        string[] outPathArrray = outPaths.Split(';');
        foreach (string outPath in outPathArrray)
        {
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }
            string filePath = Path.Combine(outPath, fileName);
            File.WriteAllText(filePath, classStr);
            Debug.Log("MakeCsv:" + fileCsv + "\nOutput:" + filePath);
        }
    }

}
