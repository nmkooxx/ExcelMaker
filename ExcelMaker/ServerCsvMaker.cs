using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class ServerCsvMaker
{
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

    private static String GetSysType(String typeName)
    {
        String retTypeName = "";
        String typeNameLower = typeName.ToLower();
        switch (typeNameLower)
        {
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

    private static string transType(string typeName)
    {
        if (typeName == "int")
        {
            return "Integer";
        }else if(typeName == "double")
        {
            return "Double";
        }else if (typeName == "float")
        {
            return "Float";
        }
        return typeName;
    }

    private static string ComposeTypeName(string typeName)
    {
        return string.Format("List<{0}>", transType(typeName));
    }

    public static void MakeCsvClass(string outPaths, string fileCsv,
    CsvHeader[] headers, string[] typeStrs, string headerfile)
    {
        string classStr = TemplateClass;

        string templateSimpe = TemplateSimpeCase;
        StringBuilder propertyBuilder = new StringBuilder();
        StringBuilder methodBuilder = new StringBuilder();
        StringBuilder simpleBuilder = new StringBuilder();
        StringBuilder nodeBuilder = new StringBuilder();

        string classKey = "";
        List<string> flagKeys = new List<string>();

        for (int i = 0; i < headers.Length; i++)
        {
            CsvHeader header = headers[i];
            if (header.skip)
            {
                continue;
            }
            bool isSimple = false;
            string paramName = string.Empty;
            string fieldName = string.Empty;
            switch (header.type)
            {
                case eFieldType.Primitive:
                    fieldName = header.name;
                    paramName = "text";
                    isSimple = true;
                    break;
                case eFieldType.Array:
                    if (flagKeys.Contains(header.baseName))
                    {
                        //Debug.Log(file + " Array skip:" + header.name);
                        continue;
                    }
                    flagKeys.Add(header.baseName);

                    fieldName = header.baseName;
                    paramName = "node";
                    break;
                case eFieldType.Class:
                    if (flagKeys.Contains(header.baseName))
                    {
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
            if (string.IsNullOrEmpty(typeStr))
            {
                continue;
            }
            if (typeStr == "define")
            {
                continue;
            }
            if (typeStr[0] == CsvConfig.comment)
            {
                typeStr = typeStr.Substring(1);
            }
            string[] subTypes = typeStr.Split(CsvConfig.classSeparator);
            string typeName = GetSysType(subTypes[0]);
            subTypes = typeName.Split(CsvConfig.arrayChars);
            typeName = GetSysType(subTypes[0]);
            string baseTypeName = GetSysType(subTypes[0]);
            string funcName = string.Empty;
            if (subTypes.Length == 1)
            {
                funcName = "Base";
            }
            else
            {
                int arrayNum = (subTypes.Length - 1) / 2;
                for (int num = 0; num < arrayNum; num++)
                {
                    funcName += "Array";
                    typeName = ComposeTypeName(typeName);
                }
            }

            string template;
            template = templateSimpe.Replace("@funName", funcName).Replace("@name", fieldName)
                    .Replace("@type", initCap(baseTypeName)).Replace("@paramName", paramName);

            if (isSimple)
            {
                simpleBuilder.Append(template);
            }
            else
            {
                nodeBuilder.Append(template);
            }

            if (header.name == CsvConfig.primaryKey)
            {
                classKey = typeName;
                if (classKey == "int")
                {
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
        foreach (string outPath in outPathArrray)
        {
            if (!Directory.Exists(outPath))
            {
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

    private static String initCap(String str)
    {
        char[] ch = str.ToCharArray();
        if (ch[0] >= 'a' && ch[0] <= 'z')
        {
            ch[0] = (char)(ch[0] - 32);
        }
        return new String(ch);
    }
}