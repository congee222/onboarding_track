# Gravity Engine (引力引擎) Batch Event Excel Format

## Template Structure

Based on `引力引擎_批量添加事件模板.xlsx`.

### Columns (A-I)

| Column | Field | Description |
|--------|-------|-------------|
| A | 事件名 | English event name for code埋点. Letters/digits/underscore only, start with letter, not `$`, ≤50 chars |
| B | 事件显示名 | Chinese display name |
| C | 是否接收 | Must be `是` (to stop receiving, toggle in Gravity Engine backend) |
| D | 触发时机 | When this event fires (for developers to understand埋点 location) |
| E | 事件说明 | Additional notes |
| F | 属性名 | English property name, same naming rules as event name |
| G | 属性显示名 | Chinese display name for property |
| H | 属性类型 | One of: 文本, 整数, 浮点数, 布尔值, 日期, 时间, 列表 |
| I | 属性说明 | Property notes |

### Merged Cells Rules

- Columns A-E: Merge vertically when an event has multiple properties (one row per property)
- Columns F-I: **Never merge** — each property is its own row
- If an event has 0 properties, still fill A-E, leave F-I empty

### Row Layout

```
Row 1: Headers (事件名 | 事件显示名 | 是否接收 | 触发时机 | 事件说明 | 属性名 | 属性显示名 | 属性类型 | 属性说明)
Row 2: Event1  | Display1   | 是      | Trigger1  | Desc1     | prop1    | propDisp1  | type1    | propDesc1
Row 3: (merged)| (merged)   | (merged)| (merged)  | (merged)  | prop2    | propDisp2  | type2    | propDesc2
Row 4: Event2  | Display2   | 是      | Trigger2  | Desc2     | prop1    | propDisp1  | type1    | propDesc1
...
```

### XLSX Internal Structure (for programmatic generation)

An .xlsx file is a ZIP containing:
- `xl/sharedStrings.xml` — all unique string values, referenced by index
- `xl/worksheets/sheet1.xml` — cell data with references to shared strings
- `xl/styles.xml` — cell styles (from template, do not modify)
- `xl/workbook.xml` — workbook metadata (from template)

#### sharedStrings.xml

```xml
<sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="N" uniqueCount="N">
  <si><t xml:space="preserve">字符串值</t></si>
  ...
</sst>
```

- `count` = `uniqueCount` = number of unique strings
- Use `System.Security.SecurityElement.Escape()` to escape special chars

#### sheet1.xml

```xml
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetPr/>
  <dimension ref="A1:I{lastRow}"/>
  <sheetViews><sheetView showGridLines="0" tabSelected="1" workbookViewId="0">
    <selection activeCell="A1" sqref="A1"/>
  </sheetView></sheetViews>
  <sheetFormatPr defaultColWidth="9" defaultRowHeight="16.8"/>
  <cols><col min="1" max="9" width="25" customWidth="1"/></cols>
  <sheetData>
    <row r="1" ht="25" customHeight="1" spans="1:9">
      <c r="A1" s="1" t="s"><v>0</v></c>
      ...
    </row>
    <row r="2" ht="25" customHeight="1" spans="1:9">
      <c r="A2" s="2" t="s"><v>5</v></c>
      <c r="B2" s="2"/>  <!-- empty cell for merged rows -->
      ...
    </row>
  </sheetData>
  <mergeCells count="N">
    <mergeCell ref="A2:A4"/>
    ...
  </mergeCells>
</worksheet>
```

- `t="s"` means the value is a shared string index
- `s="1"` = header style, `s="2"` = data style (from template's styles.xml)
- Empty cells in merged ranges: output `<c r="X2" s="2"/>` (no value)

#### Generation via C# Editor Script

```csharp
// 1. Build string list + index map
// 2. Build row data with column→stringIndex mapping
// 3. Generate sharedStrings.xml and sheet1.xml as StringBuilder
// 4. Copy template xlsx, open with ZipArchive(Update mode), replace entries
File.Copy(templatePath, outputPath, true);
using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite))
using (var archive = new ZipArchive(fs, ZipArchiveMode.Update))
{
    archive.GetEntry("xl/sharedStrings.xml")?.Delete();
    using (var w = new StreamWriter(archive.CreateEntry("xl/sharedStrings.xml").Open(), new UTF8Encoding(false)))
        w.Write(ssXml);
    
    archive.GetEntry("xl/worksheets/sheet1.xml")?.Delete();
    using (var w = new StreamWriter(archive.CreateEntry("xl/worksheets/sheet1.xml").Open(), new UTF8Encoding(false)))
        w.Write(sheetXml);
}
```

### Validation Checklist

- [ ] Header row (row 1) has all 9 column headers
- [ ] Every event row has `是` in column C
- [ ] No merged cells in columns F-I
- [ ] Event names follow naming rules (no `$` prefix, ≤50 chars)
- [ ] Property types use exact values: 文本/整数/浮点数/布尔值/日期/时间/列表
- [ ] UTF-8 encoding without BOM for XML files inside xlsx
