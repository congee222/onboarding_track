# Gravity Engine (引力引擎) Batch Event Excel Format

## Template Structure

Based on `引力引擎_批量添加事件模板.xlsx`.

### Columns (A-I)

| Column | Field | Description |
|--------|-------|-------------|
| A | 事件名 | English event name. Letters/digits/underscore only, start with letter, not `$`, ≤50 chars |
| B | 事件显示名 | Chinese display name |
| C | 是否接收 | Must be `是` |
| D | 触发时机 | When this event fires |
| E | 事件说明 | Additional notes |
| F | 属性名 | English property name, same naming rules |
| G | 属性显示名 | Chinese display name for property |
| H | 属性类型 | One of: 文本, 整数, 浮点数, 布尔值, 日期, 时间, 列表 |
| I | 属性说明 | Property notes |

### Merged Cells Rules

- Columns A-E: Merge vertically when an event has multiple properties
- Columns F-I: **Never merge**
- If an event has 0 properties, still fill A-E, leave F-I empty

## Two Generation Modes

### Mode 1: Template-Based (original)

Copy the 引力引擎 template `.xlsx`, open with `ZipArchive(Update mode)`, replace `xl/sharedStrings.xml` and `xl/worksheets/sheet1.xml`.

**Pros**: Preserves template styles (header colors, borders, fonts).
**Cons**: Requires user to provide the template file.

### Mode 2: Template-Free (improved)

Generate complete `.xlsx` from scratch by writing all XML parts into a new `ZipArchive(Create)`:
- `[Content_Types].xml`
- `_rels/.rels`
- `xl/workbook.xml`
- `xl/_rels/workbook.xml.rels`
- `xl/styles.xml` (hardcoded styles: header bold white-on-blue, data with border)
- `xl/sharedStrings.xml`
- `xl/worksheets/sheet1.xml`

**Pros**: No template file needed. Any user can generate Excel without uploading anything.
**Cons**: Styles are hardcoded (blue header, thin borders) — may not match 引力引擎 template exactly, but functionally identical for import.

## XLSX Internal Structure

### styles.xml (template-free mode)

```xml
<fonts count="2">
  <font><sz val="11"/><name val="Calibri"/></font>
  <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
</fonts>
<fills count="3">
  <fill><patternFill patternType="none"/></fill>
  <fill><patternFill patternType="gray125"/></fill>
  <fill><patternFill patternType="solid"><fgColor rgb="FF4472C4"/><bgColor indexed="64"/></patternFill></fill>
</fills>
<borders count="2">
  <border><left/><right/><top/><bottom/><diagonal/></border>
  <border>
    <left style="thin"><color rgb="FFD9D9D9"/></left>
    <right style="thin"><color rgb="FFD9D9D9"/></right>
    <top style="thin"><color rgb="FFD9D9D9"/></top>
    <bottom style="thin"><color rgb="FFD9D9D9"/></bottom>
    <diagonal/>
  </border>
</borders>
<cellXfs count="3">
  <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>           <!-- s=0: default -->
  <xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" ...>          <!-- s=1: header -->
    <alignment horizontal="center" vertical="center" wrapText="1"/>
  </xf>
  <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" ...>          <!-- s=2: data -->
    <alignment vertical="center" wrapText="1"/>
  </xf>
</cellXfs>
```

### [Content_Types].xml (template-free mode)

```xml
<Types xmlns="...">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
  <Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>
</Types>
```

### workbook.xml + rels (template-free mode)

```xml
<!-- xl/workbook.xml -->
<workbook xmlns="...">
  <sheets><sheet name="事件" sheetId="1" r:id="rId1"/></sheets>
</workbook>

<!-- xl/_rels/workbook.xml.rels -->
<Relationships xmlns="...">
  <Relationship Id="rId1" Type=".../worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type=".../styles" Target="styles.xml"/>
  <Relationship Id="rId3" Type=".../sharedStrings" Target="sharedStrings.xml"/>
</Relationships>
```

### sharedStrings.xml

```xml
<sst xmlns="..." count="N" uniqueCount="N">
  <si><t xml:space="preserve">字符串值</t></si>
</sst>
```

Use `System.Security.SecurityElement.Escape()` for XML escaping.

### sheet1.xml

```xml
<worksheet xmlns="...">
  <sheetPr/><dimension ref="A1:I{lastRow}"/>
  <sheetViews><sheetView showGridLines="0" tabSelected="1" workbookViewId="0">...</sheetView></sheetViews>
  <sheetFormatPr defaultColWidth="9" defaultRowHeight="16.8"/>
  <cols><col min="1" max="9" width="25" customWidth="1"/></cols>
  <sheetData>
    <row r="1" ht="25" customHeight="1" spans="1:9">
      <c r="A1" s="1" t="s"><v>0</v></c>...
    </row>
    <row r="2" ht="25" customHeight="1" spans="1:9">
      <c r="A2" s="2" t="s"><v>5</v></c>
      <c r="B2" s="2"/>  <!-- empty merged cell -->
      ...
    </row>
  </sheetData>
  <mergeCells count="N">
    <mergeCell ref="A2:A4"/>
  </mergeCells>
</worksheet>
```

- `t="s"` = shared string index
- `s="1"` = header style, `s="2"` = data style
- Empty cells in merged ranges: `<c r="X2" s="2"/>`

## Validation Checklist

- [ ] Header row has all 9 column headers
- [ ] Every event row has `是` in column C
- [ ] No merged cells in columns F-I
- [ ] Event names follow naming rules
- [ ] Property types use exact values: 文本/整数/浮点数/布尔值/日期/时间/列表
- [ ] UTF-8 encoding without BOM for XML files inside xlsx
