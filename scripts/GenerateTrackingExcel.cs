using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Template script for generating Gravity Engine (引力引擎) batch event Excel files.
/// 
/// Usage:
/// 1. Copy this script to Assets/1_Scripts/Editor/ (NOT Assets/Editor/ which may have asmdef isolation)
/// 2. Replace the events data in GenerateExcel() with your extracted tracking points
/// 3. Set templatePath to your 引力引擎_批量添加事件模板.xlsx
/// 4. Set outputPath to your desired output location
/// 5. Execute via Unity menu: Tools/Tracking/Generate Excel
/// 
/// For events-only (no properties), set props to new PropData[0] and use GenerateExcelEventsOnly()
/// </summary>
public class GenerateTrackingExcel
{
    // >>> CONFIG: Set these paths <<<
    static readonly string TemplatePath = @"REPLACE_WITH_TEMPLATE_PATH.xlsx";
    static readonly string OutputDir = @"REPLACE_WITH_OUTPUT_DIR";

    [MenuItem("Tools/Tracking/Generate Excel (Events + Properties)")]
    public static void GenerateExcel()
    {
        string outputPath = Path.Combine(OutputDir, "tracking_events_full.xlsx");

        // >>> CONFIG: Replace with your extracted events <<<
        var events = new List<EventData>();
        events.Add(new EventData(
            "event_name",           // 英文事件名
            "事件显示名",             // 中文显示名
            "触发时机的描述",          // 触发时机
            "事件说明",              // 事件说明
            new PropData[] {
                P("prop_name", "属性显示名", "文本", "属性说明"),
                P("prop_name2", "属性显示名2", "整数", "属性说明2"),
            }
        ));
        // Add more events...

        WriteExcel(TemplatePath, outputPath, events, withProperties: true);
    }

    [MenuItem("Tools/Tracking/Generate Excel (Events Only)")]
    public static void GenerateExcelEventsOnly()
    {
        string outputPath = Path.Combine(OutputDir, "tracking_events_only.xlsx");

        // >>> CONFIG: Replace with your extracted events (same as above, props will be ignored) <<<
        var events = new List<EventData>();
        events.Add(new EventData("event_name", "事件显示名", "触发时机", "事件说明", new PropData[0]));
        // Add more events...

        WriteExcel(TemplatePath, outputPath, events, withProperties: false);
    }

    // ==================== Core Excel Generation Engine (do not modify) ====================

    static void WriteExcel(string templatePath, string outputPath, List<EventData> events, bool withProperties)
    {
        var stringList = new List<string>();
        var stringMap = new Dictionary<string, int>();
        Func<string, int> addStr = (s) => {
            if (stringMap.ContainsKey(s)) return stringMap[s];
            int idx = stringList.Count;
            stringList.Add(s);
            stringMap[s] = idx;
            return idx;
        };

        string[] headers = { "事件名", "事件显示名", "是否接收", "触发时机", "事件说明", "属性名", "属性显示名", "属性类型", "属性说明" };
        foreach (var h in headers) addStr(h);

        var rows = new List<Dictionary<string, int>>();
        var merges = new List<string>();
        string[] colNames = { "A", "B", "C", "D", "E", "F", "G", "H", "I" };
        int curRow = 2;

        foreach (var evt in events)
        {
            var props = withProperties ? evt.props : new PropData[0];
            int startRow = curRow;
            int endRow = curRow + (props.Length > 0 ? props.Length - 1 : 0);

            if (props.Length > 1)
            {
                for (int c = 0; c < 5; c++)
                    merges.Add(string.Format("{0}{1}:{0}{2}", colNames[c], startRow, endRow));
            }

            if (props.Length == 0)
            {
                var row = new Dictionary<string, int>();
                row["RowNum"] = curRow;
                row["A"] = addStr(evt.name);
                row["B"] = addStr(evt.display);
                row["C"] = addStr("是");
                row["D"] = addStr(evt.trigger);
                row["E"] = addStr(evt.desc);
                rows.Add(row);
                curRow++;
            }
            else
            {
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    var row = new Dictionary<string, int>();
                    row["RowNum"] = curRow;
                    if (i == 0)
                    {
                        row["A"] = addStr(evt.name);
                        row["B"] = addStr(evt.display);
                        row["C"] = addStr("是");
                        row["D"] = addStr(evt.trigger);
                        row["E"] = addStr(evt.desc);
                    }
                    row["F"] = addStr(p.name);
                    row["G"] = addStr(p.display);
                    row["H"] = addStr(p.type);
                    row["I"] = addStr(p.desc);
                    rows.Add(row);
                    curRow++;
                }
            }
        }

        // Generate sharedStrings.xml
        var ss = new StringBuilder();
        ss.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        ss.Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"");
        ss.Append(stringList.Count); ss.Append("\" uniqueCount=\""); ss.Append(stringList.Count); ss.Append("\">");
        foreach (var s in stringList)
        {
            ss.Append("<si><t xml:space=\"preserve\">");
            ss.Append(SecurityElement.Escape(s));
            ss.Append("</t></si>");
        }
        ss.Append("</sst>");

        // Generate sheet1.xml
        var sh = new StringBuilder();
        sh.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sh.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        sh.Append("<sheetPr/><dimension ref=\"A1:I"); sh.Append(curRow - 1); sh.Append("\"/>");
        sh.Append("<sheetViews><sheetView showGridLines=\"0\" tabSelected=\"1\" workbookViewId=\"0\"><selection activeCell=\"A1\" sqref=\"A1\"/></sheetView></sheetViews>");
        sh.Append("<sheetFormatPr defaultColWidth=\"9\" defaultRowHeight=\"16.8\"/><cols><col min=\"1\" max=\"9\" width=\"25\" customWidth=\"1\"/></cols><sheetData>");
        sh.Append("<row r=\"1\" ht=\"25\" customHeight=\"1\" spans=\"1:9\">");
        for (int c = 0; c < 9; c++)
        {
            sh.Append("<c r=\""); sh.Append(colNames[c]); sh.Append("1\" s=\"1\" t=\"s\"><v>"); sh.Append(c); sh.Append("</v></c>");
        }
        sh.Append("</row>");
        foreach (var row in rows)
        {
            sh.Append("<row r=\""); sh.Append(row["RowNum"]); sh.Append("\" ht=\"25\" customHeight=\"1\" spans=\"1:9\">");
            foreach (var col in colNames)
            {
                sh.Append("<c r=\""); sh.Append(col); sh.Append(row["RowNum"]); sh.Append("\"");
                if (row.ContainsKey(col))
                {
                    sh.Append(" s=\"2\" t=\"s\"><v>"); sh.Append(row[col]); sh.Append("</v></c>");
                }
                else { sh.Append(" s=\"2\"/>"); }
            }
            sh.Append("</row>");
        }
        sh.Append("</sheetData>");
        if (merges.Count > 0)
        {
            sh.Append("<mergeCells count=\""); sh.Append(merges.Count); sh.Append("\">");
            foreach (var m in merges) { sh.Append("<mergeCell ref=\""); sh.Append(m); sh.Append("\"/>"); }
            sh.Append("</mergeCells>");
        }
        sh.Append("</worksheet>");

        // Write into xlsx
        Directory.CreateDirectory(OutputDir);
        File.Copy(templatePath, outputPath, true);
        using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Update))
        {
            archive.GetEntry("xl/sharedStrings.xml")?.Delete();
            using (var w = new StreamWriter(archive.CreateEntry("xl/sharedStrings.xml").Open(), new UTF8Encoding(false)))
                w.Write(ss.ToString());
            archive.GetEntry("xl/worksheets/sheet1.xml")?.Delete();
            using (var w = new StreamWriter(archive.CreateEntry("xl/worksheets/sheet1.xml").Open(), new UTF8Encoding(false)))
                w.Write(sh.ToString());
        }

        Debug.Log($"[Excel] Done! File: {outputPath}");
        Debug.Log($"[Excel] Events: {events.Count}, Rows: {rows.Count}, Strings: {stringList.Count}");
    }

    static PropData P(string name, string display, string type, string desc)
    {
        return new PropData { name = name, display = display, type = type, desc = desc };
    }

    struct EventData
    {
        public string name, display, trigger, desc;
        public PropData[] props;
        public EventData(string n, string d, string t, string de, PropData[] p)
        { name = n; display = d; trigger = t; desc = de; props = p; }
    }

    struct PropData
    {
        public string name, display, type, desc;
    }
}
