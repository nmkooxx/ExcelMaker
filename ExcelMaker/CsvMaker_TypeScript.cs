using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CsvMaker_TypeScript {
    static string TemplateCsv = @"import { CsvHelper } from ""../../frame/CsvHelper"";
#headerfile#
export class @classNameCsv implements CsvHelper.CsvTemplate {
#property#

    public SetField(field : string, text : string) : void {
        switch (field) {
        #BaseCase#
			default:
				break;
        }
    }

    public SetFieldByNode(field : string, node : CsvHelper.CsvNode) : void {
        switch (field) {
        #NodeCase#
            default:
                break;
        }
    }

#extendCsv#
}

export class @classNameCsvReader extends CsvHelper.CsvReader<@classNameCsv> {
#extendReader#
}
";
    static string TemplateCsvCase_Simple = @"
            case ""@name"":
                this.@name = @typeConverter.Inst.@funNameFrom_@paramName(@lowerParamName);
                break;";

    static string TemplateCsvCase_Enum = @"
            case ""@name"":
                this.@name = CsvHelper.EnumConverter.@funNameFrom_@paramName(@type, @lowerParamName);
                break;";

    static string TemplateCsvProperty = @"
    public @name : @type;";

    public static string TemplateDefineClass = @"export const @className = {
#property#
}
";

    public static string TemplateDefineField = @"
    ""@name"" : @value,";

    static string mSuffixName = ".ts";

    private static string GetSysType(string typeName) {
        string retTypeName = "";
        string typeNameLower = typeName.ToLower();
        switch (typeNameLower) {
            case "bool":
                retTypeName = "boolean";
                break;
            case "int":
            case "int32":
            case "uint":
            case "float":
            case "single":
                retTypeName = "number";
                break;
            case "int[]":
            case "float[]":
                retTypeName = "number[]";
                break;
            case "int[][]":
            case "float[][]":
                retTypeName = "number[][]";
                break;
            case "string":
                retTypeName = "string";
                break;
            case "vec2":
            case "vector2":
                retTypeName = "cc.Vec2";
                break;
            case "vec3":
            case "vector3":
                retTypeName = "cc.Vec3";
                break;
            case "vec4":
            case "vector4":
                retTypeName = "cc.Vec4";
                break;
            default:
                retTypeName = typeName.Replace("Vec2", "cc.Vec2")
                    .Replace("Vector2", "cc.Vec2")
                    .Replace("Vec3", "cc.Vec3")
                    .Replace("Vector3", "cc.Vec3")
                    .Replace("Vec4", "cc.Vec4")
                    .Replace("Vector4", "cc.Vec4");
                break;
        }
        return retTypeName;
    }

    private static string GetConvertType(string typeName) {
        string retTypeName = "";
        string typeNameLower = typeName.ToLower();
        switch (typeNameLower) {
            case "bool":
                retTypeName = "CsvHelper.Bool";
                break;
            case "int":
            case "int32":
            case "uint":
                retTypeName = "CsvHelper.Int";
                break;
            case "string":
                retTypeName = "CsvHelper.String";
                break;
            case "float":
            case "single":
                retTypeName = "CsvHelper.Float";
                break;
            case "vec2":
            case "vector2":
                retTypeName = "CsvHelper.Vec2";
                break;
            case "vec3":
            case "vector3":
                retTypeName = "CsvHelper.Vec3";
                break;
            case "vec4":
            case "vector4":
                retTypeName = "CsvHelper.Vec4";
                break;
            default:
                retTypeName = typeName;
                break;
        }
        return retTypeName;
    }

    private static Dictionary<string, Header> m_type2Headers;
    public static bool TryGetHeader(string type, out Header header) {
        if (m_type2Headers == null) {
            m_type2Headers = new Dictionary<string, Header>();
            string filePath = "ExcelMakerExtend/TypeScript/Type2Head.json";
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

    private static readonly Encoding m_Encoding = new UTF8Encoding(false);

    public static void MakeCsvClass(string outPaths, string fileCsv,
        List<CsvHeader> headers, List<string> typeStrs,
        string headExtend, string csvExtend, string readerExtend) {
        //string fileCsv = Path.GetFileNameWithoutExtension(file);
        string classStr = TemplateCsv;

        string headerfile;
        Header typeHeader;
        if (TryGetHeader("_BaseHeader", out typeHeader) && !string.IsNullOrEmpty(typeHeader.path)) {
            headerfile = typeHeader.path;
        }
        else {
            headerfile = string.Empty;
        }
        if (!string.IsNullOrEmpty(headExtend)) {
            headerfile += headExtend;
        }

        StringBuilder propertyBuilder = new StringBuilder();
        StringBuilder simpleBuilder = new StringBuilder();
        StringBuilder nodeBuilder = new StringBuilder();

        List<string> flagKeys = new List<string>();
        List<string> importKeys = new List<string>();

        for (int i = 0; i < headers.Count; i++) {
            CsvHeader header = headers[i];
            if (header.skip) {
                continue;
            }
            bool isSimple = false;
            string fieldName = string.Empty;
            string paramName = string.Empty;
            bool isEnum = false;
            switch (header.type) {
                case eFieldType.Primitive:
                    fieldName = header.name;
                    paramName = "Text";
                    isSimple = true;
                    break;
                case eFieldType.Array:
                    if (flagKeys.Contains(header.baseName)) {
                        //Debug.Log(file + " Array skip:" + header.name);
                        continue;
                    }
                    flagKeys.Add(header.baseName);

                    fieldName = header.baseName;
                    paramName = "Node";
                    break;
                case eFieldType.Class:
                    if (flagKeys.Contains(header.baseName)) {
                        //Debug.Log(file + " Class skip:" + header.name);
                        continue;
                    }
                    flagKeys.Add(header.baseName);

                    fieldName = header.baseName;
                    paramName = "Node";
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
                continue;
            }
            if (typeStr[0] == CsvConfig.comment) {
                typeStr = typeStr.Substring(1);
            }
            string[] subTypes = typeStr.Split(CsvConfig.classSeparator);
            string typeName = GetSysType(subTypes[0]);
            subTypes = subTypes[0].Split(CsvConfig.arrayChars);
            string baseTypeName = GetConvertType(subTypes[0]);
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
                template = TemplateCsvCase_Enum.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", baseTypeName)
                    .Replace("@paramName", paramName)
                    .Replace("@lowerParamName", paramName.ToLower());
            }
            else {
                template = TemplateCsvCase_Simple.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", baseTypeName)
                    .Replace("@paramName", paramName)
                    .Replace("@lowerParamName", paramName.ToLower());
            }

            if (isSimple) {
                simpleBuilder.Append(template);
            }
            else {
                nodeBuilder.Append(template);
            }

            template = TemplateCsvProperty.Replace("@type", typeName).Replace("@name", fieldName);
            propertyBuilder.Append(template);
        }

        string fileCsvUpper = fileCsv.Substring(0, 1).ToUpper() + fileCsv.Substring(1);
        string className = fileCsvUpper + CsvConfig.classPostfix;
        classStr = classStr.Replace("@className", fileCsvUpper);
        //classStr = classStr.Replace("@class", fileCsvUpper);
        classStr = classStr.Replace("@fileName", fileCsv);
        classStr = classStr.Replace("#headerfile#", headerfile);
        classStr = classStr.Replace("#property#", propertyBuilder.ToString());
        classStr = classStr.Replace("#BaseCase#", simpleBuilder.ToString());
        classStr = classStr.Replace("#NodeCase#", nodeBuilder.ToString());
        classStr = classStr.Replace("#extendCsv#", csvExtend);
        classStr = classStr.Replace("#extendReader#", readerExtend);

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
            File.WriteAllText(filePath, classStr, m_Encoding);
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
        if (m_defineBuilder.Length <= 0) {
            return;
        }
        string definePath = Path.Combine(codePath, className + ".ts");
        string defineClassStr = TemplateDefineClass.Replace("@className", className)
            .Replace("#property#", m_defineBuilder.ToString());
        defineClassStr = Regex.Replace(defineClassStr, "(?<!\r)\n|\r\n", "\n");
        File.WriteAllText(definePath, defineClassStr, m_Encoding);
    }

    static string TemplateCatalog;

    static string TemplateCatalogImprot = @"import { @classNameCsv, @classNameCsvReader } from ""./@classNameCsv"";
";

    static string TemplateCatalogProperty = @"
    private _@TypeName = new @TypeNameCsvReader(""@TypeName"", ()=>{
        return new @TypeNameCsv();
    });
    public get @TypeName() : @TypeNameCsvReader {
        return this._@TypeName;
    }";


    private static StringBuilder m_catalogImportBuilder;
    private static StringBuilder m_catalogPropertyBuilder;
    public static void InitCatalog() {
        m_catalogImportBuilder = new StringBuilder();
        m_catalogPropertyBuilder = new StringBuilder();
        if (string.IsNullOrEmpty(TemplateCatalog)) {
            string filePath = "ExcelMakerExtend/TypeScript/TemplateCatalog.ts";
            if (File.Exists(filePath)) {
                TemplateCatalog = File.ReadAllText(filePath);
            }
            else {
                TemplateCatalog = @"@Import

class _Csv {
    @Property

    async initBySingleCsv() {
        //cc.log(""Csv.initBySingleCsv start:"" + new Date().getTime());
        return new Promise((resolve, reject) => {
            cc.loader.loadResDir(""csv"", function(error: Error, resources: cc.Asset[], urls: string[]) {
                if (error) {
                    cc.error(""Csv.initBySingleCsv error"", error);
                    reject();
                    return;
                }
                for (let index = 0; index < resources.length; index++) {
                    const element = resources[index];
                    const key = element.name;
                    if (!this.hasOwnProperty(""_"" + key)) {
                        cc.warn(""Csv.initBySingleCsv null, "" + key);
                        continue;
                    }
                    //cc.log(""Csv.initBySingleCsv "" + key);

                    let reader = this[""_"" + key];
                    reader.SetupBytes(element.toString());
                }
                resolve();

                //cc.log(""Csv.initBySingleCsv finish:"" + new Date().getTime());
            }.bind(this));
        });
    }
}

export const Csv = new _Csv();";
            }
        }
    }

    public static void AddCatalog(string className) {
        string template = TemplateCatalogImprot.Replace("@className", className);
        m_catalogImportBuilder.Append(template);

        template = TemplateCatalogProperty.Replace("@TypeName", className);
        m_catalogPropertyBuilder.Append(template);
    }

    public static void MakeCatalog(string codePath) {
        string definePath = Path.Combine(codePath, "Csv.ts");
        string defineClassStr = TemplateCatalog.Replace("@Import", m_catalogImportBuilder.ToString())
            .Replace("@Property", m_catalogPropertyBuilder.ToString());
        defineClassStr = Regex.Replace(defineClassStr, "(?<!\r)\n|\r\n", "\n");
        File.WriteAllText(definePath, defineClassStr, m_Encoding);
    }
}
