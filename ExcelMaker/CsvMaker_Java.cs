using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CsvMaker_Java {
    static string TemplateClass = @"
package com.zs.common.csv.reader;

import java.util.List;
import com.zs.common.csv.*;
import com.zs.common.csv.conversion.*;

public class @className implements CsvTemplate<@classKey> {
#property#
#method#
    public void SetField(String field, String text) {
        switch (field) {
        #BaseCase#
			default:
				break;
        }
    }

    public void SetFieldByNode(String field, CsvNode node) {
        switch (field) {
        #NodeCase#
            default:
                break;
        }
    }
}
";
    static string TemplateReader = @"
package com.zs.common.csv.reader;

import com.zs.common.csv.*;

public class @classnameReader extends CsvLoader<@classKey, @classcsv> {
	public @classnameReader(){
		super(@classKey.class, @classcsv.class);
	}
	private static class LazyHolder {
		private static final @classnameReader INSTANCE = new @classnameReader();
	}

	public static @classnameReader getInstance() {
		return LazyHolder.INSTANCE;
	}
}
";

    static string TemplateSimpeCase = @"
			case ""@name"":
				c_@name = @typeConverter.getInstance().@funNameFrom(@paramName);
				break;";
    static string mSuffixName = ".java";

    static string TemplateProperty = @"
    private @type c_@name;
";
    static string TemplateMethod = @"
    public @type get@MethodName(){
        return c_@name;
    }
    public void set@MethodName(@type @name){
        this.c_@name = @name;
    }
";

    public static string TemplateDefineClass = @"
package com.zs.common.csv.reader;

public class @className {
#property#
}
";

    public static string TemplateDefineField = @"
    public static final @type @name = @value;";

    private static String GetSysType(String typeName) {
        String retTypeName = "";
        String typeNameLower = typeName.ToLower();
        switch (typeNameLower) {
            case "int":
            case "int32":
                retTypeName = "int";
                break;
            case "string":
                retTypeName = "String";
                break;
            case "uint":
                retTypeName = "int";
                break;
            case "float":
            case "single":
                retTypeName = "float";
                break;
            case "bool":
                retTypeName = "boolean";
                break;
            case "int16":
                retTypeName = "int";
                break;
            case "uint16":
                retTypeName = "int";
                break;
            case "double":
                retTypeName = "double";
                break;
            default:
                retTypeName = typeName;
                break;
        }
        return retTypeName;
    }

    private static string transType(string typeName) {
        if (typeName == "int") {
            return "Integer";
        }
        else if (typeName == "double") {
            return "Double";
        }
        else if (typeName == "float") {
            return "Float";
        }
        return typeName;
    }

    private static string ComposeTypeName(string typeName) {
        return string.Format("List<{0}>", transType(typeName));
    }

    private static Dictionary<string, Header> m_type2Headers;
    public static bool TryGetHeader(string type, out Header header) {
        if (m_type2Headers == null) {
            m_type2Headers = new Dictionary<string, Header>();
            string filePath = "ExcelMakerExtend/Java/Type2Head.json";
            if (File.Exists(filePath)) {
                var text = File.ReadAllText(filePath);
                var headers = JsonConvert.DeserializeObject<Header[]>(text);
                for (int i = 0; i < headers.Length; i++) {
                    var item = headers[i];
                    m_type2Headers[item.name] = item;
                    m_type2Headers[item.name.ToLower()] = item;
                }
            }
        }
        return m_type2Headers.TryGetValue(type, out header);
    }

    public static void MakeCsvClass(string outPaths, string fileCsv,
    CsvHeader[] headers, string[] typeStrs) {
        string classStr = TemplateClass;

        string headerfile;
        Header typeHeader;
        if (TryGetHeader("_BaseHeader", out typeHeader) && !string.IsNullOrEmpty(typeHeader.path)) {
            headerfile = typeHeader.path;
        }
        else {
            headerfile = string.Empty;
        }

        string templateSimpe = TemplateSimpeCase;
        StringBuilder propertyBuilder = new StringBuilder();
        StringBuilder methodBuilder = new StringBuilder();
        StringBuilder simpleBuilder = new StringBuilder();
        StringBuilder nodeBuilder = new StringBuilder();

        string classKey = "";
        int defineFieldIndex = -1;
        List<string> flagKeys = new List<string>();

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
                defineFieldIndex = i;
                continue;
            }
            if (typeStr[0] == CsvConfig.comment) {
                typeStr = typeStr.Substring(1);
            }
            string[] subTypes = typeStr.Split(CsvConfig.classSeparator);
            string typeName = GetSysType(subTypes[0]);
            subTypes = typeName.Split(CsvConfig.arrayChars);
            typeName = GetSysType(subTypes[0]);
            string baseTypeName = GetSysType(subTypes[0]);
            string funcName = string.Empty;
            if (subTypes.Length == 1) {
                funcName = "Base";
            }
            else {
                int arrayNum = (subTypes.Length - 1) / 2;
                for (int num = 0; num < arrayNum; num++) {
                    funcName += "Array";
                    typeName = ComposeTypeName(typeName);
                }
            }

            string template;
            template = templateSimpe.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", initCap(baseTypeName)).Replace("@paramName", paramName);

            if (isSimple) {
                simpleBuilder.Append(template);
            }
            else {
                nodeBuilder.Append(template);
            }

            if (header.name == CsvConfig.primaryKey) {
                classKey = typeName;
                if (classKey == "int") {
                    classKey = "Integer";
                }
            }
            propertyBuilder.Append(TemplateProperty.Replace("@type", typeName).Replace("@name", fieldName));
            methodBuilder.Append(TemplateMethod.Replace("@type", typeName).Replace("@name", fieldName).Replace("@MethodName", initCap(fieldName)));
        }

        string fileCsvUpper = fileCsv.Substring(0, 1).ToUpper() + fileCsv.Substring(1);
        string className = fileCsvUpper + CsvConfig.classPostfix;
        classStr = classStr.Replace("@className", className);
        classStr = classStr.Replace("@classKey", classKey);
        classStr = classStr.Replace("@class", fileCsvUpper);
        classStr = classStr.Replace("@fileName", fileCsv);
        classStr = classStr.Replace("#headerfile#", headerfile);
        classStr = classStr.Replace("#property#", propertyBuilder.ToString());
        classStr = classStr.Replace("#method#", methodBuilder.ToString());
        classStr = classStr.Replace("#BaseCase#", simpleBuilder.ToString());
        classStr = classStr.Replace("#NodeCase#", nodeBuilder.ToString());

        string fileName = className + mSuffixName;
        string[] outPathArrray = outPaths.Split(';');
        foreach (string outPath in outPathArrray) {
            if (!Directory.Exists(outPath)) {
                Directory.CreateDirectory(outPath);
            }
            string filePath = Path.Combine(outPath, fileName);
            File.WriteAllText(filePath, classStr);
            Debug.Log("MakeCsv:" + fileCsv + "\nOutput:" + filePath);

            string raderClassStr = TemplateReader.Replace("@classname", fileCsvUpper).Replace("@classcsv", className).Replace("@classKey", classKey);
            string readerFileName = fileCsvUpper + "Reader" + mSuffixName;
            string readerFilePath = Path.Combine(outPath, readerFileName);
            File.WriteAllText(readerFilePath, raderClassStr);
        }
    }

    private static String initCap(String str) {
        char[] ch = str.ToCharArray();
        if (ch[0] >= 'a' && ch[0] <= 'z') {
            ch[0] = (char)(ch[0] - 32);
        }
        return new String(ch);
    }

    private static StringBuilder m_defineBuilder;
    public static void InitCsvDefine() {
        m_defineBuilder = new StringBuilder();
    }

    public static void AddCsvDefine(string valueType, object value, object cellValue) {
        string template = TemplateDefineField.Replace("@type", valueType)
                        .Replace("@name", value.ToString());
        if (valueType == "string") {
            template = template.Replace("@value", '"' + cellValue.ToString() + '"');
        }
        else {
            template = template.Replace("@value", cellValue.ToString());
        }
        m_defineBuilder.Append(template);
    }

    public static void MakeCsvDefine(string codePath, string className) {
        if (m_defineBuilder.Length <= 0) {
            return;
        }
        string definePath = Path.Combine(codePath, className + ".java");
        string defineClassStr = TemplateDefineClass.Replace("@className", className)
            .Replace("#property#", m_defineBuilder.ToString());
        defineClassStr = Regex.Replace(defineClassStr, "(?<!\r)\n|\r\n", "\n");
        File.WriteAllText(definePath, defineClassStr);
    }
}