using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates Gravity Engine (引力引擎) batch event Excel files for tutorial tracking.
///
/// Usage:
/// 1. Set TemplatePath to your 引力引擎_批量添加事件模板.xlsx
/// 2. Set OutputDir to your desired output location
/// 3. Execute via Unity menu: Tools/Tracking/Generate Excel (Events + Properties)
///    or: Tools/Tracking/Generate Excel (Events Only)
/// </summary>
public class GenerateTrackingExcel
{
    // >>> CONFIG: Output directory <<<
    static readonly string OutputDir = @"Tracking";

    [MenuItem("Tools/Tracking/Generate Excel (Events + Properties)")]
    public static void GenerateExcel()
    {
        string outputPath = Path.Combine(OutputDir, "tutorial_tracking_full.xlsx");

        var events = new List<EventData>();

        // Common properties shared by all events
        PropData[] commonProps = new PropData[] {
            P("user_id", "用户ID", "文本", "Firebase匿名登录用户ID"),
            P("platform", "平台", "文本", "Android / iOS"),
            P("app_version", "应用版本", "文本", "当前应用版本号"),
            P("tutorial_id", "教程步骤ID", "整数", "触发时正在执行的TutorialTableData.ID"),
            P("group_id", "教程组ID", "整数", "TutorialTableData.Group_ID"),
            P("cleared_count", "已完成教程数", "整数", "玩家当前已完成的教程组数量"),
        };

        // 1. tutorial_start
        events.Add(new EventData(
            "tutorial_start",
            "教程开始",
            "游戏首次加载教程列表时触发（TutorialManager.StartScene加载到未完成教程）",
            "全局事件，标记玩家进入新手教程流程",
            MergeProps(commonProps, new PropData[] {
                P("first_tutorial_id", "首个教程ID", "整数", "第一个待执行的教程步骤ID（通常为1001）"),
                P("total_tutorials", "待完成教程数", "整数", "本次加载的未完成教程总数"),
                P("is_lobby", "是否在大厅", "布尔值", "true=大厅场景, false=战斗场景"),
            })
        ));

        // 2. tutorial_skill_use
        events.Add(new EventData(
            "tutorial_skill_use",
            "技能使用引导完成",
            "玩家在战斗中点击技能按钮，教程1001完成时触发",
            "组101：引导玩家在战斗中使用技能",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("energy_value", "当前能量值", "整数", "触发教程时的能量值"),
            })
        ));

        // 3. tutorial_box_add
        events.Add(new EventData(
            "tutorial_box_add",
            "添加盒子引导完成",
            "玩家在大厅点击添加盒子按钮，教程1002完成时触发",
            "组102：引导玩家花费金币添加盒子",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("own_gold", "拥有金币", "整数", "触发教程时的金币数量"),
                P("box_cost", "盒子花费", "整数", "添加盒子的金币花费（50）"),
            })
        ));

        // 4. tutorial_weapon_equip
        events.Add(new EventData(
            "tutorial_weapon_equip",
            "武器装备引导完成",
            "玩家点击武器装备按钮，教程1003完成时触发",
            "组103：引导玩家装备武器到武器槽",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("box_count", "盒子数量", "整数", "当前盒子数量"),
                P("own_gold", "拥有金币", "整数", "当前金币数量"),
            })
        ));

        // 5. tutorial_box_touch
        events.Add(new EventData(
            "tutorial_box_touch",
            "盒子点击引导完成",
            "玩家点击游戏中的盒子，教程1004完成时触发",
            "组104：引导玩家点击游戏中的3D盒子物体",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("box_count", "盒子数量", "整数", "当前盒子数量"),
                P("is_world_object", "是否为3D物体", "布尔值", "true=点击的是场景中的3D物体"),
            })
        ));

        // 6. tutorial_survivor_gun
        events.Add(new EventData(
            "tutorial_survivor_gun",
            "生还者枪械引导完成",
            "教程链1005→1006→1007全部完成时触发（打开生还者标签→选择枪械→升级枪械）",
            "组105：引导玩家进入生还者系统并升级枪械",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("stage_clear_count", "通关次数", "整数", "触发时的关卡通关次数"),
                P("barrack_number", "兵营编号", "整数", "当前兵营编号"),
            })
        ));

        // 7. tutorial_boss_stage
        events.Add(new EventData(
            "tutorial_boss_stage",
            "Boss关卡引导完成",
            "教程链1008→1009全部完成时触发（打开Boss标签→开始Boss关卡）",
            "组106：引导玩家进入Boss关卡",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("stage_clear_count", "通关次数", "整数", "触发时的关卡通关次数"),
            })
        ));

        // 8. tutorial_weapon_enhance
        events.Add(new EventData(
            "tutorial_weapon_enhance",
            "武器强化引导完成",
            "教程链1010→1011→1012全部完成时触发（打开武器标签→选择武器→强化武器）",
            "组107：引导玩家强化武器",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("boss_clear_difficulty", "Boss通关难度", "整数", "当前Boss通关难度等级"),
            })
        ));

        // 9. tutorial_survivor_equip
        events.Add(new EventData(
            "tutorial_survivor_equip",
            "生还者装备引导完成",
            "教程链1013→1014→1015→1016全部完成时触发（打开生还者标签→选择生还者→装备弹夹→装备生还者）",
            "组108：引导玩家装备生还者弹夹和装备",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("stage_clear_count", "通关次数", "整数", "触发时的关卡通关次数"),
                P("barrack_number", "兵营编号", "整数", "当前兵营编号"),
            })
        ));

        // 10. tutorial_survivor_levelup
        events.Add(new EventData(
            "tutorial_survivor_levelup",
            "生还者升级引导完成",
            "教程链1019→1020→1021全部完成时触发（打开生还者标签→选择生还者→升级生还者）",
            "组110：引导玩家升级生还者",
            MergeProps(commonProps, new PropData[] {
                P("cursor_type", "引导手指类型", "文本", "TOUCH_01 / TOUCH_02 / TOUCH_03"),
                P("boss_clear_difficulty", "Boss通关难度", "整数", "当前Boss通关难度等级"),
            })
        ));

        // 11. tutorial_all_complete
        events.Add(new EventData(
            "tutorial_all_complete",
            "全部教程完成",
            "最后一个教程组完成时触发（ClearTutorials列表包含所有教程ID）",
            "全局事件，标记玩家完成所有新手教程",
            MergeProps(commonProps, new PropData[] {
                P("total_cleared", "总完成教程数", "整数", "完成的教程组总数"),
                P("last_tutorial_id", "最后教程ID", "整数", "最后完成的教程步骤ID"),
            })
        ));

        // 12. tutorial_quit
        events.Add(new EventData(
            "tutorial_quit",
            "中途退出教程",
            "玩家在教程执行过程中退出游戏或离开教程时触发",
            "流失监测事件，需关注quit_tutorial_id分布",
            MergeProps(commonProps, new PropData[] {
                P("quit_tutorial_id", "退出时教程ID", "整数", "退出时正在执行的教程步骤ID"),
                P("quit_group_id", "退出时教程组ID", "整数", "退出时正在执行的教程组ID"),
                P("quit_step_index", "退出时步骤序号", "整数", "在教程链中的步骤位置（从1开始）"),
            })
        ));

        WriteExcel(outputPath, events, withProperties: true);
    }

    [MenuItem("Tools/Tracking/Generate Excel (Events Only)")]
    public static void GenerateExcelEventsOnly()
    {
        string outputPath = Path.Combine(OutputDir, "tutorial_tracking_events_only.xlsx");

        var events = new List<EventData>();
        events.Add(new EventData("tutorial_start", "教程开始", "游戏首次加载教程列表时触发", "全局事件", new PropData[0]));
        events.Add(new EventData("tutorial_skill_use", "技能使用引导完成", "教程1001完成", "组101", new PropData[0]));
        events.Add(new EventData("tutorial_box_add", "添加盒子引导完成", "教程1002完成", "组102", new PropData[0]));
        events.Add(new EventData("tutorial_weapon_equip", "武器装备引导完成", "教程1003完成", "组103", new PropData[0]));
        events.Add(new EventData("tutorial_box_touch", "盒子点击引导完成", "教程1004完成", "组104", new PropData[0]));
        events.Add(new EventData("tutorial_survivor_gun", "生还者枪械引导完成", "教程链1005-1007完成", "组105", new PropData[0]));
        events.Add(new EventData("tutorial_boss_stage", "Boss关卡引导完成", "教程链1008-1009完成", "组106", new PropData[0]));
        events.Add(new EventData("tutorial_weapon_enhance", "武器强化引导完成", "教程链1010-1012完成", "组107", new PropData[0]));
        events.Add(new EventData("tutorial_survivor_equip", "生还者装备引导完成", "教程链1013-1016完成", "组108", new PropData[0]));
        events.Add(new EventData("tutorial_survivor_levelup", "生还者升级引导完成", "教程链1019-1021完成", "组110", new PropData[0]));
        events.Add(new EventData("tutorial_all_complete", "全部教程完成", "最后一个教程组完成", "全局事件", new PropData[0]));
        events.Add(new EventData("tutorial_quit", "中途退出教程", "玩家退出教程时触发", "流失监测", new PropData[0]));

        WriteExcel(outputPath, events, withProperties: false);
    }

    // ==================== Core Excel Generation Engine (do not modify) ====================

    static PropData[] MergeProps(PropData[] common, PropData[] specific)
    {
        var merged = new PropData[common.Length + specific.Length];
        common.CopyTo(merged, 0);
        specific.CopyTo(merged, common.Length);
        return merged;
    }

    static void WriteExcel(string outputPath, List<EventData> events, bool withProperties)
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

        // === Generate sharedStrings.xml ===
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

        // === Generate sheet1.xml ===
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

        // === Generate styles.xml (header bold + data normal, both with border) ===
        string stylesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"2\">" +
              "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
              "<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font>" +
            "</fonts>" +
            "<fills count=\"3\">" +
              "<fill><patternFill patternType=\"none\"/></fill>" +
              "<fill><patternFill patternType=\"gray125\"/></fill>" +
              "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF4472C4\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
            "</fills>" +
            "<borders count=\"2\">" +
              "<border><left/><right/><top/><bottom/><diagonal/></border>" +
              "<border>" +
                "<left style=\"thin\"><color rgb=\"FFD9D9D9\"/></left>" +
                "<right style=\"thin\"><color rgb=\"FFD9D9D9\"/></right>" +
                "<top style=\"thin\"><color rgb=\"FFD9D9D9\"/></top>" +
                "<bottom style=\"thin\"><color rgb=\"FFD9D9D9\"/></bottom>" +
                "<diagonal/>" +
              "</border>" +
            "</borders>" +
            "<cellStyleXfs count=\"2\">" +
              "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
              "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
            "</cellStyleXfs>" +
            "<cellXfs count=\"3\">" +
              // s=0: default
              "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
              // s=1: header (bold white font, blue fill, border)
              "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\">" +
                "<alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/>" +
              "</xf>" +
              // s=2: data (normal font, border, wrap text)
              "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\" applyAlignment=\"1\">" +
                "<alignment vertical=\"center\" wrapText=\"1\"/>" +
              "</xf>" +
            "</cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
            "<dxfs count=\"0\"/>" +
            "<tableStyles count=\"0\" defaultTableStyle=\"TableStyleMedium2\" defaultPivotStyle=\"PivotStyleLight16\"/>" +
            "</styleSheet>";

        // === Generate workbook.xml ===
        string workbookXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"事件\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
            "</workbook>";

        // === Generate workbook.xml.rels ===
        string workbookRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>" +
            "</Relationships>";

        // === Generate .rels (root) ===
        string rootRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        // === Generate [Content_Types].xml ===
        string contentTypesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>" +
            "</Types>";

        // === Write xlsx from scratch ===
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
        if (File.Exists(outputPath)) File.Delete(outputPath);

        using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "[Content_Types].xml", contentTypesXml);
            WriteEntry(archive, "_rels/.rels", rootRelsXml);
            WriteEntry(archive, "xl/workbook.xml", workbookXml);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", workbookRelsXml);
            WriteEntry(archive, "xl/styles.xml", stylesXml);
            WriteEntry(archive, "xl/sharedStrings.xml", ss.ToString());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", sh.ToString());
        }

        Debug.Log($"[Excel] Done! File: {Path.GetFullPath(outputPath)}");
        Debug.Log($"[Excel] Events: {events.Count}, Rows: {rows.Count}, Strings: {stringList.Count}");
    }

    static void WriteEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using (var w = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            w.Write(content);
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
