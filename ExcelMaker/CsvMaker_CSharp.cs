using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CsvMaker_CSharp {
    static string TemplateClass = @"using System;
using UnityEngine;
#headerfile#
public sealed partial class @className : @classPostfixTemplate<@classKey>, IByteReadable
#if UNITY_EDITOR
    , IByteWriteable
#endif
{
    private @classKey c_id;
    public @classKey id {
        get { return c_id; }
        set { c_id = value; }
    }#property#

    public void SetField(string field, string text) {
        switch (field) {#BaseCase#
            default:
                break;
        }
    }

    public void SetFieldByNode(string field, CsvNode node) {
        switch (field) {#NodeCase#
            default:
                break;
        }
    }

    public void Deserialize(ByteReader reader) {#ByteRead#
    }

#if UNITY_EDITOR
    public void Serialize(CsvHelper.ByteWriter writer) {#ByteWrite#
    }
#endif
}

public sealed partial class @classNameReader : @classPostfixReader<@classKey, @className> {
    protected override void InitKeys() {
        for (int i = 0; i < m_Keys.Length; i++) {
            m_ByteReader.Read(ref m_Keys[i]);
            m_id2Indexs[m_Keys[i]] = i;
        }
    }
}

public sealed partial class @classPostfix {
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

    static string TemplateKeyCase = @"
            case ""@name"":
                #if UNITY_EDITOR
                c_@name = StringConverter.Inst.@funNameFrom(@paramName);
                #endif
                i_@name = KeyConverter.Inst.@funNameFrom(@paramName);
                break;";

    static string TemplateProperty = @"
    private @type c_@name;
    public @type @name {
        get { return c_@name; }
        #if UNITY_EDITOR
        set { c_@name = value; }
        #endif
    }";

    static string TemplateLocalizeKeyProperty = @"
    #if UNITY_EDITOR
    private @typeString c_@name;
    public @typeString @nameRaw {
        get {
            return c_@name;
        }
    }
    #endif
    private @typeInt i_@name;
    public @typeInt @name {
        get {
            return i_@name;
        }
    }";

    static string TemplatePathKeyProperty = @"
    #if UNITY_EDITOR
    private @typeString c_@name;
    public @typeString @nameRaw {
        get {
            return c_@name;
        }
    }
    #endif
    private @typeInt i_@name;
    public @typeString @name {
        get {
            return Cfg.PathKey.Convert(i_@name, c_id);
        }
    }";

    static string TemplatePathKeysProperty = @"
    #if UNITY_EDITOR
    private @typeString c_@name;
    public @typeString @nameRaw {
        get {
            return c_@name;
        }
    }
    #endif
    private @typeInt i_@name;
    private @typeString t_@name;
    public @typeString @name {
        get {
            if (null == i_@name) {
                return null;
            }
            if (null == t_@name) {
                t_@name = Cfg.PathKey.Convert(i_@name, c_id);
            }
            return t_@name;
        }
    }";

    public static string TemplateDefineClass = @"public sealed partial class @className {
#property#
}
";

    public static string TemplateDefineField = @"
    public const @type @name = @value;";

    static string TemplateSimpeRead = @"
        reader.Read(ref c_@name);";

    static string TemplateClassRead = @"
        c_@name = @typeConverter.Inst.Read@funName(reader);";

    static string TemplateSimpeWrite = @"
        writer.Write(c_@name);";

    static string TemplateClassWrite = @"
        @typeConverter.Inst.Write(writer, c_@name);";

    static string TemplateSparseRead = @"
        byte t_tag = 0;
        while (reader.TryReadTag(ref t_tag)) {
            switch (t_tag) {#ByteRead#
                default:
                    Debug.LogError($""@className.Deserialize error id:{c_id} tag:{t_tag}"");
                    break;
            }
        }";

    static string TemplateSparseSimpeRead = @"
                case @tag:
                    reader.Read(ref c_@name);
                    break;";

    static string TemplateSparseClassRead = @"
                case @tag:
                    c_@name = @typeConverter.Inst.Read@funName(reader);
                    break;";

    static string TemplateSparseSimpeWrite = @"
        writer.Write(c_@name, @tag);";

    static string TemplateSparseClassWrite = @"
        @typeConverter.Inst.Write(writer, c_@name, @tag);";

    static string mSuffixName = ".cs";

    private static string GetSysType(string typeName, out PropertyType propertyType) {
        propertyType = PropertyType.Base;
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
            case "boolean":
                retTypeName = typeof(bool).Name;
                break;
            case "byte":
                retTypeName = typeof(byte).Name;
                break;
            case "short":
            case "int16":
                retTypeName = typeof(short).Name;
                break;
            case "ushort":
            case "uint16":
                retTypeName = typeof(ushort).Name;
                break;
            case "long":
            case "int64":
                retTypeName = typeof(long).Name;
                break;
            case "ulong":
            case "uint64":
                retTypeName = typeof(ulong).Name;
                break;
            case "double":
                retTypeName = typeof(double).Name;
                break;
            case "localizekey": //LocalizeKey
                retTypeName = typeName;
                propertyType = PropertyType.LocalizeKey;
                break;
            case "pathkey": //PathKey
                retTypeName = typeName;
                propertyType = PropertyType.PathKey;
                break;
            default:
                retTypeName = typeName;
                propertyType = PropertyType.Class;
                break;
        }
        return retTypeName;
    }

    private static Dictionary<string, Header> m_type2Headers;
    public static bool TryGetHeader(string type, out Header header) {
        if (m_type2Headers == null) {
            m_type2Headers = new Dictionary<string, Header>();
            string filePath = "ExcelMakerExtend/CSharp/Type2Head.json";
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

    private enum PropertyType {
        Base,
        Class,
        PathKey,
        LocalizeKey,
    }

    public static void MakeCsvClass(string outPaths, string fileCsv,
        List<CsvHeader> headers, List<string> typeStrs, bool isSparse) {
        //string fileCsv = Path.GetFileNameWithoutExtension(file);
        string classStr = TemplateClass;
        string templateSimpeRead = TemplateSimpeRead;
        string templateClassRead = TemplateClassRead;
        string templateSimpeWrite = TemplateSimpeWrite;
        string templateClassWrite = TemplateClassWrite;
        if (isSparse) {
            templateSimpeRead = TemplateSparseSimpeRead;
            templateClassRead = TemplateSparseClassRead;
            templateSimpeWrite = TemplateSparseSimpeWrite;
            templateClassWrite = TemplateSparseClassWrite;

            classStr = classStr.Replace("#ByteRead#", TemplateSparseRead);
        }

        string headerfile;
        Header typeHeader;
        if (TryGetHeader("_BaseHeader", out typeHeader) && !string.IsNullOrEmpty(typeHeader.path)) {
            headerfile = typeHeader.path;
        }
        else {
            headerfile = string.Empty;
        }

        StringBuilder propertyBuilder = new StringBuilder();
        StringBuilder simpleBuilder = new StringBuilder();
        StringBuilder nodeBuilder = new StringBuilder();
        StringBuilder byteReadBuilder = new StringBuilder();
        StringBuilder byteWriteBuilder = new StringBuilder();

        string classKey = "";
        List<string> flagKeys = new List<string>();
        List<string> importKeys = new List<string>();

        for (int idx = 0; idx < headers.Count; idx++) {
            CsvHeader header = headers[idx];
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

            string typeStr = typeStrs[idx];
            if (string.IsNullOrEmpty(typeStr)) {
                continue;
            }
            if (typeStr == "define") {
                //define文件生成标志
                continue;
            }
            if (typeStr[0] == CsvConfig.comment) {
                typeStr = typeStr.Substring(1);
            }
            string[] subTypes = typeStr.Split(CsvConfig.classSeparator);
            PropertyType propertyType;
            string typeName = GetSysType(subTypes[0], out propertyType);
            subTypes = typeName.Split(CsvConfig.arrayChars);
            string baseTypeName = GetSysType(subTypes[0], out propertyType);
            bool isEnum = false;
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

            if (TryGetHeader(baseTypeName, out typeHeader)) {
                isEnum = typeHeader.isEnum;
                if (!string.IsNullOrEmpty(typeHeader.path) && !importKeys.Contains(baseTypeName)) {
                    headerfile += typeHeader.path;
                    importKeys.Add(baseTypeName);
                }
            }

            string template;
            if (isEnum) {
                template = TemplateEnumCase.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", baseTypeName).Replace("@paramName", paramName);
            }
            else {
                if (propertyType == PropertyType.LocalizeKey) {
                    template = TemplateKeyCase.Replace("@funName", funcName)
                        .Replace("@name", fieldName)
                        .Replace("@paramName", paramName);
                }
                else if (propertyType == PropertyType.PathKey) {
                    template = TemplateKeyCase.Replace("@funName", funcName)
                        .Replace("@name", fieldName)
                        .Replace("@paramName", paramName);
                }
                else {
                    template = TemplateSimpeCase.Replace("@funName", funcName).Replace("@name", fieldName)
                        .Replace("@type", baseTypeName).Replace("@paramName", paramName);
                }
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
            else {
                string byteRead;
                string byteWrite;
                if (propertyType == PropertyType.LocalizeKey || propertyType == PropertyType.PathKey) {
                    byteRead = templateSimpeRead.Replace("@tag", idx.ToString())
                                .Replace("c_@name", "i_" + fieldName).Replace("@name", fieldName);
                    byteWrite = templateSimpeWrite.Replace("@tag", idx.ToString())
                                .Replace("c_@name", "i_" + fieldName).Replace("@name", fieldName);
                }
                else if(propertyType != PropertyType.Class || isEnum) {
                    byteRead = templateSimpeRead.Replace("@tag", idx.ToString()).Replace("@name", fieldName);
                    byteWrite = templateSimpeWrite.Replace("@tag", idx.ToString()).Replace("@name", fieldName);
                }
                else {
                    byteRead = templateClassRead.Replace("@tag", idx.ToString()).Replace("@type", baseTypeName).Replace("@name", fieldName);
                    if (funcName != "Base") {
                        byteRead = byteRead.Replace("@funName", funcName);
                    }
                    else {
                        byteRead = byteRead.Replace("@funName", string.Empty);
                    }
                    byteWrite = templateClassWrite.Replace("@tag", idx.ToString()).Replace("@type", baseTypeName).Replace("@name", fieldName);
                }
                byteReadBuilder.Append(byteRead);
                byteWriteBuilder.Append(byteWrite);
            }

            if (idx != 0) {
                if (propertyType == PropertyType.LocalizeKey) {
                    var typeInt = typeName.Replace("LocalizeKey", "Int32");
                    var typeString = typeName.Replace("LocalizeKey", "String");
                    template = TemplateLocalizeKeyProperty.Replace("@typeInt", typeInt).Replace("@typeString", typeString)
                                    .Replace("@name", fieldName);
                }
                else if (propertyType == PropertyType.PathKey) {
                    var typeInt = typeName.Replace("PathKey", "Int32");
                    var typeString = typeName.Replace("PathKey", "String");
                    if (funcName != "Base") {
                        template = TemplatePathKeysProperty.Replace("@typeInt", typeInt).Replace("@typeString", typeString)
                                            .Replace("@name", fieldName);
                    }
                    else {
                        template = TemplatePathKeyProperty.Replace("@typeInt", typeInt).Replace("@typeString", typeString)
                                        .Replace("@name", fieldName);
                    }
                }
                else {
                    template = TemplateProperty.Replace("@type", typeName).Replace("@name", fieldName);
                }
                propertyBuilder.Append(template);
            }
        }

        string fileCsvUpper = fileCsv.Substring(0, 1).ToUpper() + fileCsv.Substring(1);
        string className = fileCsvUpper + CsvConfig.classPostfix;
        classStr = classStr.Replace("@className", className);
        classStr = classStr.Replace("@classKey", classKey);
        classStr = classStr.Replace("@classPostfix", CsvConfig.classPostfix);
        classStr = classStr.Replace("@class", fileCsvUpper);        
        classStr = classStr.Replace("@fileName", fileCsv);
        classStr = classStr.Replace("#headerfile#", headerfile);
        classStr = classStr.Replace("#property#", propertyBuilder.ToString());
        classStr = classStr.Replace("#BaseCase#", simpleBuilder.ToString());
        classStr = classStr.Replace("#NodeCase#", nodeBuilder.ToString());
        classStr = classStr.Replace("#ByteRead#", byteReadBuilder.ToString());
        classStr = classStr.Replace("#ByteWrite#", byteWriteBuilder.ToString());

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
        if (m_defineBuilder == null || m_defineBuilder.Length <= 0) {
            return;
        }
        string definePath = Path.Combine(codePath, className + ".cs");
        string defineClassStr = TemplateDefineClass.Replace("@className", className)
            .Replace("#property#", m_defineBuilder.ToString());
        defineClassStr = Regex.Replace(defineClassStr, "(?<!\r)\n|\r\n", "\n");
        File.WriteAllText(definePath, defineClassStr);
    }
}
