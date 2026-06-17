# Table Formatting Feature

## Overview
This feature allows you to convert HTML tables to formatted text during book import. You can choose to keep tables as-is, or convert them to flat lists with different ordering options. **The formatter preserves all text formatting (bold, italic, etc.) from the original table cells.**

## Key Features

? **Preserves Text Formatting**: Bold, italic, and other HTML formatting from table cells is maintained  
? **Compact Output**: Uses line breaks instead of paragraph spacing to reduce whitespace  
? **Flexible Options**: Choose between column-first, row-first, or no formatting  
? **Smart Detection**: Automatically handles small tables differently for better readability

## Configuration Options

### Table Format Modes
Located in the UI under "Table Format" dropdown:

1. **NoFormatting** (Default)
   - Tables remain as HTML tables
   - No conversion is performed
   - Original table structure is preserved

2. **ColumnFirst**
   - Data is listed column by column
   - First row values are treated as headers (bold)
   - Columns are separated by "---"
   - Best for tables where each column represents a distinct category

3. **RowFirst**
   - Data is listed row by row
   - First row values are treated as headers (bold)
   - Rows are separated by "---"
   - Best for tables where each row represents a distinct entry

### Two-Column Table Option
Located below the Table Format dropdown:

**Checkbox: "Treat all 2-column tables as Header:Value pairs"**
- When **unchecked** (default): Only very small tables (1×2, 2×1, 2×2) get special formatting
- When **checked**: ANY table with exactly 2 columns gets formatted as **Header - Value** on each line
  - Perfect for status sheets, stat blocks, or info boxes with many rows
  - Column 1 = Header (bold)
  - Column 2 = Value
  - Both cleaned of line breaks and extra spacing
  - Example: A 10-row × 2-column table becomes 10 compact lines

## Small Table Detection

The system automatically detects "small tables" and formats them differently:

### Small Table Criteria (Default):
- 1 row × 2 columns
- 2 rows × 1 column
- 2 rows × 2 columns
- **OR any table with 2 columns (if "Treat all 2-column tables" is checked)**

### Small Table Behavior:
- Formatted as **Header - Value** on a single line per row
- Headers are bold
- Very compact and readable
- Column first/row first setting is ignored for small tables
- **Newlines and `<br>` tags in both headers AND values are replaced with spaces**
- Multiple spaces in headers are condensed to " - "

**Examples:**

For a 1×2 table:
```
| Name | John |
```
Becomes:
```
**Name** - John
```

For a 2×2 table:
```
| Name  | John |
| Age   | 30   |
```
Becomes:
```
**Name** - John
**Age** - 30
```

For a table with line breaks:
```
| Purge | <br/> | 100 essence |
```
Becomes:
```
**Purge** - 100 essence
```

For a 10×2 table (with "Treat all 2-column tables" checked):
```
| Name      | John Smith    |
| Age       | 30            |
| Level     | 45            |
| HP        | 1200/1200     |
| MP        | 850/850       |
| Strength  | 125           |
| Dexterity | 98            |
| Class     | Warrior       |
| Guild     | Shadow Blade  |
| Title     | Sword Master  |
```
Becomes (compact single paragraph):
```
**Name** - John Smith
**Age** - 30
**Level** - 45
**HP** - 1200/1200
**MP** - 850/850
**Strength** - 125
**Dexterity** - 98
**Class** - Warrior
**Guild** - Shadow Blade
**Title** - Sword Master
```

### Cell Cleanup

Both headers and values are automatically cleaned:
- `<br>`, `<br/>`, `<br />` tags are removed and replaced with spaces
- Newline characters (`\n`, `\r`) are removed and replaced with spaces  
- Multiple consecutive spaces are condensed
- For headers specifically, multiple spaces become " - " for readability

Example with messy formatting:
```
| Purge<br/><br/> | <br/>100<br/>essence |
```
Becomes:
```
**Purge** - 100 essence
```

### HTML Formatting Preservation

If the original table cells contain formatting like:
```
| **Jasper Welles** | *Level 10* |
| Health | **100** |
```

The output will preserve that formatting:
```
**Jasper Welles** - *Level 10*
**Health** - **100**
```

## Large Table Formatting

Tables larger than 2×2 are formatted based on your chosen mode:

### ColumnFirst Example:
Original table:
```
| Name    | Age | City     |
| Alice   | 25  | NYC      |
| Bob     | 30  | LA       |
```

Formatted output (compact with clear column separation):
```
**Name**
Alice
Bob

---

**Age**
25
30

---

**City**
NYC
LA
```

### RowFirst Example:
Same table formatted with RowFirst (compact with clear row separation):
```
**Name**
**Age**
**City**

---

Alice
25
NYC

---

Bob
30
LA
```

## Spacing Improvements

The formatter uses:
- **Single paragraph** with `<br/>` line breaks for all content
- **Single line breaks** between items in the same column/row  
- **Double line breaks with "---" separator** between different columns/rows
- **Automatic cleanup** of `<br>` tags and newlines in cell content
- This creates compact, readable output without huge gaps in Word documents

## How to Use

1. Open Happy Book Creator
2. Enter your book details (URL, book name, etc.)
3. Select your preferred **Table Format** from the dropdown:
   - **NoFormatting**: Keep tables unchanged
   - **ColumnFirst**: Convert tables, list by columns
   - **RowFirst**: Convert tables, list by rows
4. **(Optional)** Check **"Treat all 2-column tables as Header:Value pairs"** if you want all 2-column tables (like status sheets) to be formatted compactly
5. Click **Import** to download chapters
6. Tables will be automatically formatted according to your settings

## Technical Details

### Per-Source Configuration
- The table format setting is applied per import session
- Different books/sources can use different settings
- The setting is remembered during the current session

### Reader Support
Table formatting is implemented in all readers:
- RoyalReader
- NovelFireReader
- WebArchiveRoyalReader
- WormReader

### Implementation
- Tables are processed during the HTML chapter extraction phase
- **HTML formatting (bold, italic, etc.) is preserved from the original cells**
- **Output uses single paragraph with `<br/>` tags for compact spacing**
- Formatting happens before the HTML is written to the Word document
- The original table HTML is replaced with formatted paragraph elements
- Table width adjustments still work alongside formatting

### Formatting Preservation
The following HTML tags are preserved from table cells:
- `<strong>`, `<b>` - Bold text
- `<em>`, `<i>` - Italic text
- Any other inline HTML formatting present in the original cells

## Use Cases

### Best for 2-Column Checkbox (Enabled):
- **RPG/LitRPG status sheets** (Name, Level, HP, MP, Stats, etc.)
- Character info boxes
- Item descriptions with multiple attributes
- Skill/Ability lists with descriptions
- Any vertical list of "Label: Value" pairs

### Best for ColumnFirst:
- Wide tables with many columns
- Comparison tables
- Skill lists where columns represent different skill types
- Any table where columns represent different attributes

### Best for RowFirst:
- Timeline/event tables
- Quest lists
- Achievement tables
- Any table where each row is a complete item/entry

### Keep NoFormatting:
- Complex multi-column layouts
- Tables with merged cells
- Tables that are meant to be visual/structural
- When you want maximum fidelity to the original
